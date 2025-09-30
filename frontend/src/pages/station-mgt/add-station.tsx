import React, { useState, useEffect, useRef } from 'react'
import Swal from 'sweetalert2'
import { 
  MapPin, 
  Plus, 
  ArrowLeft,
  Zap,
  Navigation,
  Building2,
  Info
} from 'lucide-react'

const connectorTypes = [
  { value: 'Type1', label: 'Type 1 (J1772)' },
  { value: 'Type2', label: 'Type 2 (Mennekes)' },
  { value: 'CHAdeMO', label: 'CHAdeMO' },
  { value: 'CCS', label: 'CCS (Combined Charging System)' },
  { value: 'Tesla', label: 'Tesla Supercharger' },
]

type StationForm = {
  stationName: string;
  address: string;
  location: string;
  latitude: string;
  longitude: string;
  connectorType: string;
  powerRatingKW: number;
  pricePerKWh: string;
  totalSlots: string;
  amenities: string;
  operatingHours: string;
  contactNumber: string;
};

export default function AddStationPage() {
  const [form, setForm] = useState<StationForm>({
    stationName: '',
    address: '',
    location: '',
    latitude: '',
    longitude: '',
    connectorType: 'Type2',
    powerRatingKW: 0,
    pricePerKWh: '',
    totalSlots: '',
    amenities: '',
    operatingHours: '24/7',
    contactNumber: '',
  })

  const [isSubmitting, setIsSubmitting] = useState(false)
  const [mapLoaded, setMapLoaded] = useState(false)
  const mapRef = useRef<HTMLDivElement>(null)
  const googleMapRef = useRef<any>(null)
  const markerRef = useRef<any>(null)

  const googleMapsApiKey = import.meta.env.VITE_GOOGLE_MAPS_API_KEY

  useEffect(() => {
    const script = document.createElement('script')
    script.src = `https://maps.googleapis.com/maps/api/js?key=${googleMapsApiKey}`
    script.async = true
    script.defer = true
    script.onload = () => setMapLoaded(true)
    document.head.appendChild(script)

    return () => {
      document.head.removeChild(script)
    }
  }, [])

  useEffect(() => {
    if (mapLoaded && mapRef.current && window.google) {
      const defaultCenter = { lat: 6.9271, lng: 79.8612 }
      const center = form.latitude && form.longitude 
        ? { lat: parseFloat(form.latitude), lng: parseFloat(form.longitude) }
        : defaultCenter

      googleMapRef.current = new window.google.maps.Map(mapRef.current, {
        center,
        zoom: 13,
        streetViewControl: false,
        mapTypeControl: false,
      })

      googleMapRef.current.addListener('click', async (e: any) => {
        const lat = e.latLng.lat()
        const lng = e.latLng.lng()
        const address = await fetchAddressFromLatLng(lat, lng)
        setForm(f => ({
          ...f,
          latitude: lat.toString(),
          longitude: lng.toString()
          // Do NOT set address here
        }))

        if (markerRef.current) {
          markerRef.current.setMap(null)
        }

        markerRef.current = new window.google.maps.Marker({
          position: { lat, lng },
          map: googleMapRef.current,
          animation: window.google.maps.Animation.DROP,
        })
      })

      if (form.latitude && form.longitude) {
        markerRef.current = new window.google.maps.Marker({
          position: center,
          map: googleMapRef.current,
        })
      }
    }
  }, [mapLoaded])

  useEffect(() => {
    if (googleMapRef.current && form.latitude && form.longitude) {
      const lat = parseFloat(form.latitude)
      const lng = parseFloat(form.longitude)
      
      if (markerRef.current) {
        markerRef.current.setMap(null)
      }

      markerRef.current = new window.google.maps.Marker({
        position: { lat, lng },
        map: googleMapRef.current,
        animation: window.google.maps.Animation.DROP,
      })

      googleMapRef.current.panTo({ lat, lng })
    }
  }, [form.latitude, form.longitude])

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target
    setForm(f => ({ ...f, [name]: value }))
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setIsSubmitting(true)
    try {
      // Prepare payload for API
      const payload = {
        stationName: form.stationName,
        address: form.address,
        location: form.location,
        latitude: parseFloat(form.latitude),
        longitude: parseFloat(form.longitude),
        connectorType: form.connectorType,
        powerRatingKW: form.powerRatingKW,
        totalSlots: parseInt(form.totalSlots)
      }
      // Dynamically import stationsApi to avoid circular import issues
      const { stationsApi } = await import('../../services/stations')
      await stationsApi.createStation(payload)
      await Swal.fire({
        icon: 'success',
        title: 'Station Created!',
        text: 'The new charging station was added successfully.',
        confirmButtonText: 'OK',
      })
      window.location.href = '/stations'
    } catch (err) {
      Swal.fire({
        icon: 'error',
        title: 'Error',
        text: 'Failed to create station. Please try again.',
      })
    }
    setIsSubmitting(false)
  }

  const goBack = () => {
    window.history.back()
  }

  const fetchAddressFromLatLng = async (lat: number, lng: number) => {
    if (!window.google) return ''
    const geocoder = new window.google.maps.Geocoder()
    return new Promise<string>((resolve) => {
      geocoder.geocode({ location: { lat, lng } }, (results, status) => {
        if (status === 'OK' && results && results[0]) {
          resolve(results[0].formatted_address)
        } else {
          resolve('')
        }
      })
    })
  }

  const handleGetCurrentLocation = () => {
    if (navigator.geolocation) {
      navigator.geolocation.getCurrentPosition(
        async (position) => {
          const lat = position.coords.latitude
          const lng = position.coords.longitude
          // Optionally, fetch address using geocoder as shown previously
          let address = ''
          if (window.google) {
            const geocoder = new window.google.maps.Geocoder()
            geocoder.geocode({ location: { lat, lng } }, (results, status) => {
              if (status === 'OK' && results && results[0]) {
                address = results[0].formatted_address
              }
              setForm(f => ({
                ...f,
                latitude: lat.toString(),
                longitude: lng.toString(),
              }))
              if (googleMapRef.current) {
                googleMapRef.current.setCenter({ lat, lng })
                if (markerRef.current) markerRef.current.setMap(null)
                markerRef.current = new window.google.maps.Marker({
                  position: { lat, lng },
                  map: googleMapRef.current,
                  animation: window.google.maps.Animation.DROP,
                })
              }
            })
          }
        },
        (error) => {
          alert('Unable to fetch your location.')
        }
      )
    } else {
      alert('Geolocation is not supported by your browser.')
    }
  }

  return (
    <div className="max-w-4xl mx-auto space-y-6 p-6">
      <div className="flex items-center gap-4">
        <button
          onClick={goBack}
          className="p-2 rounded-lg border border-gray-300 hover:bg-gray-50 transition-colors"
        >
          <ArrowLeft className="h-5 w-5" />
        </button>
        <div>
          <h1 className="text-3xl font-bold tracking-tight flex items-center gap-3">
            <Plus className="h-8 w-8 text-blue-600" />
            Create Charging Station
          </h1>
          <p className="text-gray-600">
            Add a new EV charging station to the network
          </p>
        </div>
      </div>

      <div className="space-y-6">
        <div className="grid lg:grid-cols-2 gap-6">
          <div className="space-y-6">
            <div className="rounded-lg border border-gray-200 bg-white p-6 shadow-sm">
              <h3 className="text-lg font-semibold mb-4 flex items-center gap-2">
                <Building2 className="h-5 w-5 text-blue-600" />
                Station Information
              </h3>
              
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium mb-2">Station Name *</label>
                  <input
                    type="text"
                    name="stationName"
                    value={form.stationName}
                    onChange={handleChange}
                    placeholder="e.g., Downtown Charging Hub"
                    className="w-full p-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                    required
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium mb-2">Location Name *</label>
                  <input
                    type="text"
                    name="location"
                    value={form.location}
                    onChange={handleChange}
                    placeholder="e.g., Colombo Fort"
                    className="w-full p-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                    required
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium mb-2">Full Address *</label>
                  <textarea
                    name="address"
                    value={form.address}
                    onChange={handleChange}
                    placeholder="Enter complete street address"
                    rows={3}
                    className="w-full p-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent resize-none"
                    required
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium mb-2">Operating Hours</label>
                  <input
                    type="text"
                    name="operatingHours"
                    value={form.operatingHours}
                    onChange={handleChange}
                    placeholder="e.g., 24/7 or 8:00 AM - 10:00 PM"
                    className="w-full p-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                  />
                </div>
              </div>
            </div>

            <div className="rounded-lg border border-gray-200 bg-white p-6 shadow-sm">
              <h3 className="text-lg font-semibold mb-4 flex items-center gap-2">
                <Zap className="h-5 w-5 text-yellow-600" />
                Technical Specifications
              </h3>
              
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium mb-2">Connector Type *</label>
                  <select
                    name="connectorType"
                    value={form.connectorType}
                    onChange={handleChange}
                    className="w-full p-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                    required
                  >
                    {connectorTypes.map(ct => (
                      <option key={ct.value} value={ct.value}>{ct.label}</option>
                    ))}
                  </select>
                </div>

                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium mb-2">Power Rating (kW) *</label>
                    <input
                      type="number"
                      name="powerRatingKW"
                      value={form.powerRatingKW}
                      onChange={handleChange}
                      placeholder="e.g., 50"
                      min="1"
                      step="0.1"
                      className="w-full p-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                      required
                    />
                  </div>

                  <div>
                    <label className="block text-sm font-medium mb-2">Total Slots *</label>
                    <input
                      type="number"
                      name="totalSlots"
                      value={form.totalSlots}
                      onChange={handleChange}
                      placeholder="e.g., 4"
                      min="1"
                      className="w-full p-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                      required
                    />
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium mb-2">Amenities</label>
                  <input
                    type="text"
                    name="amenities"
                    value={form.amenities}
                    onChange={handleChange}
                    placeholder="e.g., WiFi, Restroom, Cafe (comma-separated)"
                    className="w-full p-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                  />
                  <p className="text-xs text-gray-500 mt-1">Separate multiple amenities with commas</p>
                </div>
              </div>
            </div>
          </div>

          <div className="space-y-6">
            <div className="rounded-lg border border-gray-200 bg-white p-6 shadow-sm">
              <h3 className="text-lg font-semibold mb-4 flex items-center gap-2">
                <Navigation className="h-5 w-5 text-green-600" />
                Location Coordinates
              </h3>
              
              <div className="space-y-4">
                <div className="p-3 bg-blue-50 border border-blue-200 rounded-lg flex items-start gap-2">
                  <Info className="h-5 w-5 text-blue-600 mt-0.5 flex-shrink-0" />
                  <p className="text-sm text-blue-800">
                    Click on the map below to set the exact location of your charging station
                  </p>
                </div>

                <div className="rounded-lg overflow-hidden border border-gray-300">
                  {mapLoaded ? (
                    <div ref={mapRef} style={{ width: '100%', height: '400px' }} />
                  ) : (
                    <div className="w-full h-96 flex items-center justify-center bg-gray-100">
                      <div className="text-center">
                        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto mb-2"></div>
                        <p className="text-gray-600">Loading map...</p>
                      </div>
                    </div>
                  )}
                </div>

                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium mb-2">Latitude</label>
                    <input
                      type="number"
                      name="latitude"
                      value={form.latitude}
                      onChange={handleChange}
                      placeholder="Click map to set"
                      step="any"
                      className="w-full p-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent bg-gray-50"
                      readOnly
                    />
                  </div>

                  <div>
                    <label className="block text-sm font-medium mb-2">Longitude</label>
                    <input
                      type="number"
                      name="longitude"
                      value={form.longitude}
                      onChange={handleChange}
                      placeholder="Click map to set"
                      step="any"
                      className="w-full p-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent bg-gray-50"
                      readOnly
                    />
                  </div>
                </div>

                {form.latitude && form.longitude && (
                  <div className="p-3 bg-green-50 border border-green-200 rounded-lg">
                    <p className="text-sm text-green-800 font-medium">
                      <MapPin className="h-4 w-4 inline mr-1" />
                      Location set successfully
                    </p>
                  </div>
                )}

                <button
                  type="button"
                  onClick={handleGetCurrentLocation}
                  className="mb-2 px-4 py-2 bg-blue-500 text-white rounded"
                >
                  Use My Current Location
                </button>
              </div>
            </div>
          </div>
        </div>

        <div className="flex gap-4 pt-6 border-t border-gray-200">
          <button
            type="button"
            onClick={goBack}
            className="px-6 py-3 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors font-medium"
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={handleSubmit}
            disabled={isSubmitting || !form.latitude || !form.longitude}
            className="flex-1 px-6 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:bg-gray-400 disabled:cursor-not-allowed transition-colors flex items-center justify-center gap-2 font-medium shadow-sm"
          >
            {isSubmitting ? (
              <>
                <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                Creating Station...
              </>
            ) : (
              <>
                <Plus className="h-4 w-4" />
                Create Station
              </>
            )}
          </button>
        </div>
      </div>
    </div>
  )
}