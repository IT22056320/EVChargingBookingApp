import { useState, useEffect } from 'react'
import { useAuth } from '../providers/auth-provider'
import toast from '../utils/toast'
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
  X
} from 'lucide-react'

// Mock data - replace with real API calls
interface Booking {
  id: string
  evOwnerNIC: string
  evOwnerName: string
  evOwnerEmail: string
  stationId: string
  stationName: string
  stationLocation: string
  bookingDate: string
  startTime: string
  endTime: string
  duration: number // in hours
  status: 'pending' | 'confirmed' | 'completed' | 'cancelled' | 'no-show'
  createdAt: string
  vehicleType: string
  estimatedCost: number
  notes?: string
}

interface BookingStats {
  total: number
  pending: number
  confirmed: number
  completed: number
  cancelled: number
}

export function BookingsPage() {
  const { user } = useAuth()
  const [bookings, setBookings] = useState<Booking[]>([])
  const [filteredBookings, setFilteredBookings] = useState<Booking[]>([])
  const [stats, setStats] = useState<BookingStats>({ total: 0, pending: 0, confirmed: 0, completed: 0, cancelled: 0 })
  const [searchTerm, setSearchTerm] = useState('')
  const [statusFilter, setStatusFilter] = useState<'all' | 'pending' | 'confirmed' | 'completed' | 'cancelled'>('all')
  const [isLoading, setIsLoading] = useState(true)
  const [selectedBooking, setSelectedBooking] = useState<Booking | null>(null)
  const [showDetails, setShowDetails] = useState(false)

  useEffect(() => {
    loadBookings()
  }, [])

  useEffect(() => {
    applyFilters()
  }, [bookings, searchTerm, statusFilter])

  const loadBookings = async () => {
    setIsLoading(true)
    try {
      // Mock data - replace with real API call
      const mockBookings: Booking[] = [
        {
          id: '1',
          evOwnerNIC: '123456789V',
          evOwnerName: 'John Doe',
          evOwnerEmail: 'john@example.com',
          stationId: 'ST001',
          stationName: 'Downtown Charging Hub',
          stationLocation: 'Colombo 03',
          bookingDate: '2024-12-20',
          startTime: '10:00',
          endTime: '12:00',
          duration: 2,
          status: 'pending',
          createdAt: '2024-12-19T08:30:00Z',
          vehicleType: 'Tesla Model 3',
          estimatedCost: 500,
          notes: 'Fast charging requested'
        },
        {
          id: '2',
          evOwnerNIC: '987654321V',
          evOwnerName: 'Jane Smith',
          evOwnerEmail: 'jane@example.com',
          stationId: 'ST002',
          stationName: 'Mall Parking Station',
          stationLocation: 'Nugegoda',
          bookingDate: '2024-12-19',
          startTime: '14:00',
          endTime: '16:00',
          duration: 2,
          status: 'confirmed',
          createdAt: '2024-12-18T10:15:00Z',
          vehicleType: 'Nissan Leaf',
          estimatedCost: 400
        },
        {
          id: '3',
          evOwnerNIC: '456789123V',
          evOwnerName: 'Mike Johnson',
          evOwnerEmail: 'mike@example.com',
          stationId: 'ST001',
          stationName: 'Downtown Charging Hub',
          stationLocation: 'Colombo 03',
          bookingDate: '2024-12-18',
          startTime: '09:00',
          endTime: '11:00',
          duration: 2,
          status: 'completed',
          createdAt: '2024-12-17T16:45:00Z',
          vehicleType: 'BMW i3',
          estimatedCost: 450
        }
      ]

      setBookings(mockBookings)
      
      // Calculate stats
      const newStats = {
        total: mockBookings.length,
        pending: mockBookings.filter(b => b.status === 'pending').length,
        confirmed: mockBookings.filter(b => b.status === 'confirmed').length,
        completed: mockBookings.filter(b => b.status === 'completed').length,
        cancelled: mockBookings.filter(b => b.status === 'cancelled').length,
      }
      setStats(newStats)
      
      toast.success(`Loaded ${mockBookings.length} bookings`)
    } catch (error) {
      console.error('Failed to load bookings:', error)
      toast.error('Failed to load bookings')
    } finally {
      setIsLoading(false)
    }
  }

  const applyFilters = () => {
    let filtered = [...bookings]

    // Apply search filter
    if (searchTerm) {
      const term = searchTerm.toLowerCase()
      filtered = filtered.filter(booking => 
        booking.evOwnerNIC.toLowerCase().includes(term) ||
        booking.evOwnerName.toLowerCase().includes(term) ||
        booking.evOwnerEmail.toLowerCase().includes(term) ||
        booking.stationName.toLowerCase().includes(term) ||
        booking.stationLocation.toLowerCase().includes(term) ||
        booking.vehicleType.toLowerCase().includes(term)
      )
    }

    // Apply status filter
    if (statusFilter !== 'all') {
      filtered = filtered.filter(booking => booking.status === statusFilter)
    }

    // Sort by creation date (newest first)
    filtered.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())

    setFilteredBookings(filtered)
  }

  const handleUpdateStatus = async (booking: Booking, newStatus: Booking['status']) => {
    try {
      // Mock API call - replace with real implementation
      console.log(`Updating booking ${booking.id} status to ${newStatus}`)
      
      // Update local state
      setBookings(prev => prev.map(b => 
        b.id === booking.id 
          ? { ...b, status: newStatus }
          : b
      ))
      
      toast.success(`Booking ${newStatus} successfully`)
    } catch (error) {
      console.error('Failed to update booking:', error)
      toast.error('Failed to update booking')
    }
  }

  const viewDetails = (booking: Booking) => {
    setSelectedBooking(booking)
    setShowDetails(true)
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

  const getStatusColor = (status: Booking['status']) => {
    switch (status) {
      case 'pending':
        return 'bg-orange-100 text-orange-800 border-orange-200'
      case 'confirmed':
        return 'bg-blue-100 text-blue-800 border-blue-200'
      case 'completed':
        return 'bg-green-100 text-green-800 border-green-200'
      case 'cancelled':
        return 'bg-red-100 text-red-800 border-red-200'
      case 'no-show':
        return 'bg-gray-100 text-gray-800 border-gray-200'
      default:
        return 'bg-gray-100 text-gray-800 border-gray-200'
    }
  }

  const getStatusIcon = (status: Booking['status']) => {
    switch (status) {
      case 'pending':
        return <Clock className="h-3 w-3" />
      case 'confirmed':
        return <CheckCircle className="h-3 w-3" />
      case 'completed':
        return <Battery className="h-3 w-3" />
      case 'cancelled':
        return <XCircle className="h-3 w-3" />
      case 'no-show':
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
          {(['all', 'pending', 'confirmed', 'completed'] as const).map((filter) => (
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
              {filter !== 'all' && stats[filter] > 0 && (
                <span className="ml-2 px-2 py-0.5 bg-blue-500 text-white text-xs rounded-full">
                  {stats[filter]}
                </span>
              )}
            </button>
          ))}
        </div>
      </div>

      {/* Statistics Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-5">
        <div className="rounded-lg border bg-gradient-to-br from-blue-500 to-blue-600 text-white p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-blue-100 text-sm font-medium">Total Bookings</p>
              <p className="text-3xl font-bold">{stats.total}</p>
            </div>
            <Calendar className="h-8 w-8 text-blue-200" />
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
              <p className="text-cyan-100 text-sm font-medium">Confirmed</p>
              <p className="text-3xl font-bold">{stats.confirmed}</p>
            </div>
            <CheckCircle className="h-8 w-8 text-cyan-200" />
          </div>
        </div>

        <div className="rounded-lg border bg-gradient-to-br from-green-500 to-green-600 text-white p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-green-100 text-sm font-medium">Completed</p>
              <p className="text-3xl font-bold">{stats.completed}</p>
            </div>
            <Battery className="h-8 w-8 text-green-200" />
          </div>
        </div>

        <div className="rounded-lg border bg-gradient-to-br from-red-500 to-red-600 text-white p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-red-100 text-sm font-medium">Cancelled</p>
              <p className="text-3xl font-bold">{stats.cancelled}</p>
            </div>
            <XCircle className="h-8 w-8 text-red-200" />
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
                      <span className="font-mono text-sm font-medium">#{booking.id}</span>
                    </td>
                    <td className="p-4">
                      <div>
                        <p className="font-medium">{booking.evOwnerName}</p>
                        <p className="text-sm text-muted-foreground">{booking.evOwnerNIC}</p>
                      </div>
                    </td>
                    <td className="p-4">
                      <div className="flex items-center gap-2">
                        <MapPin className="h-4 w-4 text-muted-foreground" />
                        <div>
                          <p className="font-medium text-sm">{booking.stationName}</p>
                          <p className="text-xs text-muted-foreground">{booking.stationLocation}</p>
                        </div>
                      </div>
                    </td>
                    <td className="p-4">
                      <div>
                        <p className="font-medium text-sm">{formatDate(booking.bookingDate)}</p>
                        <p className="text-xs text-muted-foreground">{booking.startTime} - {booking.endTime}</p>
                      </div>
                    </td>
                    <td className="p-4">
                      <span className="text-sm">{booking.vehicleType}</span>
                    </td>
                    <td className="p-4">
                      <span className="text-sm">{booking.duration}h</span>
                    </td>
                    <td className="p-4">
                      <span className="font-medium text-sm">LKR {booking.estimatedCost}</span>
                    </td>
                    <td className="p-4">
                      <span className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${getStatusColor(booking.status)}`}>
                        {getStatusIcon(booking.status)}
                        <span className="ml-1">{booking.status.charAt(0).toUpperCase() + booking.status.slice(1)}</span>
                      </span>
                    </td>
                    <td className="p-4">
                      <div className="flex items-center justify-center gap-1">
                        <button
                          onClick={() => viewDetails(booking)}
                          className="p-2 rounded-lg hover:bg-muted transition-colors"
                          title="View Details"
                        >
                          <Eye className="h-4 w-4" />
                        </button>
                        
                        {booking.status === 'pending' && (
                          <>
                            <button
                              onClick={() => handleUpdateStatus(booking, 'confirmed')}
                              className="p-2 rounded-lg hover:bg-green-50 text-green-600 transition-colors"
                              title="Confirm Booking"
                            >
                              <Check className="h-4 w-4" />
                            </button>
                            <button
                              onClick={() => handleUpdateStatus(booking, 'cancelled')}
                              className="p-2 rounded-lg hover:bg-red-50 text-red-600 transition-colors"
                              title="Cancel Booking"
                            >
                              <X className="h-4 w-4" />
                            </button>
                          </>
                        )}
                        
                        {booking.status === 'confirmed' && (
                          <button
                            onClick={() => handleUpdateStatus(booking, 'completed')}
                            className="p-2 rounded-lg hover:bg-blue-50 text-blue-600 transition-colors"
                            title="Mark Complete"
                          >
                            <Battery className="h-4 w-4" />
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
                  <h3 className="text-lg font-semibold">Booking #{selectedBooking.id}</h3>
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
                      <p><span className="text-muted-foreground">Name:</span> {selectedBooking.evOwnerName}</p>
                      <p><span className="text-muted-foreground">NIC:</span> {selectedBooking.evOwnerNIC}</p>
                      <p><span className="text-muted-foreground">Email:</span> {selectedBooking.evOwnerEmail}</p>
                    </div>
                  </div>
                  <div>
                    <h4 className="font-medium mb-2">Vehicle Information</h4>
                    <div className="space-y-2 text-sm">
                      <p><span className="text-muted-foreground">Type:</span> {selectedBooking.vehicleType}</p>
                      <p><span className="text-muted-foreground">Duration:</span> {selectedBooking.duration} hours</p>
                      <p><span className="text-muted-foreground">Est. Cost:</span> LKR {selectedBooking.estimatedCost}</p>
                    </div>
                  </div>
                </div>

                <div className="space-y-4">
                  <div>
                    <h4 className="font-medium mb-2">Station Information</h4>
                    <div className="space-y-2 text-sm">
                      <p><span className="text-muted-foreground">Station:</span> {selectedBooking.stationName}</p>
                      <p><span className="text-muted-foreground">Location:</span> {selectedBooking.stationLocation}</p>
                      <p><span className="text-muted-foreground">Station ID:</span> {selectedBooking.stationId}</p>
                    </div>
                  </div>
                  <div>
                    <h4 className="font-medium mb-2">Booking Schedule</h4>
                    <div className="space-y-2 text-sm">
                      <p><span className="text-muted-foreground">Date:</span> {formatDate(selectedBooking.bookingDate)}</p>
                      <p><span className="text-muted-foreground">Time:</span> {selectedBooking.startTime} - {selectedBooking.endTime}</p>
                      <p><span className="text-muted-foreground">Status:</span> 
                        <span className={`ml-2 inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${getStatusColor(selectedBooking.status)}`}>
                          {getStatusIcon(selectedBooking.status)}
                          <span className="ml-1">{selectedBooking.status.charAt(0).toUpperCase() + selectedBooking.status.slice(1)}</span>
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
                {selectedBooking.status === 'pending' && (
                  <>
                    <button
                      onClick={() => {
                        handleUpdateStatus(selectedBooking, 'confirmed')
                        setShowDetails(false)
                      }}
                      className="flex-1 px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors"
                    >
                      <Check className="h-4 w-4 inline mr-2" />
                      Confirm Booking
                    </button>
                    <button
                      onClick={() => {
                        handleUpdateStatus(selectedBooking, 'cancelled')
                        setShowDetails(false)
                      }}
                      className="flex-1 px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors"
                    >
                      <X className="h-4 w-4 inline mr-2" />
                      Cancel Booking
                    </button>
                  </>
                )}
                
                {selectedBooking.status === 'confirmed' && (
                  <button
                    onClick={() => {
                      handleUpdateStatus(selectedBooking, 'completed')
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
    </div>
  )
}