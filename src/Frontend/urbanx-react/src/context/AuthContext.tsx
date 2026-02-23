import React, { useState, useEffect } from 'react';
import type { User } from 'oidc-client-ts';
import { userManager } from '../services/auth';
import { AuthContext } from './authContextDefinition';

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const [user, setUser] = useState<User | null>(null);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        userManager.getUser().then(u => {
            setUser(u);
            setIsLoading(false);
        });

        const onUserLoaded = (u: User) => setUser(u);
        const onUserUnloaded = () => setUser(null);

        userManager.events.addUserLoaded(onUserLoaded);
        userManager.events.addUserUnloaded(onUserUnloaded);

        return () => {
            userManager.events.removeUserLoaded(onUserLoaded);
            userManager.events.removeUserUnloaded(onUserUnloaded);
        };
    }, []);

    const login = () => userManager.signinRedirect();
    const logout = () => userManager.signoutRedirect();

    return (
        <AuthContext.Provider value={{ user, isLoading, login, logout }}>
            {children}
        </AuthContext.Provider>
    );
};
