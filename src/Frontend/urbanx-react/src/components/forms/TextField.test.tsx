import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { TextField } from './TextField';

describe('TextField', () => {
    it('renders with a visible label by default and associates it with the input', () => {
        render(
            <TextField
                id="email"
                name="email"
                label="Email"
                placeholder="Email"
                onBlur={() => {}}
                onChange={() => {}}
            />,
        );

        const input = screen.getByLabelText('Email');
        expect(input).toBeInTheDocument();
        expect(input.id).toBe('email');
    });

    it('renders the label as visually hidden when visuallyHiddenLabel is true', () => {
        render(
            <TextField
                id="email"
                name="email"
                label="Email"
                visuallyHiddenLabel
                placeholder="Email"
                onBlur={() => {}}
                onChange={() => {}}
            />,
        );
        // The label still exists in the DOM (sr-only) — getByLabelText should still find the input.
        expect(screen.getByLabelText('Email')).toBeInTheDocument();
    });

    it('shows the error message and links it via aria-describedby + aria-invalid', () => {
        render(
            <TextField
                id="email"
                name="email"
                label="Email"
                placeholder="Email"
                error="Enter a valid email address"
                onBlur={() => {}}
                onChange={() => {}}
            />,
        );

        const input = screen.getByLabelText('Email');
        expect(input).toHaveAttribute('aria-invalid', 'true');
        expect(input).toHaveAttribute('aria-describedby', 'email-error');

        const errorMsg = screen.getByRole('alert');
        expect(errorMsg).toHaveTextContent('Enter a valid email address');
        expect(errorMsg).toHaveAttribute('id', 'email-error');
    });

    it('forwards typed text via onChange', async () => {
        const user = userEvent.setup();
        const seen: string[] = [];

        render(
            <TextField
                id="q"
                name="q"
                label="Search"
                onBlur={() => {}}
                onChange={(e) => seen.push(e.target.value)}
            />,
        );

        await user.type(screen.getByLabelText('Search'), 'abc');
        expect(seen.at(-1)).toBe('abc');
    });
});
