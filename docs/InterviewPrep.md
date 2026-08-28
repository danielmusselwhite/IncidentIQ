Just quizzing myself on the IncidentIQ architecture and flow in preparation for interviews.

# Round 1 Foundations:

**1 What responsibility does each of these have: Domain, Application, Infrastructure, API, Worker?**

Domain = entity logic modelling the real world

- (so the business entities and rules)

Application = business logic so understanding what wants to happen (whether through commands or abstractions) but now how it actually happens

- (so the use cases and orchestrations + defining the abstractions for things it needs external of itself)

Infrastructure = the actual implementation of the applicaiton abstractions, so using the Command objects and implemnting the abstraction interfaces to actually interact with the real surrounding infrastructure eg azure service bus handler and configuration and same with cosmos.

- (implements those abstractions from the application layer)

Api = the HTTP Controllers, and the DTOs. (http boundary, request/response dtos, problem details)

Worker = the async workers that are doing background processing

- IncidentOutBoxWorker -> runs Cosmos Change Feed Processor which monitors for changes in the Cosmos, using ChangeFeedLeases to record where it has got up to
- AnalyseIncidentWorker -> Consumes Service Bus Messages

---

**2 When a user submits an incident, what is the path from POST /api/incidents down to Cosmos? Name the main classes/interfaces involved.**

My attempt (corrected)

1. POST /api/incidents (eg from React)

2. goes to IncidentController's create method

3. creates an CreateIncidentCommand from the application layer

4. uses the injected Application CreateIncidentHandler HandleAsync method which converts it to an AnalyseIncidentCommand then passes the Command to the abstracted IIncidentSubmissionStore, which is referencing the actual Infrastructure CosmosIncidentSupbmissionStore's CreateAsync method

5. Which then atomically adds to Cosmos the using the partition key of IncidentId (equal to Incident.Id) for both, and passing the correlation Id also to later be used in observability

6. Cosmos Change Feed automatically notices the change

7. IncidentOutboxWorker's Change Feed Processor automatically monitors and looks for new change Feeds triggering on change (storing CahangeFeedLeases as records/checkpoints of its progress), and enqueues a message onto the Azure Service Bus

8. the AnalyseIncidentWorker then picks up azure service bus

9. this ensures eventual completion as, if it fails to write to db then as its atomic the outbox is never created; but if it does then the ChangeFeedLease is made and then whenever that worker is available it'll enqueue it and whenever the other worker is available it'll consume it

10. then after it is being processed by the analyse worker if it fails 5 or more times it'll be dead lettered and will have to be manually re-queued via the soon to be made admin portal

11. and we will prevent same completed one from being processed twice by checking the state to ensure basic state level idempotency

Simplified version from ChatGPT:

```text
POST /api/incidents
     ↓
IncidentsController
     ↓
CreateIncidentCommand
     ↓
CreateIncidentHandler
     ↓
creates:
├── Incident
└── AnalyseIncidentCommand
     ↓
IIncidentSubmissionStore
     ↓
CosmosIncidentSubmissionStore
     ↓
Cosmos TransactionalBatch
├── IncidentDocument
└── IncidentAnalysisOutboxDocument
     ↓
Outbox document written to Incidents container
               ↓
Cosmos Change Feed contains that change
               ↓
IncidentOutboxWorker's Change Feed Processor reads it
               ↓
ChangeFeedLeases records/checkpoints the processor's progress
               ↓
IncidentOutboxWorker publishes AnalyseIncidentCommand
               ↓
Service Bus
               ↓
AnalyseIncidentWorker consumes this message and (eventually) sends it to the rag AI
```

---

**3 Why does CreateIncidentHandler depend on IIncidentSubmissionStore rather than directly using Cosmos?**

because then if we wanted to change it to use postgres all we need to do is make a new PostgresSubmissionStore : IIncidentSubmissionStore, and change the program.cs to inject that instead of Cosmos; and then CreateIncidentHandler doesn't care or need to change. It just says "This is what needs to be achieved, aka a SubmissionStore which atomically persists both" but it doesn't care where it is persisted or (depeneding on the DB) how this is done eg via Cosmos its done in a transactional batch

**4 What are the valid Incident lifecycle states we currently support, and what causes the transitions between them?**

Queued (add new item) -> Proccessing (picked up via analyse worker) -> Failed (dead lettered after too many failed retries)

Processing -> Completed (if it processed successfully).

