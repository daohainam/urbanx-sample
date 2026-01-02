import { createContext } from 'react';
import type { CartItem, Product } from '../types';

export interface CartContextType {
    items: CartItem[];
    addToCart: (product: Product, quantity?: number) => void;
    removeFromCart: (productId: string) => void;
    updateQuantity: (productId: string, quantity: number) => void;
    clearCart: () => void;
    totalItems: number;
    totalPrice: number;
    selectedItems: Set<string>;
    toggleItemSelection: (productId: string) => void;
}

export const CartContext = createContext<CartContextType | undefined>(undefined);
