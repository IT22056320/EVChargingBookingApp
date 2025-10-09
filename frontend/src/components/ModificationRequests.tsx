import React, { useState, useEffect } from 'react'
import { 
  Clock, 
  MapPin, 
  User, 
  Calendar,
  CheckCircle,
  XCircle,
  AlertCircle,
  RefreshCw
} from 'lucide-react'
import { 
  bookingApi, 
  ModificationRequestResponse,
  ModificationRequestStatus,
  ReviewModificationRequestDto
} from '../services/bookingApi'
import toast from 'react-hot-toast'
import { format, parseISO } from 'date-fns'

interface ModificationRequestsProps {
  onRequestProcessed?: () => void
}

export function ModificationRequests({ onRequestProcessed }: ModificationRequestsProps) {
  const [requests, setRequests] = useState<ModificationRequestResponse[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [processingId, setProcessingId] = useState<string | null>(null)
  const [reviewModal, setReviewModal] = useState<{
    request: ModificationRequestResponse | null
    isApproving: boolean
  }>({ request: null, isApproving: false })
  const [reviewNotes, setReviewNotes] = useState('')
  const [rejectionReason, setRejectionReason] = useState('')

  useEffect(() => {
    loadPendingRequests()
  }, [])

  const loadPendingRequests = async () => {
    setIsLoading(true)
    try {
      const pendingRequests = await bookingApi.getPendingModificationRequests()
      setRequests(pendingRequests)
    } catch (error: any) {
      toast.error('Failed to load modification requests')
      console.error('Load requests error:', error)
    } finally {
      setIsLoading(false)
    }
  }

  const handleReviewClick = (request: ModificationRequestResponse, isApproving: boolean) => {
    setReviewModal({ request, isApproving })
    setReviewNotes('')
    setRejectionReason('')
  }

  const handleReviewSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!reviewModal.request) return

    // Validation
    if (!reviewModal.isApproving && !rejectionReason.trim()) {
      toast.error('Rejection reason is required')
      return
    }

    const adminId = localStorage.getItem('userId') || 'admin'
    
    const reviewData: ReviewModificationRequestDto = {
      isApproved: reviewModal.isApproving,
      reviewedBy: adminId,
      reviewNotes: reviewNotes.trim() || undefined,
      rejectionReason: reviewModal.isApproving ? undefined : rejectionReason.trim()
    }

    setProcessingId(reviewModal.request.id)
    try {
      await bookingApi.reviewModificationRequest(reviewModal.request.id, reviewData)
      
      toast.success(
        reviewModal.isApproving 
          ? 'Modification request approved successfully!' 
          : 'Modification request rejected'
      )
      
      setReviewModal({ request: null, isApproving: false })
      await loadPendingRequests()
      onRequestProcessed?.()
    } catch (error: any) {
      const errorMsg = error.displayMessage || error.message || 'Failed to process request'
      toast.error(errorMsg)
      console.error('Review error:', error)
    } finally {
      setProcessingId(null)
    }
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
        <span className="ml-3 text-gray-600">Loading modification requests...</span>
      </div>
    )
  }

  if (requests.length === 0) {
    return (
      <div className="text-center py-12">
        <CheckCircle className="h-16 w-16 text-gray-400 mx-auto mb-4" />
        <h3 className="text-lg font-medium text-gray-900 mb-2">No Pending Requests</h3>
        <p className="text-gray-600">There are no booking modification requests awaiting review.</p>
        <button
          onClick={loadPendingRequests}
          className="mt-4 inline-flex items-center gap-2 px-4 py-2 text-blue-600 hover:text-blue-700"
        >
          <RefreshCw className="h-4 w-4" />
          Refresh
        </button>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold text-gray-900">Pending Modification Requests</h2>
          <p className="text-gray-600 mt-1">{requests.length} request(s) awaiting review</p>
        </div>
        <button
          onClick={loadPendingRequests}
          className="flex items-center gap-2 px-4 py-2 text-blue-600 hover:bg-blue-50 rounded-lg transition-colors"
        >
          <RefreshCw className="h-4 w-4" />
          Refresh
        </button>
      </div>

      {/* Requests List */}
      <div className="grid gap-6">
        {requests.map((request) => (
          <div
            key={request.id}
            className="bg-white border border-gray-200 rounded-lg shadow-sm hover:shadow-md transition-shadow p-6"
          >
            {/* Request Header */}
            <div className="flex items-start justify-between mb-4">
              <div>
                <h3 className="text-lg font-semibold text-gray-900">
                  Booking #{request.bookingNumber}
                </h3>
                <p className="text-sm text-gray-600 mt-1">
                  Requested {format(parseISO(request.createdAt), 'MMM dd, yyyy hh:mm a')}
                </p>
              </div>
              <span className="inline-flex items-center px-3 py-1 rounded-full text-xs font-medium bg-yellow-100 text-yellow-800">
                <AlertCircle className="h-3 w-3 mr-1" />
                Pending Review
              </span>
            </div>

            {/* Customer Info */}
            <div className="bg-gray-50 rounded-lg p-4 mb-4">
              <div className="flex items-center gap-2 text-sm text-gray-700 mb-2">
                <User className="h-4 w-4" />
                <span className="font-medium">{request.customerName}</span>
                <span className="text-gray-500">•</span>
                <span className="text-gray-600">{request.customerPhone}</span>
              </div>
              <div className="text-sm text-gray-700">
                <span className="font-medium">Reason:</span> {request.requestReason}
              </div>
            </div>

            {/* Changes Comparison */}
            <div className="grid md:grid-cols-2 gap-4 mb-4">
              {/* Original Details */}
              <div className="border border-gray-200 rounded-lg p-4 bg-red-50">
                <h4 className="text-sm font-semibold text-gray-900 mb-3 flex items-center gap-2">
                  <XCircle className="h-4 w-4 text-red-600" />
                  Current Booking
                </h4>
                
                <div className="space-y-2 text-sm">
                  <div className="flex items-start gap-2">
                    <MapPin className="h-4 w-4 text-gray-500 mt-0.5 flex-shrink-0" />
                    <div>
                      <p className="font-medium text-gray-700">Station</p>
                      <p className="text-gray-600">{request.originalStationName}</p>
                    </div>
                  </div>

                  <div className="flex items-start gap-2">
                    <Calendar className="h-4 w-4 text-gray-500 mt-0.5 flex-shrink-0" />
                    <div>
                      <p className="font-medium text-gray-700">Date & Time</p>
                      <p className="text-gray-600">
                        {format(parseISO(request.originalStartTime), 'MMM dd, yyyy')}
                      </p>
                      <p className="text-gray-600">
                        {format(parseISO(request.originalStartTime), 'hh:mm a')} - {format(parseISO(request.originalEndTime), 'hh:mm a')}
                      </p>
                    </div>
                  </div>

                  <div className="flex items-start gap-2">
                    <AlertCircle className="h-4 w-4 text-gray-500 mt-0.5 flex-shrink-0" />
                    <div>
                      <p className="font-medium text-gray-700">Vehicle</p>
                      <p className="text-gray-600">{request.originalVehicleNumber}</p>
                    </div>
                  </div>
                </div>
              </div>

              {/* Requested Details */}
              <div className="border border-blue-200 rounded-lg p-4 bg-blue-50">
                <h4 className="text-sm font-semibold text-gray-900 mb-3 flex items-center gap-2">
                  <CheckCircle className="h-4 w-4 text-blue-600" />
                  Requested Changes
                </h4>
                
                <div className="space-y-2 text-sm">
                  <div className="flex items-start gap-2">
                    <MapPin className="h-4 w-4 text-gray-500 mt-0.5 flex-shrink-0" />
                    <div>
                      <p className="font-medium text-gray-700">Station</p>
                      <p className={`text-gray-900 ${request.originalStationId !== request.requestedStationId ? 'font-semibold' : ''}`}>
                        {request.requestedStationName}
                      </p>
                    </div>
                  </div>

                  <div className="flex items-start gap-2">
                    <Calendar className="h-4 w-4 text-gray-500 mt-0.5 flex-shrink-0" />
                    <div>
                      <p className="font-medium text-gray-700">Date & Time</p>
                      <p className={`text-gray-900 ${request.originalStartTime !== request.requestedStartTime ? 'font-semibold' : ''}`}>
                        {format(parseISO(request.requestedStartTime), 'MMM dd, yyyy')}
                      </p>
                      <p className={`text-gray-900 ${request.originalStartTime !== request.requestedStartTime || request.originalEndTime !== request.requestedEndTime ? 'font-semibold' : ''}`}>
                        {format(parseISO(request.requestedStartTime), 'hh:mm a')} - {format(parseISO(request.requestedEndTime), 'hh:mm a')}
                      </p>
                    </div>
                  </div>

                  <div className="flex items-start gap-2">
                    <AlertCircle className="h-4 w-4 text-gray-500 mt-0.5 flex-shrink-0" />
                    <div>
                      <p className="font-medium text-gray-700">Vehicle</p>
                      <p className={`text-gray-900 ${request.originalVehicleNumber !== request.requestedVehicleNumber ? 'font-semibold' : ''}`}>
                        {request.requestedVehicleNumber}
                      </p>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            {/* Changes Summary */}
            <div className="bg-blue-50 border border-blue-200 rounded-lg p-3 mb-4">
              <p className="text-sm text-blue-900">
                <span className="font-medium">Changes: </span>
                {request.changesSummary}
              </p>
            </div>

            {/* Actions */}
            <div className="flex gap-3 justify-end">
              <button
                onClick={() => handleReviewClick(request, false)}
                disabled={processingId === request.id}
                className="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors disabled:bg-gray-300 disabled:cursor-not-allowed flex items-center gap-2"
              >
                <XCircle className="h-4 w-4" />
                Reject
              </button>
              <button
                onClick={() => handleReviewClick(request, true)}
                disabled={processingId === request.id}
                className="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors disabled:bg-gray-300 disabled:cursor-not-allowed flex items-center gap-2"
              >
                <CheckCircle className="h-4 w-4" />
                Approve
              </button>
            </div>
          </div>
        ))}
      </div>

      {/* Review Modal */}
      {reviewModal.request && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-lg shadow-xl max-w-md w-full">
            <div className="p-6">
              <h3 className="text-xl font-bold text-gray-900 mb-4">
                {reviewModal.isApproving ? 'Approve' : 'Reject'} Modification Request
              </h3>

              <form onSubmit={handleReviewSubmit} className="space-y-4">
                {reviewModal.isApproving ? (
                  <>
                    <p className="text-gray-700">
                      Are you sure you want to approve this modification request?
                    </p>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        Review Notes (Optional)
                      </label>
                      <textarea
                        value={reviewNotes}
                        onChange={(e) => setReviewNotes(e.target.value)}
                        rows={3}
                        className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-green-500"
                        placeholder="Add any notes about this approval..."
                      />
                    </div>
                  </>
                ) : (
                  <>
                    <p className="text-gray-700">
                      Please provide a reason for rejecting this modification request.
                    </p>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        Rejection Reason *
                      </label>
                      <textarea
                        value={rejectionReason}
                        onChange={(e) => setRejectionReason(e.target.value)}
                        rows={3}
                        className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-red-500"
                        placeholder="Explain why this request is being rejected..."
                        required
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        Additional Notes (Optional)
                      </label>
                      <textarea
                        value={reviewNotes}
                        onChange={(e) => setReviewNotes(e.target.value)}
                        rows={2}
                        className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-red-500"
                        placeholder="Add any additional notes..."
                      />
                    </div>
                  </>
                )}

                <div className="flex gap-3 justify-end pt-4 border-t">
                  <button
                    type="button"
                    onClick={() => setReviewModal({ request: null, isApproving: false })}
                    disabled={processingId !== null}
                    className="px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 transition-colors"
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    disabled={processingId !== null}
                    className={`px-4 py-2 text-white rounded-lg transition-colors disabled:bg-gray-300 disabled:cursor-not-allowed flex items-center gap-2 ${
                      reviewModal.isApproving 
                        ? 'bg-green-600 hover:bg-green-700' 
                        : 'bg-red-600 hover:bg-red-700'
                    }`}
                  >
                    {processingId ? (
                      <>
                        <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                        Processing...
                      </>
                    ) : (
                      <>
                        {reviewModal.isApproving ? 'Approve' : 'Reject'}
                      </>
                    )}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
