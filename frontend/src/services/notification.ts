import { HubConnectionBuilder, HubConnection, LogLevel } from '@microsoft/signalr'
import { UserRole } from '@/types'

export interface Notification {
  id: string
  title: string
  message: string
  type: 'info' | 'success' | 'warning' | 'error'
  timestamp: string
  isRead: boolean
  targetRoles: UserRole[]
  userId?: string
}

export class NotificationService {
  private connection: HubConnection | null = null
  private notifications: Notification[] = []
  private listeners: Array<(notifications: Notification[]) => void> = []

  async connect(userRole: UserRole, userId: string): Promise<void> {
    try {
      if (this.connection) {
        await this.disconnect()
      }

      this.connection = new HubConnectionBuilder()
        .withUrl('http://localhost:5001/bookingNotificationHub', {
          withCredentials: true
        })
        .configureLogging(LogLevel.Information)
        .build()

      // Set up event handlers
      this.connection.on('ReceiveNotification', (notification: Notification) => {
        console.log('Received notification:', notification)
        
        // Check if notification is for this user's role
        if (this.shouldReceiveNotification(notification, userRole, userId)) {
          this.addNotification(notification)
        }
      })

      this.connection.on('BookingStatusChanged', (bookingId: string, status: string, message: string) => {
        const notification: Notification = {
          id: `booking-${bookingId}-${Date.now()}`,
          title: 'Booking Status Update',
          message: `Booking ${bookingId}: ${message}`,
          type: 'info',
          timestamp: new Date().toISOString(),
          isRead: false,
          targetRoles: [UserRole.Backoffice, UserRole.StationOperator],
          userId: undefined
        }
        
        if (this.shouldReceiveNotification(notification, userRole, userId)) {
          this.addNotification(notification)
        }
      })

      this.connection.on('NewBookingRequest', (booking: any) => {
        const notification: Notification = {
          id: `new-booking-${booking.id}-${Date.now()}`,
          title: 'New Booking Request',
          message: `New booking request from ${booking.user?.fullName || 'Unknown User'} for ${booking.chargingStation?.stationName || 'Station'}`,
          type: 'info',
          timestamp: new Date().toISOString(),
          isRead: false,
          targetRoles: [UserRole.Backoffice, UserRole.StationOperator],
          userId: undefined
        }
        
        if (this.shouldReceiveNotification(notification, userRole, userId)) {
          this.addNotification(notification)
        }
      })

      this.connection.onclose(() => {
        console.log('SignalR connection closed')
      })

      await this.connection.start()
      console.log('SignalR connected successfully')

      // Send welcome notification based on role
      this.addWelcomeNotification(userRole)

    } catch (error) {
      console.error('SignalR connection error:', error)
    }
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop()
      this.connection = null
    }
  }

  private shouldReceiveNotification(notification: Notification, userRole: UserRole, userId: string): boolean {
    // If notification is for specific user
    if (notification.userId && notification.userId !== userId) {
      return false
    }

    // If notification has target roles, check if user's role is included
    if (notification.targetRoles.length > 0) {
      return notification.targetRoles.includes(userRole)
    }

    // Default: allow all notifications
    return true
  }

  private addNotification(notification: Notification): void {
    this.notifications.unshift(notification)
    
    // Keep only last 50 notifications
    if (this.notifications.length > 50) {
      this.notifications = this.notifications.slice(0, 50)
    }

    this.notifyListeners()
  }

  private addWelcomeNotification(userRole: UserRole): void {
    const roleText = userRole === UserRole.Backoffice ? 'Back Office Administrator' : 'Station Operator'
    const welcomeNotification: Notification = {
      id: `welcome-${Date.now()}`,
      title: 'Welcome!',
      message: `Welcome back! You are logged in as ${roleText}.`,
      type: 'success',
      timestamp: new Date().toISOString(),
      isRead: false,
      targetRoles: [userRole],
      userId: undefined
    }

    this.addNotification(welcomeNotification)
  }

  getNotifications(): Notification[] {
    return this.notifications
  }

  getUnreadCount(): number {
    return this.notifications.filter(n => !n.isRead).length
  }

  markAsRead(notificationId: string): void {
    const notification = this.notifications.find(n => n.id === notificationId)
    if (notification) {
      notification.isRead = true
      this.notifyListeners()
    }
  }

  markAllAsRead(): void {
    this.notifications.forEach(n => n.isRead = true)
    this.notifyListeners()
  }

  clearAll(): void {
    this.notifications = []
    this.notifyListeners()
  }

  subscribe(listener: (notifications: Notification[]) => void): () => void {
    this.listeners.push(listener)
    
    // Return unsubscribe function
    return () => {
      this.listeners = this.listeners.filter(l => l !== listener)
    }
  }

  private notifyListeners(): void {
    this.listeners.forEach(listener => listener(this.notifications))
  }
}

export const notificationService = new NotificationService()