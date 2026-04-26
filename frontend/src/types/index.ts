// Re-export auth types
export type { User, UserRole, LoginRequest, RegisterRequest, TokenResponse, ApiResponse, AuthContextType } from './auth';

// ─── Projects ────────────────────────────────────────────────────────────────

export type ProjectStatus =
  | 'Intake'
  | 'Active'
  | 'Discovery'
  | 'Negotiation'
  | 'Hearing'
  | 'OnHold'
  | 'Settled'
  | 'Closed'
  | 'Archived';

export interface Project {
  id: number;
  companyId: number;
  name: string;
  description: string;
  clientName?: string;
  caseNumber?: string;
  status: ProjectStatus;
  startDate?: string;
  endDate?: string;
  tags?: string;
  createdAt: string;
  documentCount: number;
}

export interface CreateProjectRequest {
  name: string;
  description: string;
  clientName?: string;
  caseNumber?: string;
  startDate?: string;
  endDate?: string;
  tags?: string;
  status: ProjectStatus;
}

export type UpdateProjectRequest = CreateProjectRequest;

// ─── Documents ───────────────────────────────────────────────────────────────

export interface Document {
  id: number;
  projectId: number;
  fileName: string;
  fileExtension: string;
  fileSizeBytes: number;
  description?: string;
  documentType: string;
  version: number;
  isLatestVersion: boolean;
  uploadedBy: string;
  createdAt: string;
  downloadUrl?: string;
}

// ─── Users / Team ────────────────────────────────────────────────────────────

export interface TeamMember {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  companyId: number;
  phoneNumber?: string;
  isActive: boolean;
}

export interface CreateTeamMemberRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  phone?: string;
  role: 'Admin' | 'User';
}

// ─── Audit Logs ──────────────────────────────────────────────────────────────

export interface AuditLog {
  id: number;
  action: string;
  entityType: string;
  entityId?: number;
  description: string;
  oldValues?: string;
  userName: string;
  userEmail: string;
  createdAt: string;
}

export interface AuditLogsResponse {
  items: AuditLog[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// ─── Platform Admin ──────────────────────────────────────────────────────────

export interface CompanyOverview {
  id: number;
  name: string;
  email: string;
  phone: string;
  subscriptionTier: string;
  isActive: boolean;
  createdAt: string;
  userCount: number;
  projectCount: number;
  documentCount: number;
  storageUsedBytes: number;
}

export interface CompanyUser {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  role: string;
  isActive: boolean;
  lastLoginAt: string | null;
}

export interface CompanyProject {
  id: number;
  name: string;
  clientName: string | null;
  status: string;
  documentCount: number;
  createdAt: string;
}

export interface CompanyDetail extends CompanyOverview {
  address: string;
  city: string;
  state: string;
  country: string;
  subscriptionEndDate: string | null;
  users: CompanyUser[];
  projects: CompanyProject[];
}

export interface CompanyDocument {
  id: number;
  fileName: string;
  documentType: string;
  fileSizeBytes: number;
  projectName: string;
  uploadedBy: string;
  createdAt: string;
}

// ─── Toast ───────────────────────────────────────────────────────────────────

export type ToastType = 'success' | 'error' | 'info' | 'warning';

export interface Toast {
  id: string;
  type: ToastType;
  message: string;
}
