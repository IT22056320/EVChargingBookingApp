import React, { useState } from 'react'
import { Button } from '@/components/ui/button'
import { 
  CheckCircle, 
  XCircle, 
  Ban, 
  Flag, 
  AlertTriangle 
} from 'lucide-react'
import toast from 'react-hot-toast'
import { apiService } from '@/services/api'

interface BookingActionsProps {
  bookingId: string
  currentStatus: string
  onActionComplete: () => void
  userRole: 'Backoffice' | 'StationOperator' | 'EVOwner'
}

export function BookingActions({ 
  bookingId, 
  currentStatus, 
  onActionComplete,
  userRole 
}: BookingActionsProps) {
  const [isLoading, setIsLoading] = useState(false)
  const [showReasonDialog, setShowReasonDialog] = useState<string | null>(null)
  const [reason, setReason] = useState('')

  const handleAction = async (action: string, requiresReason = false) => {
    if (requiresReason && !reason.trim()) {
      toast.error('Please provide a reason')
      return
    }

    setIsLoading(true)
    
    try {
      let response
      const baseUrl = `http://localhost:5001/api/bookings/${bookingId}`

      switch (action) {
        case 'approve':
          response = await fetch(`${baseUrl}/approve`, {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
            },
          })
          break

        case 'reject':
          response = await fetch(`${baseUrl}/reject`, {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
            },
            body: JSON.stringify(reason)
          })
          break

        case 'cancel':
          response = await fetch(`${baseUrl}/cancel`, {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
            },
            body: JSON.stringify(reason)
          })
          break

        case 'complete':
          response = await fetch(`${baseUrl}/complete`, {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
            },
            body: JSON.stringify(null) // Energy consumed can be null
          })
          break

        default:
          throw new Error('Unknown action')
      }

      if (!response.ok) {
        const errorData = await response.text()
        throw new Error(errorData || 'Action failed')
      }

      const result = await response.text()
      toast.success(result || `Booking ${action}d successfully`)
      
      setReason('')
      setShowReasonDialog(null)
      onActionComplete()

    } catch (error) {
      console.error(`Error ${action}ing booking:`, error)
      toast.error(error instanceof Error ? error.message : `Failed to ${action} booking`)
    } finally {
      setIsLoading(false)
    }
  }

  const canApprove = currentStatus === 'Pending' && (userRole === 'Backoffice' || userRole === 'StationOperator')
  const canReject = currentStatus === 'Pending' && (userRole === 'Backoffice' || userRole === 'StationOperator')
  const canCancel = (currentStatus === 'Pending' || currentStatus === 'Approved')
  const canComplete = currentStatus === 'Approved' && (userRole === 'Backoffice' || userRole === 'StationOperator')

  if (!canApprove && !canReject && !canCancel && !canComplete) {
    return null
  }

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap gap-2">
        {canApprove && (
          <Button
            size="sm"
            variant="outline"
            onClick={() => handleAction('approve')}
            disabled={isLoading}
            className="text-green-600 border-green-600 hover:bg-green-50 flex items-center gap-1"
          >
            <CheckCircle className="h-4 w-4" />
            Approve
          </Button>
        )}

        {canReject && (
          <Button
            size="sm"
            variant="outline"
            onClick={() => setShowReasonDialog('reject')}
            disabled={isLoading}
            className="text-red-600 border-red-600 hover:bg-red-50 flex items-center gap-1"
          >
            <XCircle className="h-4 w-4" />
            Reject
          </Button>
        )}

        {canCancel && (
          <Button
            size="sm"
            variant="outline"
            onClick={() => setShowReasonDialog('cancel')}
            disabled={isLoading}
            className="text-orange-600 border-orange-600 hover:bg-orange-50 flex items-center gap-1"
          >
            <Ban className="h-4 w-4" />
            Cancel
          </Button>
        )}

        {canComplete && (
          <Button
            size="sm"
            variant="outline"
            onClick={() => handleAction('complete')}
            disabled={isLoading}
            className="text-blue-600 border-blue-600 hover:bg-blue-50 flex items-center gap-1"
          >
            <Flag className="h-4 w-4" />
            Complete
          </Button>
        )}
      </div>

      {/* Reason Dialog */}
      {showReasonDialog && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-lg p-6 max-w-md w-full">
            <div className="flex items-center gap-2 mb-4">
              <AlertTriangle className="h-5 w-5 text-orange-500" />
              <h3 className="text-lg font-semibold">
                {showReasonDialog === 'reject' ? 'Reject Booking' : 'Cancel Booking'}
              </h3>
            </div>
            
            <p className="text-gray-600 mb-4">
              Please provide a reason for {showReasonDialog}ing this booking:
            </p>
            
            <textarea
              className="w-full p-3 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              rows={3}
              placeholder="Enter reason..."
              value={reason}
              onChange={(e) => setReason(e.target.value)}
            />
            
            <div className="flex justify-end gap-2 mt-4">
              <Button
                variant="ghost"
                onClick={() => {
                  setShowReasonDialog(null)
                  setReason('')
                }}
                disabled={isLoading}
              >
                Cancel
              </Button>
              <Button
                onClick={() => handleAction(showReasonDialog, true)}
                disabled={isLoading || !reason.trim()}
                className="bg-red-600 hover:bg-red-700 text-white"
              >
                {isLoading ? 'Processing...' : `${showReasonDialog === 'reject' ? 'Reject' : 'Cancel'} Booking`}
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}