import toast, { Toast } from 'react-hot-toast'

interface ToastOptions {
  duration?: number
  position?: 'top-center' | 'top-left' | 'top-right' | 'bottom-center' | 'bottom-left' | 'bottom-right'
}

export const customToast = {
  success: (message: string, options?: ToastOptions) => {
    return toast.success(message, {
      duration: options?.duration || 4000,
      style: {
        background: '#F0FDF4',
        color: '#065F46',
        border: '1px solid #10B981',
        borderRadius: '8px',
        padding: '16px',
        boxShadow: '0 4px 12px rgba(0, 0, 0, 0.15)',
        maxWidth: '500px',
        fontSize: '14px',
        fontWeight: '500'
      },
      iconTheme: {
        primary: '#10B981',
        secondary: '#F0FDF4',
      },
    })
  },

  error: (message: string, options?: ToastOptions) => {
    return toast.error(message, {
      duration: options?.duration || 6000,
      style: {
        background: '#FEF2F2',
        color: '#991B1B',
        border: '1px solid #EF4444',
        borderRadius: '8px',
        padding: '16px',
        boxShadow: '0 4px 12px rgba(0, 0, 0, 0.15)',
        maxWidth: '500px',
        fontSize: '14px',
        fontWeight: '500'
      },
      iconTheme: {
        primary: '#EF4444',
        secondary: '#FEF2F2',
      },
    })
  },

  loading: (message: string, options?: ToastOptions) => {
    return toast.loading(message, {
      style: {
        background: '#EFF6FF',
        color: '#1E40AF',
        border: '1px solid #3B82F6',
        borderRadius: '8px',
        padding: '16px',
        boxShadow: '0 4px 12px rgba(0, 0, 0, 0.15)',
        maxWidth: '500px',
        fontSize: '14px',
        fontWeight: '500'
      },
    })
  },

  warning: (message: string, options?: ToastOptions) => {
    return toast(message, {
      duration: options?.duration || 5000,
      icon: '⚠️',
      style: {
        background: '#FFFBEB',
        color: '#92400E',
        border: '1px solid #F59E0B',
        borderRadius: '8px',
        padding: '16px',
        boxShadow: '0 4px 12px rgba(0, 0, 0, 0.15)',
        maxWidth: '500px',
        fontSize: '14px',
        fontWeight: '500'
      },
    })
  },

  info: (message: string, options?: ToastOptions) => {
    return toast(message, {
      duration: options?.duration || 4000,
      icon: 'ℹ️',
      style: {
        background: '#EFF6FF',
        color: '#1E40AF',
        border: '1px solid #3B82F6',
        borderRadius: '8px',
        padding: '16px',
        boxShadow: '0 4px 12px rgba(0, 0, 0, 0.15)',
        maxWidth: '500px',
        fontSize: '14px',
        fontWeight: '500'
      },
    })
  },

  promise: <T>(
    promise: Promise<T>,
    msgs: {
      loading: string
      success: string | ((data: T) => string)
      error: string | ((error: any) => string)
    },
    options?: ToastOptions
  ) => {
    return toast.promise(promise, msgs, {
      loading: {
        style: {
          background: '#EFF6FF',
          color: '#1E40AF',
          border: '1px solid #3B82F6',
          borderRadius: '8px',
          padding: '16px',
          boxShadow: '0 4px 12px rgba(0, 0, 0, 0.15)',
          maxWidth: '500px',
          fontSize: '14px',
          fontWeight: '500'
        },
      },
      success: {
        duration: options?.duration || 4000,
        style: {
          background: '#F0FDF4',
          color: '#065F46',
          border: '1px solid #10B981',
          borderRadius: '8px',
          padding: '16px',
          boxShadow: '0 4px 12px rgba(0, 0, 0, 0.15)',
          maxWidth: '500px',
          fontSize: '14px',
          fontWeight: '500'
        },
        iconTheme: {
          primary: '#10B981',
          secondary: '#F0FDF4',
        },
      },
      error: {
        duration: options?.duration || 6000,
        style: {
          background: '#FEF2F2',
          color: '#991B1B',
          border: '1px solid #EF4444',
          borderRadius: '8px',
          padding: '16px',
          boxShadow: '0 4px 12px rgba(0, 0, 0, 0.15)',
          maxWidth: '500px',
          fontSize: '14px',
          fontWeight: '500'
        },
        iconTheme: {
          primary: '#EF4444',
          secondary: '#FEF2F2',
        },
      },
    })
  },

  dismiss: (toastId?: string) => {
    return toast.dismiss(toastId)
  },

  remove: (toastId?: string) => {
    return toast.remove(toastId)
  }
}

export default customToast