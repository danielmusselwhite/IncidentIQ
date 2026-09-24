import {
    createContext,
    type ReactNode,
    useContext,
    useEffect,
    useMemo,
    useState,
} from "react";

import { getCurrentUser } from "../api/currentUserApi";
import type { CurrentUser } from "../types/currentUser";

type CurrentUserContextValue = {
    user: CurrentUser | null;
    isLoading: boolean;
    error: string | null;
    isAdministrator: boolean;
};

const CurrentUserContext =
    createContext<CurrentUserContextValue | undefined>(
        undefined,
    );

type CurrentUserProviderProps = {
    children: ReactNode;
};

export function CurrentUserProvider({
    children,
}: CurrentUserProviderProps) {
    const [user, setUser] =
        useState<CurrentUser | null>(null);

    const [isLoading, setIsLoading] =
        useState(true);

    const [error, setError] =
        useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;

        async function loadCurrentUser() {
            try {
                const currentUser =
                    await getCurrentUser();

                if (!cancelled) {
                    setUser(currentUser);
                    setError(null);
                }
            } catch (loadError) {
                if (!cancelled) {
                    setError(
                        loadError instanceof Error
                            ? loadError.message
                            : "Unable to load the current user.",
                    );
                }
            } finally {
                if (!cancelled) {
                    setIsLoading(false);
                }
            }
        }

        void loadCurrentUser();

        return () => {
            cancelled = true;
        };
    }, []);

    const value = useMemo(
        () => ({
            user,
            isLoading,
            error,
            isAdministrator:
                user?.roles.includes("Administrator") ??
                false,
        }),
        [user, isLoading, error],
    );

    return (
        <CurrentUserContext.Provider value={value}>
            {children}
        </CurrentUserContext.Provider>
    );
}

export function useCurrentUser() {
    const context =
        useContext(CurrentUserContext);

    if (!context) {
        throw new Error(
            "useCurrentUser must be used inside CurrentUserProvider.",
        );
    }

    return context;
}