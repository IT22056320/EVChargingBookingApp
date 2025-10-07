/*
 * File: operator-dashboard.tsx
 * Description: Station Operator Dashboard - Role-based dashboard for station operators
 * Author: EV Charging Team
 * Date: October 6, 2025
 * 
 * Features:
 * - View assigned charging station details
 * - Monitor today's bookings and active sessions
 * - Update slot availability
 * - View booking history and statistics
 * - Quick actions for common operator tasks
 */

import { useState, useEffect } from 'react'
import { useAuth } from '../providers/auth-provider'
import { bookingApi, BookingStatus, type CreateBooking, type ChargingStation } from '../services/bookingApi'
import toast from '../utils/toast'
import { 
  Activity, 
  Zap, 
  Clock, 
  CheckCircle, 
  Calendar,
  MapPin,
  Settings,
  Smartphone,
  QrCode,
  AlertCircle,
  Users,
  BarChart3,
  RefreshCw
} from 'lucide-react'

interface OperatorBooking {
  id: string;
  bookingNumber: string;
  chargingStationId: string;
  bookingDate: string;
  startTime: string;
  endTime: string;
  status: BookingStatus;
  vehicleNumber: string;
  vehicleType: string;
  estimatedChargingTimeMinutes: number;
  energyConsumedKWh?: number;
  totalCost?: number;
}

