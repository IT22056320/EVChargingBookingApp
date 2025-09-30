import { useState, useEffect } from 'react'
import {
    MapPin,
    Zap,
    Search,
    Plus,
    Eye,
    Edit,
    Power,
    PowerOff,
    Battery,
    DollarSign,
    Plug,
    Clock,
    Navigation,
    MoreVertical,
    Check,
    AlertCircle,
    Building2
} from 'lucide-react'
import toast from '../../utils/toast'

import { StationDetailsModal, ChargingStation as ModalChargingStation } from '../../components/station-details-modal'
import Swal from 'sweetalert2'

type ConnectorType = 'Type1' | 'Type2' | 'CHAdeMO' | 'CCS' | 'Tesla'

type ChargingStation = {
    id: string
    stationName: string
    location: string
    address: string
    latitude: number
    longitude: number
    connectorType: ConnectorType
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

type StationStats = {
    totalStations: number
    activeStations: number
    inactiveStations: number
    totalSlots: number
    availableSlots: number
    totalBookings: number
    averageUtilization: number
}

export default function StationManagementPage() {
    const [stations, setStations] = useState<ChargingStation[]>([])
    const [isLoading, setIsLoading] = useState(true)

    useEffect(() => {
        const fetchStations = async () => {
            setIsLoading(true)
            try {
                // Import stationsApi from services
                const { stationsApi } = await import('../../services/stations')
                const data = await stationsApi.getStations()
                // Map ChargingStationResponse to ChargingStation
                const mappedStations: ChargingStation[] = data.map((station: any) => ({
                    id: station.id,
                    stationName: station.stationName,
                    location: station.location,
                    address: station.address,
                    latitude: station.latitude,
                    longitude: station.longitude,
                    connectorType: station.connectorType,
                    powerRatingKW: station.powerRatingKW ?? 0,
                    availableSlots: station.availableSlots ?? 0,
                    totalSlots: station.totalSlots ?? 0,
                    isAvailable: station.isAvailable ?? false,
                    amenities: station.amenities ?? [],
                    operatingHours: station.operatingHours ?? '',
                    contactNumber: station.contactNumber ?? '',
                    totalBookings: station.totalBookings ?? 0,
                    createdAt: station.createdAt ?? '',
                    updatedAt: station.updatedAt ?? ''
                }))
                setStations(mappedStations)
                toast.success(`Loaded ${mappedStations.length} stations`)
            } catch (error) {
                console.error('Failed to fetch stations:', error)
                toast.error('Failed to load stations')
                setStations([])
            } finally {
                setIsLoading(false)
            }
        }
        fetchStations()
    }, [])

    const [filteredStations, setFilteredStations] = useState<ChargingStation[]>([])
    const [stats, setStats] = useState<StationStats>({
        totalStations: 0,
        activeStations: 0,
        inactiveStations: 0,
        totalSlots: 0,
        availableSlots: 0,
        totalBookings: 0,
        averageUtilization: 0
    })

    const [searchTerm, setSearchTerm] = useState('')
    const [statusFilter, setStatusFilter] = useState<'all' | 'active' | 'inactive'>('all')
    const [selectedStation, setSelectedStation] = useState<ChargingStation | null>(null)
    const [showDetails, setShowDetails] = useState(false)

    useEffect(() => {
        calculateStats()
        applyFilters()
    }, [stations])

    useEffect(() => {
        applyFilters()
    }, [searchTerm, statusFilter])

    const calculateStats = () => {
        const totalStations = stations.length
        const activeStations = stations.filter(s => s.isAvailable).length
        const inactiveStations = stations.filter(s => !s.isAvailable).length
        const totalSlots = stations.reduce((sum, s) => sum + s.totalSlots, 0)
        const availableSlots = stations.reduce((sum, s) => sum + s.availableSlots, 0)
        const totalBookings = stations.reduce((sum, s) => sum + s.totalBookings, 0)
        const averageUtilization = totalSlots > 0 ? ((totalSlots - availableSlots) / totalSlots) * 100 : 0

        setStats({
            totalStations,
            activeStations,
            inactiveStations,
            totalSlots,
            availableSlots,
            totalBookings,
            averageUtilization
        })
    }

    const applyFilters = () => {
        let filtered = [...stations]

        if (searchTerm) {
            const term = searchTerm.toLowerCase()
            filtered = filtered.filter(station =>
                station.stationName.toLowerCase().includes(term) ||
                station.location.toLowerCase().includes(term) ||
                station.address.toLowerCase().includes(term) ||
                station.connectorType.toLowerCase().includes(term)
            )
        }

        if (statusFilter === 'active') {
            filtered = filtered.filter(s => s.isAvailable)
        } else if (statusFilter === 'inactive') {
            filtered = filtered.filter(s => !s.isAvailable)
        }

        setFilteredStations(filtered)
    }

    const toggleStationStatus = async (stationId: string) => {
        const station = stations.find(s => s.id === stationId)
        if (!station) return
        try {
            setIsLoading(true)
            const { stationsApi } = await import('../../services/stations')
            const payload = {
                stationName: station.stationName,
                address: station.address,
                location: station.location,
                latitude: station.latitude,
                longitude: station.longitude,
                connectorType: station.connectorType,
                totalSlots: station.totalSlots,
                powerRatingKW: station.powerRatingKW,
                isAvailable: !station.isAvailable
            }
            await stationsApi.updateStation(stationId, payload)
            setStations(prev => prev.map(s =>
                s.id === stationId ? { ...s, isAvailable: payload.isAvailable } : s
            ))
            toast.success(`Station set to ${payload.isAvailable ? 'Active' : 'Deactivated'}`)
        } catch (err: any) {
            let errorMsg = 'Failed to update station status';
            if (err?.response?.data) {
                errorMsg = typeof err.response.data === 'string' ? err.response.data : err.response.data.message || errorMsg;
            } else if (err?.message) {
                errorMsg = err.message;
            }
            if (errorMsg.includes('Cannot deactivate station: active bookings exist')) {
                toast.warning(errorMsg)
            } else {
                toast.error(errorMsg)
            }
            console.error('Failed to update station status', err)
        } finally {
            setIsLoading(false)
        }
    }

    const viewDetails = (station: ChargingStation) => {
        setSelectedStation(station)
        setShowDetails(true)
    }

    const createNewStation = () => {
        window.location.href = '/add-station'
    }

    const editStation = (stationId: string) => {
    window.location.href = `/edit-station/${stationId}`
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
                        <Zap className="h-8 w-8 text-blue-600" />
                        Charging Station Management
                    </h1>
                    <p className="text-gray-600">
                        Manage and monitor EV charging stations across the network
                    </p>
                </div>
                <div className="flex gap-2">
                    <button
                        onClick={createNewStation}
                        className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors flex items-center gap-2"
                    >
                        <Plus className="h-4 w-4" />
                        New Station
                    </button>
                    {(['all', 'active', 'inactive'] as const).map((filter) => (
                        <button
                            key={filter}
                            onClick={() => setStatusFilter(filter)}
                            className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${statusFilter === filter
                                    ? 'bg-blue-100 text-blue-700 border border-blue-200'
                                    : 'bg-white text-gray-700 border border-gray-300 hover:bg-gray-50'
                                }`}
                        >
                            {filter.charAt(0).toUpperCase() + filter.slice(1)}
                            {filter !== 'all' && (
                                <span className="ml-2 px-2 py-0.5 bg-blue-500 text-white text-xs rounded-full">
                                    {filter === 'active' && stats.activeStations}
                                    {filter === 'inactive' && stats.inactiveStations}
                                </span>
                            )}
                        </button>
                    ))}
                </div>
            </div>

            {/* Statistics Cards */}
            <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
                <div className="rounded-lg border bg-gradient-to-br from-blue-500 to-blue-600 text-white p-6 shadow-sm">
                    <div className="flex items-center justify-between">
                        <div>
                            <p className="text-blue-100 text-sm font-medium">Total Stations</p>
                            <p className="text-3xl font-bold">{stats.totalStations}</p>
                        </div>
                        <Building2 className="h-8 w-8 text-blue-200" />
                    </div>
                </div>

                <div className="rounded-lg border bg-gradient-to-br from-green-500 to-green-600 text-white p-6 shadow-sm">
                    <div className="flex items-center justify-between">
                        <div>
                            <p className="text-green-100 text-sm font-medium">Active Stations</p>
                            <p className="text-3xl font-bold">{stats.activeStations}</p>
                        </div>
                        <Power className="h-8 w-8 text-green-200" />
                    </div>
                </div>

                <div className="rounded-lg border bg-gradient-to-br from-orange-500 to-orange-600 text-white p-6 shadow-sm">
                    <div className="flex items-center justify-between">
                        <div>
                            <p className="text-orange-100 text-sm font-medium">Available Slots</p>
                            <p className="text-3xl font-bold">{stats.availableSlots}</p>
                        </div>
                        <Battery className="h-8 w-8 text-orange-200" />
                    </div>
                </div>

                <div className="rounded-lg border bg-gradient-to-br from-purple-500 to-purple-600 text-white p-6 shadow-sm">
                    <div className="flex items-center justify-between">
                        <div>
                            <p className="text-purple-100 text-sm font-medium">Total Bookings</p>
                            <p className="text-3xl font-bold">{stats.totalBookings}</p>
                        </div>
                        <DollarSign className="h-8 w-8 text-purple-200" />
                    </div>
                </div>
            </div>

            {/* Search Bar */}
            <div className="flex gap-4 items-center">
                <div className="relative flex-1 max-w-md">
                    <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
                    <input
                        type="text"
                        placeholder="Search by name, location, connector type..."
                        value={searchTerm}
                        onChange={(e) => setSearchTerm(e.target.value)}
                        className="pl-10 pr-4 py-2 w-full border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                    />
                </div>
            </div>

            {/* Stations Table */}
            <div className="rounded-lg border border-gray-200 bg-white shadow-sm">
                <div className="p-6 border-b border-gray-200">
                    <div className="flex items-center justify-between">
                        <h3 className="text-lg font-semibold">Stations ({filteredStations.length})</h3>
                    </div>
                </div>
                <div className="overflow-x-auto">
                    {filteredStations.length > 0 ? (
                        <table className="w-full">
                            <thead className="border-b bg-gray-50">
                                <tr>
                                    <th className="text-left p-4 font-medium text-sm">Station Name</th>
                                    <th className="text-left p-4 font-medium text-sm">Location</th>
                                    <th className="text-left p-4 font-medium text-sm">Connector</th>
                                    <th className="text-left p-4 font-medium text-sm">Slots</th>
                                    <th className="text-left p-4 font-medium text-sm">Power</th>
                                    <th className="text-left p-4 font-medium text-sm">Status</th>
                                    <th className="text-center p-4 font-medium text-sm">Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                {filteredStations.map((station) => (
                                    <tr key={station.id} className="border-b hover:bg-gray-50">
                                        <td className="p-4">
                                            <div className="flex items-center gap-2">
                                                <div className="w-10 h-10 rounded-lg bg-blue-100 flex items-center justify-center">
                                                    <Zap className="h-5 w-5 text-blue-600" />
                                                </div>
                                                <div>
                                                    <p className="font-medium">{station.stationName}</p>
                                                    <p className="text-xs text-gray-500">{station.operatingHours}</p>
                                                </div>
                                            </div>
                                        </td>
                                        <td className="p-4">
                                            <div className="flex items-center gap-2">
                                                <MapPin className="h-4 w-4 text-gray-400" />
                                                <div>
                                                    <p className="font-medium text-sm">{station.location}</p>
                                                    <p className="text-xs text-gray-500">{station.address}</p>
                                                </div>
                                            </div>
                                        </td>
                                        <td className="p-4">
                                            <div className="flex items-center gap-1">
                                                <Plug className="h-4 w-4 text-gray-400" />
                                                <span className="text-sm">{station.connectorType}</span>
                                            </div>
                                        </td>
                                        <td className="p-4">
                                            <div>
                                                <span className={`text-sm font-medium ${station.availableSlots > 0 ? 'text-green-600' : 'text-red-600'}`}>
                                                    {station.availableSlots}
                                                </span>
                                                <span className="text-sm text-gray-500">/{station.totalSlots}</span>
                                            </div>
                                        </td>
                                        <td className="p-4">
                                            <span className="text-sm">{station.powerRatingKW} kW</span>
                                        </td>
                                        <td className="p-4">
                                            {station.isAvailable ? (
                                                <span className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-green-100 text-green-800 border border-green-200">
                                                    <Power className="h-3 w-3 mr-1" />
                                                    Active
                                                </span>
                                            ) : (
                                                <span className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-red-100 text-red-800 border border-red-200">
                                                    <PowerOff className="h-3 w-3 mr-1" />
                                                    Inactive
                                                </span>
                                            )}
                                        </td>
                                        <td className="p-4">
                                            <div className="flex items-center justify-center gap-1">
                                                <button
                                                    onClick={() => viewDetails(station)}
                                                    className="p-2 rounded-lg hover:bg-gray-100 transition-colors"
                                                    title="View Details"
                                                >
                                                    <Eye className="h-4 w-4" />
                                                </button>
                                                <button
                                                    onClick={() => editStation(station.id)}
                                                    className="p-2 rounded-lg hover:bg-blue-50 text-blue-600 transition-colors"
                                                    title="Edit Station"
                                                >
                                                    <Edit className="h-4 w-4" />
                                                </button>
                                                <button
                                                    onClick={() => toggleStationStatus(station.id)}
                                                    className={`p-2 rounded-lg transition-colors ${station.isAvailable
                                                            ? 'hover:bg-red-50 text-red-600'
                                                            : 'hover:bg-green-50 text-green-600'
                                                        }`}
                                                    title={station.isAvailable ? 'Deactivate' : 'Activate'}
                                                >
                                                    {station.isAvailable ? (
                                                        <PowerOff className="h-4 w-4" />
                                                    ) : (
                                                        <Power className="h-4 w-4" />
                                                    )}
                                                </button>
                                            </div>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    ) : (
                        <div className="text-center py-12">
                            <Zap className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                            <h3 className="text-lg font-medium text-gray-600 mb-2">
                                {searchTerm || statusFilter !== 'all' ? 'No matching stations found' : 'No stations found'}
                            </h3>
                            <p className="text-sm text-gray-500">
                                {searchTerm || statusFilter !== 'all'
                                    ? 'Try adjusting your search or filter criteria'
                                    : 'Charging stations will appear here when added to the network'
                                }
                            </p>
                        </div>
                    )}
                </div>
            </div>

            {/* Details Modal */}
            <StationDetailsModal
                station={selectedStation as ModalChargingStation}
                show={showDetails}
                onClose={() => setShowDetails(false)}
                onEdit={editStation}
                onToggleStatus={toggleStationStatus}
            />
        </div>
    )
}