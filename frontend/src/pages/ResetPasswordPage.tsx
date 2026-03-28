import React, { useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

export const ResetPasswordPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token') ?? '';

  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [success, setSuccess] = useState(false);

  const { resetPassword } = useAuth();
  const navigate = useNavigate();

  if (!token) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-primary-50 to-primary-100 px-4">
        <div className="max-w-md w-full bg-white p-10 rounded-xl shadow-2xl text-center">
          <p className="text-red-600 font-medium">Invalid or missing reset token.</p>
          <Link to="/forgot-password" className="mt-4 inline-block text-primary-600 hover:underline text-sm">
            Request a new reset link
          </Link>
        </div>
      </div>
    );
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (newPassword !== confirmPassword) {
      setError('Passwords do not match.');
      return;
    }
    if (newPassword.length < 8) {
      setError('Password must be at least 8 characters.');
      return;
    }

    setIsLoading(true);
    try {
      await resetPassword(token, newPassword, confirmPassword);
      setSuccess(true);
      setTimeout(() => navigate('/login'), 3000);
    } catch (err: any) {
      setError(err.message || 'Password reset failed. The link may have expired.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-primary-50 to-primary-100 px-4">
      <div className="max-w-md w-full space-y-8 bg-white p-10 rounded-xl shadow-2xl">
        <div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
            Reset your password
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600">
            Enter your new password below.
          </p>
        </div>

        {success ? (
          <div className="bg-green-50 border border-green-200 text-green-800 px-4 py-4 rounded-lg text-sm">
            <p className="font-medium">Password reset successful!</p>
            <p className="mt-1">Redirecting you to sign in…</p>
          </div>
        ) : (
          <form className="mt-8 space-y-6" onSubmit={handleSubmit}>
            {error && (
              <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded relative">
                {error}
              </div>
            )}

            <div className="space-y-4">
              <div>
                <label htmlFor="new-password" className="block text-sm font-medium text-gray-700 mb-1">
                  New password
                </label>
                <input
                  id="new-password"
                  name="new-password"
                  type="password"
                  autoComplete="new-password"
                  required
                  minLength={8}
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                  className="appearance-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent"
                  placeholder="At least 8 characters"
                />
              </div>
              <div>
                <label htmlFor="confirm-password" className="block text-sm font-medium text-gray-700 mb-1">
                  Confirm new password
                </label>
                <input
                  id="confirm-password"
                  name="confirm-password"
                  type="password"
                  autoComplete="new-password"
                  required
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                  className="appearance-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent"
                  placeholder="Repeat your new password"
                />
              </div>
            </div>

            <button
              type="submit"
              disabled={isLoading}
              className="group relative w-full flex justify-center py-3 px-4 border border-transparent text-sm font-medium rounded-lg text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              {isLoading ? 'Resetting…' : 'Reset password'}
            </button>

            <div className="text-center text-sm">
              <Link to="/login" className="font-medium text-primary-600 hover:text-primary-500">
                Back to sign in
              </Link>
            </div>
          </form>
        )}
      </div>
    </div>
  );
};
