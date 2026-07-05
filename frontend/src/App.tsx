import { Navigate, Route, Routes, Link, NavLink } from 'react-router-dom';
import { useAuth } from './auth/AuthContext';
import { ProtectedRoute } from './auth/ProtectedRoute';
import { LoginPage } from './features/auth/LoginPage';
import { RegisterPage } from './features/auth/RegisterPage';
import { VerifyEmailPage } from './features/auth/VerifyEmailPage';
import { DestinationDetailPage } from './features/destinations/DestinationDetailPage';
import { SearchPage } from './features/destinations/SearchPage';
import { TripPlannerPage } from './features/trips/TripPlannerPage';
import { TripsPage } from './features/trips/TripsPage';

export default function App() {
  const { isAuthenticated } = useAuth();

  const navLinkClassName = ({ isActive }: { isActive: boolean }) =>
    isActive
      ? 'text-on-primary bg-primary px-4 py-1.5 font-label-md rounded-full transition-colors'
      : 'text-on-surface-variant px-4 py-1.5 font-label-md hover:text-primary transition-colors rounded-full';

  return (
    <div className="min-h-screen flex flex-col bg-background text-on-background font-body-md">
      <header className="sticky top-0 z-50 w-full bg-surface/80 backdrop-blur-md shadow-sm">
        <nav className="flex justify-between items-center w-full px-margin-mobile md:px-margin-desktop py-4 max-w-container-max mx-auto">
          <Link to="/" className="flex items-center gap-stack-sm">
            <span className="material-symbols-outlined text-primary text-[28px]" aria-hidden="true">
              flight_takeoff
            </span>
            <span className="text-headline-md font-headline-md font-extrabold text-primary">
              TripPlanner
            </span>
          </Link>
          <div className="hidden md:flex items-center gap-stack-md">
            <NavLink to="/" end className={navLinkClassName}>
              Discover
            </NavLink>
            {isAuthenticated ? (
              <NavLink to="/trips" className={navLinkClassName}>
                My trips
              </NavLink>
            ) : (
              <NavLink to="/login" className={navLinkClassName}>
                Log in
              </NavLink>
            )}
          </div>
        </nav>
      </header>

      <main className="flex-grow w-full max-w-container-max mx-auto px-margin-mobile md:px-margin-desktop py-8">
        <Routes>
          <Route path="/" element={<SearchPage />} />
          <Route path="/destinations/:providerPlaceId" element={<DestinationDetailPage />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/verify-email" element={<VerifyEmailPage />} />

          {/* Authenticated area (Feature 3). */}
          <Route element={<ProtectedRoute />}>
            <Route path="/trips" element={<TripsPage />} />
            <Route path="/trips/:tripId" element={<TripPlannerPage />} />
          </Route>

          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </main>

      <footer className="bg-surface-container-low border-t border-outline-variant w-full py-stack-lg px-margin-mobile md:px-margin-desktop mt-auto">
        <div className="max-w-container-max mx-auto flex flex-col md:flex-row justify-between items-center gap-stack-md">
          <div className="flex flex-col items-center md:items-start gap-stack-sm">
            <span className="text-label-md font-headline-md font-bold text-primary">TripPlanner</span>
            <span className="text-on-surface-variant text-label-sm">
              © 2026 TripPlanner. All rights reserved.
            </span>
          </div>
          <div className="flex items-center gap-stack-md">
            <span className="text-on-surface-variant text-label-sm">Privacy Policy</span>
            <span className="text-on-surface-variant text-label-sm">Terms of Service</span>
            <span className="text-on-surface-variant text-label-sm">Contact Us</span>
          </div>
        </div>
      </footer>
    </div>
  );
}
