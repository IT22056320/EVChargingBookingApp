import React, { createContext, useContext, useEffect, useState } from 'react'
import { authService } from '@/services/auth'
import type { User } from '@/types'
import toast from 'react-hot-toast'

interface AuthContextType {
  user: User | null
  loading: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => void
  isAuthenticated: boolean
}

const AuthContext = createContext<AuthContextType | undefined>(undefined)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    // Check if user is already logged in
    const initAuth = async () => {
      try {
        if (authService.isAuthenticated()) {
          const userData = await authService.getCurrentUser()
          setUser(userData)
        }
      } catch (error) {
        console.error('Auth initialization error:', error)
        // Clear invalid auth data
        authService.logout()
      } finally {
        setLoading(false)
      }
    }

    initAuth()
  }, [])

  const login = async (email: string, password: string) => {
    console.log('AuthProvider: Login attempt for:', email)
    try {
      const response = await authService.login({ email, password })
      console.log('AuthProvider: Login response:', response)
      
      if (response.success && response.user) {
        console.log('AuthProvider: Setting user state:', response.user)
        setUser(response.user)
        authService.saveAuthData(response.user)
        console.log('AuthProvider: User state updated, isAuthenticated will be:', !!response.user)
      } else {
        console.error('AuthProvider: Login failed:', response.message)
        throw new Error(response.message || 'Login failed')
      }
    } catch (error: any) {
      console.error('AuthProvider: Login error:', error)
      
      // Format error message for display
      let errorMessage = 'Login failed'
      
      if (error.message) {
        errorMessage = error.message
      } else if (error.response?.data?.message) {
        errorMessage = error.response.data.message
      } else if (error.response?.status === 401) {
        errorMessage = 'Invalid email or password'
      } else if (error.response?.status === 404) {
        errorMessage = 'User not found or inactive'
      } else if (error.response?.status >= 500) {
        errorMessage = 'Server error. Please try again later.'
      } else if (error.code === 'NETWORK_ERROR' || !error.response) {
        errorMessage = 'Network error. Please check your connection and try again.'
      }
      
      throw new Error(errorMessage)
    }
  }

  const logout = () => {
    setUser(null)
    authService.logout()
  }

  const value: AuthContextType = {
    user,
    loading,
    login,
    logout,
    isAuthenticated: !!user,
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}