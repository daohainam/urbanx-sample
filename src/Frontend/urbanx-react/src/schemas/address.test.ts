import { describe, expect, it } from 'vitest';
import { checkoutAddressSchema, savedAddressSchema } from './address';

describe('checkoutAddressSchema', () => {
    it('accepts a fully populated address', () => {
        const result = checkoutAddressSchema.safeParse({
            firstName: 'Jane',
            lastName: 'Doe',
            street: '123 Main St',
            city: 'New York',
            zipCode: '10001',
            country: 'USA',
        });
        expect(result.success).toBe(true);
    });

    it('rejects empty required fields with friendly messages', () => {
        const result = checkoutAddressSchema.safeParse({
            firstName: '',
            lastName: '',
            street: '',
            city: '',
            zipCode: '',
            country: '',
        });
        expect(result.success).toBe(false);
        if (!result.success) {
            const fieldErrors = result.error.flatten().fieldErrors;
            expect(fieldErrors.firstName?.[0]).toMatch(/required/i);
            expect(fieldErrors.zipCode?.[0]).toMatch(/postal/i);
        }
    });

    it('trims whitespace-only values', () => {
        const result = checkoutAddressSchema.safeParse({
            firstName: '   ',
            lastName: 'Doe',
            street: '123 Main St',
            city: 'NY',
            zipCode: '10001',
            country: 'USA',
        });
        expect(result.success).toBe(false);
    });
});

describe('savedAddressSchema', () => {
    it('accepts a valid saved address with isDefault=false', () => {
        const result = savedAddressSchema.safeParse({
            type: 'Home',
            name: 'Jane Doe',
            street: '1 Park Ave',
            city: 'Boston',
            zip: '02115',
            country: 'USA',
            isDefault: false,
        });
        expect(result.success).toBe(true);
    });
});
