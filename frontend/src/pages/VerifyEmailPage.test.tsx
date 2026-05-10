import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, waitFor, act } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import type { AuthContextType } from '../types/auth'

// ── Mocks ──────────────────────────────────────────────────────────────────────

const mockMarkEmailVerified = vi.fn()
const mockNavigate = vi.fn()
const mockVerifyEmail = vi.fn()

vi.mock('../services/api', () => ({
  apiService: { verifyEmail: mockVerifyEmail },
}))

vi.mock('../contexts/AuthContext', () => ({
  useAuth: vi.fn(),
}))

vi.mock('react-router-dom', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router-dom')>()
  return { ...actual, useNavigate: () => mockNavigate }
})

// Import after mocks
import { useAuth } from '../contexts/AuthContext'
const { VerifyEmailPage } = await import('./VerifyEmailPage')

// ── Helpers ────────────────────────────────────────────────────────────────────

function mockAuth(overrides: Partial<AuthContextType> = {}) {
  vi.mocked(useAuth).mockReturnValue({
    user: null,
    token: null,
    markEmailVerified: mockMarkEmailVerified,
    isAuthenticated: false,
    isLoading: false,
    login: vi.fn(),
    register: vi.fn(),
    logout: vi.fn(),
    forgotPassword: vi.fn(),
    resetPassword: vi.fn(),
    ...overrides,
  } as AuthContextType)
}

function renderPage(token = 'valid-token', isAuthenticated = false) {
  mockAuth({ isAuthenticated })
  return render(
    <MemoryRouter initialEntries={[`/verify-email?token=${token}`]}>
      <VerifyEmailPage />
    </MemoryRouter>,
  )
}

// ── Tests ──────────────────────────────────────────────────────────────────────

describe('VerifyEmailPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    // shouldAdvanceTime: real time still ticks (so waitFor can poll),
    // while vi.advanceTimersByTime() gives manual control over the 2 s timer.
    vi.useFakeTimers({ shouldAdvanceTime: true })
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('redirects an authenticated user to /dashboard after successful verification', async () => {
    mockVerifyEmail.mockResolvedValueOnce({ success: true })
    renderPage('valid-token', /* isAuthenticated */ true)

    // Await the API call to resolve and the success state to render
    await waitFor(() => expect(screen.getByText('Email Verified!')).toBeInTheDocument())
    expect(mockMarkEmailVerified).toHaveBeenCalledOnce()

    // Advance past the 2 s delay
    act(() => { vi.advanceTimersByTime(2000) })

    expect(mockNavigate).toHaveBeenCalledWith('/dashboard', { replace: true })
  })

  it('redirects an unauthenticated user to /login after successful verification', async () => {
    mockVerifyEmail.mockResolvedValueOnce({ success: true })
    renderPage('valid-token', /* isAuthenticated */ false)

    await waitFor(() => expect(screen.getByText('Email Verified!')).toBeInTheDocument())

    act(() => { vi.advanceTimersByTime(2000) })

    expect(mockNavigate).toHaveBeenCalledWith('/login', { replace: true })
  })

  it('does not navigate before the 2 s delay elapses', async () => {
    mockVerifyEmail.mockResolvedValueOnce({ success: true })
    renderPage('valid-token', true)

    await waitFor(() => expect(screen.getByText('Email Verified!')).toBeInTheDocument())

    act(() => { vi.advanceTimersByTime(1000) })

    expect(mockNavigate).not.toHaveBeenCalled()
  })

  it('clears the redirect timer when the component unmounts before the delay', async () => {
    mockVerifyEmail.mockResolvedValueOnce({ success: true })
    const { unmount } = renderPage('valid-token', true)

    await waitFor(() => expect(screen.getByText('Email Verified!')).toBeInTheDocument())

    unmount()

    // Even after the delay, navigate should NOT have been called on an unmounted component
    act(() => { vi.advanceTimersByTime(2000) })

    expect(mockNavigate).not.toHaveBeenCalled()
  })

  it('shows Verification Failed when the API returns success: false', async () => {
    mockVerifyEmail.mockResolvedValueOnce({ success: false, message: 'Token expired' })
    renderPage()

    await waitFor(() => expect(screen.getByText('Verification Failed')).toBeInTheDocument())
    expect(mockNavigate).not.toHaveBeenCalled()
  })

  it('shows Verification Failed when the API call throws', async () => {
    mockVerifyEmail.mockRejectedValueOnce(new Error('Network error'))
    renderPage()

    await waitFor(() => expect(screen.getByText('Verification Failed')).toBeInTheDocument())
    expect(mockNavigate).not.toHaveBeenCalled()
  })

  it('shows Verification Failed immediately when no token is present in the URL', async () => {
    mockAuth({ isAuthenticated: false })
    render(
      <MemoryRouter initialEntries={['/verify-email']}>
        <VerifyEmailPage />
      </MemoryRouter>,
    )

    // The microtask-based state update resolves quickly
    await waitFor(() => expect(screen.getByText('Verification Failed')).toBeInTheDocument())
    expect(mockVerifyEmail).not.toHaveBeenCalled()
  })
})
