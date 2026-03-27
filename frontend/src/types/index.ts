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

export interface UpdateProjectRequest extends CreateProjectRequest {}

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

// ─── Toast ───────────────────────────────────────────────────────────────────

export type ToastType = 'success' | 'error' | 'info' | 'warning';

export interface Toast {
  id: string;
  type: ToastType;
  message: string;
}
