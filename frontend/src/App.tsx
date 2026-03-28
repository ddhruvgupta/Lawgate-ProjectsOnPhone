import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthProvider } from './contexts/AuthContext';
import { ToastProvider } from './contexts/ToastContext';
import { ToastContainer } from './components/ToastContainer';
import { ProtectedRoute } from './components/ProtectedRoute';
import { Layout } from './components/Layout';
import { ErrorBoundary } from './components/ErrorBoundary';
import { LoginPage } from './pages/LoginPage';
import { RegisterPage } from './pages/RegisterPage';
import { ForgotPasswordPage } from './pages/ForgotPasswordPage';
import { ResetPasswordPage } from './pages/ResetPasswordPage';
import { VerifyEmailPage } from './pages/VerifyEmailPage';
import { DashboardPage } from './pages/DashboardPage';
import { ProjectsPage } from './pages/ProjectsPage';
import { ProjectDetailPage } from './pages/ProjectDetailPage';
import { TeamPage } from './pages/TeamPage';
import { ActivityPage } from './pages/ActivityPage';
import { RoleGuard } from './components/RoleGuard';
import { PlatformAdminPage } from './pages/admin/PlatformAdminPage';
import { PlatformCompanyDetailPage } from './pages/admin/PlatformCompanyDetailPage';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 1000 * 30,
      retry: 1,
    },
  },
});

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AuthProvider>
          <ToastProvider>
            <Routes>
              {/* Public */}
              <Route path="/login" element={<LoginPage />} />
              <Route path="/register" element={<RegisterPage />} />
              <Route path="/forgot-password" element={<ForgotPasswordPage />} />
              <Route path="/reset-password" element={<ResetPasswordPage />} />
              <Route path="/verify-email" element={<VerifyEmailPage />} />

              {/* Protected — all rendered inside the sidebar Layout */}
              <Route
                path="/*"
                element={
                  <ProtectedRoute>
                    <Layout>
                      <ErrorBoundary>
                        <Routes>
                          <Route path="/dashboard" element={<DashboardPage />} />
                          <Route path="/projects" element={<ProjectsPage />} />
                          <Route path="/projects/:id" element={<ProjectDetailPage />} />
                          <Route path="/team" element={
                            <RoleGuard allowedRoles={['CompanyOwner', 'Admin']}>
                              <TeamPage />
                            </RoleGuard>
                          } />
                          <Route path="/activity" element={
                            <RoleGuard allowedRoles={['CompanyOwner', 'Admin']}>
                              <ActivityPage />
                            </RoleGuard>
                          } />
                          {/* Platform admin routes */}
                          <Route path="/admin" element={
                            <RoleGuard allowedRoles={['PlatformAdmin', 'PlatformSuperAdmin']}>
                              <PlatformAdminPage />
                            </RoleGuard>
                          } />
                          <Route path="/admin/companies/:id" element={
                            <RoleGuard allowedRoles={['PlatformAdmin', 'PlatformSuperAdmin']}>
                              <PlatformCompanyDetailPage />
                            </RoleGuard>
                          } />
                          <Route path="/" element={<Navigate to="/dashboard" replace />} />
                          <Route path="*" element={<Navigate to="/dashboard" replace />} />
                        </Routes>
                      </ErrorBoundary>
                    </Layout>
                  </ProtectedRoute>
                }
              />
            </Routes>
            <ToastContainer />
          </ToastProvider>
        </AuthProvider>
      </BrowserRouter>
    </QueryClientProvider>
  );
}

export default App;
