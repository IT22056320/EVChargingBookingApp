import { useState, useEffect } from 'react'
import { useAuth } from '../providers/auth-provider'
import { dashboardApi, DashboardStatistics, ActivityLog } from '../services/dashboardApi'
import toast from '../utils/toast'
import { Users, UserCheck, Clock, Car, Shield, Activity, Zap, ArrowUpRight, ArrowRight } from 'lucide-react'

export function DashboardPage() {
  const { user } = useAuth()
  const [statistics, setStatistics] = useState<DashboardStatistics | null>(null)
  const [activities, setActivities] = useState<ActivityLog[]>([])
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    loadDashboardData()
  }, [])

  const loadDashboardData = async () => {
    setIsLoading(true)
    try {
      const [stats, recentActivities] = await Promise.all([
        dashboardApi.getDashboardStatistics(),
        Promise.resolve(dashboardApi.getRecentActivity())
      ])
      
      setStatistics(stats)
      setActivities(recentActivities)
      toast.success('Dashboard data loaded successfully')
    } catch (error) {
      console.error('Failed to load dashboard data:', error)
      toast.error('Failed to load dashboard data')
      
      // Set fallback data
      setStatistics({
        totalWebUsers: 0,
        backofficeUsers: 0,
        stationOperators: 0,
        activeWebUsers: 0,
        totalEVOwners: 0,
        approvedEVOwners: 0,
        pendingApprovals: 0,
        activeEVOwners: 0,
      })
    } finally {
      setIsLoading(false)
    }
  }

  const formatRelativeTime = (timestamp: string) => {
    const now = new Date()
    const time = new Date(timestamp)
    const diffInHours = Math.floor((now.getTime() - time.getTime()) / (1000 * 60 * 60))
    
    if (diffInHours === 0) return 'Just now'
    if (diffInHours === 1) return '1 hour ago'
    if (diffInHours < 24) return `${diffInHours} hours ago`
    
    const diffInDays = Math.floor(diffInHours / 24)
    if (diffInDays === 1) return '1 day ago'
    return `${diffInDays} days ago`
  }

  const navigateToEVOwners = () => {
    window.location.href = '/ev-owners'
  }

  const isBackoffice = user?.role === 'Backoffice'

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-center h-64">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight flex items-center gap-3">
            <Activity className="h-8 w-8 text-blue-600" />
            {isBackoffice ? 'Back Office Dashboard' : 'Station Operator Dashboard'}
          </h1>
          <p className="text-muted-foreground">
            Welcome back, {user?.fullName || 'User'}
          </p>
        </div>
        <div className="flex items-center gap-3">
          <span className="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium bg-blue-100 text-blue-800">
            <Shield className="h-4 w-4 mr-1" />
            {user?.role || 'User'}
          </span>
        </div>
      </div>

      {/* Statistics Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {/* Staff Users */}
        <div className="rounded-lg border bg-gradient-to-br from-blue-500 to-blue-600 text-white p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-blue-100 text-sm font-medium">Staff Users</p>
              <p className="text-3xl font-bold">{statistics?.totalWebUsers || 0}</p>
              <p className="text-blue-100 text-xs">Backoffice & Operators</p>
            </div>
            <Users className="h-8 w-8 text-blue-200" />
          </div>
        </div>

        {/* EV Owners */}
        <div 
          className="rounded-lg border bg-gradient-to-br from-green-500 to-green-600 text-white p-6 cursor-pointer hover:from-green-600 hover:to-green-700 transition-all transform hover:scale-105"
          onClick={navigateToEVOwners}
        >
          <div className="flex items-center justify-between">
            <div>
              <p className="text-green-100 text-sm font-medium">EV Owners</p>
              <p className="text-3xl font-bold">{statistics?.totalEVOwners || 0}</p>
              <p className="text-green-100 text-xs">{statistics?.approvedEVOwners || 0} approved</p>
            </div>
            <Car className="h-8 w-8 text-green-200" />
          </div>
        </div>

        {/* Pending Approvals */}
        <div 
          className="rounded-lg border bg-gradient-to-br from-orange-500 to-orange-600 text-white p-6 cursor-pointer hover:from-orange-600 hover:to-orange-700 transition-all transform hover:scale-105"
          onClick={navigateToEVOwners}
        >
          <div className="flex items-center justify-between">
            <div>
              <p className="text-orange-100 text-sm font-medium">Pending Approvals</p>
              <p className="text-3xl font-bold">{statistics?.pendingApprovals || 0}</p>
              <p className="text-orange-100 text-xs">Awaiting review</p>
            </div>
            <Clock className="h-8 w-8 text-orange-200" />
          </div>
        </div>

        {/* Active Accounts */}
        <div className="rounded-lg border bg-gradient-to-br from-cyan-500 to-cyan-600 text-white p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-cyan-100 text-sm font-medium">Active Accounts</p>
              <p className="text-3xl font-bold">{statistics?.activeEVOwners || 0}</p>
              <p className="text-cyan-100 text-xs">Active EV owners</p>
            </div>
            <UserCheck className="h-8 w-8 text-cyan-200" />
          </div>
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-12">
        {/* Quick Actions */}
        <div className="lg:col-span-4">
          <div className="rounded-lg border bg-card shadow-sm h-full">
            <div className="p-6 border-b">
              <div className="flex items-center gap-2">
                <Zap className="h-5 w-5 text-blue-600" />
                <h3 className="text-lg font-semibold">Quick Actions</h3>
              </div>
            </div>
            <div className="p-6 space-y-3">
              {isBackoffice && (
                <>
                  <button className="w-full p-4 rounded-lg border bg-card hover:bg-accent transition-colors text-left group">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-3">
                        <div className="p-2 rounded-lg bg-blue-100 text-blue-600">
                          <Users className="h-4 w-4" />
                        </div>
                        <div>
                          <p className="font-medium">Manage Staff Users</p>
                          <p className="text-sm text-muted-foreground">Create Backoffice & Operators</p>
                        </div>
                      </div>
                      <ArrowRight className="h-4 w-4 text-muted-foreground group-hover:text-foreground" />
                    </div>
                  </button>

                  <button 
                    className="w-full p-4 rounded-lg border bg-card hover:bg-accent transition-colors text-left group"
                    onClick={navigateToEVOwners}
                  >
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-3">
                        <div className="p-2 rounded-lg bg-green-100 text-green-600">
                          <Car className="h-4 w-4" />
                        </div>
                        <div>
                          <p className="font-medium">EV Owner Management</p>
                          <p className="text-sm text-muted-foreground">View & manage customer accounts</p>
                        </div>
                      </div>
                      <ArrowRight className="h-4 w-4 text-muted-foreground group-hover:text-foreground" />
                    </div>
                  </button>

                  <button 
                    className="w-full p-4 rounded-lg border bg-card hover:bg-accent transition-colors text-left group"
                    onClick={navigateToEVOwners}
                  >
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-3">
                        <div className="p-2 rounded-lg bg-orange-100 text-orange-600">
                          <Clock className="h-4 w-4" />
                        </div>
                        <div>
                          <p className="font-medium">Approve Registrations</p>
                          <p className="text-sm text-muted-foreground">Review pending EV owner requests</p>
                        </div>
                      </div>
                      <div className="flex items-center gap-2">
                        {statistics && statistics.pendingApprovals > 0 && (
                          <span className="px-2 py-1 bg-red-500 text-white text-xs rounded-full">
                            {statistics.pendingApprovals}
                          </span>
                        )}
                        <ArrowRight className="h-4 w-4 text-muted-foreground group-hover:text-foreground" />
                      </div>
                    </div>
                  </button>

                  <button className="w-full p-4 rounded-lg border bg-card hover:bg-accent transition-colors text-left group">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-3">
                        <div className="p-2 rounded-lg bg-purple-100 text-purple-600">
                          <Zap className="h-4 w-4" />
                        </div>
                        <div>
                          <p className="font-medium">Charging Stations</p>
                          <p className="text-sm text-muted-foreground">Manage charging station network</p>
                        </div>
                      </div>
                      <ArrowRight className="h-4 w-4 text-muted-foreground group-hover:text-foreground" />
                    </div>
                  </button>
                </>
              )}
            </div>
          </div>
        </div>

        {/* System Overview */}
        <div className="lg:col-span-8">
          <div className="rounded-lg border bg-card shadow-sm h-full">
            <div className="p-6 border-b">
              <div className="flex items-center gap-2">
                <Activity className="h-5 w-5 text-green-600" />
                <h3 className="text-lg font-semibold">System Overview</h3>
              </div>
            </div>
            <div className="p-6">
              <div className="grid md:grid-cols-2 gap-6">
                {/* Staff Breakdown */}
                <div>
                  <h4 className="text-sm font-medium text-muted-foreground mb-4">Staff Distribution</h4>
                  <div className="space-y-3">
                    <div className="flex justify-between items-center">
                      <span className="text-sm">Backoffice Users:</span>
                      <span className="font-semibold text-blue-600">{statistics?.backofficeUsers || 0}</span>
                    </div>
                    <div className="flex justify-between items-center">
                      <span className="text-sm">Station Operators:</span>
                      <span className="font-semibold text-green-600">{statistics?.stationOperators || 0}</span>
                    </div>
                    <div className="flex justify-between items-center">
                      <span className="text-sm">Active Staff:</span>
                      <span className="font-semibold text-cyan-600">{statistics?.activeWebUsers || 0}</span>
                    </div>
                  </div>
                </div>

                {/* Customer Breakdown */}
                <div>
                  <h4 className="text-sm font-medium text-muted-foreground mb-4">Customer Overview</h4>
                  <div className="space-y-3">
                    <div className="flex justify-between items-center">
                      <span className="text-sm">Total Registrations:</span>
                      <span className="font-semibold">{statistics?.totalEVOwners || 0}</span>
                    </div>
                    <div className="flex justify-between items-center">
                      <span className="text-sm">Approved Users:</span>
                      <span className="font-semibold text-green-600">{statistics?.approvedEVOwners || 0}</span>
                    </div>
                    <div className="flex justify-between items-center">
                      <span className="text-sm">Pending Review:</span>
                      <span className="font-semibold text-orange-600">{statistics?.pendingApprovals || 0}</span>
                    </div>
                  </div>
                </div>
              </div>

              {statistics && statistics.pendingApprovals > 0 && (
                <div className="mt-6 p-4 bg-orange-50 border border-orange-200 rounded-lg">
                  <div className="flex items-start gap-3">
                    <Clock className="h-5 w-5 text-orange-500 mt-0.5" />
                    <div className="flex-1">
                      <p className="font-medium text-orange-800">Action Required</p>
                      <p className="text-sm text-orange-700">
                        You have {statistics.pendingApprovals} EV owner registration(s) pending approval.
                      </p>
                      <button 
                        onClick={navigateToEVOwners}
                        className="mt-2 text-sm text-orange-700 hover:text-orange-800 font-medium inline-flex items-center gap-1"
                      >
                        Review Now <ArrowUpRight className="h-3 w-3" />
                      </button>
                    </div>
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Recent Activity */}
      <div className="rounded-lg border bg-card shadow-sm">
        <div className="p-6 border-b">
          <div className="flex items-center gap-2">
            <Clock className="h-5 w-5 text-blue-600" />
            <h3 className="text-lg font-semibold">Recent Activity</h3>
          </div>
        </div>
        <div className="p-6">
          <div className="space-y-4">
            {activities.map((activity, index) => (
              <div key={activity.id} className="flex gap-4">
                <div className="flex flex-col items-center">
                  <div className={`p-2 rounded-full ${
                    activity.color === 'text-green-600' ? 'bg-green-100' :
                    activity.color === 'text-blue-600' ? 'bg-blue-100' :
                    'bg-gray-100'
                  }`}>
                    {activity.icon === 'check-circle' && <UserCheck className={`h-4 w-4 ${activity.color}`} />}
                    {activity.icon === 'user-plus' && <Users className={`h-4 w-4 ${activity.color}`} />}
                    {activity.icon === 'check' && <UserCheck className={`h-4 w-4 ${activity.color}`} />}
                  </div>
                  {index < activities.length - 1 && (
                    <div className="w-px h-6 bg-border mt-2" />
                  )}
                </div>
                <div className="flex-1 pb-4">
                  <p className="font-medium text-sm">{activity.message}</p>
                  <p className="text-xs text-muted-foreground">{formatRelativeTime(activity.timestamp)}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  )
}