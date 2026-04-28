import { z } from 'zod';
import { email, password, nonEmpty } from './common';

export const signUpSchema = z
  .object({
    name: nonEmpty('Full name').min(2, 'Enter your full name'),
    email,
    password,
    confirmPassword: z.string().min(1, 'Confirm your password'),
    termsAccepted: z.boolean().refine((v) => v, {
      message: 'You must accept the Terms of Service to continue',
    }),
  })
  .refine((d) => d.password === d.confirmPassword, {
    path: ['confirmPassword'],
    message: "Passwords don't match",
  });

export type SignUpForm = z.infer<typeof signUpSchema>;

export const forgotPasswordSchema = z.object({ email });
export type ForgotPasswordForm = z.infer<typeof forgotPasswordSchema>;

export const resetPasswordSchema = z
  .object({
    code: nonEmpty('Verification code').min(4, 'Enter the code we sent to your email'),
    newPassword: password,
    confirmPassword: z.string().min(1, 'Confirm your new password'),
  })
  .refine((d) => d.newPassword === d.confirmPassword, {
    path: ['confirmPassword'],
    message: "Passwords don't match",
  });

export type ResetPasswordForm = z.infer<typeof resetPasswordSchema>;
