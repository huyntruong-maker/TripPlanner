import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../../auth/AuthContext';
import { readReturnTo } from '../../auth/returnTo';
import { getApiErrorMessage } from '../../api/errors';
import { useToast } from '../../components/toast/ToastProvider';
import { loginSchema, type LoginFormValues } from './schemas';

/** Feature 4 / US3 — log in with email and password. */
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
    <div className="card">
      <h1>Log in</h1>
      <form onSubmit={handleSubmit(onSubmit)} noValidate>
        <label>
          Email
          <input
            type="email"
            autoComplete="email"
            {...register('email')}
            aria-invalid={Boolean(errors.email)}
            aria-describedby={errors.email ? 'login-email-error' : undefined}
          />
          {errors.email && (
            <p className="error" id="login-email-error">
              {errors.email.message}
            </p>
          )}
        </label>
        <label>
          Password
          <input
            type="password"
            autoComplete="current-password"
            {...register('password')}
            aria-invalid={Boolean(errors.password)}
            aria-describedby={errors.password ? 'login-password-error' : undefined}
          />
          {errors.password && (
            <p className="error" id="login-password-error">
              {errors.password.message}
            </p>
          )}
        </label>
        <label className="checkbox-label">
          <input type="checkbox" {...register('rememberMe')} />
          Remember me
        </label>
        {formError && (
          <p className="error" role="alert">
            {formError}
          </p>
        )}
        <button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Signing in…' : 'Log in'}
        </button>
      </form>
      <p>
        No account? <Link to="/register">Sign up</Link>
      </p>
    </div>
  );
}
