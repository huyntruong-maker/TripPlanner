import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../../auth/AuthContext';
import { readReturnTo } from '../../auth/returnTo';
import { getApiErrorMessage } from '../../api/errors';
import { useToast } from '../../components/toast/ToastProvider';
import { loginSchema, type LoginFormValues } from './schemas';

const INPUT_CLASSES =
  'w-full border border-outline-variant rounded-lg px-4 py-3 text-body-md focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all outline-none';

export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const { showToast } = useToast();
  const [formError, setFormError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '', rememberMe: false },
  });

  async function onSubmit(values: LoginFormValues) {
    setFormError(null);
    try {
      await login(values.email, values.password, values.rememberMe);
      navigate(readReturnTo(location.search), { replace: true });
    } catch (err) {
      const message = getApiErrorMessage(err, 'Login failed.');
      setFormError(message);
      showToast(message);
    }
  }

  return (
    <div className="flex items-center justify-center">
      <div className="w-full max-w-[480px]">
        <div className="bg-surface-container-lowest rounded-xl p-8 md:p-10 elevation-l1 border border-outline-variant/30">
          <div className="mb-8 text-center md:text-left">
            <h1 className="text-headline-lg font-headline-lg text-on-surface mb-2">Log in</h1>
            <p className="text-body-md font-body-md text-on-surface-variant">
              Continue your journey with TripPlanner.
            </p>
          </div>
          <form className="space-y-6" onSubmit={handleSubmit(onSubmit)} noValidate>
            <div className="space-y-2">
              <label
                className="block text-label-md font-label-md text-on-surface-variant"
                htmlFor="login-email"
              >
                Email
              </label>
              <input
                id="login-email"
                type="email"
                autoComplete="email"
                className={INPUT_CLASSES}
                {...register('email')}
                aria-invalid={Boolean(errors.email)}
                aria-describedby={errors.email ? 'login-email-error' : undefined}
              />
              {errors.email && (
                <p className="text-error text-label-sm font-semibold" id="login-email-error">
                  {errors.email.message}
                </p>
              )}
            </div>
            <div className="space-y-2">
              <label
                className="block text-label-md font-label-md text-on-surface-variant"
                htmlFor="login-password"
              >
                Password
              </label>
              <input
                id="login-password"
                type="password"
                autoComplete="current-password"
                className={INPUT_CLASSES}
                {...register('password')}
                aria-invalid={Boolean(errors.password)}
                aria-describedby={errors.password ? 'login-password-error' : undefined}
              />
              {errors.password && (
                <p className="text-error text-label-sm font-semibold" id="login-password-error">
                  {errors.password.message}
                </p>
              )}
            </div>
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                className="h-4 w-4 rounded border-outline-variant accent-primary focus:ring-2 focus:ring-primary/20"
                {...register('rememberMe')}
              />
              <span className="text-label-md font-label-md text-on-surface-variant select-none">
                Remember me
              </span>
            </label>
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
              {isSubmitting ? 'Signing in…' : 'Log in'}
            </button>
          </form>
          <div className="mt-8 text-center">
            <p className="text-body-md font-body-md text-on-surface-variant">
              No account?{' '}
              <Link to="/register" className="text-primary font-bold hover:underline">
                Sign up
              </Link>
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
