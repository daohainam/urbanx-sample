import React, { createContext, useContext, useState, useEffect } from 'react';
import type { CartItem, Product } from '../types';

interface CartContextType {
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

const CartContext = createContext<CartContextType | undefined>(undefined);

export const CartProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const [items, setItems] = useState<CartItem[]>(() => {
        const saved = localStorage.getItem('urbanx_cart');
        return saved ? JSON.parse(saved) : [];
    });

    const [selectedItems, setSelectedItems] = useState<Set<string>>(new Set<string>());

    useEffect(() => {
        localStorage.setItem('urbanx_cart', JSON.stringify(items));
    }, [items]);

    const addToCart = (product: Product, quantity: number = 1) => {
        setItems((prev: CartItem[]) => {
            const existing = prev.find(item => item.id === product.id);
            if (existing) {
                return prev.map(item => item.id === product.id ? { ...item, quantity: item.quantity + quantity } : item);
            }
            return [...prev, { ...product, quantity }];
        });
        setSelectedItems((prev: Set<string>) => {
            const next = new Set(prev);
            next.add(product.id);
            return next;
        });
    };

    const removeFromCart = (productId: string) => {
        setItems((prev: CartItem[]) => prev.filter(item => item.id !== productId));
        setSelectedItems((prev: Set<string>) => {
            const next = new Set(prev);
            next.delete(productId);
            return next;
        });
    };

    const updateQuantity = (productId: string, quantity: number) => {
        if (quantity <= 0) {
            removeFromCart(productId);
            return;
        }
        setItems((prev: CartItem[]) => prev.map(item => item.id === productId ? { ...item, quantity } : item));
    };

    const toggleItemSelection = (productId: string) => {
        setSelectedItems((prev: Set<string>) => {
            const next = new Set(prev);
            if (next.has(productId)) next.delete(productId);
            else next.add(productId);
            return next;
        });
    };

    const clearCart = () => {
        setItems([]);
        setSelectedItems(new Set());
    };

    const totalItems = items.reduce((sum, item) => sum + item.quantity, 0);
    const totalPrice = items
        .filter(item => selectedItems.has(item.id))
        .reduce((sum, item) => sum + item.price * item.quantity, 0);

    return (
        <CartContext.Provider value={{
            items,
            addToCart,
            removeFromCart,
            updateQuantity,
            clearCart,
            totalItems,
            totalPrice,
            selectedItems,
            toggleItemSelection
        }}>
            {children}
        </CartContext.Provider>
    );
};

export const useCart = () => {
    const context = useContext(CartContext);
    if (!context) throw new Error('useCart must be used within a CartProvider');
    return context;
};
