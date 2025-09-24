import { useState, useEffect } from 'react'
import { useAuth } from '../providers/auth-provider'
import { bookingApi, CreateBooking, ChargingStation, BookingStatus } from '../services/bookingApi'
import { dashboardApi } from '../services/dashboardApi'
import toast from '../utils/toast'
import { 
  Calendar, 
  Clock, 
  MapPin, 
  Car, 
  Zap, 
  Plus, 
  ArrowLeft,
  CheckCircle,
  AlertCircle,
  Battery,
  Info
} from 'lucide-react'

export function CreateBookingPage() {
  const { user } = useAuth()
  const [chargingStations, setChargingStations] = useState<ChargingStation[]>([])
  const [evOwners, setEVOwners] = useState<any[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [availabilityChecking, setAvailabilityChecking] = useState(false)
  const [availabilityResult, setAvailabilityResult] = useState<{ isAvailable: boolean; message: string } | null>(null)

  const [formData, setFormData] = useState<CreateBooking>({
    userId: '',
    chargingStationId: '',
    bookingDate: '',
    startTime: '',
    endTime: '',
    vehicleNumber: '',
    vehicleType: '',
    estimatedChargingTimeMinutes: 60,
    notes: ''
  })

  useEffect(() => {
    loadInitialData()
  }, [])

  useEffect(() => {
    if (formData.chargingStationId && formData.bookingDate && formData.startTime && formData.endTime) {
      checkAvailability()
    }
  }, [formData.chargingStationId, formData.bookingDate, formData.startTime, formData.endTime])

  const loadInitialData = async () => {
    setIsLoading(true)
    try {
      const [stations, owners] = await Promise.all([
        bookingApi.getChargingStations(),
        dashboardApi.getEVOwners()
      ])
      
      setChargingStations(stations)
      setEVOwners(owners.filter(owner => owner.isApproved && owner.isActive))
      
      // Set default date to today
      const today = new Date()
      today.setHours(0, 0, 0, 0)
      setFormData(prev => ({
        ...prev,
        bookingDate: today.toISOString().split('T')[0]
      }))
      
    } catch (error) {
      console.error('Failed to load initial data:', error)
      toast.error('Failed to load required data')
    } finally {
      setIsLoading(false)
    }
  }

  const checkAvailability = async () => {
    if (!formData.chargingStationId || !formData.bookingDate || !formData.startTime || !formData.endTime) {
      return
    }

    setAvailabilityChecking(true)
    try {
      const startDateTime = new Date(`${formData.bookingDate}T${formData.startTime}:00`)
      const endDateTime = new Date(`${formData.bookingDate}T${formData.endTime}:00`)

      const result = await bookingApi.checkTimeSlotAvailability({
        chargingStationId: formData.chargingStationId,
        date: formData.bookingDate,
        startTime: startDateTime.toISOString(),
        endTime: endDateTime.toISOString()
      })

      setAvailabilityResult(result)
    } catch (error) {
      console.error('Failed to check availability:', error)
      setAvailabilityResult({ isAvailable: false, message: 'Failed to check availability' })
    } finally {
      setAvailabilityChecking(false)
    }
  }

  const handleInputChange = (field: keyof CreateBooking, value: string | number) => {
    setFormData(prev => ({ ...prev, [field]: value }))
    
    // Reset availability when relevant fields change
    if (['chargingStationId', 'bookingDate', 'startTime', 'endTime'].includes(field)) {
      setAvailabilityResult(null)
    }

    // Calculate estimated charging time when time slots change
    if (field === 'startTime' || field === 'endTime') {
      const start = field === 'startTime' ? value as string : formData.startTime
      const end = field === 'endTime' ? value as string : formData.endTime
      
      if (start && end) {
        const startTime = new Date(`2000-01-01T${start}:00`)
        const endTime = new Date(`2000-01-01T${end}:00`)
        const diffMs = endTime.getTime() - startTime.getTime()
        const diffMinutes = Math.max(0, diffMs / (1000 * 60))
        
        setFormData(prev => ({
          ...prev,
          estimatedChargingTimeMinutes: diffMinutes
        }))
      }
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    
    if (!availabilityResult?.isAvailable) {
      toast.error('Please select an available time slot')
      return
    }

    setIsSubmitting(true)
    try {
      const startDateTime = new Date(`${formData.bookingDate}T${formData.startTime}:00`)
      const endDateTime = new Date(`${formData.bookingDate}T${formData.endTime}:00`)

      const bookingData: CreateBooking = {
        ...formData,
        startTime: startDateTime.toISOString(),
        endTime: endDateTime.toISOString()
      }

      const result = await bookingApi.createBooking(bookingData)
      
      toast.success('Booking created successfully!')
      
      // Reset form
      setFormData({
        userId: '',
        chargingStationId: '',
        bookingDate: new Date().toISOString().split('T')[0],
        startTime: '',
        endTime: '',
        vehicleNumber: '',
        vehicleType: '',
        estimatedChargingTimeMinutes: 60,
        notes: ''
      })
      setAvailabilityResult(null)
      
      // Redirect to bookings page after a short delay
      setTimeout(() => {
        window.location.href = '/bookings'
      }, 2000)
      
    } catch (error) {
      console.error('Failed to create booking:', error)
      toast.error('Failed to create booking. Please try again.')
    } finally {
      setIsSubmitting(false)
    }
  }

  const goBack = () => {
    window.history.back()
  }

  const getMinDate = () => {
    return new Date().toISOString().split('T')[0]
  }

  const getMinTime = () => {
    const now = new Date()
    const selectedDate = new Date(formData.bookingDate)
    const today = new Date()
    today.setHours(0, 0, 0, 0)
    selectedDate.setHours(0, 0, 0, 0)
    
    // If selected date is today, set minimum time to current time + 1 hour
    if (selectedDate.getTime() === today.getTime()) {
      now.setHours(now.getHours() + 1)
      return now.toTimeString().slice(0, 5)
    }
    
    return '06:00' // Default minimum time for future dates
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
    <div className="max-w-4xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <button
          onClick={goBack}
          className="p-2 rounded-lg border hover:bg-muted transition-colors"
        >
          <ArrowLeft className="h-5 w-5" />
        </button>
        <div>
          <h1 className="text-3xl font-bold tracking-tight flex items-center gap-3">
            <Plus className="h-8 w-8 text-blue-600" />
            Create New Booking
          </h1>
          <p className="text-muted-foreground">
            Schedule a new EV charging session
          </p>
        </div>
      </div>

      <form onSubmit={handleSubmit} className="space-y-6">
        <div className="grid lg:grid-cols-2 gap-6">
          {/* Left Column - Basic Information */}
          <div className="space-y-6">
            <div className="rounded-lg border bg-card p-6">
              <h3 className="text-lg font-semibold mb-4 flex items-center gap-2">
                <Car className="h-5 w-5 text-blue-600" />
                Customer & Vehicle Information
              </h3>
              
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium mb-2">Customer *</label>
                  <select
                    value={formData.userId}
                    onChange={(e) => handleInputChange('userId', e.target.value)}
                    className="w-full p-3 border rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                    required
                  >
                    <option value="">Select Customer</option>
                    {evOwners.map(owner => (
                      <option key={owner.id} value={owner.id}>
                        {owner.fullName} ({owner.nic}) - {owner.email}
                      </option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="block text-sm font-medium mb-2">Vehicle Number *</label>
                  <input
                    type="text"
                    value={formData.vehicleNumber}
                    onChange={(e) => handleInputChange('vehicleNumber', e.target.value)}
                    placeholder="e.g., WP ABC-1234"
                    className="w-full p-3 border rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                    required
                    maxLength={20}
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium mb-2">Vehicle Type</label>
                  <input
                    type="text"
                    value={formData.vehicleType}
                    onChange={(e) => handleInputChange('vehicleType', e.target.value)}
                    placeholder="e.g., Tesla Model 3, Nissan Leaf"
                    className="w-full p-3 border rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
              </div>
            </div>

            <div className="rounded-lg border bg-card p-6">
              <h3 className="text-lg font-semibold mb-4 flex items-center gap-2">
                <MapPin className="h-5 w-5 text-green-600" />
                Charging Station
              </h3>
              
              <div>
                <label className="block text-sm font-medium mb-2">Select Station *</label>
                <select
                  value={formData.chargingStationId}
                  onChange={(e) => handleInputChange('chargingStationId', e.target.value)}
                  className="w-full p-3 border rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                  required
                >
                  <option value="">Choose a charging station</option>
                  {chargingStations.map(station => (
                    <option key={station.id} value={station.id}>
                      {station.stationName} - {station.location} ({bookingApi.getConnectorTypeDisplayName(station.connectorType)}, {station.powerRatingKW}kW)
                    </option>
                  ))}
                </select>
                
                {formData.chargingStationId && (
                  <div className="mt-3 p-3 bg-muted rounded-lg">
                    {(() => {
                      const station = chargingStations.find(s => s.id === formData.chargingStationId)
                      return station ? (
                        <div className="space-y-1 text-sm">
                          <p><span className="font-medium">Address:</span> {station.address}</p>
                          <p><span className="font-medium">Power Rating:</span> {station.powerRatingKW} kW</p>
                          <p><span className="font-medium">Price:</span> LKR {station.pricePerKWh}/kWh</p>
                          <p><span className="font-medium">Connector:</span> {bookingApi.getConnectorTypeDisplayName(station.connectorType)}</p>
                          {station.amenities && (
                            <p><span className="font-medium">Amenities:</span> {station.amenities.join(', ')}</p>
                          )}
                        </div>
                      ) : null
                    })()}
                  </div>
                )}
              </div>
            </div>
          </div>

          {/* Right Column - Date & Time */}
          <div className="space-y-6">
            <div className="rounded-lg border bg-card p-6">
              <h3 className="text-lg font-semibold mb-4 flex items-center gap-2">
                <Calendar className="h-5 w-5 text-purple-600" />
                Date & Time
              </h3>
              
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium mb-2">Booking Date *</label>
                  <input
                    type="date"
                    value={formData.bookingDate}
                    onChange={(e) => handleInputChange('bookingDate', e.target.value)}
                    min={getMinDate()}
                    className="w-full p-3 border rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                    required
                  />
                </div>

                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium mb-2">Start Time *</label>
                    <input
                      type="time"
                      value={formData.startTime}
                      onChange={(e) => handleInputChange('startTime', e.target.value)}
                      min={getMinTime()}
                      className="w-full p-3 border rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                      required
                    />
                  </div>

                  <div>
                    <label className="block text-sm font-medium mb-2">End Time *</label>
                    <input
                      type="time"
                      value={formData.endTime}
                      onChange={(e) => handleInputChange('endTime', e.target.value)}
                      min={formData.startTime}
                      className="w-full p-3 border rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                      required
                    />
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium mb-2">Estimated Duration</label>
                  <div className="flex items-center gap-2 p-3 bg-muted rounded-lg">
                    <Clock className="h-4 w-4 text-muted-foreground" />
                    <span>{Math.floor(formData.estimatedChargingTimeMinutes / 60)}h {formData.estimatedChargingTimeMinutes % 60}m</span>
                  </div>
                </div>
              </div>
            </div>

            {/* Availability Check */}
            {formData.chargingStationId && formData.bookingDate && formData.startTime && formData.endTime && (
              <div className="rounded-lg border bg-card p-6">
                <h3 className="text-lg font-semibold mb-4 flex items-center gap-2">
                  <CheckCircle className="h-5 w-5 text-green-600" />
                  Availability Check
                </h3>
                
                {availabilityChecking ? (
                  <div className="flex items-center gap-2 text-muted-foreground">
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-blue-600"></div>
                    Checking availability...
                  </div>
                ) : availabilityResult ? (
                  <div className={`flex items-start gap-3 p-3 rounded-lg ${
                    availabilityResult.isAvailable 
                      ? 'bg-green-50 border border-green-200' 
                      : 'bg-red-50 border border-red-200'
                  }`}>
                    {availabilityResult.isAvailable ? (
                      <CheckCircle className="h-5 w-5 text-green-600 mt-0.5" />
                    ) : (
                      <AlertCircle className="h-5 w-5 text-red-600 mt-0.5" />
                    )}
                    <div>
                      <p className={`font-medium ${
                        availabilityResult.isAvailable ? 'text-green-800' : 'text-red-800'
                      }`}>
                        {availabilityResult.isAvailable ? 'Available' : 'Not Available'}
                      </p>
                      <p className={`text-sm ${
                        availabilityResult.isAvailable ? 'text-green-700' : 'text-red-700'
                      }`}>
                        {availabilityResult.message}
                      </p>
                    </div>
                  </div>
                ) : (
                  <div className="flex items-center gap-2 text-muted-foreground">
                    <Info className="h-4 w-4" />
                    Select all required fields to check availability
                  </div>
                )}
              </div>
            )}

            {/* Estimated Cost */}
            {formData.chargingStationId && formData.estimatedChargingTimeMinutes > 0 && (
              <div className="rounded-lg border bg-card p-6">
                <h3 className="text-lg font-semibold mb-4 flex items-center gap-2">
                  <Battery className="h-5 w-5 text-yellow-600" />
                  Estimated Cost
                </h3>
                
                {(() => {
                  const station = chargingStations.find(s => s.id === formData.chargingStationId)
                  if (!station) return null
                  
                  const estimatedKWh = (formData.estimatedChargingTimeMinutes / 60) * (station.powerRatingKW * 0.8) // 80% efficiency
                  const estimatedCost = estimatedKWh * station.pricePerKWh
                  
                  return (
                    <div className="space-y-2 text-sm">
                      <div className="flex justify-between">
                        <span>Duration:</span>
                        <span>{Math.floor(formData.estimatedChargingTimeMinutes / 60)}h {formData.estimatedChargingTimeMinutes % 60}m</span>
                      </div>
                      <div className="flex justify-between">
                        <span>Estimated Energy:</span>
                        <span>{estimatedKWh.toFixed(1)} kWh</span>
                      </div>
                      <div className="flex justify-between">
                        <span>Rate:</span>
                        <span>LKR {station.pricePerKWh}/kWh</span>
                      </div>
                      <hr />
                      <div className="flex justify-between font-semibold">
                        <span>Estimated Cost:</span>
                        <span>LKR {estimatedCost.toFixed(2)}</span>
                      </div>
                    </div>
                  )
                })()}
              </div>
            )}
          </div>
        </div>

        {/* Notes */}
        <div className="rounded-lg border bg-card p-6">
          <h3 className="text-lg font-semibold mb-4">Additional Notes</h3>
          <textarea
            value={formData.notes}
            onChange={(e) => handleInputChange('notes', e.target.value)}
            placeholder="Any special requests or additional information..."
            rows={3}
            maxLength={500}
            className="w-full p-3 border rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
          />
          <p className="text-xs text-muted-foreground mt-1">{formData.notes.length}/500 characters</p>
        </div>

        {/* Submit Button */}
        <div className="flex gap-4 pt-6 border-t">
          <button
            type="button"
            onClick={goBack}
            className="px-6 py-3 border rounded-lg hover:bg-muted transition-colors"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={isSubmitting || !availabilityResult?.isAvailable}
            className="flex-1 px-6 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:bg-gray-400 disabled:cursor-not-allowed transition-colors flex items-center justify-center gap-2"
          >
            {isSubmitting ? (
              <>
                <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                Creating Booking...
              </>
            ) : (
              <>
                <Plus className="h-4 w-4" />
                Create Booking
              </>
            )}
          </button>
        </div>
      </form>
    </div>
  )
}