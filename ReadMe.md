# IncidentIQ

- Architecture Explanation here


## IncidentIQ.API

- Asp.Net Core Web API
- Uses Clean Architecture principles
- Other tech here
- When incident is posted, puts message on azure service bus queue for IncidentIQ.Worker to process + puts it in cosmo db


## IncidentIQ.Worker

- Proccesses it then does RAG LLM stuff to summarise it and link it with other incidents in the database

## IncidentIQ.Web

- React website
- Speaks with API to blah blah blah