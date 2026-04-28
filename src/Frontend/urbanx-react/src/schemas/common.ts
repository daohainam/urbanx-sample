import { z } from 'zod';

// Reusable primitives. Keep error messages user-facing — they appear next to the field.

export const email = z
  .string()
  .trim()
  .min(1, 'Email is required')
  .email('Enter a valid email address');

export const password = z
  .string()
  .min(8, 'Must be at least 8 characters')
  .max(128, 'Must be at most 128 characters')
  .regex(/[A-Za-z]/, 'Must contain a letter')
  .regex(/[0-9]/, 'Must contain a number');

export const nonEmpty = (label: string) =>
  z.string().trim().min(1, `${label} is required`);

// Permissive ZIP/postal — server is the source of truth. Just block obvious garbage.
export const postalCode = z
  .string()
  .trim()
  .min(3, 'Enter a valid postal code')
  .max(12, 'Enter a valid postal code');
