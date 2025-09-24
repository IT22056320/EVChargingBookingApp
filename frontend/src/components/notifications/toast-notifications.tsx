import React, { useEffect, useState } from 'react'
import { useNotifications } from '@/providers/notification-provider'
import { X, CheckCircle, AlertTriangle, Info } from 'lucide-react'

export function ToastNotifications() {
  const { notifications } = useNotifications()
  const [toasts, setToasts] = useState<any[]>([])

  useEffect(() => {
    // Show toast for new unread notifications
    const newNotifications = notifications
      .filter(n => !n.isRead)
      .slice(0, 3) // Show only last 3 new notifications

    newNotifications.forEach(notification => {
      if (!toasts.some(t => t.id === notification.id)) {
        const toast = {
          ...notification,
          show: true
        }
        
        setToasts(prev => [toast, ...prev.slice(0, 4)]) // Keep max 5 toasts

        // Auto-hide after 5 seconds for info/success, 8 seconds for warnings/errors
        const timeout = notification.type === 'error' || notification.type === 'warning' ? 8000 : 5000
        setTimeout(() => {
          setToasts(prev => prev.filter(t => t.id !== notification.id))
        }, timeout)
      }
    })
  }, [notifications])

  const getToastIcon = (type: string) => {
    switch (type) {
      case 'success':
        return <CheckCircle className="h-5 w-5 text-green-400" />
      case 'warning':
        return <AlertTriangle className="h-5 w-5 text-yellow-400" />
      case 'error':
        return <X className="h-5 w-5 text-red-400" />
      default:
        return <Info className="h-5 w-5 text-blue-400" />
    }
  }

  const getToastBgColor = (type: string) => {
    switch (type) {
      case 'success':
        return 'bg-green-50 border-green-200'
      case 'warning':
        return 'bg-yellow-50 border-yellow-200'
      case 'error':
        return 'bg-red-50 border-red-200'
      default:
        return 'bg-blue-50 border-blue-200'
    }
  }

  const removeToast = (id: string) => {
    setToasts(prev => prev.filter(t => t.id !== id))
  }

  if (toasts.length === 0) return null

  return (
    <div className="fixed top-4 right-4 z-50 space-y-2">
      {toasts.map((toast) => (
        <div
          key={toast.id}
          className={`max-w-sm w-full border rounded-lg p-4 shadow-lg transition-all duration-300 ${getToastBgColor(toast.type)}`}
        >
          <div className="flex items-start">
            <div className="flex-shrink-0">
              {getToastIcon(toast.type)}
            </div>
            <div className="ml-3 w-0 flex-1">
              <p className="text-sm font-medium text-gray-900">
                {toast.title}
              </p>
              <p className="mt-1 text-sm text-gray-600">
                {toast.message}
              </p>
            </div>
            <div className="ml-4 flex-shrink-0 flex">
              <button
                onClick={() => removeToast(toast.id)}
                className="rounded-md inline-flex text-gray-400 hover:text-gray-600 focus:outline-none"
              >
                <X className="h-4 w-4" />
              </button>
            </div>
          </div>
        </div>
      ))}
    </div>
  )
}