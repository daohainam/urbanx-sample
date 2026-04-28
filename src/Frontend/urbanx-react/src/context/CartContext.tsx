import React, { useState, useEffect, useMemo, useCallback } from 'react';
import type { CartItem, Product } from '../types';
import { CartContext } from './cartContextDefinition';
import { cartItemsSchema } from '../schemas/cart';
import { logger } from '../lib/logger';

const STORAGE_KEY = 'urbanx_cart';

// Selection is intentionally ephemeral — it represents a per-session "what am I about
// to check out" intent, not a long-lived preference. We do not persist it.

function loadInitialCart(): CartItem[] {
    if (typeof window === 'undefined') return [];
    const raw = window.localStorage.getItem(STORAGE_KEY);
    if (!raw) return [];
    try {
        const parsed = JSON.parse(raw);
        const result = cartItemsSchema.safeParse(parsed);
        if (!result.success) {
            logger.warn('Discarding malformed cart from localStorage', { issues: result.error.issues });
            window.localStorage.removeItem(STORAGE_KEY);
            return [];
        }
        // The validated schema fills in optional defaults; the result is structurally a CartItem[].
        return result.data as CartItem[];
    } catch (err) {
        logger.warn('Failed to parse cart from localStorage; resetting', { err });
        window.localStorage.removeItem(STORAGE_KEY);
        return [];
    }
}

export const CartProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const [items, setItems] = useState<CartItem[]>(loadInitialCart);
    const [selectedItems, setSelectedItems] = useState<Set<string>>(new Set<string>());

    useEffect(() => {
        try {
            window.localStorage.setItem(STORAGE_KEY, JSON.stringify(items));
        } catch (err) {
            // Quota exceeded or storage disabled — non-fatal.
            logger.warn('Failed to persist cart to localStorage', { err });
        }
    }, [items]);

    const addToCart = useCallback((product: Product, quantity: number = 1) => {
        setItems((prev) => {
            const existing = prev.find((item) => item.id === product.id);
            if (existing) {
                return prev.map((item) =>
                    item.id === product.id ? { ...item, quantity: item.quantity + quantity } : item,
                );
            }
            return [...prev, { ...product, quantity }];
        });
        setSelectedItems((prev) => {
            const next = new Set(prev);
            next.add(product.id);
            return next;
        });
    }, []);

    const removeFromCart = useCallback((productId: string) => {
        setItems((prev) => prev.filter((item) => item.id !== productId));
        setSelectedItems((prev) => {
            const next = new Set(prev);
            next.delete(productId);
            return next;
        });
    }, []);

    const updateQuantity = useCallback((productId: string, quantity: number) => {
        if (quantity <= 0) {
            setItems((prev) => prev.filter((item) => item.id !== productId));
            setSelectedItems((prev) => {
                const next = new Set(prev);
                next.delete(productId);
                return next;
            });
            return;
        }
        setItems((prev) => prev.map((item) => (item.id === productId ? { ...item, quantity } : item)));
    }, []);

    const toggleItemSelection = useCallback((productId: string) => {
        setSelectedItems((prev) => {
            const next = new Set(prev);
            if (next.has(productId)) next.delete(productId);
            else next.add(productId);
            return next;
        });
    }, []);

    const clearCart = useCallback(() => {
        setItems([]);
        setSelectedItems(new Set());
    }, []);

    const totalItems = useMemo(
        () => items.reduce((sum, item) => sum + item.quantity, 0),
        [items],
    );

    const totalPrice = useMemo(
        () =>
            items
                .filter((item) => selectedItems.has(item.id))
                .reduce((sum, item) => sum + item.price * item.quantity, 0),
        [items, selectedItems],
    );

    const value = useMemo(
        () => ({
            items,
            addToCart,
            removeFromCart,
            updateQuantity,
            clearCart,
            totalItems,
            totalPrice,
            selectedItems,
            toggleItemSelection,
        }),
        [
            items,
            addToCart,
            removeFromCart,
            updateQuantity,
            clearCart,
            totalItems,
            totalPrice,
            selectedItems,
            toggleItemSelection,
        ],
    );

    return <CartContext.Provider value={value}>{children}</CartContext.Provider>;
};
