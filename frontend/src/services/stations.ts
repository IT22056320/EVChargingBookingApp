import { apiService } from './api'

export interface ChargingStationDto {
    stationName: string
    address: string
    location: string
    latitude: number
    longitude: number
    connectorType: string
    totalSlots: number
    powerRatingKW: number
}

export interface ChargingStationResponse {
    operatingHours: string
    id: string
    stationName: string
    address: string
    location: string
    latitude: number
    longitude: number
    connectorType: string
    totalSlots: number
    powerRatingKW: number
}

class ChargingStationApiService {
    // Get all stations
    async getStations(): Promise<ChargingStationResponse[]> {
        return await apiService.get<ChargingStationResponse[]>('/ChargingStations')
    }

    // Get station by ID
    async getStationById(id: string): Promise<ChargingStationResponse> {
        return await apiService.get<ChargingStationResponse>(`/ChargingStations/${id}`)
    }

    // Create a new station
    async createStation(stationData: ChargingStationDto): Promise<ChargingStationResponse> {
        return await apiService.post<ChargingStationResponse>('/ChargingStations', stationData)
    }

    // Update a station
    async updateStation(id: string, stationData: ChargingStationDto): Promise<{ message: string }> {
        return await apiService.put<{ message: string }>(`/ChargingStations/${id}`, stationData)
    }

    // Delete a station
    async deleteStation(id: string): Promise<{ message: string }> {
        return await apiService.delete<{ message: string }>(`/ChargingStations/${id}`)
    }
}

export const stationsApi = new ChargingStationApiService()
export default stationsApi