export function OperatorDashboardPage() {
  const { user } = useAuth()
  const [station, setStation] = useState<ChargingStation | null>(null)
  const [todaysBookings, setTodaysBookings] = useState<OperatorBooking[]>([])
  const [activeBookings, setActiveBookings] = useState<OperatorBooking[]>([])
  const [completedToday, setCompletedToday] = useState<OperatorBooking[]>([])
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    loadOperatorData()
  }, [])

  const loadOperatorData = async () => {
    setIsLoading(true)
    try {
      // Get station ID from user profile (assigned by backoffice)
      const stationId = user?.assignedStationId || localStorage.getItem('operatorStationId') || ''
      
      if (!stationId) {
        toast.error('No charging station assigned to your account')
        setIsLoading(false)
        return
      }
      
      // Save for consistency across pages
      localStorage.setItem('operatorStationId', stationId)
      
      // Fetch station details directly from API using assigned stationId
      try {
        const { stationsApi } = await import('../services/stations')
        const allStations = await stationsApi.getStations()
        const assignedStation = allStations.find((s: any) => s.id === stationId)
        
        if (assignedStation) {
          setStation({
            id: assignedStation.id,
            stationName: assignedStation.stationName,
            location: assignedStation.location,
            address: assignedStation.address,
            status: 0, // Default to Active
            connectorType: assignedStation.connectorType as any,
            powerRatingKW: assignedStation.powerRatingKW,
            pricePerKWh: 50 // Default price
          })
        } else {
          toast.error(`Station with ID ${stationId} not found`)
        }
      } catch (error) {
        console.error('Failed to fetch station details:', error)
        toast.error('Failed to load station information')
      }
      
      // Get all bookings (returns paginated response)
      const bookingsResponse = await bookingApi.getBookings()
      const bookings = bookingsResponse.bookings || []

      // Filter bookings for this station
      const stationBookings = bookings.filter((b: any) => b.chargingStationId === stationId) as OperatorBooking[]
      
      // Get today's date
      const today = new Date()
      today.setHours(0, 0, 0, 0)
      const todayStr = today.toISOString().split('T')[0]

      // Filter bookings
      const todays = stationBookings.filter((b: OperatorBooking) => 
        b.bookingDate.startsWith(todayStr)
      )
      const active = stationBookings.filter((b: OperatorBooking) => 
        b.status === BookingStatus.Approved
      )
      const completed = stationBookings.filter((b: OperatorBooking) => 
        b.status === BookingStatus.Completed &&
        b.bookingDate.startsWith(todayStr)
      )

      setTodaysBookings(todays)
      setActiveBookings(active)
      setCompletedToday(completed)

      toast.success('Dashboard loaded successfully')
    } catch (error) {
      console.error('Failed to load operator data:', error)
      toast.error('Failed to load dashboard data')
    } finally {
      setIsLoading(false)
    }
  }

  const handleUpdateSlotAvailability = async () => {
    if (!station) return

    try {
      // In production, this would call stationApi.updateStation
      toast.info('Slot availability update feature - use web station management')
      // const newStatus = station.status === 'Active' ? 'Inactive' : 'Active'
      // await stationApi.updateStation(station.id!, { ...station, status: newStatus })
      // loadOperatorData()
    } catch (error) {
      toast.error('Failed to update station status')
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

  if (!station) {
    return (
      <div className="space-y-6">
        <div className="rounded-lg border border-yellow-200 bg-yellow-50 p-6">
          <div className="flex items-center gap-3">
            <AlertCircle className="h-6 w-6 text-yellow-600" />
            <div>
              <h3 className="font-semibold text-yellow-900">No Station Assigned</h3>
              <p className="text-sm text-yellow-700">
                Please contact the back office to assign a charging station to your account.
              </p>
            </div>
          </div>
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
            Station Operator Dashboard
          </h1>
          <p className="text-muted-foreground">
            Welcome back, {user?.fullName || 'Operator'}
          </p>
        </div>
        <div className="flex items-center gap-3">
          <button
            onClick={loadOperatorData}
            className="inline-flex items-center px-4 py-2 border rounded-md hover:bg-gray-50"
          >
            <RefreshCw className="h-4 w-4 mr-2" />
            Refresh
          </button>
        </div>
      </div>

      {/* Station Info Card */}
      <div className="rounded-lg border bg-gradient-to-br from-blue-600 to-blue-700 text-white p-6">
        <div className="flex items-start justify-between">
          <div className="flex-1">
            <div className="flex items-center gap-2 mb-2">
              <Zap className="h-6 w-6" />
              <h2 className="text-2xl font-bold">{station.stationName}</h2>
            </div>
            <div className="flex items-center gap-4 text-blue-100">
              <span className="flex items-center gap-1">
                <MapPin className="h-4 w-4" />
                {station.address}, {station.location}
              </span>
              <span className="flex items-center gap-1">
                <Zap className="h-4 w-4" />
                {station.connectorType} - {station.powerRatingKW}kW
              </span>
            </div>
          </div>
          <div className="text-right">
            <div className={`inline-flex items-center px-3 py-1 rounded-full text-sm font-medium ${
              station.status === 0
                ? 'bg-green-500 bg-opacity-20 text-white' 
                : 'bg-red-500 bg-opacity-20 text-white'
            }`}>
              {station.status === 0 ? 'Active' : 'Inactive'}
            </div>
            <p className="text-blue-100 text-sm mt-2">
              Slots available
            </p>
          </div>
        </div>
      </div>

      {/* Statistics Cards */}
      <div className="grid gap-4 md:grid-cols-3">
        {/* Today's Bookings */}
        <div className="rounded-lg border bg-white p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-muted-foreground">Today's Bookings</p>
              <p className="text-3xl font-bold">{todaysBookings.length}</p>
              <p className="text-xs text-muted-foreground mt-1">Scheduled for today</p>
            </div>
            <Calendar className="h-8 w-8 text-blue-600" />
          </div>
        </div>

        {/* Active Sessions */}
        <div className="rounded-lg border bg-white p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-muted-foreground">Active Sessions</p>
              <p className="text-3xl font-bold text-green-600">{activeBookings.length}</p>
              <p className="text-xs text-muted-foreground mt-1">Currently charging</p>
            </div>
            <Zap className="h-8 w-8 text-green-600" />
          </div>
        </div>

        {/* Completed Today */}
        <div className="rounded-lg border bg-white p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-muted-foreground">Completed Today</p>
              <p className="text-3xl font-bold text-purple-600">{completedToday.length}</p>
              <p className="text-xs text-muted-foreground mt-1">Sessions finished</p>
            </div>
            <CheckCircle className="h-8 w-8 text-purple-600" />
          </div>
        </div>
      </div>

      {/* Quick Actions */}
      <div className="rounded-lg border bg-white p-6">
        <h3 className="text-lg font-semibold mb-4 flex items-center gap-2">
          <Settings className="h-5 w-5" />
          Quick Actions
        </h3>
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
          <button
            onClick={handleUpdateSlotAvailability}
            className="p-4 rounded-lg border hover:bg-gray-50 transition-colors text-left group"
          >
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-lg bg-blue-100 text-blue-600">
                <Settings className="h-5 w-5" />
              </div>
              <div>
                <p className="font-medium">Update Status</p>
                <p className="text-sm text-muted-foreground">
                  Manage Station
                </p>
              </div>
            </div>
          </button>

          <button
            className="p-4 rounded-lg border hover:bg-gray-50 transition-colors text-left group"
            onClick={() => window.open('/app-debug.apk', '_blank')}
          >
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-lg bg-green-100 text-green-600">
                <Smartphone className="h-5 w-5" />
              </div>
              <div>
                <p className="font-medium">Mobile App</p>
                <p className="text-sm text-muted-foreground">Download APK</p>
              </div>
            </div>
          </button>

          <button
            className="p-4 rounded-lg border hover:bg-gray-50 transition-colors text-left group"
            onClick={() => toast.info('Use mobile app to scan QR codes')}
          >
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-lg bg-purple-100 text-purple-600">
                <QrCode className="h-5 w-5" />
              </div>
              <div>
                <p className="font-medium">QR Scanner</p>
                <p className="text-sm text-muted-foreground">Open in mobile</p>
              </div>
            </div>
          </button>

          <button
            onClick={() => window.location.href = '/bookings'}
            className="p-4 rounded-lg border hover:bg-gray-50 transition-colors text-left group"
          >
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-lg bg-orange-100 text-orange-600">
                <BarChart3 className="h-5 w-5" />
              </div>
              <div>
                <p className="font-medium">View All</p>
                <p className="text-sm text-muted-foreground">All bookings</p>
              </div>
            </div>
          </button>
        </div>
      </div>

      {/* Active Bookings List */}
      <div className="rounded-lg border bg-white">
        <div className="p-6 border-b">
          <h3 className="text-lg font-semibold flex items-center gap-2">
            <Users className="h-5 w-5" />
            Active Bookings ({activeBookings.length})
          </h3>
        </div>
        <div className="divide-y">
          {activeBookings.length === 0 ? (
            <div className="p-8 text-center text-muted-foreground">
              <Clock className="h-12 w-12 mx-auto mb-3 opacity-50" />
              <p>No active bookings at the moment</p>
            </div>
          ) : (
            activeBookings.map((booking) => (
              <div key={booking.id} className="p-4 hover:bg-gray-50">
                <div className="flex items-center justify-between">
                  <div className="flex-1">
                    <div className="flex items-center gap-2">
                      <span className="font-semibold">{booking.bookingNumber}</span>
                      <span className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-green-100 text-green-800">
                        Active
                      </span>
                    </div>
                    <div className="text-sm text-muted-foreground mt-1">
                      {booking.vehicleNumber} • {booking.vehicleType}
                    </div>
                    <div className="text-sm text-muted-foreground">
                      {new Date(booking.startTime).toLocaleTimeString()} - {new Date(booking.endTime).toLocaleTimeString()}
                    </div>
                  </div>
                  <div className="text-right">
                    <p className="text-sm font-medium">{booking.estimatedChargingTimeMinutes} min</p>
                    <p className="text-xs text-muted-foreground">Estimated time</p>
                  </div>
                </div>
              </div>
            ))
          )}
        </div>
      </div>

      {/* Completed Today */}
      <div className="rounded-lg border bg-white">
        <div className="p-6 border-b">
          <h3 className="text-lg font-semibold flex items-center gap-2">
            <CheckCircle className="h-5 w-5" />
            Completed Today ({completedToday.length})
          </h3>
        </div>
        <div className="divide-y">
          {completedToday.length === 0 ? (
            <div className="p-8 text-center text-muted-foreground">
              <CheckCircle className="h-12 w-12 mx-auto mb-3 opacity-50" />
              <p>No sessions completed today yet</p>
            </div>
          ) : (
            completedToday.slice(0, 5).map((booking) => (
              <div key={booking.id} className="p-4">
                <div className="flex items-center justify-between">
                  <div className="flex-1">
                    <div className="flex items-center gap-2">
                      <span className="font-semibold">{booking.bookingNumber}</span>
                      <span className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-gray-100 text-gray-800">
                        Completed
                      </span>
                    </div>
                    <div className="text-sm text-muted-foreground mt-1">
                      {booking.vehicleNumber} • {booking.energyConsumedKWh?.toFixed(2) || 'N/A'} kWh
                    </div>
                  </div>
                  <div className="text-right">
                    <p className="text-sm font-medium">LKR {booking.totalCost?.toFixed(2) || '0.00'}</p>
                    <p className="text-xs text-muted-foreground">Total cost</p>
                  </div>
                </div>
              </div>
            ))
          )}
        </div>
      </div>
    </div>
  )
}
