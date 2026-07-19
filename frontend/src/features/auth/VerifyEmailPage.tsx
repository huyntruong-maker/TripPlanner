import { useEffect, useState, type ReactNode } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { verifyEmail } from '../../api/auth';
import { getApiErrorCode, getApiErrorMessage } from '../../api/errors';

const ALREADY_VERIFIED_ERROR_CODE = 'Auth.VerifyEmail.AlreadyVerified';
/** How long the success state waits before auto-redirecting to /login. */
const REDIRECT_DELAY_SECONDS = 3;

/** Reads `?token=` and shows success / invalid / expired / already-verified / missing-token state; auto-redirects to /login on success. */
export function VerifyEmailPage() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const token = searchParams.get('token');

  const { status, error } = useQuery({
    queryKey: ['verify-email', token],
    // TanStack Query rejects `undefined` results; map void success to a sentinel.
    queryFn: async () => {
      await verifyEmail(token as string);
      return true as const;
    },
    enabled: Boolean(token),
    retry: false,
  });

  const isVerified = Boolean(token) && status === 'success';
  const [secondsRemaining, setSecondsRemaining] = useState(REDIRECT_DELAY_SECONDS);

  // Standard SaaS pattern: land the user on /login (not straight into the app) after email
  // verification, with a short visible countdown so the redirect isn't jarring.
  useEffect(() => {
    if (!isVerified) return;

    if (secondsRemaining <= 0) {
      navigate('/login', { replace: true, state: { justVerified: true } });
      return;
    }

    const timeoutId = setTimeout(() => setSecondsRemaining((current) => current - 1), 1000);
    return () => clearTimeout(timeoutId);
  }, [isVerified, secondsRemaining, navigate]);

  function goToLoginNow() {
    navigate('/login', { replace: true, state: { justVerified: true } });
  }

  if (!token) {
    return (
      <VerifyEmailLayout title="Verify your email" icon="mail" tone="neutral">
        <p className="text-error text-body-md" role="alert">
          This link is missing a verification token. Check the link from your email.
        </p>
      </VerifyEmailLayout>
    );
  }

  if (status === 'pending') {
    return (
      <VerifyEmailLayout title="Verify your email" icon="hourglass_top" tone="neutral">
        <p className="text-on-surface-variant text-body-md">Verifying your account…</p>
      </VerifyEmailLayout>
    );
  }

  if (status === 'error') {
    const isAlreadyVerified = getApiErrorCode(error) === ALREADY_VERIFIED_ERROR_CODE;
    return (
      <VerifyEmailLayout
        title={isAlreadyVerified ? 'Already verified' : 'Verification failed'}
        icon={isAlreadyVerified ? 'check_circle' : 'error'}
        tone={isAlreadyVerified ? 'success' : 'error'}
      >
        {isAlreadyVerified ? (
          <p className="text-on-surface-variant text-body-md">
            Your email is already verified — you can log in.
          </p>
        ) : (
          <p className="text-error text-body-md" role="alert">
            {getApiErrorMessage(error, 'This verification link is invalid or has expired.')}
          </p>
        )}
      </VerifyEmailLayout>
    );
  }

  return (
    <VerifyEmailLayout title="Email verified" icon="check_circle" tone="success" hideFooterLink>
      <p className="text-on-surface-variant text-body-md mb-6">
        Your account is verified. Redirecting to log in… ({secondsRemaining}s)
      </p>
      <button
        type="button"
        onClick={goToLoginNow}
        className="w-full bg-primary text-on-primary py-3 rounded-full font-label-md hover:opacity-90 active:scale-[0.98] transition-all"
      >
        Go to log in
      </button>
    </VerifyEmailLayout>
  );
}

interface VerifyEmailLayoutProps {
  title: string;
  icon: string;
  tone: 'neutral' | 'success' | 'error';
  children: ReactNode;
  /** Success already shows its own "Go to log in" action; skip the redundant footer link there. */
  hideFooterLink?: boolean;
}

const TONE_CLASSES: Record<VerifyEmailLayoutProps['tone'], string> = {
  neutral: 'bg-surface-container text-on-surface-variant',
  success: 'bg-primary-container text-primary',
  error: 'bg-error-container text-error',
};

function VerifyEmailLayout({ title, icon, tone, children, hideFooterLink }: VerifyEmailLayoutProps) {
  return (
    <div className="flex items-center justify-center">
      <div className="w-full max-w-[480px]">
        <div className="bg-surface-container-lowest rounded-xl p-8 md:p-10 elevation-l1 border border-outline-variant/30 text-center">
          <div
            className={`mx-auto mb-6 flex h-16 w-16 items-center justify-center rounded-full ${TONE_CLASSES[tone]}`}
            aria-hidden="true"
          >
            <span className="material-symbols-outlined text-[32px]">{icon}</span>
          </div>
          <h1 className="text-headline-md font-headline-md text-on-surface mb-4">{title}</h1>
          {children}
          {!hideFooterLink && (
            <p className="text-body-md font-body-md text-on-surface-variant mt-6">
              <Link to="/login" className="text-primary font-bold hover:underline">
                Back to log in
              </Link>
            </p>
          )}
        </div>
      </div>
    </div>
  );
}
