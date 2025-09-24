# EV Charging Booking System - React Frontend

A modern, responsive React frontend for the EV Charging Booking System, built with Vite, TypeScript, Tailwind CSS, and shadcn/ui components.

## 🚀 Quick Start

### Prerequisites
- Node.js (v18 or higher)
- npm or yarn
- Backend API running on http://localhost:5001

### Installation & Setup

1. **Automated Setup (Recommended):**
   ```bash
   # Windows (Command Prompt)
   start-frontend.bat
   
   # Windows (PowerShell)
   .\start-frontend.ps1
   ```

2. **Manual Setup:**
   ```bash
   cd frontend
   npm install
   npm run dev
   ```

The frontend will be available at **http://localhost:3000**

## 🎯 Features

### ✅ Core Features
- **Modern React Architecture** - Built with React 18, TypeScript, and Vite
- **Responsive Design** - Mobile-first approach with Tailwind CSS
- **Component Library** - shadcn/ui components for consistent UI
- **State Management** - Zustand for global state and React Query for server state
- **Authentication** - Protected routes with JWT token handling
- **Real-time Updates** - WebSocket integration for live booking updates
- **Performance Optimized** - Code splitting, lazy loading, and optimized builds

### 📱 User Interface
- **Dashboard** - Statistics, charts, and real-time booking overview
- **Booking Management** - Full CRUD operations with advanced filtering
- **EV Owner Management** - Approve/reject registrations with bulk actions
- **Responsive Tables** - Advanced data tables with sorting and pagination
- **Interactive Charts** - Visual analytics with Recharts
- **Toast Notifications** - User feedback with Sonner toast library

### 🔒 Security
- **Protected Routes** - Authentication-based route protection
- **Token Management** - Automatic token refresh and secure storage
- **Role-based Access** - Different interfaces for Backoffice and Station Operators
- **API Error Handling** - Comprehensive error handling and user feedback

## 🏗️ Architecture

```
frontend/
├── src/
│   ├── components/          # Reusable UI components
│   │   ├── ui/             # shadcn/ui base components
│   │   ├── auth/           # Authentication components
│   │   ├── layout/         # Layout components
│   │   ├── booking/        # Booking-related components
│   │   └── charts/         # Chart components
│   ├── pages/              # Page components
│   │   ├── auth/           # Login/auth pages
│   │   ├── dashboard/      # Dashboard page
│   │   ├── bookings/       # Booking management
│   │   └── ev-owners/      # EV owner management
│   ├── services/           # API services
│   │   ├── api.ts          # Base API configuration
│   │   ├── auth.ts         # Authentication services
│   │   └── booking.ts      # Booking services
│   ├── hooks/              # Custom React hooks
│   ├── providers/          # Context providers
│   ├── types/              # TypeScript type definitions
│   ├── lib/                # Utility functions
│   └── assets/             # Static assets
├── public/                 # Public assets
└── dist/                   # Production build
```

## 🛠️ Technology Stack

| Technology | Purpose | Version |
|------------|---------|---------|
| **React** | UI Framework | ^18.2.0 |
| **TypeScript** | Type Safety | ^5.2.2 |
| **Vite** | Build Tool | ^4.5.0 |
| **Tailwind CSS** | Styling | ^3.3.5 |
| **shadcn/ui** | Component Library | Latest |
| **React Router** | Routing | ^6.18.0 |
| **React Query** | Server State | ^5.8.4 |
| **Zustand** | Client State | ^4.4.6 |
| **Axios** | HTTP Client | ^1.6.0 |
| **Recharts** | Charts | ^2.8.0 |
| **React Hook Form** | Forms | ^7.47.0 |
| **Zod** | Validation | ^3.22.4 |
| **Sonner** | Notifications | ^1.2.0 |

## 📊 Pages & Features

### 🏠 Dashboard (`/dashboard`)
- **Statistics Cards** - Total bookings, revenue, EV owners
- **Real-time Charts** - Booking trends and status distribution
- **Recent Activity** - Latest bookings and approvals
- **Quick Actions** - Direct access to common tasks

### 📋 Booking Management (`/bookings`)
- **Advanced Filtering** - By status, date range, station, user
- **Bulk Operations** - Approve/reject multiple bookings
- **QR Code Generation** - For approved bookings
- **Export Functionality** - CSV export with custom date ranges
- **Real-time Updates** - Live status changes via WebSocket

### 👥 EV Owner Management (`/ev-owners`)
- **Registration Approvals** - Review and approve new registrations
- **Account Management** - Activate/deactivate accounts
- **Bulk Actions** - Mass approval/rejection of registrations
- **Search & Filter** - Find users by various criteria

### 🔐 Authentication (`/login`)
- **Secure Login** - JWT-based authentication
- **Remember Me** - Persistent login sessions
- **Error Handling** - Clear error messages and validation
- **Auto-redirect** - Redirect to intended page after login

## 🎨 UI/UX Features

