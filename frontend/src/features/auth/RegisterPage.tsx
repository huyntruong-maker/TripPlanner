import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link } from 'react-router-dom';
import { useAuth } from '../../auth/AuthContext';
import { getApiErrorMessage } from '../../api/errors';
import { useToast } from '../../components/toast/ToastProvider';
import { registerSchema, type RegisterFormValues } from './schemas';

/**
 * Feature 4 / US1 — sign up with email and password.
 *
 * The account requires email verification before it can log in (backend
 * rejects unverified logins with Auth.Login.InActive), so this does not
 * navigate to /trips on success — it shows a "check your email" message.
 */
export function RegisterPage() {
  const { register: registerUser } = useAuth();
  const { showToast } = useToast();
  const [formError, setFormError] = useState<string | null>(null);
  const [registeredEmail, setRegisteredEmail] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: { firstName: '', email: '', password: '' },
  });

  async function onSubmit(values: RegisterFormValues) {
    setFormError(null);
    try {
      await registerUser(values.email, values.password, values.firstName);
      setRegisteredEmail(values.email);
    } catch (err) {
      const message = getApiErrorMessage(err, 'Registration failed.');
      setFormError(message);
      showToast(message);
    }
  }

  if (registeredEmail) {
    return (
      <div className="card">
        <h1>Check your email</h1>
        <p>We sent a verification link to {registeredEmail}. Verify your account, then log in.</p>
        <p>
          <Link to="/login">Log in</Link>
        </p>
      </div>
    );
  }

  return (
    <div className="card">
      <h1>Create account</h1>
      <form onSubmit={handleSubmit(onSubmit)} noValidate>
        <label>
          First name
          <input
            autoComplete="given-name"
            {...register('firstName')}
            aria-invalid={Boolean(errors.firstName)}
            aria-describedby={errors.firstName ? 'register-firstName-error' : undefined}
          />
          {errors.firstName && (
            <p className="error" id="register-firstName-error">
              {errors.firstName.message}
            </p>
          )}
        </label>
        <label>
          Email
          <input
            type="email"
            autoComplete="email"
            {...register('email')}
            aria-invalid={Boolean(errors.email)}
            aria-describedby={errors.email ? 'register-email-error' : undefined}
          />
          {errors.email && (
            <p className="error" id="register-email-error">
              {errors.email.message}
            </p>
          )}
        </label>
        <label>
          Password (min 8 characters)
          <input
            type="password"
            autoComplete="new-password"
            {...register('password')}
            aria-invalid={Boolean(errors.password)}
            aria-describedby={errors.password ? 'register-password-error' : undefined}
          />
          {errors.password && (
            <p className="error" id="register-password-error">
              {errors.password.message}
            </p>
          )}
        </label>
        {formError && (
          <p className="error" role="alert">
            {formError}
          </p>
        )}
        <button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Creating…' : 'Sign up'}
        </button>
      </form>
      <p>
        Already have an account? <Link to="/login">Log in</Link>
      </p>
    </div>
  );
}
