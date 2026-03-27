import axios, { type AxiosInstance, type AxiosError } from 'axios';
import type { ApiResponse, LoginRequest, RegisterRequest, TokenResponse } from '../types/auth';
import type { Project, CreateProjectRequest, UpdateProjectRequest, Document, TeamMember, CreateTeamMemberRequest } from '../types';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5059/api';

class ApiService {
  private api: AxiosInstance;

  constructor() {
    this.api = axios.create({
      baseURL: API_BASE_URL,
      headers: { 'Content-Type': 'application/json' },
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
      (error: AxiosError<ApiResponse<never>>) => {
        if (error.response?.status === 401) {
          localStorage.removeItem('token');
          localStorage.removeItem('user');
          window.location.href = '/login';
        }
        return Promise.reject(error);
      }
    );
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

  async getCurrentUser(): Promise<ApiResponse<any>> {
    const response = await this.api.get<ApiResponse<any>>('/auth/me');
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
}

export const apiService = new ApiService();
