import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { BrowserRouter } from "react-router-dom";

import App from "./App";

import "./index.css";

/**
 * Application entry point.
 *
 * Finds the root HTML element, creates the React application inside it,
 * and wraps the app with the providers it needs to run.
 */
createRoot(document.getElementById("root")!).render(
    <StrictMode>
        {/*
         * BrowserRouter enables client-side routing using the browser URL.
         *
         * Components inside it can use React Router features such as
         * <Routes>, <Link>, useNavigate(), and useParams().
         */}
        <BrowserRouter>
            <App />
        </BrowserRouter>
    </StrictMode>,
);