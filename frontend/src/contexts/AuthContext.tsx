import React, { createContext, useContext, useState, useEffect, type ReactNode } from 'react';
import { apiService } from '../services/api';
import type { AuthContextType, User, RegisterRequest } from '../types/auth';

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // Load user and token from localStorage on mount
  useEffect(() => {
    const storedToken = localStorage.getItem('token');
    const storedUser = localStorage.getItem('user');

    if (storedToken && storedUser) {
      setToken(storedToken);
      setUser(JSON.parse(storedUser));
    }
    setIsLoading(false);
  }, []);

  // Sync auth state across tabs — when another tab calls markEmailVerified(),
  // logout(), or login(), the 'storage' event fires here and we re-read localStorage.
  useEffect(() => {
    const handleStorage = (e: StorageEvent) => {
      if (e.key === 'user') {
        setUser(e.newValue ? (JSON.parse(e.newValue) as User) : null);
      }
      if (e.key === 'token') {
        setToken(e.newValue);
      }
    };
    window.addEventListener('storage', handleStorage);
    return () => window.removeEventListener('storage', handleStorage);
  }, []);

  const login = async (email: string, password: string) => {
    try {
      const response = await apiService.login({ email, password });

      if (response.success && response.data) {
        const { token: newToken, refreshToken, user: newUser } = response.data;
        setToken(newToken);
        setUser(newUser);
        localStorage.setItem('token', newToken);
        localStorage.setItem('refreshToken', refreshToken);
        localStorage.setItem('user', JSON.stringify(newUser));
      } else {
        throw new Error(response.message || 'Login failed');
      }
    } catch (error: unknown) {
      const msg = error instanceof Error ? error.message : 'Login failed';
      const axiosMsg = (error as { response?: { data?: { message?: string } } })?.response?.data?.message;
      console.error('Login error:', error);
      throw new Error(axiosMsg || msg);
    }
  };

  const register = async (data: RegisterRequest) => {
    try {
      const response = await apiService.register(data);

      if (response.success && response.data) {
        const { token: newToken, refreshToken, user: newUser } = response.data;
        setToken(newToken);
        setUser(newUser);
        localStorage.setItem('token', newToken);
        localStorage.setItem('refreshToken', refreshToken);
        localStorage.setItem('user', JSON.stringify(newUser));
      } else {
        throw new Error(response.message || 'Registration failed');
      }
    } catch (error: unknown) {
      const msg = error instanceof Error ? error.message : 'Registration failed';
      const axiosMsg = (error as { response?: { data?: { message?: string } } })?.response?.data?.message;
      console.error('Registration error:', error);
      throw new Error(axiosMsg || msg);
    }
  };

  const logout = () => {
    setToken(null);
    setUser(null);
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
  };

  const forgotPassword = async (email: string) => {
    await apiService.forgotPassword(email);
  };

  const resetPassword = async (resetToken: string, newPassword: string, confirmPassword: string) => {
    const response = await apiService.resetPassword(resetToken, newPassword, confirmPassword);
    if (!response.success) {
      throw new Error(response.message || 'Password reset failed');
    }
  };

  const markEmailVerified = () => {
    if (!user) return;
    const updatedUser = { ...user, isEmailVerified: true };
    setUser(updatedUser);
    localStorage.setItem('user', JSON.stringify(updatedUser));
  };

  const value: AuthContextType = {
    user,
    token,
    login,
    register,
    logout,
    forgotPassword,
    resetPassword,
    markEmailVerified,
    isAuthenticated: !!token && !!user,
    isLoading,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

// eslint-disable-next-line react-refresh/only-export-components
export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
