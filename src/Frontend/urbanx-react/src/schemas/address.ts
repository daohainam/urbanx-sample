import { z } from 'zod';
import { nonEmpty, postalCode } from './common';

// Used by the saved-addresses CRUD page (Profile → Addresses).
export const savedAddressSchema = z.object({
  type: nonEmpty('Address type'),
  name: nonEmpty('Full name').min(2, 'Enter your full name'),
  street: nonEmpty('Street address'),
  city: nonEmpty('City'),
  zip: postalCode,
  country: nonEmpty('Country'),
  isDefault: z.boolean(),
});

export type SavedAddressForm = z.infer<typeof savedAddressSchema>;

// Used by the checkout flow (separate first/last name fields, no isDefault).
export const checkoutAddressSchema = z.object({
  firstName: nonEmpty('First name'),
  lastName: nonEmpty('Last name'),
  street: nonEmpty('Street address'),
  city: nonEmpty('City'),
  zipCode: postalCode,
  country: nonEmpty('Country'),
});

export type CheckoutAddressForm = z.infer<typeof checkoutAddressSchema>;