**5 Why is AnalyseIncidentHandler in the Application project rather than the Worker project?**

Because the worker project only holds the actual workers, analysing the incident itself (aka speaking to the rag etc.) is business level logic in terms of what it is doing so it goes in the application layer

- Worker: "I received this command"
- Application: "Here is what analysing an Incident means"
- Infrastructure: "Here is how I talk to Cosmos / Azure AI / Vector Storage / etc."

---

# Round 2 — Messaging & Reliability

**1 Why do we use Service Bus between the outbox relay and analysis Worker instead of having IncidentOutboxWorker call AnalyseIncidentHandler directly?**

To stop it hanging on large tasks, eg the AnalyseIncidentHandler may be time consuming as it has to go to the RAG (when implemented) so its better suited to being in the service bus queue and having those each consumed as there is better monitoring and observability and reliability off of that (eg dead lettering, can see how many messages are in queue etc.) vs the outbox is just to say oh we have a new thing lets queue it ready (is this a good answer?)

In short: _Service Bus decouples relaying the request from actually performing the analysis._

This gives us

- Durable buffering if the analysis worker is unavailable
- Independent retry/DLQ behaviour
- Backpressure - 1000 incidents can safely wait in the queue
- Independent Worker scaling later (eg if lots of messages hanging in queue, vs lots of new - change feeds)
- Separation between deliver this work and perform this work
- Better operational visibility into queued work

_The outbox makes sure the work request isn't lost; Service Bus makes sure that work can be processed independently and reliably._

**2 What happens step-by-step if AnalyseIncidentHandler throws on delivery 1 of 5?**

1. Service Bus Delivery #1
1. AnalyseIncidentWorker receives message
1. It deserializes the AnalyseIncidentCommand from the azure service bus message
1. It calls AnalyseIncidentHandler.HandleAsync()
1. It gets from Cosmos the actual Incident relating to this IncidentId
1. It checks that it hasn't Completed yet, so it doesn't exit early
1. It then goes to StartProcessingAttempt(), increaasing AttemptCount
1. AnalyseIncidentHandler then persists this AttemptCount increase, and continues its HandleAsync (and lets assume an exception now occurs here)
1. It gets caught by the catch in AnalyseIncidentWorker, which checks the Azure Service Bus's Message arg and sees its 'DeliveryCount' is 1, and as 1 < 5 (what we define in our ServiceBusOptions class's MaxDeliveryCount variable), it does NOT dead letter queue and instead re-throws the exception ready for it to be tried again via in built azure service bus logic

**3 What happens differently if it throws on delivery 5 of 5?**

1. Same, except DeliveryCount is now equal to ServiceBusOptions.MaxDeliveryCount, so it explictly DeadLetters the queue instead and updates in Cosmos its status to Failed
1. (In future will then have an admin page which lets you requeue and/or reset the failure count)

**4 Why do we set AutoCompleteMessages = false?**

_We disable auto-complete because we only want to settle a message as successfully completed after the application workflow has actually succeeded. It also gives us control over retries and explicit dead-lettering._

**5 Suppose the same AnalyseIncidentCommand reaches the Worker twice. What mechanisms currently reduce the chance of us performing the analysis twice?**

The basic state-based idempotency I mentioned in question 2 of this section aka it checks it hasn't been marked as completed yet, and if so it returns early no-op

There is also ServiceBusDuplicateDetection so when the command is created, if the Outbox relay worker attempts to publish the same command again with the same `CommandId`, then service bus duplicate detection can suppress this duplicate itself.

Only major weakness at hte moment is if the worker scales up so lets say we have Worker A and Worker B, both may begin processing the same message before it is marked as completed so the work may be done twice. This is why we called it basic state-based idempotency and it is 'At Least Once' guaranteed, not 'Exactly Once' guaranteed. Could later be strengthend via ETags/Optimistic Concurrency or Explicit Command-Processing Records.

---

# Round 3 — Distributed Systems

**1 What exactly was the dual-write problem in our original Incident creation implementation? Give me a concrete failure scenario.**

- We need to write both the cosmos and azure service bus message at the same time.
- Issue would be, we may have written the cosmos but then failed at the azure service bus message, and now we have a cosmos document that will never be processed (without manual intervention)

**2 Why does storing an Incident and an Outbox document in Cosmos solve that problem? What guarantee does the transactional batch give us?**

