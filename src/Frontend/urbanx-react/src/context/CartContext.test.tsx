import { describe, expect, it } from 'vitest';
import { act, renderHook } from '@testing-library/react';
import type { ReactNode } from 'react';
import { CartProvider } from './CartContext';
import { useCart } from './useCart';
import type { Product } from '../types';

const PRODUCT_A: Product = {
    id: 'p1',
    name: 'Headphones',
    description: 'desc',
    price: 100,
    imageUrl: 'http://example.com/a.png',
    category: 'headphones',
    merchantId: 'm1',
    stockQuantity: 5,
};

const PRODUCT_B: Product = {
    id: 'p2',
    name: 'Watch',
    description: 'desc',
    price: 200,
    imageUrl: 'http://example.com/b.png',
    category: 'watches',
    merchantId: 'm1',
    stockQuantity: 3,
};

const wrapper = ({ children }: { children: ReactNode }) => <CartProvider>{children}</CartProvider>;

describe('CartContext', () => {
    it('starts empty when localStorage is empty', () => {
        const { result } = renderHook(() => useCart(), { wrapper });
        expect(result.current.items).toEqual([]);
        expect(result.current.totalItems).toBe(0);
        expect(result.current.totalPrice).toBe(0);
    });

    it('adds a product, defaulting quantity to 1 and selecting it', () => {
        const { result } = renderHook(() => useCart(), { wrapper });

        act(() => result.current.addToCart(PRODUCT_A));

        expect(result.current.items).toHaveLength(1);
        expect(result.current.items[0]).toMatchObject({ id: 'p1', quantity: 1 });
        expect(result.current.selectedItems.has('p1')).toBe(true);
        expect(result.current.totalItems).toBe(1);
        expect(result.current.totalPrice).toBe(100);
    });

    it('increments quantity when the same product is added twice', () => {
        const { result } = renderHook(() => useCart(), { wrapper });

        act(() => result.current.addToCart(PRODUCT_A, 2));
        act(() => result.current.addToCart(PRODUCT_A, 3));

        expect(result.current.items).toHaveLength(1);
        expect(result.current.items[0].quantity).toBe(5);
        expect(result.current.totalItems).toBe(5);
        expect(result.current.totalPrice).toBe(500);
    });

    it('totalPrice only sums selected items', () => {
        const { result } = renderHook(() => useCart(), { wrapper });

        act(() => result.current.addToCart(PRODUCT_A));
        act(() => result.current.addToCart(PRODUCT_B));
        // Both are selected by default. Deselect A → only B counts.
        act(() => result.current.toggleItemSelection('p1'));

        expect(result.current.totalItems).toBe(2); // unchanged — totalItems counts everything
        expect(result.current.totalPrice).toBe(200); // only B
    });

    it('updateQuantity to 0 removes the item and unselects it', () => {
        const { result } = renderHook(() => useCart(), { wrapper });
        act(() => result.current.addToCart(PRODUCT_A, 3));

        act(() => result.current.updateQuantity('p1', 0));

        expect(result.current.items).toHaveLength(0);
        expect(result.current.selectedItems.has('p1')).toBe(false);
    });

    it('removeFromCart drops the item and the selection', () => {
        const { result } = renderHook(() => useCart(), { wrapper });
        act(() => result.current.addToCart(PRODUCT_A));
        act(() => result.current.addToCart(PRODUCT_B));

        act(() => result.current.removeFromCart('p1'));

        expect(result.current.items.map((i) => i.id)).toEqual(['p2']);
        expect(result.current.selectedItems.has('p1')).toBe(false);
        expect(result.current.selectedItems.has('p2')).toBe(true);
    });

    it('clearCart empties items and selection', () => {
        const { result } = renderHook(() => useCart(), { wrapper });
        act(() => result.current.addToCart(PRODUCT_A));
        act(() => result.current.addToCart(PRODUCT_B));

        act(() => result.current.clearCart());

        expect(result.current.items).toEqual([]);
        expect(result.current.selectedItems.size).toBe(0);
    });

    it('persists items across remounts via localStorage', () => {
        const first = renderHook(() => useCart(), { wrapper });
        act(() => first.result.current.addToCart(PRODUCT_A, 2));
        first.unmount();

        const second = renderHook(() => useCart(), { wrapper });
        expect(second.result.current.items).toHaveLength(1);
        expect(second.result.current.items[0].quantity).toBe(2);
    });

    it('drops malformed localStorage data and starts empty', () => {
        window.localStorage.setItem('urbanx_cart', '{ this is not valid JSON');

        const { result } = renderHook(() => useCart(), { wrapper });

        expect(result.current.items).toEqual([]);
        // The entry should have been cleared so a future write replaces it cleanly.
        expect(window.localStorage.getItem('urbanx_cart')).toBe('[]');
    });

    it('rejects localStorage data that fails the schema (e.g. negative quantity)', () => {
        window.localStorage.setItem(
            'urbanx_cart',
            JSON.stringify([{ id: 'p1', name: 'x', price: 100, quantity: -1 }]),
        );

        const { result } = renderHook(() => useCart(), { wrapper });
        expect(result.current.items).toEqual([]);
    });
});
