import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from './AuthContext';
import { buildLoginUrl } from './returnTo';

/** Redirects anonymous visitors to login with a `returnTo` back-link. */
export function ProtectedRoute() {
  const { isAuthenticated } = useAuth();
  const location = useLocation();

  if (!isAuthenticated) {
    const currentPath = `${location.pathname}${location.search}`;
    return <Navigate to={buildLoginUrl(currentPath)} replace />;
  }

  return <Outlet />;
}
