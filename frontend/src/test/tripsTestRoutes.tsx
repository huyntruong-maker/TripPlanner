import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { AuthProvider } from '../auth/AuthContext';
import { ProtectedRoute } from '../auth/ProtectedRoute';
import { ToastProvider } from '../components/toast/ToastProvider';
import { LoginPage } from '../features/auth/LoginPage';
import { TripPlannerPage } from '../features/trips/TripPlannerPage';
import { TripsPage } from '../features/trips/TripsPage';
import { buildFakeJwt } from './buildFakeJwt';

export function signInForTest(email = 'jane@example.com') {
  localStorage.setItem('tripplanner.token', buildFakeJwt({ nameid: 'user-1', unique_name: email }));
  localStorage.setItem('tripplanner.refreshToken', 'refresh-1');
}

export function renderTripsRoutes(initialEntries: string[]) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter initialEntries={initialEntries}>
          <AuthProvider>
            <Routes>
              <Route path="/login" element={<LoginPage />} />
              <Route element={<ProtectedRoute />}>
                <Route path="/trips" element={<TripsPage />} />
                <Route path="/trips/:tripId" element={<TripPlannerPage />} />
              </Route>
            </Routes>
          </AuthProvider>
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
}
