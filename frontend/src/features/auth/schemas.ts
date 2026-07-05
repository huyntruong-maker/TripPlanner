import { z } from 'zod';

const MIN_PASSWORD_LENGTH = 8;

export const loginSchema = z.object({
  email: z.string().min(1, 'Email is required').email('Enter a valid email address'),
  password: z.string().min(1, 'Password is required'),
  rememberMe: z.boolean(),
});
export type LoginFormValues = z.infer<typeof loginSchema>;

export const registerSchema = z.object({
  firstName: z.string().min(1, 'First name is required').max(100, 'First name is too long'),
  email: z.string().min(1, 'Email is required').email('Enter a valid email address'),
  password: z
    .string()
    .min(MIN_PASSWORD_LENGTH, `Password must be at least ${MIN_PASSWORD_LENGTH} characters`),
});
export type RegisterFormValues = z.infer<typeof registerSchema>;
