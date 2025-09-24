import { apiService } from './api'

export interface DashboardStatistics {
  // Web Users
  totalWebUsers: number
  backofficeUsers: number
  stationOperators: number
  activeWebUsers: number
  
  // EV Owners
  totalEVOwners: number
  approvedEVOwners: number
  pendingApprovals: number
  activeEVOwners: number
}

export interface WebUser {
  id: string
  email: string
  fullName: string
  role: string
  roleId: number
  isActive: boolean
  createdAt: string
  lastLoginAt?: string
  createdBy?: string
}

export interface EVOwner {
  id: string
  nic: string
  fullName: string
  email: string
  phoneNumber: string
  address: string
  isActive: boolean
  isApproved: boolean
  registeredAt: string
  approvedAt?: string
  approvedBy?: string
  lastLoginAt?: string
}

export interface PendingApproval {
  id: string
  nic: string
  fullName: string
  email: string
  phoneNumber: string
  registeredAt: string
}

export interface ActivityLog {
  id: string
  type: 'login' | 'registration' | 'approval' | 'status_change'
  message: string
  timestamp: string
  user?: string
  icon: string
  color: string
}

class DashboardApiService {
  // Get dashboard statistics
  async getDashboardStatistics(): Promise<DashboardStatistics> {
    try {
      // Fetch web users and EV owners in parallel
      const [webUsers, evOwners, pendingApprovals] = await Promise.all([
        this.getWebUsers(),
        this.getEVOwners(),
        this.getPendingApprovals()
      ])

      // Calculate statistics
      const stats: DashboardStatistics = {
        // Web Users stats
        totalWebUsers: webUsers.length,
        backofficeUsers: webUsers.filter(u => u.roleId === 0).length,
        stationOperators: webUsers.filter(u => u.roleId === 1).length,
        activeWebUsers: webUsers.filter(u => u.isActive).length,
        
        // EV Owners stats
        totalEVOwners: evOwners.length,
        approvedEVOwners: evOwners.filter(u => u.isApproved).length,
        pendingApprovals: pendingApprovals.length,
        activeEVOwners: evOwners.filter(u => u.isActive).length,
      }

      return stats
    } catch (error) {
      console.error('Error fetching dashboard statistics:', error)
      throw error
    }
  }

  // Get all web users
  async getWebUsers(): Promise<WebUser[]> {
    return await apiService.get<WebUser[]>('/WebUsers')
  }

  // Get all EV owners
  async getEVOwners(): Promise<EVOwner[]> {
    return await apiService.get<EVOwner[]>('/EVOwners')
  }

  // Get pending approvals
  async getPendingApprovals(): Promise<PendingApproval[]> {
    return await apiService.get<PendingApproval[]>('/EVOwners/pending-approvals')
  }

  // Get EV owner by NIC
  async getEVOwnerByNIC(nic: string): Promise<EVOwner> {
    return await apiService.get<EVOwner>(`/EVOwners/${nic}`)
  }

  // Approve/Reject EV owner
  async updateApprovalStatus(nic: string, isApproved: boolean, approvedBy: string): Promise<void> {
    await apiService.patch(`/EVOwners/${nic}/approval`, {
      isApproved,
      approvedBy
    })
  }

  // Activate/Deactivate EV owner
  async updateEVOwnerStatus(nic: string, isActive: boolean): Promise<void> {
    await apiService.patch(`/EVOwners/${nic}/status`, {
      isActive
    })
  }

  // Create web user
  async createWebUser(user: {
    email: string
    fullName: string
    password: string
    role: number
    createdBy?: string
  }): Promise<void> {
    await apiService.post('/WebUsers', user)
  }

  // Update web user status
  async updateWebUserStatus(id: string, isActive: boolean): Promise<void> {
    await apiService.patch(`/WebUsers/${id}/status`, isActive)
  }

  // Get recent activity (mock for now - can be enhanced with real activity logging)
  getRecentActivity(): ActivityLog[] {
    // Mock recent activity data
    return [
      {
        id: '1',
        type: 'login',
        message: 'You logged into the system successfully',
        timestamp: new Date().toISOString(),
        icon: 'check-circle',
        color: 'text-green-600'
      },
      {
        id: '2',
        type: 'registration',
        message: 'New customer registered via mobile app - awaiting approval',
        timestamp: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(), // 2 hours ago
        icon: 'user-plus',
        color: 'text-blue-600'
      },
      {
        id: '3',
        type: 'approval',
        message: 'Customer account activated and approved for service',
        timestamp: new Date(Date.now() - 5 * 60 * 60 * 1000).toISOString(), // 5 hours ago
        icon: 'check',
        color: 'text-green-600'
      }
    ]
  }
}

export const dashboardApi = new DashboardApiService()
export default dashboardApi