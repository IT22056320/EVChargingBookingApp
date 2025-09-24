import axios, { AxiosInstance, AxiosResponse } from 'axios'
import toast from 'react-hot-toast'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5001/api'

class ApiService {
  private api: AxiosInstance

  constructor() {
    this.api = axios.create({
      baseURL: API_BASE_URL,
      timeout: 10000,
      headers: {
        'Content-Type': 'application/json',
      },
    })

    // Request interceptor
    this.api.interceptors.request.use(
      (config) => {
        console.log('API Request:', {
          method: config.method?.toUpperCase(),
          url: (config.baseURL || '') + (config.url || ''),
          data: config.data
        })
        
        const token = localStorage.getItem('authToken')
        if (token) {
          config.headers.Authorization = `Bearer ${token}`
        }
        return config
      },
      (error) => {
        console.error('API Request Error:', error)
        return Promise.reject(error)
      }
    )

    // Response interceptor
    this.api.interceptors.response.use(
      (response) => {
        console.log('API Response:', response.status, response.data)
        return response
      },
      (error) => {
        console.error('API Error:', error)
        console.error('Error Response:', error.response?.data)
        console.error('Error Status:', error.response?.status)
        
        // Handle different types of errors
        if (!error.response) {
          // Network error - no response from server
          toast.error('Network error. Please check your connection and try again.', {
            duration: 8000
          })
        } else if (error.response.status >= 500) {
          // Server error
          toast.error('Server error. Please try again later.', {
            duration: 6000
          })
        } else if (error.response.status === 401) {
          // Unauthorized - only clear auth if not on login page
          if (!window.location.pathname.includes('/login')) {
            localStorage.removeItem('authToken')
            localStorage.removeItem('user')
            toast.error('Your session has expired. Please log in again.')
            setTimeout(() => {
              window.location.href = '/login'
            }, 1500)
          }
        }
        
        // Format error message for display
        const errorMessage = error.response?.data?.message || 
                           error.response?.data?.error || 
                           error.message || 
                           'An unexpected error occurred'
                           
        error.displayMessage = errorMessage
        return Promise.reject(error)
      }
    )
  }

  // Generic API methods
  async get<T>(url: string, params?: any): Promise<T> {
    const response: AxiosResponse<T> = await this.api.get(url, { params })
    return response.data
  }

  async post<T>(url: string, data?: any): Promise<T> {
    const response: AxiosResponse<T> = await this.api.post(url, data)
    return response.data
  }

  async put<T>(url: string, data?: any): Promise<T> {
    const response: AxiosResponse<T> = await this.api.put(url, data)
    return response.data
  }

  async patch<T>(url: string, data?: any): Promise<T> {
    const response: AxiosResponse<T> = await this.api.patch(url, data)
    return response.data
  }

  async delete<T>(url: string): Promise<T> {
    const response: AxiosResponse<T> = await this.api.delete(url)
    return response.data
  }

  // File download
  async downloadFile(url: string, filename: string): Promise<void> {
    const response = await this.api.get(url, {
      responseType: 'blob',
    })
    
    const blob = new Blob([response.data])
    const downloadUrl = window.URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = downloadUrl
    link.download = filename
    link.click()
    window.URL.revokeObjectURL(downloadUrl)
  }
}

export const apiService = new ApiService()
export default apiService