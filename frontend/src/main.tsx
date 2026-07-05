import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { QueryClientProvider } from '@tanstack/react-query';
import App from './App';
import { AuthProvider } from './auth/AuthContext';
import { ToastProvider } from './components/toast/ToastProvider';
import { createAppQueryClient } from './queryClient';
import './styles.css';

// A single shared QueryClient for all server-state data fetching (TanStack
// Query). Wires every query/mutation failure to the global error toast.
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
