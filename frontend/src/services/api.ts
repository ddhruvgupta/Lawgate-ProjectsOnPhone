import axios, { type AxiosInstance, type AxiosError } from 'axios';
import type { ApiResponse, LoginRequest, RegisterRequest, TokenResponse } from '../types/auth';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5059/api';

class ApiService {
  private api: AxiosInstance;

  constructor() {
    this.api = axios.create({
      baseURL: API_BASE_URL,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Add request interceptor to include auth token
    this.api.interceptors.request.use(
      (config) => {
        const token = localStorage.getItem('token');
        if (token) {
          config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
      },
      (error) => Promise.reject(error)
    );

    // Add response interceptor to handle errors
    this.api.interceptors.response.use(
      (response) => response,
      (error: AxiosError<ApiResponse<never>>) => {
        if (error.response?.status === 401) {
          // Token expired or invalid
          localStorage.removeItem('token');
          localStorage.removeItem('user');
          window.location.href = '/login';
        }
        return Promise.reject(error);
      }
    );
  }

  // Authentication
  async login(credentials: LoginRequest): Promise<ApiResponse<TokenResponse>> {
    const response = await this.api.post<ApiResponse<TokenResponse>>('/auth/login', credentials);
    return response.data;
  }

  async register(data: RegisterRequest): Promise<ApiResponse<TokenResponse>> {
    const response = await this.api.post<ApiResponse<TokenResponse>>('/auth/register', data);
    return response.data;
  }

  async validateToken(token: string): Promise<ApiResponse<{ isValid: boolean; userId: number }>> {
    const response = await this.api.post<ApiResponse<{ isValid: boolean; userId: number }>>('/auth/validate', { token });
    return response.data;
  }

  async getCurrentUser(): Promise<ApiResponse<any>> {
    const response = await this.api.get<ApiResponse<any>>('/auth/me');
    return response.data;
  }

  // Generic API methods
  async get<T>(url: string): Promise<ApiResponse<T>> {
    const response = await this.api.get<ApiResponse<T>>(url);
    return response.data;
  }

  async post<T>(url: string, data?: any): Promise<ApiResponse<T>> {
    const response = await this.api.post<ApiResponse<T>>(url, data);
    return response.data;
  }

  async put<T>(url: string, data?: any): Promise<ApiResponse<T>> {
    const response = await this.api.put<ApiResponse<T>>(url, data);
    return response.data;
  }

  async delete<T>(url: string): Promise<ApiResponse<T>> {
    const response = await this.api.delete<ApiResponse<T>>(url);
    return response.data;
  }
}

export const apiService = new ApiService();
