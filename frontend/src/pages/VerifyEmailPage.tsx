import React, { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { apiService } from '../services/api';

type Status = 'verifying' | 'success' | 'error';

export const VerifyEmailPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token') ?? '';
  const [status, setStatus] = useState<Status>('verifying');

  useEffect(() => {
    if (!token) {
      // Use a microtask to avoid setState synchronously inside the effect body
      Promise.resolve().then(() => setStatus('error'));
      return;
    }

    apiService
      .verifyEmail(token)
      .then((res) => setStatus(res.success ? 'success' : 'error'))
      .catch(() => setStatus('error'));
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
            <Link
              to="/login"
              className="inline-block mt-2 px-6 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors text-sm font-medium"
            >
              Sign in
            </Link>
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
