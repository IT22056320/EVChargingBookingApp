import React, { createContext, useContext, useEffect, useState } from 'react'
import { notificationService, Notification } from '@/services/notification'
import { useAuth } from './auth-provider'

interface NotificationContextType {
  notifications: Notification[]
  unreadCount: number
  markAsRead: (id: string) => void
  markAllAsRead: () => void
  clearAll: () => void
}

const NotificationContext = createContext<NotificationContextType | undefined>(undefined)

export function NotificationProvider({ children }: { children: React.ReactNode }) {
  const { user, isAuthenticated } = useAuth()
  const [notifications, setNotifications] = useState<Notification[]>([])

  useEffect(() => {
    if (isAuthenticated && user) {
      // Connect to SignalR hub
      notificationService.connect(user.role, user.id).catch(console.error)

      // Subscribe to notifications
      const unsubscribe = notificationService.subscribe((notifications) => {
        setNotifications(notifications)
      })

      // Initial load
      setNotifications(notificationService.getNotifications())

      return () => {
        unsubscribe()
        notificationService.disconnect().catch(console.error)
      }
    } else {
      // Clear notifications when not authenticated
      setNotifications([])
    }
  }, [isAuthenticated, user])

  const markAsRead = (id: string) => {
    notificationService.markAsRead(id)
  }

  const markAllAsRead = () => {
    notificationService.markAllAsRead()
  }

  const clearAll = () => {
    notificationService.clearAll()
  }

  const unreadCount = notifications.filter(n => !n.isRead).length

  const value: NotificationContextType = {
    notifications,
    unreadCount,
    markAsRead,
    markAllAsRead,
    clearAll
  }

  return (
    <NotificationContext.Provider value={value}>
      {children}
    </NotificationContext.Provider>
  )
}

export function useNotifications() {
  const context = useContext(NotificationContext)
  if (context === undefined) {
    throw new Error('useNotifications must be used within a NotificationProvider')
  }
  return context
}