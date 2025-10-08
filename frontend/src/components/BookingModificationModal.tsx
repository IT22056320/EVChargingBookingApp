import React, { useState, useEffect } from 'react'
import { X, Calendar, Clock, MapPin, AlertCircle, CheckCircle } from 'lucide-react'
import { 
  BookingResponse, 
  ChargingStation, 
  RequestBookingModificationDto,
  bookingApi,
  TimeSlotAvailability
} from '../services/bookingApi'
import toast from 'react-hot-toast'
import { format, parseISO, addDays } from 'date-fns'

interface BookingModificationModalProps {
  booking: BookingResponse
  userId: string
  isOpen: boolean
  onClose: () => void
  onSuccess: () => void
}

export function BookingModificationModal({ 
  booking, 
  userId, 
  isOpen, 
  onClose, 
  onSuccess 
}: BookingModificationModalProps) {
  const [stations, setStations] = useState<ChargingStation[]>([])
  const [formData, setFormData] = useState<RequestBookingModificationDto>({
    requestedStationId: booking.chargingStationId,
    requestedStartTime: booking.startTime,
    requestedEndTime: booking.endTime,
    requestedVehicleNumber: booking.vehicleNumber,
    requestReason: ''
  })
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [isCheckingAvailability, setIsCheckingAvailability] = useState(false)
  const [availabilityResult, setAvailabilityResult] = useState<TimeSlotAvailability | null>(null)
  const [hasCheckedAvailability, setHasCheckedAvailability] = useState(false)

  useEffect(() => {
    if (isOpen) {
      loadStations()
    }
  }, [isOpen])

  const loadStations = async () => {
    try {
      const stationsList = await bookingApi.getChargingStations()
      setStations(stationsList)
    } catch (error: any) {
      toast.error('Failed to load charging stations')
      console.error('Load stations error:', error)
    }
  }

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    const { name, value } = e.target
    setFormData(prev => ({ ...prev, [name]: value }))
    setHasCheckedAvailability(false) // Reset availability check when form changes
    setAvailabilityResult(null)
  }

  const checkAvailability = async () => {
    // Validate date/time first
    const startTime = new Date(formData.requestedStartTime)
    const endTime = new Date(formData.requestedEndTime)
    const now = new Date()
    const minDate = addDays(now, 7) // 7-day advance requirement

    if (startTime < minDate) {
      toast.error('New booking time must be at least 7 days in advance')
      return
    }

    if (startTime >= endTime) {
      toast.error('End time must be after start time')
      return
    }

    setIsCheckingAvailability(true)
    try {
      const result = await bookingApi.checkTimeSlotAvailability({
        chargingStationId: formData.requestedStationId,
        date: format(startTime, 'yyyy-MM-dd'),
        startTime: formData.requestedStartTime,
        endTime: formData.requestedEndTime,
        excludeBookingId: booking.id // Exclude current booking from conflict check
      })

      setAvailabilityResult(result)
      setHasCheckedAvailability(true)

      if (result.isAvailable) {
        toast.success('Time slot is available!')
      } else {
        toast.error(result.message || 'Time slot is not available')
      }
    } catch (error: any) {
      toast.error('Failed to check availability')
      console.error('Availability check error:', error)
    } finally {
      setIsCheckingAvailability(false)
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    // Validation
    if (formData.requestReason.length < 10 || formData.requestReason.length > 500) {
      toast.error('Reason must be between 10 and 500 characters')
      return
    }

    if (!hasCheckedAvailability) {
      toast.error('Please check availability before submitting')
      return
    }

    if (!availabilityResult?.isAvailable) {
      toast.error('Cannot submit - selected time slot is not available')
      return
    }

    setIsSubmitting(true)
    try {
      await bookingApi.requestBookingModification(booking.id, userId, formData)
      
      toast.success('Modification request submitted successfully! Waiting for admin approval.')
      onSuccess()
      onClose()
    } catch (error: any) {
      const errorMsg = error.displayMessage || error.message || 'Failed to submit modification request'
      toast.error(errorMsg)
      console.error('Submit modification error:', error)
    } finally {
      setIsSubmitting(false)
    }
  }

  const selectedStation = stations.find(s => s.id === formData.requestedStationId)
  const originalStation = stations.find(s => s.id === booking.chargingStationId)

  if (!isOpen) return null

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50 overflow-y-auto">
      <div className="bg-white rounded-lg shadow-xl max-w-3xl w-full max-h-[90vh] overflow-y-auto">
        {/* Header */}
        <div className="sticky top-0 bg-white border-b border-gray-200 px-6 py-4 flex items-center justify-between">
          <div>
            <h2 className="text-2xl font-bold text-gray-900">Request Booking Modification</h2>
            <p className="text-sm text-gray-600 mt-1">Booking #{booking.bookingNumber}</p>
          </div>
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600 transition-colors"
          >
            <X className="h-6 w-6" />
          </button>
        </div>

        {/* Content */}
        <form onSubmit={handleSubmit} className="p-6 space-y-6">
          {/* Current vs New Comparison */}
          <div className="grid md:grid-cols-2 gap-6">
            {/* Current Details */}
            <div className="bg-gray-50 rounded-lg p-4 border border-gray-200">
              <h3 className="text-lg font-semibold text-gray-900 mb-3 flex items-center gap-2">
                <AlertCircle className="h-5 w-5 text-gray-500" />
                Current Booking
              </h3>
              
              <div className="space-y-3 text-sm">
                <div className="flex items-start gap-2">
                  <MapPin className="h-4 w-4 text-gray-500 mt-0.5" />
                  <div>
                    <p className="font-medium text-gray-700">Station</p>
                    <p className="text-gray-600">{originalStation?.stationName || 'Loading...'}</p>
                  </div>
                </div>

                <div className="flex items-start gap-2">
                  <Calendar className="h-4 w-4 text-gray-500 mt-0.5" />
                  <div>
                    <p className="font-medium text-gray-700">Date & Time</p>
                    <p className="text-gray-600">
                      {format(parseISO(booking.startTime), 'MMM dd, yyyy')}
                    </p>
                    <p className="text-gray-600">
                      {format(parseISO(booking.startTime), 'hh:mm a')} - {format(parseISO(booking.endTime), 'hh:mm a')}
                    </p>
                  </div>
                </div>

                <div className="flex items-start gap-2">
                  <AlertCircle className="h-4 w-4 text-gray-500 mt-0.5" />
                  <div>
                    <p className="font-medium text-gray-700">Vehicle</p>
                    <p className="text-gray-600">{booking.vehicleNumber}</p>
                  </div>
                </div>
              </div>
            </div>

            {/* New Details Form */}
            <div className="bg-blue-50 rounded-lg p-4 border border-blue-200">
              <h3 className="text-lg font-semibold text-gray-900 mb-3 flex items-center gap-2">
                <CheckCircle className="h-5 w-5 text-blue-600" />
                Requested Changes
              </h3>

              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    <MapPin className="inline h-4 w-4 mr-1" />
                    Station *
                  </label>
                  <select
                    name="requestedStationId"
                    value={formData.requestedStationId}
                    onChange={handleInputChange}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
                    required
                  >
                    {stations.map(station => (
                      <option key={station.id} value={station.id}>
                        {station.stationName} - {station.location}
                      </option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    <Clock className="inline h-4 w-4 mr-1" />
                    Start Time *
                  </label>
                  <input
                    type="datetime-local"
                    name="requestedStartTime"
                    value={formData.requestedStartTime.slice(0, 16)}
                    onChange={handleInputChange}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
                    required
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    <Clock className="inline h-4 w-4 mr-1" />
                    End Time *
                  </label>
                  <input
                    type="datetime-local"
                    name="requestedEndTime"
                    value={formData.requestedEndTime.slice(0, 16)}
                    onChange={handleInputChange}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
                    required
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Vehicle Number *
                  </label>
                  <input
                    type="text"
                    name="requestedVehicleNumber"
                    value={formData.requestedVehicleNumber}
                    onChange={handleInputChange}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
                    required
                  />
                </div>
              </div>
            </div>
          </div>

          {/* Reason */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Reason for Modification * (10-500 characters)
            </label>
            <textarea
              name="requestReason"
              value={formData.requestReason}
              onChange={handleInputChange}
              rows={4}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
              placeholder="Please explain why you need to modify this booking..."
              minLength={10}
              maxLength={500}
              required
            />
            <p className="text-xs text-gray-500 mt-1">
              {formData.requestReason.length} / 500 characters
            </p>
          </div>

          {/* Availability Check */}
          <div className="border-t pt-4">
            <button
              type="button"
              onClick={checkAvailability}
              disabled={isCheckingAvailability}
              className="w-full bg-blue-600 hover:bg-blue-700 text-white font-medium py-2 px-4 rounded-lg transition-colors disabled:bg-gray-300 disabled:cursor-not-allowed flex items-center justify-center gap-2"
            >
              {isCheckingAvailability ? (
                <>
                  <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                  Checking Availability...
                </>
              ) : (
                <>
                  <CheckCircle className="h-5 w-5" />
                  Check Availability
                </>
              )}
            </button>

            {/* Availability Result */}
            {availabilityResult && (
              <div className={`mt-4 p-4 rounded-lg border ${
                availabilityResult.isAvailable 
                  ? 'bg-green-50 border-green-200' 
                  : 'bg-red-50 border-red-200'
              }`}>
                <div className="flex items-start gap-2">
                  {availabilityResult.isAvailable ? (
                    <CheckCircle className="h-5 w-5 text-green-600 mt-0.5" />
                  ) : (
                    <AlertCircle className="h-5 w-5 text-red-600 mt-0.5" />
                  )}
                  <div>
                    <p className={`font-medium ${
                      availabilityResult.isAvailable ? 'text-green-900' : 'text-red-900'
                    }`}>
                      {availabilityResult.isAvailable ? 'Available' : 'Not Available'}
                    </p>
                    <p className={`text-sm ${
                      availabilityResult.isAvailable ? 'text-green-700' : 'text-red-700'
                    }`}>
                      {availabilityResult.message}
                    </p>
                    
                    {availabilityResult.conflictingBookings && availabilityResult.conflictingBookings.length > 0 && (
                      <div className="mt-2">
                        <p className="text-sm font-medium text-red-800">Conflicting Bookings:</p>
                        <ul className="text-xs text-red-700 mt-1 space-y-1">
                          {availabilityResult.conflictingBookings.map((conflict, idx) => (
                            <li key={idx}>
                              {format(parseISO(conflict.startTime), 'hh:mm a')} - {format(parseISO(conflict.endTime), 'hh:mm a')} ({conflict.userName})
                            </li>
                          ))}
                        </ul>
                      </div>
                    )}
                  </div>
                </div>
              </div>
            )}
          </div>

          {/* Important Notice */}
          <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
            <div className="flex gap-2">
              <AlertCircle className="h-5 w-5 text-yellow-600 flex-shrink-0 mt-0.5" />
              <div className="text-sm text-yellow-800">
                <p className="font-medium">Important Notes:</p>
                <ul className="list-disc list-inside mt-1 space-y-1">
                  <li>Your modification request will be sent to the admin for approval</li>
                  <li>The current booking remains active until the modification is approved</li>
                  <li>New start time must be at least 7 days in advance</li>
                  <li>You will receive a notification once the admin reviews your request</li>
                </ul>
              </div>
            </div>
          </div>

          {/* Actions */}
          <div className="flex gap-3 justify-end border-t pt-4">
            <button
              type="button"
              onClick={onClose}
              disabled={isSubmitting}
              className="px-6 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 transition-colors disabled:opacity-50"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={isSubmitting || !hasCheckedAvailability || !availabilityResult?.isAvailable}
              className="px-6 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors disabled:bg-gray-300 disabled:cursor-not-allowed flex items-center gap-2"
            >
              {isSubmitting ? (
                <>
                  <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                  Submitting...
                </>
              ) : (
                <>
                  Submit Request
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
