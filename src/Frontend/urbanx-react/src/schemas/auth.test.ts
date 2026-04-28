import { describe, expect, it } from 'vitest';
import { signUpSchema, forgotPasswordSchema, resetPasswordSchema } from './auth';

describe('signUpSchema', () => {
    const validInput = {
        name: 'Jane Doe',
        email: 'jane@example.com',
        password: 'hunter2!',
        confirmPassword: 'hunter2!',
        termsAccepted: true,
    };

    it('accepts a well-formed signup', () => {
        expect(signUpSchema.safeParse(validInput).success).toBe(true);
    });

    it('rejects a short password', () => {
        const result = signUpSchema.safeParse({ ...validInput, password: 'a1', confirmPassword: 'a1' });
        expect(result.success).toBe(false);
        if (!result.success) {
            const issues = result.error.flatten().fieldErrors;
            expect(issues.password?.[0]).toMatch(/at least 8/i);
        }
    });

    it('rejects a password with no digits', () => {
        const result = signUpSchema.safeParse({ ...validInput, password: 'noNumbers', confirmPassword: 'noNumbers' });
        expect(result.success).toBe(false);
        if (!result.success) {
            const issues = result.error.flatten().fieldErrors;
            expect(issues.password?.[0]).toMatch(/number/i);
        }
    });

    it('reports a confirmPassword mismatch under the confirmPassword path', () => {
        const result = signUpSchema.safeParse({
            ...validInput,
            confirmPassword: 'different1',
        });
        expect(result.success).toBe(false);
        if (!result.success) {
            const fieldErrors = result.error.flatten().fieldErrors;
            expect(fieldErrors.confirmPassword?.[0]).toMatch(/match/i);
        }
    });

    it('rejects when terms are not accepted', () => {
        const result = signUpSchema.safeParse({ ...validInput, termsAccepted: false });
        expect(result.success).toBe(false);
        if (!result.success) {
            const fieldErrors = result.error.flatten().fieldErrors;
            expect(fieldErrors.termsAccepted?.[0]).toMatch(/Terms/i);
        }
    });

    it('rejects malformed emails', () => {
        const result = signUpSchema.safeParse({ ...validInput, email: 'not-an-email' });
        expect(result.success).toBe(false);
    });
});

describe('forgotPasswordSchema', () => {
    it('accepts a valid email', () => {
        expect(forgotPasswordSchema.safeParse({ email: 'a@b.co' }).success).toBe(true);
    });

    it('rejects empty email with a required-style message', () => {
        const result = forgotPasswordSchema.safeParse({ email: '' });
        expect(result.success).toBe(false);
        if (!result.success) {
            expect(result.error.flatten().fieldErrors.email?.[0]).toMatch(/required/i);
        }
    });
});

describe('resetPasswordSchema', () => {
    it('requires the two passwords to match', () => {
        const result = resetPasswordSchema.safeParse({
            code: '123456',
            newPassword: 'hunter2!',
            confirmPassword: 'different9',
        });
        expect(result.success).toBe(false);
        if (!result.success) {
            expect(result.error.flatten().fieldErrors.confirmPassword?.[0]).toMatch(/match/i);
        }
    });
});
