import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { QueryClientProvider } from '@tanstack/react-query';
import App from './App';
import { AuthProvider } from './auth/AuthContext';
import { ToastProvider } from './components/toast/ToastProvider';
import { createAppQueryClient } from './queryClient';
import './styles.css';
// Global (not per-component) so component tests don't need to process Leaflet's CSS under jsdom.
import 'leaflet/dist/leaflet.css';

// Shared QueryClient for all server-state fetching; wires failures to the global error toast.
const queryClient = createAppQueryClient();

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <BrowserRouter>
          <AuthProvider>
            <App />
          </AuthProvider>
        </BrowserRouter>
      </ToastProvider>
    </QueryClientProvider>
  </StrictMode>,
);
