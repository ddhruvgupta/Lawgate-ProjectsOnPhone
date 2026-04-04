import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { renderHook } from '@testing-library/react'
import type { User, AuthContextType } from '../types/auth'

// We mock the AuthContext module so useAuth returns a controlled user.
// usePermissions calls useAuth() internally.
const mockUseAuth = vi.fn<() => AuthContextType>()

vi.mock('../contexts/AuthContext', () => ({
  useAuth: () => mockUseAuth(),
}))

// Import after the mock is set up
const { usePermissions } = await import('./usePermissions')

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

describe('usePermissions', () => {
  afterEach(() => {
    vi.clearAllMocks()
  })

  describe('with CompanyOwner role', () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue(makeAuthContext(makeUser('CompanyOwner')))
    })

    it('isAdmin returns true', () => {
      const { result } = renderHook(() => usePermissions())
      expect(result.current.isAdmin).toBe(true)
    })

    it('isOwner returns true', () => {
      const { result } = renderHook(() => usePermissions())
      expect(result.current.isOwner).toBe(true)
    })

    it('isPlatformAdmin returns false', () => {
      const { result } = renderHook(() => usePermissions())
      expect(result.current.isPlatformAdmin).toBe(false)
    })

    it('isPlatformSuperAdmin returns false', () => {
      const { result } = renderHook(() => usePermissions())
      expect(result.current.isPlatformSuperAdmin).toBe(false)
    })
  })

  describe('with PlatformSuperAdmin role', () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue(makeAuthContext(makeUser('PlatformSuperAdmin')))
    })

    it('isPlatformAdmin returns true', () => {
      const { result } = renderHook(() => usePermissions())
      expect(result.current.isPlatformAdmin).toBe(true)
    })

    it('isPlatformSuperAdmin returns true', () => {
      const { result } = renderHook(() => usePermissions())
      expect(result.current.isPlatformSuperAdmin).toBe(true)
    })

    it('isAdmin returns false', () => {
      const { result } = renderHook(() => usePermissions())
      expect(result.current.isAdmin).toBe(false)
    })

    it('isOwner returns false', () => {
      const { result } = renderHook(() => usePermissions())
      expect(result.current.isOwner).toBe(false)
    })
  })

  describe('with User role', () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue(makeAuthContext(makeUser('User')))
    })

    it('isAdmin returns false', () => {
      const { result } = renderHook(() => usePermissions())
      expect(result.current.isAdmin).toBe(false)
    })

    it('isOwner returns false', () => {
      const { result } = renderHook(() => usePermissions())
      expect(result.current.isOwner).toBe(false)
    })

    it('isPlatformAdmin returns false', () => {
      const { result } = renderHook(() => usePermissions())
      expect(result.current.isPlatformAdmin).toBe(false)
    })

    it('canCreateProject returns true', () => {
      const { result } = renderHook(() => usePermissions())
      expect(result.current.canCreateProject).toBe(true)
    })

    it('canDeleteProject returns false', () => {
      const { result } = renderHook(() => usePermissions())
      expect(result.current.canDeleteProject).toBe(false)
    })
  })

  describe('with no token / unauthenticated', () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue(makeAuthContext(null))
    })

    it('isAdmin returns false', () => {
      const { result } = renderHook(() => usePermissions())
      expect(result.current.isAdmin).toBe(false)
    })

    it('isOwner returns false', () => {
      const { result } = renderHook(() => usePermissions())
      expect(result.current.isOwner).toBe(false)
    })

    it('isPlatformAdmin returns false', () => {
      const { result } = renderHook(() => usePermissions())
      expect(result.current.isPlatformAdmin).toBe(false)
    })

    it('role is undefined', () => {
      const { result } = renderHook(() => usePermissions())
      expect(result.current.role).toBeUndefined()
    })
  })
})
