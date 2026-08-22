import { useEffect, useState } from "react";
import "./App.css";

function App() {
    const [apiStatus, setApiStatus] = useState("Checking...");

    useEffect(() => {
        const checkApi = async () => {
            try {
                const response = await fetch(
                    `${import.meta.env.VITE_API_BASE_URL}/api/health`,
                );

                if (!response.ok) {
                    throw new Error();
                }

                const status = await response.text();
                setApiStatus(status);
            } catch {
                setApiStatus("Unavailable");
            }
        };

        checkApi();
    }, []);

    return (
        <main>
            <h1>IncidentIQ</h1>
            <p>API Status: {apiStatus}</p>
        </main>
    );
}

export default App;