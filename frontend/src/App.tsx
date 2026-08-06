import { useEffect, useRef, useState } from 'react';
import { Navigate, Route, Routes } from 'react-router-dom';
import { ProtectedRoute } from './auth/ProtectedRoute';
import { AppHeader } from './components/AppHeader';
import { LoginPage } from './features/auth/pages/LoginPage';
import { RegisterPage } from './features/auth/pages/RegisterPage';
import { VerifyEmailPage } from './features/auth/pages/VerifyEmailPage';
import { DestinationDetailPage } from './features/destinations/pages/DestinationDetailPage';
import { SearchPage } from './features/destinations/pages/SearchPage';
import { TripPlannerPage } from './features/trips/pages/TripPlannerPage';
import { TripsPage } from './features/trips/pages/TripsPage';

// Docked like AppHeader, but at the bottom: fixed instead of sticky since it's the last element
// in flow, so `main` needs its rendered height added as padding to avoid hiding content under it.
export default function App() {
  const footerRef = useRef<HTMLElement>(null);
  const [footerHeight, setFooterHeight] = useState(0);

  useEffect(() => {
    const footer = footerRef.current;
    if (!footer) return;

    const observer = new ResizeObserver(([entry]) => setFooterHeight(entry.contentRect.height));
    observer.observe(footer);
    return () => observer.disconnect();
  }, []);

  return (
    <div className="min-h-screen flex flex-col bg-background text-on-background font-body-md">
      <AppHeader />

      <main
        className="flex-grow w-full max-w-container-max mx-auto px-margin-mobile md:px-margin-desktop pt-8"
        style={{ paddingBottom: `calc(2rem + ${footerHeight}px)` }}
      >
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

      <footer
        ref={footerRef}
        className="fixed bottom-0 inset-x-0 z-40 bg-surface-container-low border-t border-outline-variant w-full py-stack-lg px-margin-mobile md:px-margin-desktop"
      >
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
