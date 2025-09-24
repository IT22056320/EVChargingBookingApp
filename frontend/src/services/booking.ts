import { apiService } from './api'
import type {
  Booking,
  CreateBookingDto,
  UpdateBookingDto,
  UpdateBookingStatusDto,
  BookingSearchDto,
  BookingPagedResponse,
  BookingStats,
  TimeSlotCheckDto,
  TimeSlotAvailabilityDto,
  ApiResponse,
  ChargingStation
} from '@/types'

export class BookingService {
  async getAllBookings(searchParams?: BookingSearchDto): Promise<BookingPagedResponse> {
    return apiService.get<BookingPagedResponse>('/bookings', searchParams)
  }

  async getBookingById(id: string): Promise<Booking> {
    return apiService.get<Booking>(`/bookings/${id}`)
  }

  async getUserBookings(userId: string, page = 1, pageSize = 20): Promise<BookingPagedResponse> {
    return apiService.get<BookingPagedResponse>(`/bookings/user/${userId}`, {
      page,
      pageSize
    })
  }

  async getStationBookings(stationId: string, page = 1, pageSize = 20): Promise<BookingPagedResponse> {
    return apiService.get<BookingPagedResponse>(`/bookings/station/${stationId}`, {
      page,
      pageSize
    })
  }

  async createBooking(data: CreateBookingDto): Promise<Booking> {
    return apiService.post<Booking>('/bookings', data)
  }

  async updateBooking(id: string, data: UpdateBookingDto): Promise<Booking> {
    return apiService.put<Booking>(`/bookings/${id}`, data)
  }

  async updateBookingStatus(id: string, data: UpdateBookingStatusDto): Promise<ApiResponse> {
    return apiService.patch<ApiResponse>(`/bookings/${id}/status`, data)
  }

  async approveBooking(id: string, data: {
    isApproved: boolean
    approvedBy: string
    reason?: string
  }): Promise<ApiResponse> {
    return apiService.post<ApiResponse>(`/bookings/${id}/approve`, data)
  }

  async cancelBooking(id: string, data: {
    cancelledBy: string
    cancellationReason: string
  }): Promise<ApiResponse> {
    return apiService.post<ApiResponse>(`/bookings/${id}/cancel`, data)
  }

  async deleteBooking(id: string, deletedBy: string): Promise<ApiResponse> {
    return apiService.delete<ApiResponse>(`/bookings/${id}?deletedBy=${deletedBy}`)
  }

  async checkTimeSlotAvailability(data: TimeSlotCheckDto): Promise<TimeSlotAvailabilityDto> {
    return apiService.post<TimeSlotAvailabilityDto>('/bookings/check-availability', data)
  }

  async getBookingStatistics(fromDate?: string, toDate?: string): Promise<BookingStats> {
    const params: any = {}
    if (fromDate) params.fromDate = fromDate
    if (toDate) params.toDate = toDate
    
    return apiService.get<BookingStats>('/bookings/statistics', params)
  }

  async getQRCode(id: string): Promise<Blob> {
    const response = await fetch(`${apiService['api'].defaults.baseURL}/bookings/${id}/qrcode`)
    if (!response.ok) {
      throw new Error('Failed to fetch QR code')
    }
    return response.blob()
  }

  async validateQRCode(qrData: string): Promise<{
    isValid: boolean
    message: string
    booking?: Booking
  }> {
    return apiService.post('/bookings/validate-qr', qrData)
  }

  async bulkApproveBookings(bookingIds: string[], approvedBy: string): Promise<ApiResponse[]> {
    const promises = bookingIds.map(id => 
      this.approveBooking(id, { isApproved: true, approvedBy })
    )
    return Promise.all(promises)
  }

  async bulkRejectBookings(bookingIds: string[], rejectedBy: string, reason?: string): Promise<ApiResponse[]> {
    const promises = bookingIds.map(id => 
      this.approveBooking(id, { isApproved: false, approvedBy: rejectedBy, reason })
    )
    return Promise.all(promises)
  }
}

export class ChargingStationService {
  async getAllChargingStations(): Promise<ChargingStation[]> {
    return apiService.get<ChargingStation[]>('/chargingstations')
  }

  async getChargingStationById(id: string): Promise<ChargingStation> {
    return apiService.get<ChargingStation>(`/chargingstations/${id}`)
  }

  async getAvailableStations(date: string, startTime: string, endTime: string): Promise<ChargingStation[]> {
    return apiService.get<ChargingStation[]>('/chargingstations/available', {
      date,
      startTime,
      endTime
    })
  }
}

export const bookingService = new BookingService()
export const chargingStationService = new ChargingStationService()