import { useAuth } from '../contexts/AuthContext';
import type { UserRole } from '../types/auth';

/**
 * Permission model:
 *
 *  CompanyOwner — full access to everything (founder / account owner)
 *  Admin        — can manage team, projects, view activity; cannot change billing/ownership
 *  User         — can create & edit projects, upload documents; cannot manage team
 *  Viewer       — read-only access; cannot create, edit, or delete anything
 */

const ADMIN_ROLES: UserRole[] = ['CompanyOwner', 'Admin'];

export function usePermissions() {
  const { user } = useAuth();
  const role = user?.role as UserRole | undefined;

  const hasRole = (...roles: UserRole[]) => !!role && roles.includes(role);

  return {
    role,

    /** True for CompanyOwner and Admin */
    isAdmin: hasRole('CompanyOwner', 'Admin'),

    /** True only for CompanyOwner */
    isOwner: hasRole('CompanyOwner'),

    // ── Route-level access ──────────────────────────────────────────────────

    /** Can view and manage the Team page */
    canAccessTeam: hasRole('CompanyOwner', 'Admin'),

    /** Can view the Activity / audit-log page */
    canAccessActivity: hasRole('CompanyOwner', 'Admin'),

    // ── In-page actions ─────────────────────────────────────────────────────

    /** Can invite or deactivate team members */
    canManageTeam: hasRole('CompanyOwner', 'Admin'),

    /** Can create a new project */
    canCreateProject: hasRole('CompanyOwner', 'Admin', 'User'),

    /** Can edit an existing project's details */
    canEditProject: hasRole('CompanyOwner', 'Admin', 'User'),

    /** Can delete a project (destructive — owners/admins only) */
    canDeleteProject: hasRole('CompanyOwner', 'Admin'),

    /** Can upload documents to a project */
    canUploadDocument: hasRole('CompanyOwner', 'Admin', 'User'),

    /** Can delete a document */
    canDeleteDocument: hasRole('CompanyOwner', 'Admin'),

    /** Helper: check arbitrary roles */
    hasRole,

    /** Allowed roles for admin-only areas */
    ADMIN_ROLES,
  };
}