- Because now, we can send both up ATOMICALLY meaning they both have to be uploaded at the same time.
- This means that, if the Incident gets uploaded then so does its Outbox, _the durable intent to process it_.
- Which then solves the problem as we have an Outbox worker which monitors for changes in the Cosmos, so when it sees this it will take that Outbox message and enqueue it into Azure Service Bus.
- This means that we are guaranteed if something uploads to the DB, then it will be processed to the Azure Service Bus, even if it is currently down, as if it is the OutboxWorker will try to enqueue it, fail, and then not mark the ChangeFeedLease as being processed so it will retry until eventually the Azure Service Bus is back up.

**3 Why did we have to change the Incidents container partition key from /id to /incidentId?**

- Because Id is automatically populated with a unique value for each so if we partitioned it on that then the outbox and incident would have different partitions keys.

- So instead we set incidentId = 'incident.id' on both. So now both can be partioned on what is effectively the incidentId and we can relate them both togther in a transactional batch upload + when it is detected by the IncidentOutboxWorker which pulls it from the change feed, it can use the incidentId in the message being sent to the azure service bus so it can later get the incident itself; without having to get the incident itself within the outbox worker where it isn't needed.

- As cosmos requires a shared paritionId in order to upload in a transactional batch

eg

```text
Incident:
id = incident-123
incidentId = incident-123

Outbox:
id = outbox-command-456
incidentId = incident-123
```

**4 Suppose IncidentOutboxWorker successfully publishes a command to Service Bus, but crashes before the Change Feed checkpoint is saved. What might happen when it restarts, and why is that okay?**

- Because when it is booted up the ChangeFeedLease won't have been published so it won't know that is as far as it got and it will redo the work. But due to the regular checkpoints it won't have a lot to redo, and it will publish the message with the same commandID, so azure service bus duplicate detection will prevent the actual heavy processing work being duplicated.

```text
Change Feed record
      ↓
publish Service Bus message successfully
      ↓
CRASH
      ↓
Change Feed checkpoint never advances
      ↓
Worker restarts
      ↓
same outbox record may be processed again
      ↓
same CommandId / MessageId published again
```

**5 Why do we say IncidentIQ provides at-least-once processing with duplicate handling, rather than exactly-once processing?**

Because, at the moment we only handle for Duplicate/ identical requests, and State-based Idempotency on Completed state.
So if we scale horizontally and add more Pods, both may see the same message (not duplicate), and begin processing it, then as lets say the first one is still speaking to the RAG which may take several minutes to process, it sees that it isn't completed yet so it also begins to process it. This may result in the same work being done twice.

---

# Round 4 - Pop Quiz

**1. What is the difference between a Service Bus message lock, DeliveryCount, and our Incident AttemptCount?**

- Service Bus Message Lock (PeakLock), is a service buys receive mode that says lock this message as it is has started being processed by a worker
- DeliveryCount: Managed by azure, automatically increments each time a message is consumed (as azure automatically re-sends the message whilst its state is marked as processing) - we compare this against MaxDeliveryCount to determine in our worker when to DLQ it
- AttemptCount: we manage that internally, incrementing it by 1 on each attempt and then persisting it to Cosmos - this is just used for monitoring purposes in admin portal (?)

How Azure Service Bus knows to requeue message

```text
Successful processing
→ CompleteMessageAsync()
→ message removed

Processing fails / message abandoned
→ lock released
→ message becomes available again

Worker dies / stops responding
→ lock eventually expires
→ message becomes available again
```

**2. Why would a Cosmos ETag give us stronger idempotency than simply checking Status == Completed?**

- An ETag is basically a version identifier for a Cosmos document.
- Two Workers somehow read it at roughly the same time, without concurrency protection both may now think they own the work
- With optimistic concurrency, each says "Only perform my update if the document is still version-7"
- So eg only one worker can 'win' as it will finish processing first, making version-8, then the other worker cannot commit its work as it is no longer version-7.

_It's "optimistic" because we don't lock the document while somebody is working. We allow concurrent reads and simply reject a write if the data changed underneath us._

**3. If our Worker successfully finishes the AI analysis and writes Completed to Cosmos, but crashes before CompleteMessageAsync() succeeds, what do you expect Service Bus to do next, and what should our application do when that happens?**

- Service Bus will requeue and mark it for redilvery (how does it know when does it just wait a certain amount of time or something?), but the state will be completed so we will reach our state based idempotency check and then return no-op as work has been done
