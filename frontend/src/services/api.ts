import axios, { type AxiosInstance, type AxiosError } from 'axios';
import type { ApiResponse, LoginRequest, RegisterRequest, TokenResponse, User } from '../types/auth';
import type { Project, CreateProjectRequest, UpdateProjectRequest, Document, UploadDocumentRequest, UploadUrlResponse, TeamMember, CreateTeamMemberRequest, AuditLogsResponse, CompanyOverview, CompanyDetail, CompanyDocument } from '../types';
import { config } from '../config';

class ApiService {
  private api: AxiosInstance;
  private isRefreshing = false;
  private pendingRequests: Array<(token: string | null) => void> = [];

  constructor() {
    this.api = axios.create({
      baseURL: config.apiUrl,
      headers: { 'Content-Type': 'application/json' },
      timeout: 15000, // 15 s — surfaces network errors promptly instead of hanging indefinitely
    });

    this.api.interceptors.request.use(
      (config) => {
        const token = localStorage.getItem('token');
        if (token) config.headers.Authorization = `Bearer ${token}`;
        return config;
      },
      (error) => Promise.reject(error)
    );

    this.api.interceptors.response.use(
      (response) => response,
      async (error: AxiosError<ApiResponse<never>>) => {
        const originalRequest = error.config as typeof error.config & { _retry?: boolean };

        if (error.response?.status === 401 && !originalRequest._retry) {
          const storedRefreshToken = localStorage.getItem('refreshToken');

          if (!storedRefreshToken) {
            this._clearAuthAndRedirect();
            return Promise.reject(error);
          }

          if (this.isRefreshing) {
            // Queue this request until the refresh completes
            return new Promise((resolve, reject) => {
              this.pendingRequests.push((newToken) => {
                if (!newToken) return reject(error);
                originalRequest.headers!['Authorization'] = `Bearer ${newToken}`;
                resolve(this.api(originalRequest));
              });
            });
          }

          originalRequest._retry = true;
          this.isRefreshing = true;

          try {
            const response = await this.refreshTokens(storedRefreshToken);
            if (response.success && response.data) {
              const { token, refreshToken } = response.data;
              localStorage.setItem('token', token);
              localStorage.setItem('refreshToken', refreshToken);
              this.api.defaults.headers.common['Authorization'] = `Bearer ${token}`;
              this.pendingRequests.forEach((cb) => cb(token));
              this.pendingRequests = [];
              originalRequest.headers!['Authorization'] = `Bearer ${token}`;
              return this.api(originalRequest);
            } else {
              this._clearAuthAndRedirect();
              return Promise.reject(error);
            }
          } catch {
            this.pendingRequests.forEach((cb) => cb(null));
            this.pendingRequests = [];
            this._clearAuthAndRedirect();
            return Promise.reject(error);
          } finally {
            this.isRefreshing = false;
          }
        }

        return Promise.reject(error);
      }
    );
  }

  private _clearAuthAndRedirect() {
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
    window.location.href = '/login';
  }

  // ─── Auth ──────────────────────────────────────────────────────────────────

  async login(credentials: LoginRequest): Promise<ApiResponse<TokenResponse>> {
    const response = await this.api.post<ApiResponse<TokenResponse>>('/auth/login', credentials);
    return response.data;
  }

  async register(data: RegisterRequest): Promise<ApiResponse<TokenResponse>> {
    const response = await this.api.post<ApiResponse<TokenResponse>>('/auth/register', data);
    return response.data;
  }

  async getCurrentUser(): Promise<ApiResponse<User>> {
    const response = await this.api.get<ApiResponse<User>>('/auth/me');
    return response.data;
  }

  async refreshTokens(refreshToken: string): Promise<ApiResponse<TokenResponse>> {
    // Use raw axios to avoid the interceptor triggering infinitely
    const response = await axios.post<ApiResponse<TokenResponse>>(
      `${config.apiUrl}/auth/refresh`,
      { refreshToken },
      { headers: { 'Content-Type': 'application/json' } }
    );
    return response.data;
  }

