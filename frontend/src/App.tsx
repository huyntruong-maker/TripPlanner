import { Navigate, Route, Routes } from 'react-router-dom';
import { ProtectedRoute } from './auth/ProtectedRoute';
import { AppHeader } from './components/AppHeader';
import { LoginPage } from './features/auth/LoginPage';
import { RegisterPage } from './features/auth/RegisterPage';
import { VerifyEmailPage } from './features/auth/VerifyEmailPage';
import { DestinationDetailPage } from './features/destinations/DestinationDetailPage';
import { SearchPage } from './features/destinations/SearchPage';
import { TripPlannerPage } from './features/trips/TripPlannerPage';
import { TripsPage } from './features/trips/TripsPage';

export default function App() {
  return (
    <div className="min-h-screen flex flex-col bg-background text-on-background font-body-md">
      <AppHeader />

      <main className="flex-grow w-full max-w-container-max mx-auto px-margin-mobile md:px-margin-desktop py-8">
        <Routes>
          <Route path="/" element={<SearchPage />} />
          <Route path="/destinations/:providerPlaceId" element={<DestinationDetailPage />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/verify-email" element={<VerifyEmailPage />} />

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
