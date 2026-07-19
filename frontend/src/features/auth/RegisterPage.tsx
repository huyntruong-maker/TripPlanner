import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link } from 'react-router-dom';
import { useAuth } from '../../auth/AuthContext';
import { getApiErrorMessage } from '../../api/errors';
import { useToast } from '../../components/toast/ToastProvider';
import { registerSchema, PASSWORD_POLICY_MESSAGE, type RegisterFormValues } from './schemas';

const INPUT_CLASSES =
  'w-full border border-outline-variant rounded-lg px-4 py-3 text-body-md focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all outline-none';

/** Doesn't navigate to /trips on success — shows a "check your email" message since the account needs verification first. */
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
      <div className="flex items-center justify-center">
        <div className="w-full max-w-[480px]">
          <div className="bg-surface-container-lowest rounded-xl p-8 md:p-10 elevation-l1 border border-outline-variant/30 text-center">
            <div
              className="mx-auto mb-6 flex h-16 w-16 items-center justify-center rounded-full bg-primary-container"
              aria-hidden="true"
            >
              <span className="material-symbols-outlined text-primary text-[32px]">
                mark_email_read
              </span>
            </div>
            <h1 className="text-headline-md font-headline-md text-on-surface mb-2">
              Check your email
            </h1>
            <p className="text-body-md font-body-md text-on-surface-variant mb-2">
              We sent a verification link to <strong className="text-on-surface">{registeredEmail}</strong>.
              Click the link to activate your account, then come back and log in.
            </p>
            <p className="text-label-sm font-label-sm text-on-surface-variant mb-6">
              Don&apos;t see it? Check your spam or junk folder.
            </p>
            <p className="text-body-md font-body-md text-on-surface-variant">
              <Link to="/login" className="text-primary font-bold hover:underline">
                Back to log in
              </Link>
            </p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="flex items-center justify-center">
      <div className="w-full max-w-[480px]">
        <div className="bg-surface-container-lowest rounded-xl p-8 md:p-10 elevation-l1 border border-outline-variant/30">
          <div className="mb-8">
            <h1 className="text-headline-md font-headline-md text-on-surface mb-2">
              Create account
            </h1>
            <p className="text-body-md font-body-md text-on-surface-variant">
              Start your next journey with personalized planning tools.
            </p>
          </div>
          <form className="space-y-6" onSubmit={handleSubmit(onSubmit)} noValidate>
            <div className="space-y-2">
              <label
                className="block text-label-md font-label-md text-on-surface-variant"
                htmlFor="register-firstName"
              >
                First name
              </label>
              <input
                id="register-firstName"
                autoComplete="given-name"
                className={INPUT_CLASSES}
                {...register('firstName')}
                aria-invalid={Boolean(errors.firstName)}
                aria-describedby={errors.firstName ? 'register-firstName-error' : undefined}
              />
              {errors.firstName && (
                <p className="text-error text-label-sm font-semibold" id="register-firstName-error">
                  {errors.firstName.message}
                </p>
              )}
            </div>
            <div className="space-y-2">
              <label
                className="block text-label-md font-label-md text-on-surface-variant"
                htmlFor="register-email"
              >
                Email
              </label>
              <input
                id="register-email"
                type="email"
                autoComplete="email"
                className={INPUT_CLASSES}
                {...register('email')}
                aria-invalid={Boolean(errors.email)}
                aria-describedby={errors.email ? 'register-email-error' : undefined}
              />
              {errors.email && (
                <p className="text-error text-label-sm font-semibold" id="register-email-error">
                  {errors.email.message}
                </p>
              )}
            </div>
            <div className="space-y-2">
              <label
                className="block text-label-md font-label-md text-on-surface-variant"
                htmlFor="register-password"
              >
                Password
              </label>
              <input
                id="register-password"
                type="password"
                autoComplete="new-password"
                className={INPUT_CLASSES}
                {...register('password')}
                aria-invalid={Boolean(errors.password)}
                aria-describedby="register-password-hint register-password-error"
              />
              <p className="text-label-sm text-on-surface-variant" id="register-password-hint">
                {PASSWORD_POLICY_MESSAGE}
              </p>
              {errors.password && (
                <p className="text-error text-label-sm font-semibold" id="register-password-error">
                  {errors.password.message}
                </p>
              )}
            </div>
            {formError && (
              <p className="text-error text-label-sm font-semibold" role="alert">
                {formError}
              </p>
            )}
            <button
              type="submit"
              disabled={isSubmitting}
              className="w-full bg-primary text-on-primary py-4 rounded-full font-label-md hover:opacity-90 active:scale-[0.98] transition-all disabled:opacity-60 disabled:cursor-not-allowed"
            >
              {isSubmitting ? 'Creating…' : 'Sign up'}
            </button>
          </form>
          <div className="mt-8 text-center">
            <p className="text-body-md font-body-md text-on-surface-variant">
              Already have an account?{' '}
              <Link to="/login" className="text-primary font-bold hover:underline">
                Log in
              </Link>
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
