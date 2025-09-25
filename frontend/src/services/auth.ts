import { apiService } from './api'
import type {
  LoginRequest,
  LoginResponse,
  User,
  EVOwner,
  RegisterEVOwnerRequest,
  ApprovalRequest,
  ApiResponse
} from '@/types'

export class AuthService {
  async login(credentials: LoginRequest): Promise<LoginResponse> {
    console.log('AuthService: Attempting login with:', credentials.email)
    console.log('API Base URL:', import.meta.env.VITE_API_BASE_URL || 'http://localhost:5001/api')
    
    try {
      const response = await apiService.post<any>('/webusers/login', credentials)
      console.log('AuthService: Login response:', response)
      
      // Transform the response to match our expected format
      if (response.success && response.user) {
        const transformedUser: User = {
          id: response.user.id,
          email: response.user.email,
          fullName: response.user.fullName,
          role: response.user.roleId,
          isActive: response.user.isActive,
          createdAt: response.user.createdAt,
          lastLoginAt: response.user.lastLoginAt
        }

        console.log('AuthService: Transformed user:', transformedUser)
        
        // Save user data and create a simple auth token (user session indicator)
        this.saveAuthData(transformedUser, 'authenticated')
        
        return {
          success: true,
          message: response.message,
          user: transformedUser
        }
      } else {
        // Handle case where response doesn't have success flag or user
        throw new Error(response.message || 'Login failed - invalid response format')
      }
    } catch (error: any) {
      console.error('AuthService: Login error:', error)
      console.error('Error response:', error.response?.data)
      console.error('Error status:', error.response?.status)
      
      // Use the formatted error message from the API interceptor
      const errorMessage = error.displayMessage || 
                          error.response?.data?.message || 
                          error.message || 
                          'Login failed'
                          
      throw new Error(errorMessage)
    }
  }

  async getCurrentUser(): Promise<User> {
    const userData = localStorage.getItem('user')
    if (!userData) {
      throw new Error('No user data found')
    }
    return JSON.parse(userData)
  }

  logout(): void {
    localStorage.removeItem('authToken')
    localStorage.removeItem('user')
    window.location.href = '/login'
  }

  isAuthenticated(): boolean {
    // Check if both user data and auth token exist
    const userData = localStorage.getItem('user')
    const authToken = localStorage.getItem('authToken')
    return !!(userData && authToken)
  }

  saveAuthData(user: User, token?: string): void {
    localStorage.setItem('user', JSON.stringify(user))
    if (token) {
      localStorage.setItem('authToken', token)
    }
  }
}

export class EVOwnerService {
  async getAllEVOwners(): Promise<EVOwner[]> {
    return apiService.get<EVOwner[]>('/evowners')
  }

  async getEVOwnerByNIC(nic: string): Promise<EVOwner> {
    return apiService.get<EVOwner>(`/evowners/${nic}`)
  }

  async registerEVOwner(data: RegisterEVOwnerRequest): Promise<ApiResponse> {
    return apiService.post<ApiResponse>('/evowners/register', data)
  }

  async updateApprovalStatus(nic: string, data: ApprovalRequest): Promise<ApiResponse> {
    return apiService.patch<ApiResponse>(`/evowners/${nic}/approval`, data)
  }

  async updateAccountStatus(nic: string, isActive: boolean): Promise<ApiResponse> {
    return apiService.patch<ApiResponse>(`/evowners/${nic}/status`, { isActive })
  }

  async getPendingApprovals(): Promise<EVOwner[]> {
    return apiService.get<EVOwner[]>('/evowners/pending-approvals')
  }
}

export class WebUserService {
  async getAllWebUsers(): Promise<User[]> {
    return apiService.get<User[]>('/webusers')
  }

  async createWebUser(data: {
    email: string
    fullName: string
    password: string
    role: number
    createdBy?: string
  }): Promise<ApiResponse> {
    return apiService.post<ApiResponse>('/webusers', data)
  }

  async updateUserStatus(id: string, isActive: boolean): Promise<ApiResponse> {
    return apiService.patch<ApiResponse>(`/webusers/${id}/status`, isActive)
  }
}

export const authService = new AuthService()
export const evOwnerService = new EVOwnerService()
export const webUserService = new WebUserService()