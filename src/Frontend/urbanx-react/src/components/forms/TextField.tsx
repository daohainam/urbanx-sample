import React from 'react';

type TextFieldProps = {
    id: string;
    label: string;
    placeholder?: string;
    type?: React.HTMLInputTypeAttribute;
    autoComplete?: string;
    icon?: React.ReactNode;
    error?: string;
    /** When true, the label is rendered for screen readers only. */
    visuallyHiddenLabel?: boolean;
    name: string;
    onBlur: React.FocusEventHandler<HTMLInputElement>;
    onChange: React.ChangeEventHandler<HTMLInputElement>;
    ref?: React.Ref<HTMLInputElement>;
};

/**
 * Standard text input wired to react-hook-form. Spread the result of `register('field')`
 * onto this component; pass `error={errors.field?.message}` for inline validation.
 */
export const TextField = ({
    id,
    label,
    placeholder,
    type = 'text',
    autoComplete,
    icon,
    error,
    visuallyHiddenLabel = false,
    ...rest
}: TextFieldProps) => (
    <div>
        <label htmlFor={id} className={visuallyHiddenLabel ? 'sr-only' : 'text-xs font-bold uppercase tracking-wider text-gray-500 block mb-2'}>
            {label}
        </label>
        <div className="relative">
            {icon && (
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-gray-400">
                    {icon}
                </div>
            )}
            <input
                id={id}
                type={type}
                autoComplete={autoComplete}
                aria-invalid={Boolean(error)}
                aria-describedby={error ? `${id}-error` : undefined}
                placeholder={placeholder}
                className={`appearance-none relative block w-full ${icon ? 'pl-10' : 'pl-4'} pr-3 py-3 border placeholder-gray-500 text-gray-900 rounded-md focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary transition-all sm:text-sm bg-white ${
                    error ? 'border-red-400' : 'border-gray-300'
                }`}
                {...rest}
            />
        </div>
        {error && (
            <p id={`${id}-error`} role="alert" className="mt-1.5 text-sm text-red-600">
                {error}
            </p>
        )}
    </div>
);
