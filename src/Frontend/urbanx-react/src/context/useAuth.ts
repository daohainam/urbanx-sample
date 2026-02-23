import { useContext } from 'react';
import { AuthContext } from './authContextDefinition';

export const useAuth = () => useContext(AuthContext);
