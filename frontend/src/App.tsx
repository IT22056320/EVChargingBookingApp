import { Routes, Route, Navigate } from 'react-router-dom'
import { AuthProvider } from '@/providers/auth-provider'
import { NotificationProvider } from '@/providers/notification-provider'
import { LoginPage } from '@/pages/auth/login'
import { DashboardPage } from '@/pages/dashboard'
import { BookingsPage } from '@/pages/bookings'
import { CreateBookingPage } from '@/pages/create-booking'
import { EVOwnersPage } from '@/pages/ev-owners'
import { ProtectedRoute } from '@/components/auth/protected-route'
import { Layout } from '@/components/layout/layout'
import { ToastNotifications } from '@/components/notifications/toast-notifications'
import { Toaster } from 'react-hot-toast'

function App() {
  return (
    <AuthProvider>
      <NotificationProvider>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route
            path="/*"
            element={
              <ProtectedRoute>
                <Layout>
                  <Routes>
                    <Route path="/" element={<Navigate to="/dashboard" replace />} />
                    <Route path="/dashboard" element={<DashboardPage />} />
                    <Route path="/bookings" element={<BookingsPage />} />
                    <Route path="/create-booking" element={<CreateBookingPage />} />
                    <Route path="/ev-owners" element={<EVOwnersPage />} />
                  </Routes>
                </Layout>
              </ProtectedRoute>
            }
          />
        </Routes>
        <Toaster 
          position="top-center"
          toastOptions={{
            duration: 4000,
            style: {
              background: '#fff',
              color: '#363636',
              boxShadow: '0 4px 12px rgba(0, 0, 0, 0.15)',
              borderRadius: '8px',
              padding: '16px',
              maxWidth: '500px',
            },
            success: {
              style: {
                border: '1px solid #10B981',
                background: '#F0FDF4',
                color: '#065F46',
              },
              iconTheme: {
                primary: '#10B981',
                secondary: '#F0FDF4',
              },
            },
            error: {
              style: {
                border: '1px solid #EF4444',
                background: '#FEF2F2',
                color: '#991B1B',
              },
              iconTheme: {
                primary: '#EF4444',
                secondary: '#FEF2F2',
              },
              duration: 6000, // Keep error messages longer
            },
            loading: {
              style: {
                border: '1px solid #3B82F6',
                background: '#EFF6FF',
                color: '#1E40AF',
              },
            },
          }}
        />
        <ToastNotifications />
      </NotificationProvider>
    </AuthProvider>
  )
}

export default App