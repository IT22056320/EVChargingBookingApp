import { useState, useEffect } from 'react'
import { useAuth } from '../providers/auth-provider'
import { useNotifications } from '../providers/notification-provider'
import { bookingApi, BookingResponse, BookingStatus, BookingStats } from '../services/bookingApi'
import { UserRole } from '../types'
import toast from '../utils/toast'
import { BookingModificationModal } from '../components/BookingModificationModal'
import { AdminUpdateBookingModal } from '../components/AdminUpdateBookingModal'
import { ModificationRequests } from '../components/ModificationRequests'
import { 
  Calendar, 
  Clock, 
  MapPin, 
  User, 
  Search, 
  Filter,
  CheckCircle,
  XCircle,
  AlertCircle,
  Battery,
  Zap,
  MoreVertical,
  Eye,
  Check,
  X,
  Plus,
  Edit,
  FileEdit,
  Trash2
} from 'lucide-react'

export function BookingsPage() {
  const { user } = useAuth()
  const { notifications } = useNotifications()
  const [bookings, setBookings] = useState<BookingResponse[]>([])
  const [filteredBookings, setFilteredBookings] = useState<BookingResponse[]>([])
  const [stats, setStats] = useState<BookingStats>({ 
    totalBookings: 0, 
    pendingBookings: 0, 
    approvedBookings: 0, 
    completedBookings: 0, 
    cancelledBookings: 0, 
    rejectedBookings: 0,
    totalRevenue: 0,
    averageBookingDuration: 0,
    dailyStats: []
  })
  const [searchTerm, setSearchTerm] = useState('')
  const [statusFilter, setStatusFilter] = useState<'all' | 'pending' | 'approved' | 'completed' | 'cancelled'>('all')
  const [isLoading, setIsLoading] = useState(true)
  const [selectedBooking, setSelectedBooking] = useState<BookingResponse | null>(null)
  const [showDetails, setShowDetails] = useState(false)
  const [currentPage, setCurrentPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [modifyingBooking, setModifyingBooking] = useState<BookingResponse | null>(null)
  const [showModificationModal, setShowModificationModal] = useState(false)
  const [activeTab, setActiveTab] = useState<'bookings' | 'modifications'>('bookings')

  useEffect(() => {
    loadBookings()
    loadBookingStats()
  }, [currentPage])

  useEffect(() => {
    applyFilters()
  }, [bookings, searchTerm, statusFilter])

  // Listen for new booking notifications and refresh the list
  useEffect(() => {
    const latestNotification = notifications[0]
    if (latestNotification && 
        latestNotification.title === 'New Booking Created' && 
        !latestNotification.isRead) {
      // Refresh bookings when a new booking is created
      loadBookings()
    }
  }, [notifications])

  const loadBookings = async () => {
    setIsLoading(true)
    try {
      const result = await bookingApi.getBookings({
        page: currentPage,
        pageSize: 50,
        sortBy: 'CreatedAt',
        sortDescending: true
      })
      
      // Role-based filtering: Station Operators only see bookings for their assigned station
      let filteredResult = result.bookings
      if (user?.role === UserRole.StationOperator) {
        // Get stationId from user profile (assigned by backoffice)
        const stationId = user.assignedStationId || localStorage.getItem('operatorStationId') || ''
        
        if (stationId) {
          // Save for future use
          localStorage.setItem('operatorStationId', stationId)
          
          filteredResult = result.bookings.filter(
            booking => booking.chargingStationId === stationId
          )
          console.log(`Station Operator: Filtered ${result.bookings.length} bookings to ${filteredResult.length} for station ${stationId}`)
        } else {
          console.warn('Station Operator has no assigned station')
          filteredResult = []
        }
      }
      
      setBookings(filteredResult)
      setTotalPages(result.totalPages)
      
      toast.success(`Loaded ${filteredResult.length} bookings`)
    } catch (error) {
      console.error('Failed to load bookings:', error)
      toast.error('Failed to load bookings')
      setBookings([]) // Set empty array on error
    } finally {
      setIsLoading(false)
    }
  }

  const loadBookingStats = async () => {
    try {
      const bookingStats = await bookingApi.getBookingStatistics()
      setStats(bookingStats)
    } catch (error) {
      console.error('Failed to load booking stats:', error)
      // Keep default stats on error
    }
  }

  const applyFilters = () => {
    let filtered = [...bookings]

    // Apply search filter
    if (searchTerm) {
      const term = searchTerm.toLowerCase()
      filtered = filtered.filter(booking => 
        booking.user?.nic.toLowerCase().includes(term) ||
        booking.user?.fullName.toLowerCase().includes(term) ||
        booking.user?.email.toLowerCase().includes(term) ||
        booking.chargingStation?.stationName.toLowerCase().includes(term) ||
        booking.chargingStation?.location.toLowerCase().includes(term) ||
        booking.vehicleType.toLowerCase().includes(term) ||
        booking.vehicleNumber.toLowerCase().includes(term)
      )
    }

    // Apply status filter
    if (statusFilter !== 'all') {
      const statusMap: Record<string, BookingStatus> = {
        'pending': BookingStatus.Pending,
        'approved': BookingStatus.Approved,
        'completed': BookingStatus.Completed,
        'cancelled': BookingStatus.Cancelled
      }
      
      filtered = filtered.filter(booking => booking.status === statusMap[statusFilter])
    }

    setFilteredBookings(filtered)
  }

  const handleUpdateStatus = async (booking: BookingResponse, newStatus: BookingStatus) => {
    if (!user?.fullName) {
      toast.error('User information not available')
      return
    }

    try {
      if (newStatus === BookingStatus.Approved) {
        await bookingApi.approveBooking(booking.id, true, user.fullName)
        toast.success('Booking approved successfully')
      } else if (newStatus === BookingStatus.Cancelled) {
        await bookingApi.cancelBooking(booking.id, user.fullName, 'Cancelled by administrator')
        toast.success('Booking cancelled successfully')
      } else {
        await bookingApi.updateBookingStatus(booking.id, newStatus, user.fullName)
        toast.success(`Booking status updated to ${bookingApi.getStatusDisplayName(newStatus)}`)
      }
      
      // Reload bookings
      await loadBookings()
      await loadBookingStats()
    } catch (error) {
      console.error('Failed to update booking:', error)
      toast.error('Failed to update booking')
    }
  }

  const viewDetails = (booking: BookingResponse) => {
    setSelectedBooking(booking)
    setShowDetails(true)
  }

  const handleModifyBooking = (booking: BookingResponse) => {
    setModifyingBooking(booking)
    setShowModificationModal(true)
  }

  const handleModificationSuccess = async () => {
    setShowModificationModal(false)
    setModifyingBooking(null)
    await loadBookings()
    await loadBookingStats()
  }

  const handleDeleteBooking = async (booking: BookingResponse) => {
    if (!user?.id) {
      toast.error('User information not available')
      return
    }

    // Confirm deletion
    const confirmed = window.confirm(
      `Are you sure you want to delete booking #${booking.bookingNumber}?\n\n` +
      `Customer: ${booking.user?.fullName || 'Unknown'}\n` +
      `Station: ${booking.chargingStation?.stationName || 'Unknown'}\n` +
      `Date: ${formatDate(booking.startTime)}\n\n` +
      `This action cannot be undone and the customer will be notified.`
    )

    if (!confirmed) return

    const deletionReason = window.prompt(
      'Please provide a reason for deleting this booking (10-500 characters):'
    )

    if (!deletionReason) {
      toast.error('Deletion cancelled - reason is required')
      return
    }

    if (deletionReason.length < 10 || deletionReason.length > 500) {
      toast.error('Reason must be between 10 and 500 characters')
      return
    }

    try {
      await bookingApi.adminDeleteBooking(booking.id, {
        deletedBy: user.id || user.email,
        deletionReason: deletionReason,
        notifyCustomer: true
      })

      toast.success('Booking deleted successfully. Customer has been notified.')
      await loadBookings()
      await loadBookingStats()
    } catch (error: any) {
      const errorMsg = error.displayMessage || error.message || 'Failed to delete booking'
      toast.error(errorMsg)
      console.error('Delete booking error:', error)
    }
  }

  const createNewBooking = () => {
    window.location.href = '/create-booking'
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    })
  }

  const formatDateTime = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    })
  }

  const formatTime = (dateString: string) => {
    return new Date(dateString).toLocaleTimeString('en-US', {
      hour: '2-digit',
      minute: '2-digit'
    })
  }

  const getStatusColor = (status: BookingStatus) => {
    switch (status) {
      case BookingStatus.Pending:
        return 'bg-orange-100 text-orange-800 border-orange-200'
      case BookingStatus.Approved:
        return 'bg-blue-100 text-blue-800 border-blue-200'
      case BookingStatus.InProgress:
        return 'bg-purple-100 text-purple-800 border-purple-200'
      case BookingStatus.Completed:
        return 'bg-green-100 text-green-800 border-green-200'
      case BookingStatus.Cancelled:
        return 'bg-red-100 text-red-800 border-red-200'
      case BookingStatus.Rejected:
        return 'bg-gray-100 text-gray-800 border-gray-200'
      case BookingStatus.NoShow:
      case BookingStatus.Missed:
        return 'bg-yellow-100 text-yellow-800 border-yellow-200'
      default:
        return 'bg-gray-100 text-gray-800 border-gray-200'
    }
  }

  const getStatusIcon = (status: BookingStatus) => {
    switch (status) {
      case BookingStatus.Pending:
        return <Clock className="h-3 w-3" />
      case BookingStatus.Approved:
        return <CheckCircle className="h-3 w-3" />
      case BookingStatus.InProgress:
        return <Zap className="h-3 w-3" />
      case BookingStatus.Completed:
        return <Battery className="h-3 w-3" />
      case BookingStatus.Cancelled:
        return <XCircle className="h-3 w-3" />
      case BookingStatus.Rejected:
        return <X className="h-3 w-3" />
      case BookingStatus.NoShow:
      case BookingStatus.Missed:
        return <AlertCircle className="h-3 w-3" />
      default:
        return <Clock className="h-3 w-3" />
    }
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
            <Calendar className="h-8 w-8 text-blue-600" />
            Booking Management
          </h1>
          <p className="text-muted-foreground">
            Manage EV charging station bookings and reservations
          </p>
        </div>
        <div className="flex gap-2">
          {activeTab === 'bookings' && (
            <>
              <button
                onClick={createNewBooking}
                className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors flex items-center gap-2"
              >
                <Plus className="h-4 w-4" />
                New Booking
              </button>
              {(['all', 'pending', 'approved', 'completed'] as const).map((filter) => (
                <button
                  key={filter}
                  onClick={() => setStatusFilter(filter)}
                  className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
                    statusFilter === filter 
                      ? 'bg-blue-100 text-blue-700 border border-blue-200' 
                      : 'bg-white text-gray-700 border hover:bg-gray-50'
                  }`}
                >
                  {filter.charAt(0).toUpperCase() + filter.slice(1)}
                  {filter !== 'all' && (
                    <span className="ml-2 px-2 py-0.5 bg-blue-500 text-white text-xs rounded-full">
                      {filter === 'pending' && stats.pendingBookings}
                      {filter === 'approved' && stats.approvedBookings}
                      {filter === 'completed' && stats.completedBookings}
                    </span>
                  )}
                </button>
              ))}
            </>
          )}
        </div>
      </div>

      {/* Tabs */}
      {user?.role === UserRole.Backoffice && (
        <div className="border-b border-gray-200">
          <nav className="-mb-px flex space-x-8">
            <button
              onClick={() => setActiveTab('bookings')}
              className={`${
                activeTab === 'bookings'
                  ? 'border-blue-500 text-blue-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
              } whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm flex items-center gap-2`}
            >
              <Calendar className="h-5 w-5" />
              All Bookings
              <span className="ml-2 px-2 py-0.5 bg-gray-100 text-gray-600 text-xs rounded-full">
                {bookings.length}
              </span>
            </button>
            <button
              onClick={() => setActiveTab('modifications')}
              className={`${
                activeTab === 'modifications'
                  ? 'border-blue-500 text-blue-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
              } whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm flex items-center gap-2`}
            >
              <FileEdit className="h-5 w-5" />
              Modification Requests
            </button>
          </nav>
        </div>
      )}

      {/* Content based on active tab */}
      {activeTab === 'modifications' ? (
        <ModificationRequests 
          onRequestProcessed={() => {
            loadBookings()
            loadBookingStats()
          }} 
        />
      ) : (
        <>

      {/* Statistics Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-6">
        <div className="rounded-lg border bg-gradient-to-br from-blue-500 to-blue-600 text-white p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-blue-100 text-sm font-medium">Total Bookings</p>
              <p className="text-3xl font-bold">{stats.totalBookings}</p>
            </div>
            <Calendar className="h-8 w-8 text-blue-200" />
          </div>
        </div>

        <div className="rounded-lg border bg-gradient-to-br from-orange-500 to-orange-600 text-white p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-orange-100 text-sm font-medium">Pending</p>
              <p className="text-3xl font-bold">{stats.pendingBookings}</p>
            </div>
            <Clock className="h-8 w-8 text-orange-200" />
          </div>
        </div>

        <div className="rounded-lg border bg-gradient-to-br from-cyan-500 to-cyan-600 text-white p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-cyan-100 text-sm font-medium">Approved</p>
              <p className="text-3xl font-bold">{stats.approvedBookings}</p>
            </div>
            <CheckCircle className="h-8 w-8 text-cyan-200" />
          </div>
        </div>

        <div className="rounded-lg border bg-gradient-to-br from-green-500 to-green-600 text-white p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-green-100 text-sm font-medium">Completed</p>
              <p className="text-3xl font-bold">{stats.completedBookings}</p>
            </div>
            <Battery className="h-8 w-8 text-green-200" />
          </div>
        </div>

        <div className="rounded-lg border bg-gradient-to-br from-red-500 to-red-600 text-white p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-red-100 text-sm font-medium">Cancelled</p>
              <p className="text-3xl font-bold">{stats.cancelledBookings}</p>
            </div>
            <XCircle className="h-8 w-8 text-red-200" />
          </div>
        </div>

        <div className="rounded-lg border bg-gradient-to-br from-purple-500 to-purple-600 text-white p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-purple-100 text-sm font-medium">Revenue</p>
              <p className="text-2xl font-bold">LKR {stats.totalRevenue.toFixed(0)}</p>
            </div>
            <Zap className="h-8 w-8 text-purple-200" />
          </div>
        </div>
      </div>

      {/* Search and Filters */}
      <div className="flex gap-4 items-center">
        <div className="relative flex-1 max-w-md">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <input
            type="text"
            placeholder="Search by NIC, name, station, vehicle..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="pl-10 pr-4 py-2 w-full border rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>
      </div>

      {/* Bookings Table */}
      <div className="rounded-lg border bg-card shadow-sm">
        <div className="p-6 border-b">
          <div className="flex items-center justify-between">
            <h3 className="text-lg font-semibold">Bookings ({filteredBookings.length})</h3>
          </div>
        </div>
        <div className="overflow-x-auto">
          {filteredBookings.length > 0 ? (
            <table className="w-full">
              <thead className="border-b bg-muted/50">
                <tr>
                  <th className="text-left p-4 font-medium text-sm">Booking ID</th>
                  <th className="text-left p-4 font-medium text-sm">Customer</th>
                  <th className="text-left p-4 font-medium text-sm">Station</th>
                  <th className="text-left p-4 font-medium text-sm">Date & Time</th>
                  <th className="text-left p-4 font-medium text-sm">Vehicle</th>
                  <th className="text-left p-4 font-medium text-sm">Duration</th>
                  <th className="text-left p-4 font-medium text-sm">Cost</th>
                  <th className="text-left p-4 font-medium text-sm">Status</th>
                  <th className="text-center p-4 font-medium text-sm">Actions</th>
                </tr>
              </thead>
              <tbody>
                {filteredBookings.map((booking) => (
                  <tr key={booking.id} className="border-b hover:bg-muted/30">
                    <td className="p-4">
                      <span className="font-mono text-sm font-medium">#{booking.bookingNumber}</span>
                    </td>
                    <td className="p-4">
                      <div>
                        <p className="font-medium">{booking.user?.fullName || 'Unknown User'}</p>
                        <p className="text-sm text-muted-foreground">{booking.user?.nic || 'No NIC'}</p>
                      </div>
                    </td>
                    <td className="p-4">
                      <div className="flex items-center gap-2">
                        <MapPin className="h-4 w-4 text-muted-foreground" />
                        <div>
                          <p className="font-medium text-sm">{booking.chargingStation?.stationName || 'Unknown Station'}</p>
                          <p className="text-xs text-muted-foreground">{booking.chargingStation?.location || 'Unknown Location'}</p>
                        </div>
                      </div>
                    </td>
                    <td className="p-4">
                      <div>
                        <p className="font-medium text-sm">{formatDate(booking.bookingDate)}</p>
                        <p className="text-xs text-muted-foreground">{formatTime(booking.startTime)} - {formatTime(booking.endTime)}</p>
                      </div>
                    </td>
                    <td className="p-4">
                      <div>
                        <p className="text-sm">{booking.vehicleType}</p>
                        <p className="text-xs text-muted-foreground">{booking.vehicleNumber}</p>
                      </div>
                    </td>
                    <td className="p-4">
                      <span className="text-sm">{Math.floor(booking.durationMinutes / 60)}h {booking.durationMinutes % 60}m</span>
                    </td>
                    <td className="p-4">
                      <span className="font-medium text-sm">LKR {booking.totalCost?.toFixed(2) || '0.00'}</span>
                    </td>
                    <td className="p-4">
                      <span className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${getStatusColor(booking.status)}`}>
                        {getStatusIcon(booking.status)}
                        <span className="ml-1">{bookingApi.getStatusDisplayName(booking.status)}</span>
                      </span>
                    </td>
                    <td className="p-4">
                      <div className="flex items-center justify-center gap-1">
                        {/* View Details - Always visible */}
                        <button
                          onClick={() => viewDetails(booking)}
                          className="p-2 rounded-lg hover:bg-gray-100 transition-colors"
                          title="View Details"
                        >
                          <Eye className="h-4 w-4" />
                        </button>
                        
                        {/* Admin Actions */}
                        {user?.role === UserRole.Backoffice && (
                          <>
                            {/* Edit/Update - Show for Pending or Approved */}
                            {(booking.status === BookingStatus.Pending || booking.status === BookingStatus.Approved) && (
                              <button
                                onClick={() => handleModifyBooking(booking)}
                                className="p-2 rounded-lg hover:bg-blue-50 text-blue-600 transition-colors"
                                title="Update Booking"
                              >
                                <Edit className="h-4 w-4" />
                              </button>
                            )}
                            
                            {/* Approve - Show for Pending only */}
                            {booking.status === BookingStatus.Pending && (
                              <button
                                onClick={() => handleUpdateStatus(booking, BookingStatus.Approved)}
                                className="p-2 rounded-lg hover:bg-green-50 text-green-600 transition-colors"
                                title="Approve Booking"
                              >
                                <Check className="h-4 w-4" />
                              </button>
                            )}
                            
                            {/* Delete - Show for all non-completed statuses */}
                            {booking.status !== BookingStatus.Completed && (
                              <button
                                onClick={() => handleDeleteBooking(booking)}
                                className="p-2 rounded-lg hover:bg-red-50 text-red-600 transition-colors"
                                title="Delete Booking"
                              >
                                <Trash2 className="h-4 w-4" />
                              </button>
                            )}
                          </>
                        )}
                        
                        {/* Customer/Station Operator Actions */}
                        {user?.role !== UserRole.Backoffice && booking.status === BookingStatus.Pending && (
                          <button
                            onClick={() => handleModifyBooking(booking)}
                            className="p-2 rounded-lg hover:bg-blue-50 text-blue-600 transition-colors"
                            title="Request Modification"
                          >
                            <Edit className="h-4 w-4" />
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
              <Calendar className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
              <h3 className="text-lg font-medium text-muted-foreground mb-2">
                {searchTerm || statusFilter !== 'all' ? 'No matching bookings found' : 'No bookings found'}
              </h3>
              <p className="text-sm text-muted-foreground">
                {searchTerm || statusFilter !== 'all' 
                  ? 'Try adjusting your search or filter criteria'
                  : 'Bookings will appear here when EV owners make reservations'
                }
              </p>
            </div>
          )}
        </div>
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between">
          <p className="text-sm text-muted-foreground">
            Page {currentPage} of {totalPages}
          </p>
          <div className="flex gap-2">
            <button
              onClick={() => setCurrentPage(prev => Math.max(1, prev - 1))}
              disabled={currentPage === 1}
              className="px-3 py-1 border rounded disabled:opacity-50 disabled:cursor-not-allowed hover:bg-muted"
            >
              Previous
            </button>
            <button
              onClick={() => setCurrentPage(prev => Math.min(totalPages, prev + 1))}
              disabled={currentPage === totalPages}
              className="px-3 py-1 border rounded disabled:opacity-50 disabled:cursor-not-allowed hover:bg-muted"
            >
              Next
            </button>
          </div>
        </div>
      )}

      {/* Details Modal */}
      {showDetails && selectedBooking && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50" onClick={() => setShowDetails(false)}>
          <div className="bg-white rounded-lg shadow-xl max-w-2xl w-full mx-4" onClick={(e) => e.stopPropagation()}>
            <div className="p-6 border-b">
              <div className="flex items-center justify-between">
                <h2 className="text-xl font-semibold">Booking Details</h2>
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
                  <Calendar className="h-8 w-8" />
                </div>
                <div>
                  <h3 className="text-lg font-semibold">Booking #{selectedBooking.bookingNumber}</h3>
                  <p className="text-muted-foreground">
                    Created: {formatDateTime(selectedBooking.createdAt)}
                  </p>
                </div>
              </div>

              <div className="grid md:grid-cols-2 gap-6">
                <div className="space-y-4">
                  <div>
                    <h4 className="font-medium mb-2">Customer Information</h4>
                    <div className="space-y-2 text-sm">
                      <p><span className="text-muted-foreground">Name:</span> {selectedBooking.user?.fullName || 'Unknown User'}</p>
                      <p><span className="text-muted-foreground">NIC:</span> {selectedBooking.user?.nic || 'No NIC'}</p>
                      <p><span className="text-muted-foreground">Email:</span> {selectedBooking.user?.email || 'No Email'}</p>
                    </div>
                  </div>
                  <div>
                    <h4 className="font-medium mb-2">Vehicle Information</h4>
                    <div className="space-y-2 text-sm">
                      <p><span className="text-muted-foreground">Type:</span> {selectedBooking.vehicleType}</p>
                      <p><span className="text-muted-foreground">Number:</span> {selectedBooking.vehicleNumber}</p>
                      <p><span className="text-muted-foreground">Duration:</span> {Math.floor(selectedBooking.durationMinutes / 60)}h {selectedBooking.durationMinutes % 60}m</p>
                      <p><span className="text-muted-foreground">Est. Cost:</span> LKR {selectedBooking.totalCost?.toFixed(2) || '0.00'}</p>
                    </div>
                  </div>
                </div>

                <div className="space-y-4">
                  <div>
                    <h4 className="font-medium mb-2">Station Information</h4>
                    <div className="space-y-2 text-sm">
                      <p><span className="text-muted-foreground">Station:</span> {selectedBooking.chargingStation?.stationName || 'Unknown Station'}</p>
                      <p><span className="text-muted-foreground">Location:</span> {selectedBooking.chargingStation?.location || 'Unknown Location'}</p>
                      <p><span className="text-muted-foreground">Station ID:</span> {selectedBooking.chargingStationId}</p>
                    </div>
                  </div>
                  <div>
                    <h4 className="font-medium mb-2">Booking Schedule</h4>
                    <div className="space-y-2 text-sm">
                      <p><span className="text-muted-foreground">Date:</span> {formatDate(selectedBooking.bookingDate)}</p>
                      <p><span className="text-muted-foreground">Time:</span> {formatTime(selectedBooking.startTime)} - {formatTime(selectedBooking.endTime)}</p>
                      <p><span className="text-muted-foreground">Status:</span> 
                        <span className={`ml-2 inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${getStatusColor(selectedBooking.status)}`}>
                          {getStatusIcon(selectedBooking.status)}
                          <span className="ml-1">{bookingApi.getStatusDisplayName(selectedBooking.status)}</span>
                        </span>
                      </p>
                    </div>
                  </div>
                </div>
              </div>

              {selectedBooking.notes && (
                <div>
                  <h4 className="font-medium mb-2">Notes</h4>
                  <p className="text-sm text-muted-foreground">{selectedBooking.notes}</p>
                </div>
              )}

              <div className="flex gap-3 pt-6 border-t">
                {selectedBooking.status === BookingStatus.Pending && (
                  <>
                    <button
                      onClick={() => {
                        handleUpdateStatus(selectedBooking, BookingStatus.Approved)
                        setShowDetails(false)
                      }}
                      className="flex-1 px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors"
                    >
                      <Check className="h-4 w-4 inline mr-2" />
                      Approve Booking
                    </button>
                    <button
                      onClick={() => {
                        handleUpdateStatus(selectedBooking, BookingStatus.Cancelled)
                        setShowDetails(false)
                      }}
                      className="flex-1 px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors"
                    >
                      <X className="h-4 w-4 inline mr-2" />
                      Cancel Booking
                    </button>
                  </>
                )}
                
                {selectedBooking.status === BookingStatus.Approved && (
                  <button
                    onClick={() => {
                      handleUpdateStatus(selectedBooking, BookingStatus.Completed)
                      setShowDetails(false)
                    }}
                    className="flex-1 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
                  >
                    <Battery className="h-4 w-4 inline mr-2" />
                    Mark as Complete
                  </button>
                )}
              </div>
            </div>
          </div>
        </div>
      )}
      </>
      )}

      {/* Booking Modification Modal */}
      {modifyingBooking && user && (
        <>
          {/* Admin uses direct update modal, customers use modification request modal */}
          {user.role === UserRole.Backoffice ? (
            <AdminUpdateBookingModal
              booking={modifyingBooking}
              adminId={user.id || user.email}
              isOpen={showModificationModal}
              onClose={() => {
                setShowModificationModal(false)
                setModifyingBooking(null)
              }}
              onSuccess={handleModificationSuccess}
            />
          ) : (
            <BookingModificationModal
              booking={modifyingBooking}
              userId={modifyingBooking.userId || user.id || ''}
              isOpen={showModificationModal}
              onClose={() => {
                setShowModificationModal(false)
                setModifyingBooking(null)
              }}
              onSuccess={handleModificationSuccess}
            />
          )}
        </>
      )}
    </div>
  )
}