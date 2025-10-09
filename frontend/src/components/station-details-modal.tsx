import React from 'react'
import {
    MapPin,
    Power,
    PowerOff,
    Navigation,
    Plug,
    DollarSign,
    Clock,
    Edit
} from 'lucide-react'

export type ChargingStation = {
    id: string
    stationName: string
    location: string
    address: string
    latitude: number
    longitude: number
    connectorType: string
    powerRatingKW: number
    availableSlots: number
    totalSlots: number
    isAvailable: boolean
    amenities: string[]
    operatingHours: string
    contactNumber: string
    totalBookings: number
    createdAt: string
    updatedAt: string
}

interface StationDetailsModalProps {
    station: ChargingStation
    show: boolean
    onClose: () => void
    onEdit: (id: string) => void
    onToggleStatus: (id: string) => void
}

const getConnectorTypeLabel = (type: string) => {
    const labels: Record<string, string> = {
        'Type1': 'Type 1 (J1772)',
        'Type2': 'Type 2 (Mennekes)',
        'CHAdeMO': 'CHAdeMO',
        'CCS': 'CCS',
        'Tesla': 'Tesla Supercharger'
    }
    return labels[type] || type
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

export const StationDetailsModal: React.FC<StationDetailsModalProps> = ({
    station,
    show,
    onClose,
    onEdit,
    onToggleStatus
}) => {
    if (!show || !station) return null
    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50" onClick={onClose}>
            <div className="bg-white rounded-lg shadow-xl max-w-2xl w-full mx-4 max-h-[90vh] overflow-y-auto" onClick={e => e.stopPropagation()}>
                <div className="p-6 border-b border-gray-200 sticky top-0 bg-white">
                    <div className="flex items-center justify-between">
                        <h2 className="text-xl font-semibold">Station Details</h2>
                        <button
                            onClick={onClose}
                            className="p-2 rounded-lg hover:bg-gray-100 transition-colors"
                        >
                            <svg className="h-5 w-5" viewBox="0 0 20 20" fill="none"><path d="M6 6l8 8M6 14L14 6" stroke="currentColor" strokeWidth="2" strokeLinecap="round" /></svg>
                        </button>
                    </div>
                </div>

                <div className="p-6 space-y-6">
                    <div className="flex items-center gap-4">
                        <div className="w-16 h-16 rounded-full bg-blue-100 text-blue-600 flex items-center justify-center">
                            <svg className="h-8 w-8" viewBox="0 0 24 24" fill="none"><path d="M13 2v8h8" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" /><path d="M13 10l-9 9" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" /></svg>
                        </div>
                        <div>
                            <h3 className="text-lg font-semibold">{station.stationName}</h3>
                            <p className="text-gray-600 flex items-center gap-1">
                                <MapPin className="h-4 w-4" />
                                {station.location}
                            </p>
                        </div>
                        <div className="ml-auto">
                            {station.isAvailable ? (
                                <span className="inline-flex items-center px-3 py-1.5 rounded-full text-sm font-medium bg-green-100 text-green-800 border border-green-200">
                                    <Power className="h-4 w-4 mr-1" />
                                    Active
                                </span>
                            ) : (
                                <span className="inline-flex items-center px-3 py-1.5 rounded-full text-sm font-medium bg-red-100 text-red-800 border border-red-200">
                                    <PowerOff className="h-4 w-4 mr-1" />
                                    Inactive
                                </span>
                            )}
                        </div>
                    </div>

                    <div className="grid md:grid-cols-2 gap-6">
                        <div className="rounded-lg border border-gray-200 p-4">
                            <h4 className="font-medium mb-3 flex items-center gap-2">
                                <Navigation className="h-4 w-4 text-blue-600" />
                                Location Information
                            </h4>
                            <div className="space-y-2 text-sm">
                                <p><span className="text-gray-600">Address:</span> <span className="font-medium">{station.address}</span></p>
                                <p><span className="text-gray-600">Coordinates:</span> <span className="font-medium">{station.latitude.toFixed(4)}, {station.longitude.toFixed(4)}</span></p>
                                <p><span className="text-gray-600">Hours:</span> <span className="font-medium">{station.operatingHours}</span></p>
                            </div>
                        </div>

                        <div className="rounded-lg border border-gray-200 p-4">
                            <h4 className="font-medium mb-3 flex items-center gap-2">
                                <Plug className="h-4 w-4 text-yellow-600" />
                                Technical Specifications
                            </h4>
                            <div className="space-y-2 text-sm">
                                <p><span className="text-gray-600">Connector Type:</span> <span className="font-medium">{getConnectorTypeLabel(station.connectorType)}</span></p>
                                <p><span className="text-gray-600">Power Rating (kW):</span> <span className="font-medium">{station.powerRatingKW}</span></p>
                                <p><span className="text-gray-600">Total Slots:</span> <span className="font-medium">{station.totalSlots}</span></p>
                                <p><span className="text-gray-600">Available Slots:</span> <span className={`font-medium ${station.availableSlots > 0 ? 'text-green-600' : 'text-red-600'}`}>{station.availableSlots}</span></p>
                            </div>
                        </div>
                    </div>

                    <div className="flex gap-3 pt-6 border-t border-gray-200">
                        <button
                            onClick={() => onEdit(station.id)}
                            className="flex-1 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors flex items-center justify-center gap-2"
                        >
                            <Edit className="h-4 w-4" />
                            Edit Station
                        </button>
                        <button
                            onClick={() => {
                                onToggleStatus(station.id)
                                onClose()
                            }}
                            className={`flex-1 px-4 py-2 rounded-lg transition-colors flex items-center justify-center gap-2 ${station.isAvailable
                                    ? 'bg-red-600 text-white hover:bg-red-700'
                                    : 'bg-green-600 text-white hover:bg-green-700'
                                }`}
                        >
                            {station.isAvailable ? (
                                <>
                                    <PowerOff className="h-4 w-4" />
                                    Deactivate Station
                                </>
                            ) : (
                                <>
                                    <Power className="h-4 w-4" />
                                    Activate Station
                                </>
                            )}
                        </button>
                    </div>
                </div>
            </div>
        </div>
    )
}
