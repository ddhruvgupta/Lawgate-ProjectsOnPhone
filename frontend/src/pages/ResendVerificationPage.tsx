import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { apiService } from '../services/api';

/**
 * Handles the /resend-verification route that the email-verification banner
 * links to.  Automatically fires the resend request on mount (the user has
 * already expressed intent by clicking the banner link) and shows clear
 * success/failure feedback so they know what happened.
 */
export const ResendVerificationPage: React.FC = () => {
  const { user } = useAuth();
  const [status, setStatus] = useState<'loading' | 'success' | 'error'>('loading');

  useEffect(() => {
    if (!user?.email) {
      setStatus('error');
      return;
    }

    apiService
      .resendVerification(user.email)
      .then(() => setStatus('success'))
      .catch(() => setStatus('error'));
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <div className="p-8 max-w-md mx-auto mt-16 text-center">
      {status === 'loading' && (
        <p className="text-gray-500 text-sm">Sending verification email…</p>
      )}

      {status === 'success' && (
        <div className="bg-green-50 border border-green-200 text-green-800 px-6 py-5 rounded-xl">
          <p className="font-semibold text-base mb-1">Verification email sent</p>
          <p className="text-sm">
            We sent a verification link to{' '}
            <span className="font-mono">{user?.email}</span>. Check your inbox
            (and spam folder).
          </p>
          <Link
            to="/dashboard"
            className="mt-4 inline-block text-sm font-medium text-green-700 underline"
          >
            Back to dashboard
          </Link>
        </div>
      )}

      {status === 'error' && (
        <div className="bg-red-50 border border-red-200 text-red-800 px-6 py-5 rounded-xl">
          <p className="font-semibold text-base mb-1">Could not send email</p>
          <p className="text-sm">
            Something went wrong. Please try again later or contact support.
          </p>
          <Link
            to="/dashboard"
            className="mt-4 inline-block text-sm font-medium text-red-700 underline"
          >
            Back to dashboard
          </Link>
        </div>
      )}
    </div>
  );
};
