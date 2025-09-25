// User Types
export interface User {
  id: string
  email: string
  fullName: string
  role: UserRole
  isActive: boolean
  createdAt: string
  lastLoginAt?: string
}

export enum UserRole {
  Backoffice = 0,
  StationOperator = 1
}

export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  success: boolean
  message: string
  user: User
}

// EV Owner Types
export interface EVOwner {
  id: string
  nic: string
  fullName: string
  email: string
  phoneNumber?: string
  address?: string
  isActive: boolean
  isApproved: boolean
  registeredAt: string
  approvedAt?: string
  approvedBy?: string
  lastLoginAt?: string
}

export interface RegisterEVOwnerRequest {
  nic: string
  fullName: string
  email: string
  password: string
  phoneNumber?: string
  address?: string
}

export interface ApprovalRequest {
  isApproved: boolean
  approvedBy?: string
}

// Booking Types
export enum BookingStatus {
  Pending = 0,
  Approved = 1,
  Rejected = 2,
  Completed = 3,
  Cancelled = 4,
  InProgress = 5
}

export enum VehicleType {
  Car = 0,
  Bus = 1,
  Van = 2,
  Truck = 3,
  Motorcycle = 4,
  Other = 5
}

export interface Booking {
  id: string
  bookingNumber: string
  userId: string
  chargingStationId: string
  bookingDate: string
  startTime: string
  endTime: string
  status: BookingStatus
  vehicleNumber: string
  vehicleType: VehicleType
  estimatedChargingTimeMinutes: number
  notes?: string
  qrCode?: string
  qrCodeGeneratedAt?: string
  createdAt: string
  modifiedAt?: string
  approvedAt?: string
  approvedBy?: string
  completedAt?: string
  cancelledAt?: string
  cancelledBy?: string
  cancellationReason?: string
  rejectedAt?: string
  rejectedBy?: string
  rejectionReason?: string
  actualStartTime?: string
  actualEndTime?: string
  totalCost?: number
  energyConsumedKWh?: number
  isWithinBookingWindow: boolean
  canBeModified: boolean
  canBeCancelled: boolean
  isActive: boolean
  durationMinutes: number
  user?: UserResponseDto
  chargingStation?: ChargingStationResponseDto
}

export interface UserResponseDto {
  id: string
  nic: string
  fullName: string
  email: string
  phoneNumber?: string
  address?: string
  isActive: boolean
  isApproved: boolean
}

export interface ChargingStationResponseDto {
  id: string
  stationName: string
  location: string
  address: string
  connectorType: string
  powerRatingKW: number
  pricePerKWh: number
  status: string
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

export interface CreateBookingDto {
  userId: string
  chargingStationId: string
  bookingDate: string
  startTime: string
  endTime: string
  vehicleNumber: string
  vehicleType: VehicleType
  estimatedChargingTimeMinutes: number
  notes?: string
}

export interface UpdateBookingDto {
  bookingDate?: string
  startTime?: string
  endTime?: string
  vehicleNumber?: string
  vehicleType?: VehicleType
  estimatedChargingTimeMinutes?: number
  notes?: string
  updatedBy: string
}

export interface UpdateBookingStatusDto {
  status: BookingStatus
  updatedBy: string
  reason?: string
}

export interface BookingSearchDto {
  userId?: string
  chargingStationId?: string
  status?: BookingStatus
  startDate?: string
  endDate?: string
  vehicleNumber?: string
  page?: number
  pageSize?: number
  sortBy?: string
  sortDirection?: 'asc' | 'desc'
}

export interface BookingPagedResponse {
  bookings: Booking[]
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
  todayBookings: number
  thisWeekBookings: number
  thisMonthBookings: number
  averageDurationMinutes: number
  totalEnergyConsumedKWh: number
  totalRevenue: number
}

export interface TimeSlotCheckDto {
  chargingStationId: string
  bookingDate: string
  startTime: string
  endTime: string
  excludeBookingId?: string
}

export interface TimeSlotAvailabilityDto {
  isAvailable: boolean
  conflictingBookings: Booking[]
  message: string
}

// Charging Station Types
export interface ChargingStation {
  id: string
  stationName: string
  location: string
  address: string
  connectorType: string
  powerRatingKW: number
  pricePerKWh: number
  status: ChargingStationStatus
  isActive: boolean
  createdAt: string
  lastMaintenanceDate?: string
  nextMaintenanceDate?: string
}

export enum ChargingStationStatus {
  Available = 0,
  Occupied = 1,
  Maintenance = 2,
  OutOfOrder = 3
}

// API Response Types
export interface ApiResponse<T = any> {
  success: boolean
  message: string
  data?: T
}

export interface PaginatedResponse<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

// Form Types
export interface LoginFormData {
  email: string
  password: string
}

export interface BookingFormData {
  userId: string
  chargingStationId: string
  bookingDate: string
  startTime: string
  endTime: string
  vehicleNumber: string
  vehicleType: VehicleType
  estimatedChargingTimeMinutes: number
  notes?: string
}

// Dashboard Types
export interface DashboardStats {
  totalBookings: number
  pendingBookings: number
  approvedBookings: number
  completedBookings: number
  totalRevenue: number
  totalEVOwners: number
  activeEVOwners: number
  pendingApprovals: number
}

export interface ChartData {
  name: string
  value: number
  fill?: string
}

// Filter Types
export interface BookingFilters {
  status?: BookingStatus[]
  dateRange?: {
    from: Date
    to: Date
  }
  chargingStationId?: string
  userId?: string
  vehicleNumber?: string
}

export interface EVOwnerFilters {
  isApproved?: boolean
  isActive?: boolean
  searchTerm?: string
}