  async forgotPassword(email: string): Promise<ApiResponse<null>> {
    const response = await this.api.post<ApiResponse<null>>('/auth/forgot-password', { email });
    return response.data;
  }

  async resetPassword(token: string, newPassword: string, confirmPassword: string): Promise<ApiResponse<null>> {
    const response = await this.api.post<ApiResponse<null>>('/auth/reset-password', {
      token,
      newPassword,
      confirmPassword,
    });
    return response.data;
  }

  async verifyEmail(token: string): Promise<ApiResponse<null>> {
    const response = await this.api.post<ApiResponse<null>>('/auth/verify-email', { token });
    return response.data;
  }

  async resendVerification(email: string): Promise<ApiResponse<null>> {
    const response = await this.api.post<ApiResponse<null>>('/auth/resend-verification', { email });
    return response.data;
  }

  // ─── Projects ──────────────────────────────────────────────────────────────

  async getProjects(): Promise<Project[]> {
    const response = await this.api.get<Project[]>('/projects');
    return response.data;
  }

  async getProject(id: number): Promise<Project> {
    const response = await this.api.get<Project>(`/projects/${id}`);
    return response.data;
  }

  async createProject(data: CreateProjectRequest): Promise<Project> {
    const response = await this.api.post<Project>('/projects', data);
    return response.data;
  }

  async updateProject(id: number, data: UpdateProjectRequest): Promise<Project> {
    const response = await this.api.put<Project>(`/projects/${id}`, data);
    return response.data;
  }

  async deleteProject(id: number): Promise<void> {
    await this.api.delete(`/projects/${id}`);
  }

  // ─── Documents ─────────────────────────────────────────────────────────────

  async getProjectDocuments(projectId: number): Promise<Document[]> {
    const response = await this.api.get<Document[]>(`/documents/project/${projectId}`);
    return response.data;
  }

  async getDocument(id: number): Promise<Document> {
    const response = await this.api.get<Document>(`/documents/${id}`);
    return response.data;
  }

  async getDownloadUrl(id: number): Promise<string> {
    const response = await this.api.get<{ downloadUrl: string }>(`/documents/${id}/download-url`);
    return response.data.downloadUrl;
  }

  async deleteDocument(id: number): Promise<void> {
    await this.api.delete(`/documents/${id}`);
  }

  async generateUploadUrl(dto: UploadDocumentRequest): Promise<UploadUrlResponse> {
    const response = await this.api.post<UploadUrlResponse>('/documents/upload-url', dto);
    return response.data;
  }

  async confirmUpload(documentId: number): Promise<Document> {
    const response = await this.api.post<Document>(`/documents/${documentId}/confirm`);
    return response.data;
  }

  // ─── Team / Users ──────────────────────────────────────────────────────────

  async getTeamMembers(): Promise<TeamMember[]> {
    const response = await this.api.get<TeamMember[]>('/users');
    return response.data;
  }

  async createTeamMember(data: CreateTeamMemberRequest): Promise<TeamMember> {
    const response = await this.api.post<TeamMember>('/users', data);
    return response.data;
  }

  async toggleUserStatus(id: number): Promise<TeamMember> {
    const response = await this.api.post<TeamMember>(`/users/${id}/toggle-status`);
    return response.data;
  }

  // ─── Audit Logs ────────────────────────────────────────────────────────────

  async getAuditLogs(params?: {
    entityType?: string;
    entityId?: number;
    page?: number;
    pageSize?: number;
  }): Promise<AuditLogsResponse> {
    const response = await this.api.get<AuditLogsResponse>('/audit', { params });
    return response.data;
  }
  // ── Platform Admin ────────────────────────────────────────────────────────

  getPlatformCompanies(): Promise<CompanyOverview[]> {
    return this.api.get('/admin/companies').then((r) => r.data);
  }

  getPlatformCompany(id: number): Promise<CompanyDetail> {
    return this.api.get(`/admin/companies/${id}`).then((r) => r.data);
  }

  getPlatformCompanyDocuments(id: number): Promise<CompanyDocument[]> {
    return this.api.get(`/admin/companies/${id}/documents`).then((r) => r.data);
  }
}

export const apiService = new ApiService();
