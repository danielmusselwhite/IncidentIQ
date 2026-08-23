import { Navigate, Route, Routes } from "react-router-dom";

import AppLayout from "./layout/AppLayout";
import IncidentDetailPage from "./pages/IncidentDetail/IncidentDetailPage";
import SubmitIncidentPage from "./pages/SubmitIncident/SubmitIncidentPage";

export default function App() {
    return (
        <Routes>
            <Route element={<AppLayout />}>
                <Route
                    path="/"
                    element={<Navigate to="/incidents/new" replace />}
                />

                <Route
                    path="/incidents/new"
                    element={<SubmitIncidentPage />}
                />

                <Route
                    path="/incidents/:id"
                    element={<IncidentDetailPage />}
                />
            </Route>
        </Routes>
    );
}