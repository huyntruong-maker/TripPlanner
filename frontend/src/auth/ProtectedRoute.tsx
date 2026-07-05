import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from './AuthContext';
import { buildLoginUrl } from './returnTo';

/**
 * Wraps routes that require a signed-in user. Anonymous visitors are redirected
 * to the login page with a `returnTo` back-link (Feature 3 / US8 — require
 * login to save trips, then send the user back to where they were).
 */
export function ProtectedRoute() {
  const { isAuthenticated } = useAuth();
  const location = useLocation();

  if (!isAuthenticated) {
    const currentPath = `${location.pathname}${location.search}`;
    return <Navigate to={buildLoginUrl(currentPath)} replace />;
  }

  return <Outlet />;
}
