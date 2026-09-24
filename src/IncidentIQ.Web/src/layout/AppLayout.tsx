import { NavLink, Outlet } from "react-router-dom";
import { useMsal } from "@azure/msal-react";
import "./AppLayout.css";
import { useCurrentUser } from "../auth/CurrentUserContext";

/**
 * Defines the navigation items shown in the application sidebar.
 *
 * Keeping navigation data in an array makes it easier to add, remove,
 * or reorder links without duplicating the NavLink markup.
 */
const navigation = [
    { label: "Dashboard", path: "/incidents" },
    { label: "Submit Incident", path: "/incidents/new" },
    { label: "Runbooks", path: "/runbooks" },
    { label: "Assistant", path: "/assistant" },
    { label: "Operations", path: "/operations", administratorOnly: true },
];

/**
 * Provides the shared layout used by the application's pages.
 *
 * The layout contains the sidebar, top navigation bar, and main content area.
 * React Router renders the currently selected child route inside <Outlet />.
 */
export default function AppLayout() {
    //#region Authentication and User Info
    const { instance, accounts } = useMsal();

    const { isAdministrator } = useCurrentUser();

    const account = accounts[0];

    const displayName =
        account?.name ??
        account?.username ??
        "Signed-in user";

    const initials = displayName
        .split(" ")
        .map(part => part[0])
        .join("")
        .slice(0, 2)
        .toUpperCase();

    const handleLogout = async () => {
        await instance.logoutRedirect({
            account,
            postLogoutRedirectUri: window.location.origin,
        });
    };
    //#endregion

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

                    {navigation
                        .filter(item => !item.administratorOnly || isAdministrator) // Only include items that are not admin-only or the user is an admin
                        .map((item) => ( // Convert each navigation item into a NavLink
                            <NavLink
                                key={item.path}
                                to={item.path}

                                /*
                                 * Without "end", "/incidents" would also be considered
                                 * active for routes such as "/incidents/new".
                                 *
                                 * We therefore require an exact match specifically
                                 * for the Dashboard route.
                                 */
                                end={item.path === "/incidents"}

                                /*
                                 * NavLink provides isActive automatically based on
                                 * whether its route matches the current URL.
                                 *
                                 * An additional CSS class is added when active so
                                 * the selected navigation item can be highlighted.
                                 */
                                className={({ isActive }) =>
                                    `app-sidebar__link${isActive
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
                        <div className="app-topbar__avatar">
                            {initials}
                        </div>

                        <div className="app-topbar__profile-text">
                            <strong>{displayName}</strong>
                            <span>{account?.username}</span>
                        </div>

                        <button
                            type="button"
                            className="button button--secondary"
                            onClick={handleLogout}
                        >
                            Sign out
                        </button>
                    </div>
                </header>

                <div className="app-content">
                    {/*
                     * Outlet is the position where React Router renders
                     * whichever child route currently matches the URL.
                     *
                     * For example, navigating to "/incidents" causes
                     * <IncidentsPage /> to appear here.
                     */}
                    <Outlet />
                </div>
            </div>
        </div>
    );
}