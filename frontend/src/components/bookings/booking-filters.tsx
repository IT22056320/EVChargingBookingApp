import React, { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Filter, X } from 'lucide-react'

export interface BookingFilters {
  status: string
  stationId: string
  startDate: string
  endDate: string
  searchTerm: string
  userId: string
}

interface BookingFiltersProps {
  filters: BookingFilters
  onFiltersChange: (filters: BookingFilters) => void
  chargingStations: Array<{ id: string; name: string }>
  onClearFilters: () => void
}

export function BookingFiltersComponent({ 
  filters, 
  onFiltersChange, 
  chargingStations,
  onClearFilters 
}: BookingFiltersProps) {
  const [showFilters, setShowFilters] = useState(false)

  const updateFilter = (key: keyof BookingFilters, value: string) => {
    onFiltersChange({
      ...filters,
      [key]: value
    })
  }

  const hasActiveFilters = () => {
    return filters.status || filters.stationId || filters.startDate || filters.endDate || 
           filters.searchTerm || filters.userId
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-2">
        <Button
          variant="outline"
          size="sm"
          onClick={() => setShowFilters(!showFilters)}
          className="flex items-center gap-2"
        >
          <Filter className="h-4 w-4" />
          Filters
          {hasActiveFilters() && (
            <span className="bg-blue-500 text-white text-xs px-1.5 py-0.5 rounded-full ml-2">
              Active
            </span>
          )}
        </Button>
        
        {hasActiveFilters() && (
          <Button
            variant="ghost"
            size="sm"
            onClick={onClearFilters}
            className="flex items-center gap-2 text-gray-500"
          >
            <X className="h-4 w-4" />
            Clear All
          </Button>
        )}
      </div>

      {showFilters && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4 p-4 border rounded-lg bg-gray-50">
          {/* Search Term */}
          <div className="space-y-2">
            <label className="text-sm font-medium">Search</label>
            <Input
              placeholder="Vehicle number, user name..."
              value={filters.searchTerm}
              onChange={(e) => updateFilter('searchTerm', e.target.value)}
            />
          </div>

          {/* Status Filter */}
          <div className="space-y-2">
            <label className="text-sm font-medium">Status</label>
            <select 
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={filters.status} 
              onChange={(e) => updateFilter('status', e.target.value)}
            >
              <option value="">All Statuses</option>
              <option value="Pending">Pending</option>
              <option value="Approved">Approved</option>
              <option value="Completed">Completed</option>
              <option value="Cancelled">Cancelled</option>
              <option value="Rejected">Rejected</option>
            </select>
          </div>

          {/* Charging Station Filter */}
          <div className="space-y-2">
            <label className="text-sm font-medium">Charging Station</label>
            <select 
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={filters.stationId} 
              onChange={(e) => updateFilter('stationId', e.target.value)}
            >
              <option value="">All Stations</option>
              {chargingStations.map((station) => (
                <option key={station.id} value={station.id}>
                  {station.name}
                </option>
              ))}
            </select>
          </div>

          {/* Start Date Filter */}
          <div className="space-y-2">
            <label className="text-sm font-medium">From Date</label>
            <input
              type="date"
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={filters.startDate}
              onChange={(e) => updateFilter('startDate', e.target.value)}
            />
          </div>

          {/* End Date Filter */}
          <div className="space-y-2">
            <label className="text-sm font-medium">To Date</label>
            <input
              type="date"
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={filters.endDate}
              onChange={(e) => updateFilter('endDate', e.target.value)}
            />
          </div>

          {/* User ID Filter - for admin/operator use */}
          <div className="space-y-2">
            <label className="text-sm font-medium">User ID</label>
            <Input
              placeholder="Filter by user ID..."
              value={filters.userId}
              onChange={(e) => updateFilter('userId', e.target.value)}
            />
          </div>
        </div>
      )}
    </div>
  )
}