### Responsive Design
- **Mobile-first** approach with breakpoints at 640px, 768px, 1024px, 1280px
- **Adaptive layouts** that work on phones, tablets, and desktops
- **Touch-friendly** interactions for mobile devices
- **Progressive enhancement** for larger screens

### Component Design System
- **Consistent spacing** using Tailwind's spacing scale
- **Color palette** with semantic color names (primary, secondary, destructive)
- **Typography hierarchy** with proper font sizes and weights
- **Interactive states** with hover, focus, and active states

### Performance Optimizations
- **Code splitting** at route level
- **Lazy loading** of components and images
- **Memoization** of expensive calculations
- **Optimized bundle size** with tree shaking
- **Efficient re-renders** with proper dependency arrays

## 🔧 Development

### Available Scripts
```bash
# Development server
npm run dev

# Production build
npm run build

# Preview production build
npm run preview

# Type checking
npm run type-check

# Linting
npm run lint
```

### Environment Variables
Create a `.env` file in the frontend directory:
```env
VITE_API_BASE_URL=http://localhost:5001/api
VITE_APP_TITLE=EV Charging Booking System
```

### Custom Hooks
- **useAuth** - Authentication state management
- **useBookings** - Booking data fetching and caching
- **useEVOwners** - EV owner management
- **useLocalStorage** - Persistent local storage
- **useDebounce** - Debounced values for search

### State Management
- **Authentication State** - User session and auth status
- **UI State** - Modals, loading states, notifications
- **Server State** - API data with React Query caching
- **Form State** - Form data with React Hook Form

## 🌐 API Integration

### Base Configuration
- **Base URL**: `http://localhost:5001/api`
- **Timeout**: 10 seconds
- **Headers**: Content-Type: application/json
- **Interceptors**: Request/response interceptors for auth

### Endpoints
- **Authentication**: `/webusers/login`
- **Bookings**: `/bookings/*`
- **EV Owners**: `/evowners/*`
- **Statistics**: `/bookings/statistics`
- **QR Codes**: `/bookings/{id}/qrcode`

### Error Handling
- **Network errors** - Retry logic and offline handling
- **HTTP errors** - Status code specific error messages
- **Validation errors** - Form field error highlighting
- **Auth errors** - Automatic logout and redirect

## 📱 Responsive Breakpoints

| Breakpoint | Min Width | Target Device |
|------------|-----------|---------------|
| **sm** | 640px | Large phones |
| **md** | 768px | Tablets |
| **lg** | 1024px | Laptops |
| **xl** | 1280px | Desktops |
| **2xl** | 1536px | Large displays |

## 🚀 Deployment

### Production Build
```bash
npm run build
```

### Build Optimizations
- **Bundle splitting** - Separate vendor and app bundles
- **Asset optimization** - Compressed images and fonts
- **Tree shaking** - Remove unused code
- **Minification** - Compressed CSS and JavaScript

### Deployment Options
1. **Static hosting** - Netlify, Vercel, AWS S3
2. **CDN deployment** - CloudFront, CloudFlare
3. **Container deployment** - Docker with Nginx
4. **Server deployment** - Node.js with Express

## 🔍 Testing

### Test Setup
```bash
# Install testing dependencies
npm install --save-dev @testing-library/react @testing-library/jest-dom vitest

# Run tests
npm run test
```

### Test Coverage
- **Component tests** - UI component behavior
- **Hook tests** - Custom hook functionality
- **Integration tests** - API service integration
- **E2E tests** - Critical user journeys

## 🐛 Troubleshooting

### Common Issues

1. **Dependencies not installing**
   ```bash
   # Clear npm cache
   npm cache clean --force
   
   # Delete node_modules and reinstall
   rm -rf node_modules package-lock.json
   npm install
   ```

2. **Development server won't start**
   ```bash
   # Check if port 3000 is in use
   netstat -an | findstr :3000
   
   # Kill the process using the port
   taskkill /PID <process-id> /F
   ```

3. **API connection issues**
   - Verify backend is running on http://localhost:5001
   - Check CORS configuration in backend
   - Verify API endpoints in browser network tab

4. **Build errors**
   ```bash
   # Type check the project
   npm run type-check
   
   # Fix ESLint issues
   npm run lint --fix
   ```

## 📚 Documentation

### Component Documentation
Each component includes:
- **Props interface** with TypeScript definitions
- **Usage examples** in component files
- **Styling guidelines** with Tailwind classes
- **Accessibility considerations** with ARIA attributes

### API Documentation
- **Service methods** with parameter and return types
- **Error handling** examples and patterns
- **Authentication** requirements and flows
- **Rate limiting** and caching strategies

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/new-feature`)
3. Commit changes (`git commit -am 'Add new feature'`)
4. Push to branch (`git push origin feature/new-feature`)
5. Create Pull Request

### Code Standards
- **TypeScript** for type safety
- **ESLint** for code quality
- **Prettier** for code formatting
- **Conventional commits** for commit messages

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

---

**Frontend URL**: http://localhost:3000
**Backend API**: http://localhost:5001/api
**Documentation**: http://localhost:5001/swagger