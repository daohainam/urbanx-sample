import { useContext } from 'react';
import { CartContext } from './cartContextDefinition';

export const useCart = () => {
    const context = useContext(CartContext);
    if (!context) throw new Error('useCart must be used within a CartProvider');
    return context;
};
