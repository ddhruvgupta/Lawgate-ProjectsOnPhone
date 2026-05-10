import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, act, waitFor } from '@testing-library/react'
import type { User } from '../types/auth'

// ── Mocks ──────────────────────────────────────────────────────────────────────

// Prevent real API calls from the initial-mount effect
vi.mock('../services/api', () => ({
  apiService: {},
}))

// Import after mocks so the module picks up the mock
const { AuthProvider, useAuth } = await import('../contexts/AuthContext')

// ── Helpers ────────────────────────────────────────────────────────────────────

/** A minimal verified user fixture. */
const verifiedUser: User = {
  id: 1,
  email: 'alice@example.com',
  firstName: 'Alice',
  lastName: 'Smith',
  role: 'User',
  companyId: 1,
  isActive: true,
  isEmailVerified: true,
}

/** A minimal unverified user fixture. */
const unverifiedUser: User = {
  ...verifiedUser,
  isEmailVerified: false,
}

/** Renders a probe component that exposes AuthContext values via test-ids. */
function AuthProbe() {
  const { user, token, isAuthenticated } = useAuth()
  return (
    <div>
      <span data-testid="verified">{String(user?.isEmailVerified ?? 'null')}</span>
      <span data-testid="email">{user?.email ?? 'null'}</span>
      <span data-testid="token">{token ?? 'null'}</span>
      <span data-testid="authenticated">{String(isAuthenticated)}</span>
    </div>
  )
}

function renderAuthProvider(initialLocalStorage: Record<string, string> = {}) {
  // Seed localStorage before mounting
  Object.entries(initialLocalStorage).forEach(([k, v]) => localStorage.setItem(k, v))

  return render(
    <AuthProvider>
      <AuthProbe />
    </AuthProvider>,
  )
}

/** Dispatch a 'storage' event as if another tab wrote to localStorage. */
function dispatchStorageEvent(key: string, newValue: string | null) {
  const event = new StorageEvent('storage', { key, newValue })
  window.dispatchEvent(event)
}

// ── Tests ──────────────────────────────────────────────────────────────────────

describe('AuthContext — cross-tab storage sync', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.clear()
  })

  it('updates user.isEmailVerified when another tab writes a verified user to localStorage', async () => {
    // Start logged-in with an unverified user
    localStorage.setItem('token', 'tok123')
    localStorage.setItem('user', JSON.stringify(unverifiedUser))
    renderAuthProvider()

    // Wait for the initial mount effect to load the stored values
    await waitFor(() => expect(screen.getByTestId('verified').textContent).toBe('false'))

    // Another tab verifies email and writes updated user to localStorage
    const updated = { ...unverifiedUser, isEmailVerified: true }
    localStorage.setItem('user', JSON.stringify(updated))

    act(() => dispatchStorageEvent('user', JSON.stringify(updated)))

    await waitFor(() => expect(screen.getByTestId('verified').textContent).toBe('true'))
  })

  it('updates both token and user atomically when another tab logs in', async () => {
    renderAuthProvider() // start logged-out

    localStorage.setItem('token', 'new-token')
    localStorage.setItem('user', JSON.stringify(verifiedUser))

    // Simulate the token key being the last write (the event that triggers sync)
    act(() => dispatchStorageEvent('token', 'new-token'))

    await waitFor(() => {
      expect(screen.getByTestId('token').textContent).toBe('new-token')
      expect(screen.getByTestId('email').textContent).toBe(verifiedUser.email)
      expect(screen.getByTestId('authenticated').textContent).toBe('true')
    })
  })

  it('logs the user out in this tab when another tab clears localStorage (token → null)', async () => {
    localStorage.setItem('token', 'tok123')
    localStorage.setItem('user', JSON.stringify(verifiedUser))
    renderAuthProvider()

    await waitFor(() => expect(screen.getByTestId('authenticated').textContent).toBe('true'))

    // Another tab logs out — clears localStorage first, then fires the event
    localStorage.removeItem('token')
    localStorage.removeItem('user')

    act(() => dispatchStorageEvent('token', null))

    await waitFor(() => {
      expect(screen.getByTestId('token').textContent).toBe('null')
      expect(screen.getByTestId('email').textContent).toBe('null')
      expect(screen.getByTestId('authenticated').textContent).toBe('false')
    })
  })

  it('falls back to null user when localStorage contains corrupted JSON', async () => {
    localStorage.setItem('token', 'tok123')
    localStorage.setItem('user', JSON.stringify(verifiedUser))
    renderAuthProvider()

    await waitFor(() => expect(screen.getByTestId('email').textContent).toBe(verifiedUser.email))

    // Corrupt the stored user value
    localStorage.setItem('user', 'not-valid-json}}}')

    act(() => dispatchStorageEvent('user', 'not-valid-json}}}'))

    // User should be cleared; auth should reflect no valid user
    await waitFor(() => expect(screen.getByTestId('email').textContent).toBe('null'))
  })

  it('ignores storage events for unrelated keys', async () => {
    localStorage.setItem('token', 'tok123')
    localStorage.setItem('user', JSON.stringify(verifiedUser))
    renderAuthProvider()

    await waitFor(() => expect(screen.getByTestId('email').textContent).toBe(verifiedUser.email))

    act(() => dispatchStorageEvent('theme', 'dark'))

    // State should be unchanged
    expect(screen.getByTestId('email').textContent).toBe(verifiedUser.email)
    expect(screen.getByTestId('token').textContent).toBe('tok123')
  })
})
