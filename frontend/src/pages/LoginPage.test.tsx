import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import React from 'react'
import { MemoryRouter } from 'react-router-dom'
import type { AuthContextType } from '../types/auth'

// ── Mock dependencies ──────────────────────────────────────────────────────────

const mockLogin = vi.fn()
const mockNavigate = vi.fn()
const mockShowToast = vi.fn()

vi.mock('../contexts/AuthContext', () => ({
  useAuth: (): Partial<AuthContextType> => ({
    login: mockLogin,
    isAuthenticated: false,
    isLoading: false,
    user: null,
    token: null,
    logout: vi.fn(),
    register: vi.fn(),
    forgotPassword: vi.fn(),
    resetPassword: vi.fn(),
  }),
}))

vi.mock('../contexts/ToastContext', () => ({
  useToast: () => ({ showToast: mockShowToast }),
}))

vi.mock('react-router-dom', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router-dom')>()
  return { ...actual, useNavigate: () => mockNavigate }
})

// ── Import after mocks ─────────────────────────────────────────────────────────

const { LoginPage } = await import('./LoginPage')

function renderLoginPage() {
  return render(
    <MemoryRouter>
      <LoginPage />
    </MemoryRouter>
  )
}

// ── Tests ──────────────────────────────────────────────────────────────────────

describe('LoginPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('successful login', () => {
    it('navigates to /dashboard on success', async () => {
      mockLogin.mockResolvedValueOnce(undefined)
      renderLoginPage()

      await userEvent.type(screen.getByLabelText(/email address/i), 'user@test.com')
      await userEvent.type(screen.getByLabelText(/^password$/i), 'secret123')
      fireEvent.submit(screen.getByRole('button', { name: /sign in/i }))

      await waitFor(() => expect(mockNavigate).toHaveBeenCalledWith('/dashboard'))
    })
  })

  // BUG-003: error message and error toast should both appear on failed login
  describe('failed login (BUG-003)', () => {
    it('shows an inline error message when login fails', async () => {
      mockLogin.mockRejectedValueOnce(new Error('Invalid credentials'))
      renderLoginPage()

      await userEvent.type(screen.getByLabelText(/email address/i), 'bad@test.com')
      await userEvent.type(screen.getByLabelText(/^password$/i), 'wrongpass')
      fireEvent.submit(screen.getByRole('button', { name: /sign in/i }))

      await waitFor(() =>
        expect(screen.getByRole('alert')).toHaveTextContent('Invalid credentials')
      )
    })

    it('fires an error toast when login fails', async () => {
      mockLogin.mockRejectedValueOnce(new Error('Invalid credentials'))
      renderLoginPage()

      await userEvent.type(screen.getByLabelText(/email address/i), 'bad@test.com')
      await userEvent.type(screen.getByLabelText(/^password$/i), 'wrongpass')
      fireEvent.submit(screen.getByRole('button', { name: /sign in/i }))

      await waitFor(() =>
        expect(mockShowToast).toHaveBeenCalledWith('Invalid credentials', 'error')
      )
    })

    it('uses a fallback message when the error has no message', async () => {
      mockLogin.mockRejectedValueOnce('network error')
      renderLoginPage()

      await userEvent.type(screen.getByLabelText(/email address/i), 'bad@test.com')
      await userEvent.type(screen.getByLabelText(/^password$/i), 'wrongpass')
      fireEvent.submit(screen.getByRole('button', { name: /sign in/i }))

      await waitFor(() =>
        expect(screen.getByRole('alert')).toHaveTextContent(/check your credentials/i)
      )
    })
  })

  // BUG-018: password visibility toggle
  describe('password visibility toggle (BUG-018)', () => {
    it('password field is type=password by default', () => {
      renderLoginPage()
      expect(screen.getByLabelText(/^password$/i)).toHaveAttribute('type', 'password')
    })

    it('toggles password to visible when the eye button is clicked', async () => {
      renderLoginPage()
      const toggle = screen.getByRole('button', { name: /show password/i })
      await userEvent.click(toggle)
      expect(screen.getByLabelText(/^password$/i)).toHaveAttribute('type', 'text')
    })

    it('hides password again on second click', async () => {
      renderLoginPage()
      const showBtn = screen.getByRole('button', { name: /show password/i })
      await userEvent.click(showBtn)
      // After first click the aria-label changes to "Hide password"
      const hideBtn = screen.getByRole('button', { name: /hide password/i })
      await userEvent.click(hideBtn)
      expect(screen.getByLabelText(/^password$/i)).toHaveAttribute('type', 'password')
    })
  })
})
