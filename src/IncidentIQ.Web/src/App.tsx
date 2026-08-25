import { Navigate, Route, Routes } from "react-router-dom";

import AppLayout from "./layout/AppLayout";
import IncidentDetailPage from "./pages/IncidentDetail/IncidentDetailPage";
import SubmitIncidentPage from "./pages/SubmitIncident/SubmitIncidentPage";
import IncidentsPage from "./pages/Incidents/IncidentsPage";
import RunbooksPage from "./pages/Runbooks/RunbooksPage";
import CreateRunbookPage from "./pages/CreateRunbook/CreateRunbookPage";
import EditRunbookPage from "./pages/EditRunbook/EditRunbookPage";
import RunbookDetailPage from "./pages/RunbookDetail/RunbookDetailPage";

/**
 * Defines the application's top-level routes.
 *
 * Each URL path is mapped to the React page component that should be shown.
 * All routes are wrapped in AppLayout so shared UI such as navigation can
 * remain consistent across the application.
 */
export default function App() {
    return (
        // Routes looks at the current URL and renders the matching Route.
        <Routes>
            {/*
             * This parent route has no path of its own.
             * Instead, it wraps all child routes with AppLayout.
             *
             * AppLayout should contain an <Outlet /> where the currently
             * selected child page will be rendered.
             */}
            <Route element={<AppLayout />}>
                {/*
                 * Redirect the root URL to the incident submission page.
                 *
                 * replace prevents "/" from being added as a separate entry
                 * in the browser history, so pressing Back does not immediately
                 * return the user to the redirect.
                 */}
                <Route
                    path="/"
                    element={<Navigate to="/incidents/new" replace />}
                />

                {/* Displays the form for submitting a new incident. */}
                <Route
                    path="/incidents/new"
                    element={<SubmitIncidentPage />}
                />

                {/*
                 * ":id" is a dynamic route parameter.
                 *
                 * For example:
                 * /incidents/123
                 *
                 * IncidentDetailPage can read "123" using useParams().
                 */}
                <Route
                    path="/incidents/:id"
                    element={<IncidentDetailPage />}
                />

                {/* Displays the incident dashboard/list. */}
                <Route
                    path="/incidents"
                    element={<IncidentsPage />}
                />

                {/* Displays the runbook management page. */}
                <Route
                    path="runbooks"
                    element={<RunbooksPage />}
                />

                {/* Displays the page for creating a new runbook. */}
                <Route
                    path="runbooks/new"
                    element={<CreateRunbookPage />}
                />

                {/* Displays the details of an existing runbook. */}
                <Route
                    path="runbooks/:id"
                    element={<RunbookDetailPage />}
                />

                {/* Displays the page for editing an existing runbook. */}
                <Route
                    path="runbooks/:id/edit"
                    element={<EditRunbookPage />}
                />
            </Route>
        </Routes>
    );
}