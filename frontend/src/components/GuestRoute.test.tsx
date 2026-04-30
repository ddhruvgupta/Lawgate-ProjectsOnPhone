import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import React from 'react'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import type { AuthContextType } from '../types/auth'

// ── Mocks ──────────────────────────────────────────────────────────────────────

const mockUseAuth = vi.fn<() => Partial<AuthContextType>>()

vi.mock('../contexts/AuthContext', () => ({
  useAuth: () => mockUseAuth(),
}))

const { GuestRoute } = await import('./GuestRoute')

// ── Helpers ────────────────────────────────────────────────────────────────────

function renderGuestRoute(isAuthenticated: boolean, isLoading = false) {
  mockUseAuth.mockReturnValue({ isAuthenticated, isLoading })

  return render(
    <MemoryRouter initialEntries={['/login']}>
      <Routes>
        <Route
          path="/login"
          element={
            <GuestRoute>
              <div data-testid="login-page">Login Page</div>
            </GuestRoute>
          }
        />
        <Route path="/dashboard" element={<div data-testid="dashboard">Dashboard</div>} />
      </Routes>
    </MemoryRouter>
  )
}

// ── Tests ──────────────────────────────────────────────────────────────────────

// BUG-012: GuestRoute should prevent authenticated users from revisiting login

describe('GuestRoute (BUG-012)', () => {
  it('renders children for unauthenticated users', () => {
    renderGuestRoute(false)
    expect(screen.getByTestId('login-page')).toBeInTheDocument()
  })

  it('redirects authenticated users to /dashboard', () => {
    renderGuestRoute(true)
    expect(screen.queryByTestId('login-page')).not.toBeInTheDocument()
    expect(screen.getByTestId('dashboard')).toBeInTheDocument()
  })

  it('renders nothing while auth is loading', () => {
    renderGuestRoute(false, true)
    expect(screen.queryByTestId('login-page')).not.toBeInTheDocument()
  })
})
