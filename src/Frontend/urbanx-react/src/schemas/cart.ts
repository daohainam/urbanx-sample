import { z } from 'zod';

// Validates the shape we trust to deserialise from localStorage. Server payloads
// from the catalog API may be richer; this is just a defensive contract.
export const cartItemSchema = z.object({
  id: z.string().min(1),
  name: z.string(),
  description: z.string().default(''),
  price: z.number().nonnegative(),
  imageUrl: z.string().default(''),
  category: z.string().default(''),
  merchantId: z.string().default(''),
  stockQuantity: z.number().int().nonnegative().default(0),
  quantity: z.number().int().positive(),
});

export const cartItemsSchema = z.array(cartItemSchema);

export type ValidatedCartItem = z.infer<typeof cartItemSchema>;
