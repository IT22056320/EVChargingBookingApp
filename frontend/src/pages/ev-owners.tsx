import { useState, useEffect } from 'react'
import { useAuth } from '../providers/auth-provider'
import { dashboardApi, EVOwner, PendingApproval } from '../services/dashboardApi'
import toast from '../utils/toast'
import { 
  Users, 
  UserCheck, 
  Clock, 
  Search, 
  Eye, 
  Check, 
  X, 
  Play, 
  Pause, 
  Filter,
  MoreVertical,
  Mail,
  Phone,
  MapPin,
  Calendar,
  AlertTriangle,
  CheckCircle
} from 'lucide-react'

interface EVOwnerStats {
  total: number
  approved: number
  pending: number
  active: number
}

export function EVOwnersPage() {
  const { user } = useAuth()
  const [evOwners, setEVOwners] = useState<EVOwner[]>([])
  const [filteredOwners, setFilteredOwners] = useState<EVOwner[]>([])
  const [stats, setStats] = useState<EVOwnerStats>({ total: 0, approved: 0, pending: 0, active: 0 })
  const [searchTerm, setSearchTerm] = useState('')
  const [filter, setFilter] = useState<'all' | 'approved' | 'pending' | 'active'>('all')
  const [isLoading, setIsLoading] = useState(true)
  const [selectedOwner, setSelectedOwner] = useState<EVOwner | null>(null)
  const [showDetails, setShowDetails] = useState(false)

  useEffect(() => {
    loadEVOwners()
  }, [])

  useEffect(() => {
    applyFilters()
  }, [evOwners, searchTerm, filter])

  const loadEVOwners = async () => {
    setIsLoading(true)
    try {
      const owners = await dashboardApi.getEVOwners()
      setEVOwners(owners)
      
      // Calculate stats
      const newStats = {
        total: owners.length,
        approved: owners.filter(o => o.isApproved).length,
        pending: owners.filter(o => !o.isApproved && o.isActive).length,
        active: owners.filter(o => o.isActive).length,
      }
      setStats(newStats)
      
      toast.success(`Loaded ${owners.length} EV owners`)
    } catch (error) {
      console.error('Failed to load EV owners:', error)
      toast.error('Failed to load EV owners')
    } finally {
      setIsLoading(false)
    }
  }

  const applyFilters = () => {
    let filtered = [...evOwners]

    // Apply search filter
    if (searchTerm) {
      const term = searchTerm.toLowerCase()
      filtered = filtered.filter(owner => 
        owner.nic.toLowerCase().includes(term) ||
        owner.fullName.toLowerCase().includes(term) ||
        owner.email.toLowerCase().includes(term) ||
        owner.phoneNumber.toLowerCase().includes(term)
      )
    }

    // Apply status filter
    switch (filter) {
      case 'approved':
        filtered = filtered.filter(owner => owner.isApproved)
        break
      case 'pending':
        filtered = filtered.filter(owner => !owner.isApproved && owner.isActive)
        break
      case 'active':
        filtered = filtered.filter(owner => owner.isActive)
        break
      default:
        // Show all
        break
    }

    // Sort: pending first, then by registration date
    filtered.sort((a, b) => {
      if (!a.isApproved && b.isApproved) return -1
      if (a.isApproved && !b.isApproved) return 1
      return new Date(b.registeredAt).getTime() - new Date(a.registeredAt).getTime()
    })

    setFilteredOwners(filtered)
  }

  const handleApprove = async (owner: EVOwner) => {
    if (!user?.fullName) {
      toast.error('User information not available')
      return
    }

    try {
      await dashboardApi.updateApprovalStatus(owner.nic, true, user.fullName)
      toast.success(`${owner.fullName} has been approved`)
      loadEVOwners() // Reload data
    } catch (error) {
      console.error('Failed to approve EV owner:', error)
      toast.error('Failed to approve EV owner')
    }
  }

  const handleReject = async (owner: EVOwner) => {
    if (!user?.fullName) {
      toast.error('User information not available')
      return
    }

    try {
      await dashboardApi.updateApprovalStatus(owner.nic, false, user.fullName)
      toast.success(`${owner.fullName} has been rejected`)
      loadEVOwners() // Reload data
    } catch (error) {
      console.error('Failed to reject EV owner:', error)
      toast.error('Failed to reject EV owner')
    }
  }

  const handleToggleStatus = async (owner: EVOwner) => {
    try {
      const newStatus = !owner.isActive
      await dashboardApi.updateEVOwnerStatus(owner.nic, newStatus)
      toast.success(`${owner.fullName} has been ${newStatus ? 'activated' : 'deactivated'}`)
      loadEVOwners() // Reload data
    } catch (error) {
      console.error('Failed to update EV owner status:', error)
      toast.error('Failed to update EV owner status')
    }
  }

  const viewDetails = (owner: EVOwner) => {
    setSelectedOwner(owner)
    setShowDetails(true)
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    })
  }

  const getStatusColor = (owner: EVOwner) => {
    if (!owner.isApproved) return 'bg-orange-100 text-orange-800 border-orange-200'
    if (owner.isActive) return 'bg-green-100 text-green-800 border-green-200'
    return 'bg-gray-100 text-gray-800 border-gray-200'
  }

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
            <Users className="h-8 w-8 text-green-600" />
            EV Owner Management
          </h1>
          <p className="text-muted-foreground">
            Manage EV owner registrations and approvals
          </p>
        </div>
        <div className="flex gap-2">
          <button
            onClick={() => setFilter('all')}
            className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
              filter === 'all' 
                ? 'bg-blue-100 text-blue-700 border border-blue-200' 
                : 'bg-white text-gray-700 border hover:bg-gray-50'
            }`}
          >
            All Users
          </button>
          <button
            onClick={() => setFilter('pending')}
            className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
              filter === 'pending' 
                ? 'bg-orange-100 text-orange-700 border border-orange-200' 
                : 'bg-white text-gray-700 border hover:bg-gray-50'
            }`}
          >
            Pending Approvals
            {stats.pending > 0 && (
              <span className="ml-2 px-2 py-0.5 bg-orange-500 text-white text-xs rounded-full">
                {stats.pending}
              </span>
            )}
          </button>
        </div>
      </div>

      {/* Statistics Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <div className="rounded-lg border bg-gradient-to-br from-blue-500 to-blue-600 text-white p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-blue-100 text-sm font-medium">Total EV Owners</p>
              <p className="text-3xl font-bold">{stats.total}</p>
            </div>
            <Users className="h-8 w-8 text-blue-200" />
          </div>
        </div>

        <div className="rounded-lg border bg-gradient-to-br from-green-500 to-green-600 text-white p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-green-100 text-sm font-medium">Approved</p>
              <p className="text-3xl font-bold">{stats.approved}</p>
            </div>
            <UserCheck className="h-8 w-8 text-green-200" />
          </div>
        </div>

        <div className="rounded-lg border bg-gradient-to-br from-orange-500 to-orange-600 text-white p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-orange-100 text-sm font-medium">Pending</p>
              <p className="text-3xl font-bold">{stats.pending}</p>
            </div>
            <Clock className="h-8 w-8 text-orange-200" />
          </div>
        </div>

        <div className="rounded-lg border bg-gradient-to-br from-cyan-500 to-cyan-600 text-white p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-cyan-100 text-sm font-medium">Active</p>
              <p className="text-3xl font-bold">{stats.active}</p>
            </div>
            <CheckCircle className="h-8 w-8 text-cyan-200" />
          </div>
        </div>
      </div>

      {/* Search and Filters */}
      <div className="flex gap-4 items-center">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <input
            type="text"
            placeholder="Search by NIC, name, email..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="pl-10 pr-4 py-2 w-full border rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>
      </div>

      {/* EV Owners Table */}
      <div className="rounded-lg border bg-card shadow-sm">
        <div className="p-6 border-b">
          <div className="flex items-center justify-between">
            <h3 className="text-lg font-semibold">EV Owners ({filteredOwners.length})</h3>
          </div>
        </div>
        <div className="overflow-x-auto">
          {filteredOwners.length > 0 ? (
            <table className="w-full">
              <thead className="border-b bg-muted/50">
                <tr>
                  <th className="text-left p-4 font-medium text-sm">NIC</th>
                  <th className="text-left p-4 font-medium text-sm">Full Name</th>
                  <th className="text-left p-4 font-medium text-sm">Email</th>
                  <th className="text-left p-4 font-medium text-sm">Phone</th>
                  <th className="text-left p-4 font-medium text-sm">Status</th>
                  <th className="text-left p-4 font-medium text-sm">Approval</th>
                  <th className="text-left p-4 font-medium text-sm">Registered</th>
                  <th className="text-center p-4 font-medium text-sm">Actions</th>
                </tr>
              </thead>
              <tbody>
                {filteredOwners.map((owner, index) => (
                  <tr 
                    key={owner.id}
                    className={`border-b hover:bg-muted/30 ${!owner.isApproved ? 'bg-orange-50' : ''}`}
                  >
                    <td className="p-4">
                      <span className="font-mono text-sm font-medium">{owner.nic}</span>
                    </td>
                    <td className="p-4">
                      <div className="flex items-center gap-3">
                        <div className="w-8 h-8 rounded-full bg-blue-100 text-blue-600 flex items-center justify-center text-sm font-medium">
                          {owner.fullName.charAt(0).toUpperCase()}
                        </div>
                        <span className="font-medium">{owner.fullName}</span>
                      </div>
                    </td>
                    <td className="p-4">
                      <span className="text-sm text-muted-foreground">{owner.email}</span>
                    </td>
                    <td className="p-4">
                      <span className="text-sm text-muted-foreground">
                        {owner.phoneNumber || 'N/A'}
                      </span>
                    </td>
                    <td className="p-4">
                      <span className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${getStatusColor(owner)}`}>
                        {owner.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td className="p-4">
                      {owner.isApproved ? (
                        <div className="flex flex-col gap-1">
                          <span className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-green-100 text-green-800">
                            <Check className="h-3 w-3 mr-1" />
                            Approved
                          </span>
                          {owner.approvedAt && (
                            <span className="text-xs text-muted-foreground">
                              {formatDate(owner.approvedAt)}
                            </span>
                          )}
                        </div>
                      ) : (
                        <span className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-orange-100 text-orange-800">
                          <Clock className="h-3 w-3 mr-1" />
                          Pending
                        </span>
                      )}
                    </td>
                    <td className="p-4">
                      <span className="text-xs text-muted-foreground">
                        {formatDate(owner.registeredAt)}
                      </span>
                    </td>
                    <td className="p-4">
                      <div className="flex items-center justify-center gap-1">
                        <button
                          onClick={() => viewDetails(owner)}
                          className="p-2 rounded-lg hover:bg-muted transition-colors"
                          title="View Details"
                        >
                          <Eye className="h-4 w-4" />
                        </button>
                        
                        {!owner.isApproved ? (
                          <>
                            <button
                              onClick={() => handleApprove(owner)}
                              className="p-2 rounded-lg hover:bg-green-50 text-green-600 transition-colors"
                              title="Approve"
                            >
                              <Check className="h-4 w-4" />
                            </button>
                            <button
                              onClick={() => handleReject(owner)}
                              className="p-2 rounded-lg hover:bg-red-50 text-red-600 transition-colors"
                              title="Reject"
                            >
                              <X className="h-4 w-4" />
                            </button>
                          </>
                        ) : (
                          <button
                            onClick={() => handleToggleStatus(owner)}
                            className={`p-2 rounded-lg transition-colors ${
                              owner.isActive
                                ? 'hover:bg-orange-50 text-orange-600'
                                : 'hover:bg-green-50 text-green-600'
                            }`}
                            title={owner.isActive ? 'Deactivate' : 'Activate'}
                          >
                            {owner.isActive ? <Pause className="h-4 w-4" /> : <Play className="h-4 w-4" />}
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : (
            <div className="text-center py-12">
              <Users className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
              <h3 className="text-lg font-medium text-muted-foreground mb-2">
                {searchTerm || filter !== 'all' ? 'No matching EV owners found' : 'No EV owners found'}
              </h3>
              <p className="text-sm text-muted-foreground">
                {searchTerm || filter !== 'all' 
                  ? 'Try adjusting your search or filter criteria'
                  : 'EV owners will appear here once they register via the mobile app'
                }
              </p>
            </div>
          )}
        </div>
      </div>

      {/* Details Modal */}
      {showDetails && selectedOwner && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50" onClick={() => setShowDetails(false)}>
          <div className="bg-white rounded-lg shadow-xl max-w-2xl w-full mx-4" onClick={(e) => e.stopPropagation()}>
            <div className="p-6 border-b">
              <div className="flex items-center justify-between">
                <h2 className="text-xl font-semibold">EV Owner Details</h2>
                <button
                  onClick={() => setShowDetails(false)}
                  className="p-2 rounded-lg hover:bg-muted transition-colors"
                >
                  <X className="h-5 w-5" />
                </button>
              </div>
            </div>
            <div className="p-6 space-y-6">
              <div className="flex items-center gap-4">
                <div className="w-16 h-16 rounded-full bg-blue-100 text-blue-600 flex items-center justify-center text-xl font-bold">
                  {selectedOwner.fullName.charAt(0).toUpperCase()}
                </div>
                <div>
                  <h3 className="text-lg font-semibold">{selectedOwner.fullName}</h3>
                  <p className="text-muted-foreground">NIC: {selectedOwner.nic}</p>
                </div>
              </div>

              <div className="grid md:grid-cols-2 gap-6">
                <div className="space-y-4">
                  <div className="flex items-center gap-3">
                    <Mail className="h-5 w-5 text-muted-foreground" />
                    <div>
                      <p className="text-sm text-muted-foreground">Email</p>
                      <p className="font-medium">{selectedOwner.email}</p>
                    </div>
                  </div>
                  <div className="flex items-center gap-3">
                    <Phone className="h-5 w-5 text-muted-foreground" />
                    <div>
                      <p className="text-sm text-muted-foreground">Phone</p>
                      <p className="font-medium">{selectedOwner.phoneNumber || 'Not provided'}</p>
                    </div>
                  </div>
                  <div className="flex items-center gap-3">
                    <MapPin className="h-5 w-5 text-muted-foreground" />
                    <div>
                      <p className="text-sm text-muted-foreground">Address</p>
                      <p className="font-medium">{selectedOwner.address || 'Not provided'}</p>
                    </div>
                  </div>
                </div>

                <div className="space-y-4">
                  <div className="flex items-center gap-3">
                    <Calendar className="h-5 w-5 text-muted-foreground" />
                    <div>
                      <p className="text-sm text-muted-foreground">Registered</p>
                      <p className="font-medium">{formatDate(selectedOwner.registeredAt)}</p>
                    </div>
                  </div>
                  {selectedOwner.approvedAt && (
                    <div className="flex items-center gap-3">
                      <CheckCircle className="h-5 w-5 text-green-600" />
                      <div>
                        <p className="text-sm text-muted-foreground">Approved</p>
                        <p className="font-medium">{formatDate(selectedOwner.approvedAt)}</p>
                        {selectedOwner.approvedBy && (
                          <p className="text-sm text-muted-foreground">by {selectedOwner.approvedBy}</p>
                        )}
                      </div>
                    </div>
                  )}
                  {selectedOwner.lastLoginAt && (
                    <div className="flex items-center gap-3">
                      <Clock className="h-5 w-5 text-muted-foreground" />
                      <div>
                        <p className="text-sm text-muted-foreground">Last Login</p>
                        <p className="font-medium">{formatDate(selectedOwner.lastLoginAt)}</p>
                      </div>
                    </div>
                  )}
                </div>
              </div>

              <div className="flex gap-3 pt-6 border-t">
                {!selectedOwner.isApproved ? (
                  <>
                    <button
                      onClick={() => {
                        handleApprove(selectedOwner)
                        setShowDetails(false)
                      }}
                      className="flex-1 px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors"
                    >
                      <Check className="h-4 w-4 inline mr-2" />
                      Approve User
                    </button>
                    <button
                      onClick={() => {
                        handleReject(selectedOwner)
                        setShowDetails(false)
                      }}
                      className="flex-1 px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors"
                    >
                      <X className="h-4 w-4 inline mr-2" />
                      Reject User
                    </button>
                  </>
                ) : (
                  <button
                    onClick={() => {
                      handleToggleStatus(selectedOwner)
                      setShowDetails(false)
                    }}
                    className={`flex-1 px-4 py-2 text-white rounded-lg transition-colors ${
                      selectedOwner.isActive
                        ? 'bg-orange-600 hover:bg-orange-700'
                        : 'bg-green-600 hover:bg-green-700'
                    }`}
                  >
                    {selectedOwner.isActive ? (
                      <>
                        <Pause className="h-4 w-4 inline mr-2" />
                        Deactivate User
                      </>
                    ) : (
                      <>
                        <Play className="h-4 w-4 inline mr-2" />
                        Activate User
                      </>
                    )}
                  </button>
                )}
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}