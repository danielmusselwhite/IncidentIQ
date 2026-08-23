import { NavLink, Outlet } from "react-router-dom";

import "./AppLayout.css";

const navigation = [
    { label: "Dashboard", path: "/incidents" },
    { label: "Submit Incident", path: "/incidents/new" },
    { label: "Runbooks", path: "/runbooks" },
    { label: "Operations", path: "/operations" },
];

export default function AppLayout() {
    return (
        <div className="app-shell">
            <aside className="app-sidebar">
                <div className="app-sidebar__brand">
                    <div className="app-sidebar__logo">IQ</div>

                    <div>
                        <div className="app-sidebar__name">IncidentIQ</div>
                        <div className="app-sidebar__subtitle">
                            Incident Intelligence
                        </div>
                    </div>
                </div>

                <nav className="app-sidebar__nav">
                    <p className="app-sidebar__section-label">
                        Workspace
                    </p>

                    {navigation.map((item) => (
                        <NavLink
                            key={item.path}
                            to={item.path}
                            end={item.path === "/incidents"}
                            className={({ isActive }) =>
                                `app-sidebar__link${
                                    isActive
                                        ? " app-sidebar__link--active"
                                        : ""
                                }`
                            }
                        >
                            <span>{item.label}</span>
                        </NavLink>
                    ))}
                </nav>

                <div className="app-sidebar__footer">
                    <div className="app-sidebar__environment">
                        <span className="app-sidebar__environment-dot" />

                        Development
                    </div>

                    <span className="app-sidebar__version">v0.1</span>
                </div>
            </aside>

            <div className="app-main">
                <header className="app-topbar">
                    <div className="app-topbar__status">
                        <span className="app-topbar__status-dot" />
                        System operational
                    </div>

                    <div className="app-topbar__profile">
                        <div className="app-topbar__avatar">DU</div>

                        <div className="app-topbar__profile-text">
                            <strong>Development User</strong>
                            <span>Engineer</span>
                        </div>
                    </div>
                </header>

                <div className="app-content">
                    <Outlet />
                </div>
            </div>
        </div>
    );
}