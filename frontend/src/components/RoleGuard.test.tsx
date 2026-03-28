import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import React from 'react'
import type { User, AuthContextType } from '../types/auth'
import { MemoryRouter } from 'react-router-dom'

const mockUseAuth = vi.fn<() => AuthContextType>()

vi.mock('../contexts/AuthContext', () => ({
  useAuth: () => mockUseAuth(),
}))

const { RoleGuard } = await import('./RoleGuard')

function makeAuthContext(user: User | null): AuthContextType {
  return {
    user,
    token: user ? 'fake-token' : null,
    login: vi.fn(),
    register: vi.fn(),
    logout: vi.fn(),
    forgotPassword: vi.fn(),
    resetPassword: vi.fn(),
    isAuthenticated: user !== null,
    isLoading: false,
  }
}

function makeUser(role: User['role']): User {
  return {
    id: 1,
    email: 'user@test.com',
    firstName: 'Test',
    lastName: 'User',
    role,
    companyId: 1,
    isActive: true,
  }
}

function renderWithRouter(ui: React.ReactElement) {
  return render(<MemoryRouter>{ui}</MemoryRouter>)
}

describe('RoleGuard', () => {
  describe('when user has a required role', () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue(makeAuthContext(makeUser('CompanyOwner')))
    })

    it('renders children', () => {
      renderWithRouter(
        <RoleGuard allowedRoles={['CompanyOwner', 'Admin']}>
          <div data-testid="protected-content">Secret Content</div>
        </RoleGuard>
      )

      expect(screen.getByTestId('protected-content')).toBeInTheDocument()
      expect(screen.getByText('Secret Content')).toBeInTheDocument()
    })

    it('does not render the access denied message', () => {
      renderWithRouter(
        <RoleGuard allowedRoles={['CompanyOwner']}>
          <div>Protected</div>
        </RoleGuard>
      )

      expect(screen.queryByText(/Access Denied/i)).not.toBeInTheDocument()
    })
  })

  describe('when user lacks the required role', () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue(makeAuthContext(makeUser('User')))
    })

    it('renders the access denied fallback', () => {
      renderWithRouter(
        <RoleGuard allowedRoles={['CompanyOwner', 'Admin']}>
          <div data-testid="protected-content">Secret Content</div>
        </RoleGuard>
      )

      expect(screen.getByText(/Access Denied/i)).toBeInTheDocument()
      expect(screen.queryByTestId('protected-content')).not.toBeInTheDocument()
    })

    it('renders a "Back to Dashboard" link pointing to /dashboard', () => {
      renderWithRouter(
        <RoleGuard allowedRoles={['CompanyOwner']}>
          <div>Protected</div>
        </RoleGuard>
      )

      const link = screen.getByRole('link', { name: /back to dashboard/i })
      expect(link).toBeInTheDocument()
      expect(link).toHaveAttribute('href', '/dashboard')
    })

    it('shows the user role in the access denied message', () => {
      renderWithRouter(
        <RoleGuard allowedRoles={['CompanyOwner']}>
          <div>Protected</div>
        </RoleGuard>
      )

      expect(screen.getByText(/User/)).toBeInTheDocument()
    })
  })

  describe('when there is no authenticated user', () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue(makeAuthContext(null))
    })

    it('renders the access denied fallback', () => {
      renderWithRouter(
        <RoleGuard allowedRoles={['CompanyOwner']}>
          <div data-testid="protected-content">Secret Content</div>
        </RoleGuard>
      )

      expect(screen.getByText(/Access Denied/i)).toBeInTheDocument()
      expect(screen.queryByTestId('protected-content')).not.toBeInTheDocument()
    })
  })
})
