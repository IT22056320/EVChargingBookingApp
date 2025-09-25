import React, { useEffect, useState } from 'react'
import { 
  TrendingUp, 
  Calendar, 
  DollarSign, 
  Users, 
  Clock,
  Battery,
  MapPin,
  Activity
} from 'lucide-react'

interface BookingStats {
  totalBookings: number
  totalRevenue: number
  averageSessionDuration: number
  totalEnergyDelivered: number
  bookingsByStatus: {
    pending: number
    approved: number
    completed: number
    cancelled: number
    rejected: number
  }
  topStations: Array<{
    id: string
    name: string
    bookingCount: number
    revenue: number
  }>
  recentTrends: Array<{
    date: string
    bookings: number
    revenue: number
  }>
}

interface BookingAnalyticsProps {
  dateRange?: {
    start: string
    end: string
  }
}

export function BookingAnalytics({ dateRange }: BookingAnalyticsProps) {
  const [stats, setStats] = useState<BookingStats | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    fetchAnalytics()
  }, [dateRange])

  const fetchAnalytics = async () => {
    setLoading(true)
    try {
      // This would be replaced with actual API calls
      const mockStats: BookingStats = {
        totalBookings: 1247,
        totalRevenue: 15680.50,
        averageSessionDuration: 142, // minutes
        totalEnergyDelivered: 3420.8, // kWh
        bookingsByStatus: {
          pending: 23,
          approved: 45,
          completed: 1089,
          cancelled: 67,
          rejected: 23
        },
        topStations: [
          { id: '1', name: 'Mall Parking Station', bookingCount: 234, revenue: 3450.20 },
          { id: '2', name: 'Highway Rest Stop', bookingCount: 198, revenue: 2890.15 },
          { id: '3', name: 'City Center Hub', bookingCount: 156, revenue: 2234.80 },
        ],
        recentTrends: [
          { date: '2025-09-19', bookings: 45, revenue: 670.50 },
          { date: '2025-09-20', bookings: 52, revenue: 780.20 },
          { date: '2025-09-21', bookings: 38, revenue: 560.80 },
          { date: '2025-09-22', bookings: 61, revenue: 912.40 },
          { date: '2025-09-23', bookings: 48, revenue: 724.30 },
          { date: '2025-09-24', bookings: 55, revenue: 825.60 },
          { date: '2025-09-25', bookings: 42, revenue: 634.20 },
        ]
      }

      setStats(mockStats)
    } catch (error) {
      console.error('Error fetching analytics:', error)
    } finally {
      setLoading(false)
    }
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center p-8">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500"></div>
        <span className="ml-2">Loading analytics...</span>
      </div>
    )
  }

  if (!stats) {
    return (
      <div className="p-8 text-center text-gray-500">
        Failed to load analytics data
      </div>
    )
  }

  const statusColors = {
    pending: 'bg-yellow-100 text-yellow-800',
    approved: 'bg-blue-100 text-blue-800',
    completed: 'bg-green-100 text-green-800',
    cancelled: 'bg-gray-100 text-gray-800',
    rejected: 'bg-red-100 text-red-800'
  }

  return (
    <div className="space-y-6">
      {/* Key Metrics */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <div className="bg-white p-6 rounded-lg border shadow-sm">
          <div className="flex items-center">
            <div className="p-2 bg-blue-100 rounded-lg">
              <Calendar className="h-6 w-6 text-blue-600" />
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-500">Total Bookings</p>
              <p className="text-2xl font-semibold text-gray-900">{stats.totalBookings.toLocaleString()}</p>
            </div>
          </div>
        </div>

        <div className="bg-white p-6 rounded-lg border shadow-sm">
          <div className="flex items-center">
            <div className="p-2 bg-green-100 rounded-lg">
              <DollarSign className="h-6 w-6 text-green-600" />
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-500">Total Revenue</p>
              <p className="text-2xl font-semibold text-gray-900">${stats.totalRevenue.toLocaleString()}</p>
            </div>
          </div>
        </div>

        <div className="bg-white p-6 rounded-lg border shadow-sm">
          <div className="flex items-center">
            <div className="p-2 bg-purple-100 rounded-lg">
              <Clock className="h-6 w-6 text-purple-600" />
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-500">Avg Session</p>
              <p className="text-2xl font-semibold text-gray-900">{stats.averageSessionDuration}min</p>
            </div>
          </div>
        </div>

        <div className="bg-white p-6 rounded-lg border shadow-sm">
          <div className="flex items-center">
            <div className="p-2 bg-orange-100 rounded-lg">
              <Battery className="h-6 w-6 text-orange-600" />
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-500">Energy Delivered</p>
              <p className="text-2xl font-semibold text-gray-900">{stats.totalEnergyDelivered.toLocaleString()} kWh</p>
            </div>
          </div>
        </div>
      </div>

      {/* Booking Status Distribution */}
      <div className="bg-white p-6 rounded-lg border shadow-sm">
        <h3 className="text-lg font-semibold mb-4 flex items-center gap-2">
          <Activity className="h-5 w-5" />
          Booking Status Distribution
        </h3>
        <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
          {Object.entries(stats.bookingsByStatus).map(([status, count]) => (
            <div key={status} className="text-center">
              <div className={`inline-flex px-3 py-1 rounded-full text-sm font-medium ${statusColors[status as keyof typeof statusColors]}`}>
                {status.charAt(0).toUpperCase() + status.slice(1)}
              </div>
              <p className="text-2xl font-bold text-gray-900 mt-2">{count}</p>
            </div>
          ))}
        </div>
      </div>

      {/* Top Performing Stations */}
      <div className="bg-white p-6 rounded-lg border shadow-sm">
        <h3 className="text-lg font-semibold mb-4 flex items-center gap-2">
          <MapPin className="h-5 w-5" />
          Top Performing Stations
        </h3>
        <div className="space-y-4">
          {stats.topStations.map((station, index) => (
            <div key={station.id} className="flex items-center justify-between p-4 bg-gray-50 rounded-lg">
              <div className="flex items-center gap-3">
                <div className="w-8 h-8 bg-blue-100 rounded-full flex items-center justify-center">
                  <span className="text-blue-600 font-semibold">{index + 1}</span>
                </div>
                <div>
                  <p className="font-medium text-gray-900">{station.name}</p>
                  <p className="text-sm text-gray-500">{station.bookingCount} bookings</p>
                </div>
              </div>
              <div className="text-right">
                <p className="font-semibold text-gray-900">${station.revenue.toLocaleString()}</p>
                <p className="text-sm text-gray-500">Revenue</p>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Recent Trends */}
      <div className="bg-white p-6 rounded-lg border shadow-sm">
        <h3 className="text-lg font-semibold mb-4 flex items-center gap-2">
          <TrendingUp className="h-5 w-5" />
          7-Day Trend
        </h3>
        <div className="space-y-2">
          {stats.recentTrends.map((trend, index) => (
            <div key={trend.date} className="flex items-center justify-between py-2">
              <div className="flex items-center gap-3">
                <span className="text-sm text-gray-500 w-20">
                  {new Date(trend.date).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}
                </span>
                <div className="flex items-center gap-4">
                  <span className="text-sm font-medium">{trend.bookings} bookings</span>
                  <span className="text-sm font-medium text-green-600">${trend.revenue}</span>
                </div>
              </div>
              <div className="w-24 h-2 bg-gray-200 rounded-full overflow-hidden">
                <div 
                  className="h-full bg-blue-500 rounded-full"
                  style={{ width: `${(trend.bookings / 70) * 100}%` }}
                ></div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}