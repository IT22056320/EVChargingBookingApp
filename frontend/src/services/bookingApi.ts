import { apiService } from './api'

// Interfaces matching the backend DTOs
export interface CreateBooking {
  userId: string
  chargingStationId: string
  bookingDate: string // ISO date string
  startTime: string // ISO datetime string
  endTime: string // ISO datetime string
  vehicleNumber: string
  vehicleType: string
  estimatedChargingTimeMinutes: number
  notes?: string
}

export interface BookingResponse {
  id: string
  bookingNumber: string
  userId: string
  chargingStationId: string
  bookingDate: string
  startTime: string
  endTime: string
  status: BookingStatus
  vehicleNumber: string
  vehicleType: string
  estimatedChargingTimeMinutes: number
  notes: string
  qrCode: string
  qrCodeGeneratedAt?: string
  createdAt: string
  modifiedAt?: string
  approvedAt?: string
  approvedBy: string
  completedAt?: string
  cancelledAt?: string
  cancelledBy: string
  cancellationReason: string
  rejectedAt?: string
  rejectedBy: string
  rejectionReason: string
  actualStartTime?: string
  actualEndTime?: string
  totalCost?: number
  energyConsumedKWh?: number
  user?: UserResponse
  chargingStation?: ChargingStationResponse
  isWithinBookingWindow: boolean
  canBeModified: boolean
  canBeCancelled: boolean
  isActive: boolean
  durationMinutes: number
}

export interface UserResponse {
  id: string
  nic: string
  fullName: string
  email: string
  phoneNumber: string
  address: string
  isActive: boolean
  isApproved: boolean
}

export interface ChargingStationResponse {
  id: string
  stationName: string
  location: string
  address: string
  connectorType: ConnectorType
  powerRatingKW: number
  pricePerKWh: number
  status: ChargingStationStatus
  description: string
  amenities: string[]
  operatingHours: string
  isAvailable: boolean
  maxBookingDurationMinutes: number
  coordinates?: {
    latitude: number
    longitude: number
  }
}

export enum BookingStatus {
  Pending = 0,
  Approved = 1,
  InProgress = 2,
  Completed = 3,
  Cancelled = 4,
  Rejected = 5,
  NoShow = 6
}

export enum ConnectorType {
  TypeA = 0,
  TypeB = 1,
  CHAdeMO = 2,
  CCS = 3,
  Tesla = 4
}

export enum ChargingStationStatus {
  Available = 0,
  Occupied = 1,
  OutOfService = 2,
  Maintenance = 3
}

export interface BookingSearchParams {
  userId?: string
  chargingStationId?: string
  status?: BookingStatus
  bookingDateFrom?: string
  bookingDateTo?: string
  createdFrom?: string
  createdTo?: string
  vehicleNumber?: string
  userNIC?: string
  userName?: string
  stationName?: string
  page?: number
  pageSize?: number
  sortBy?: string
  sortDescending?: boolean
}

