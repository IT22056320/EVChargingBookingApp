import React from 'react'
import { useAuth } from '@/providers/auth-provider'
import { Button } from '@/components/ui/button'
import { NotificationCenter } from '@/components/notifications/notification-center'
import { UserRole } from '@/types'

export function Header() {
  const { user, logout } = useAuth()

  const getRoleText = (role: UserRole) => {
    return role === UserRole.Backoffice ? 'Back Office Admin' : 'Station Operator'
  }

  return (
    <header className="bg-white shadow-sm border-b">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="flex h-16 justify-between items-center">
          <div className="flex items-center">
            <h1 className="text-xl font-semibold">EV Charging System</h1>
          </div>
          
          <div className="flex items-center space-x-4">
            <div className="text-sm text-gray-700">
              <div className="font-medium">{user?.fullName}</div>
              <div className="text-xs text-gray-500">
                {user?.role !== undefined ? getRoleText(user.role) : 'User'}
              </div>
            </div>
            <NotificationCenter />
            <Button variant="outline" onClick={logout}>
              Logout
            </Button>
          </div>
        </div>
      </div>
    </header>
  )
}