import type { ReactNode } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link, useSearchParams } from 'react-router-dom';
import { verifyEmail } from '../../api/auth';
import { getApiErrorCode, getApiErrorMessage } from '../../api/errors';

const ALREADY_VERIFIED_ERROR_CODE = 'Auth.VerifyEmail.AlreadyVerified';

/**
 * Feature 4 / US2 — email verification landing page.
 * Reads `?token=` (docs/API.md GET /auth/verify-email) and shows a
 * success / invalid / expired / already-verified / missing-token state.
 */
export function VerifyEmailPage() {
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token');

  const { status, error } = useQuery({
    queryKey: ['verify-email', token],
    // TanStack Query rejects `undefined` query results; verifyEmail() resolves
    // to void on success, so map it to an explicit sentinel value.
    queryFn: async () => {
      await verifyEmail(token as string);
      return true as const;
    },
    enabled: Boolean(token),
    retry: false,
  });

  if (!token) {
    return (
      <VerifyEmailLayout title="Verify your email">
        <p className="error" role="alert">
          This link is missing a verification token. Check the link from your email.
        </p>
      </VerifyEmailLayout>
    );
  }

  if (status === 'pending') {
    return (
      <VerifyEmailLayout title="Verify your email">
        <p>Verifying your account…</p>
      </VerifyEmailLayout>
    );
  }

  if (status === 'error') {
    const isAlreadyVerified = getApiErrorCode(error) === ALREADY_VERIFIED_ERROR_CODE;
    return (
      <VerifyEmailLayout title={isAlreadyVerified ? 'Already verified' : 'Verification failed'}>
        <p className={isAlreadyVerified ? undefined : 'error'} role={isAlreadyVerified ? undefined : 'alert'}>
          {isAlreadyVerified
            ? 'Your email is already verified — you can log in.'
            : getApiErrorMessage(error, 'This verification link is invalid or has expired.')}
        </p>
      </VerifyEmailLayout>
    );
  }

  return (
    <VerifyEmailLayout title="Email verified">
      <p>Your account is verified. You can log in now.</p>
    </VerifyEmailLayout>
  );
}

function VerifyEmailLayout({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div className="card">
      <h1>{title}</h1>
      {children}
      <p>
        <Link to="/login">Back to log in</Link>
      </p>
    </div>
  );
}