export interface BookingPagedResponse {
  bookings: BookingResponse[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

export interface BookingStats {
  totalBookings: number
  pendingBookings: number
  approvedBookings: number
  completedBookings: number
  cancelledBookings: number
  rejectedBookings: number
  totalRevenue: number
  averageBookingDuration: number
  dailyStats: DailyBookingStats[]
}

export interface DailyBookingStats {
  date: string
  bookingCount: number
  revenue: number
}

export interface TimeSlotCheck {
  chargingStationId: string
  date: string
  startTime: string
  endTime: string
  excludeBookingId?: string
}

export interface TimeSlotAvailability {
  isAvailable: boolean
  message: string
  conflictingBookings: ConflictingBooking[]
}

export interface ConflictingBooking {
  bookingId: string
  startTime: string
  endTime: string
  status: BookingStatus
  userName: string
}

export interface ChargingStation {
  id: string
  stationName: string
  location: string
  address: string
  connectorType: ConnectorType
  powerRatingKW: number
  pricePerKWh: number
  status: ChargingStationStatus
  coordinates?: {
    latitude: number
    longitude: number
  }
  amenities?: string[]
  description?: string
}

class BookingApiService {
  // Create a new booking
  async createBooking(bookingData: CreateBooking): Promise<BookingResponse> {
    return await apiService.post<BookingResponse>('/Bookings', bookingData)
  }

  // Get booking by ID
  async getBookingById(id: string): Promise<BookingResponse> {
    return await apiService.get<BookingResponse>(`/Bookings/${id}`)
  }

  // Get bookings with search and filtering
  async getBookings(searchParams: BookingSearchParams = {}): Promise<BookingPagedResponse> {
    return await apiService.get<BookingPagedResponse>('/Bookings', searchParams)
  }

  // Get bookings for a specific user
  async getUserBookings(userId: string, page: number = 1, pageSize: number = 20): Promise<BookingPagedResponse> {
    return await apiService.get<BookingPagedResponse>(`/Bookings/user/${userId}`, { page, pageSize })
  }

  // Get bookings for a specific station
  async getStationBookings(stationId: string, page: number = 1, pageSize: number = 20): Promise<BookingPagedResponse> {
    return await apiService.get<BookingPagedResponse>(`/Bookings/station/${stationId}`, { page, pageSize })
  }

  // Update booking status
  async updateBookingStatus(id: string, status: BookingStatus, updatedBy?: string, reason?: string): Promise<void> {
    await apiService.patch(`/Bookings/${id}/status`, {
      status,
      updatedBy,
      reason
    })
  }

  // Approve a booking
  async approveBooking(id: string, isApproved: boolean, approvedBy: string, reason?: string): Promise<{ message: string; qrCode?: string; approved: boolean }> {
    return await apiService.post(`/Bookings/${id}/approve`, {
      isApproved,
      approvedBy,
      reason
    })
  }

  // Cancel a booking
  async cancelBooking(id: string, cancelledBy: string, cancellationReason: string): Promise<{ message: string }> {
    return await apiService.post(`/Bookings/${id}/cancel`, {
      cancelledBy,
      cancellationReason
    })
  }

  // Check time slot availability
  async checkTimeSlotAvailability(timeSlot: TimeSlotCheck): Promise<TimeSlotAvailability> {
    return await apiService.post<TimeSlotAvailability>('/Bookings/check-availability', timeSlot)
  }

  // Get booking statistics
  async getBookingStatistics(fromDate?: string, toDate?: string): Promise<BookingStats> {
    const params: any = {}
    if (fromDate) params.fromDate = fromDate
    if (toDate) params.toDate = toDate
    
    return await apiService.get<BookingStats>('/Bookings/statistics', params)
  }

  // Get QR code for booking
  async getQRCode(id: string): Promise<Blob> {
    const response = await fetch(`${import.meta.env.VITE_API_BASE_URL || 'http://localhost:5001/api'}/Bookings/${id}/qrcode`, {
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('authToken')}`
      }
    })
    
    if (!response.ok) {
      throw new Error('Failed to fetch QR code')
    }
    
    return await response.blob()
  }

  // Get all charging stations (for booking creation)
  async getChargingStations(): Promise<ChargingStation[]> {
    return await apiService.get('/ChargingStations')
  }

  // Get status display name
  getStatusDisplayName(status: BookingStatus): string {
    switch (status) {
      case BookingStatus.Pending: return 'Pending'
      case BookingStatus.Approved: return 'Approved'
      case BookingStatus.InProgress: return 'In Progress'
      case BookingStatus.Completed: return 'Completed'
      case BookingStatus.Cancelled: return 'Cancelled'
      case BookingStatus.Rejected: return 'Rejected'
      case BookingStatus.NoShow: return 'No Show'
      default: return 'Unknown'
    }
  }

  // Get connector type display name
  getConnectorTypeDisplayName(type: ConnectorType): string {
    switch (type) {
      case ConnectorType.TypeA: return 'Type A'
      case ConnectorType.TypeB: return 'Type B'
      case ConnectorType.CHAdeMO: return 'CHAdeMO'
      case ConnectorType.CCS: return 'CCS'
      case ConnectorType.Tesla: return 'Tesla'
      default: return 'Unknown'
    }
  }

  // Get station status display name
  getStationStatusDisplayName(status: ChargingStationStatus): string {
    switch (status) {
      case ChargingStationStatus.Available: return 'Available'
      case ChargingStationStatus.Occupied: return 'Occupied'
      case ChargingStationStatus.OutOfService: return 'Out of Service'
      case ChargingStationStatus.Maintenance: return 'Maintenance'
      default: return 'Unknown'
    }
  }
}

export const bookingApi = new BookingApiService()
export default bookingApi