import React, { useEffect, useState } from 'react';
import { Link, useSearchParams, useNavigate } from 'react-router-dom';
import { apiService } from '../services/api';
import { useAuth } from '../contexts/AuthContext';

type Status = 'verifying' | 'success' | 'error';

export const VerifyEmailPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token') ?? '';
  const [status, setStatus] = useState<Status>('verifying');
  const { markEmailVerified, isAuthenticated } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    if (!token) {
      // Use a microtask to avoid setState synchronously inside the effect body
      Promise.resolve().then(() => setStatus('error'));
      return;
    }

    apiService
      .verifyEmail(token)
      .then((res) => {
        if (res.success) {
          markEmailVerified();
          setStatus('success');
          // Redirect after a brief pause so the user sees the confirmation.
          // Authenticated users go to the dashboard (which will now show verified);
          // unauthenticated users go to login.
          setTimeout(() => {
            navigate(isAuthenticated ? '/dashboard' : '/login', { replace: true });
          }, 2000);
        } else {
          setStatus('error');
        }
      })
      .catch(() => setStatus('error'));
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token]);

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-primary-50 to-primary-100 px-4">
      <div className="max-w-md w-full bg-white p-10 rounded-xl shadow-2xl text-center space-y-4">
        {status === 'verifying' && (
          <>
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600 mx-auto" />
            <p className="text-gray-600">Verifying your email…</p>
          </>
        )}

        {status === 'success' && (
          <>
            <div className="text-green-500 text-5xl">✓</div>
            <h2 className="text-2xl font-bold text-gray-900">Email Verified!</h2>
            <p className="text-gray-600">Your email has been verified successfully.</p>
            <p className="text-sm text-gray-400">Redirecting you now…</p>
          </>
        )}

        {status === 'error' && (
          <>
            <div className="text-red-400 text-5xl">✗</div>
            <h2 className="text-2xl font-bold text-gray-900">Verification Failed</h2>
            <p className="text-gray-600">
              The verification link is invalid or has expired.
            </p>
            <div className="flex flex-col gap-2 items-center mt-2">
              <Link
                to="/login"
                className="text-sm font-medium text-primary-600 hover:text-primary-500"
              >
                Back to sign in
              </Link>
            </div>
          </>
        )}
      </div>
    </div>
  );
};
