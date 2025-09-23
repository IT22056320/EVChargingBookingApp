# EV Charging Booking System

A comprehensive EV charging station booking system with real-time updates, built with ASP.NET Core 8.0, MongoDB, and SignalR.

## 🚀 Quick Start

### Prerequisites
- .NET 8.0 SDK
- MongoDB (Atlas or local instance)
- Git

### 📡 Application Ports (UPDATED)

| Application | Service | URL | Status |
|-------------|---------|-----|--------|
| **Backend API** | HTTP | http://localhost:5001 | ✅ Primary |
| **Backend API** | HTTPS | https://localhost:7001 | ⚠️ Requires SSL cert |
| **API Documentation** | Swagger | http://localhost:5001/swagger | ✅ Working |
| **Frontend Web App** | HTTP | http://localhost:5000 | ✅ Primary |
| **Frontend Web App** | HTTPS | https://localhost:7000 | ⚠️ Requires SSL cert |
| **SignalR Hub** | WebSocket | http://localhost:5001/bookingNotificationHub | ✅ Working |

> **Note**: For development, use HTTP ports (5001 for API, 5000 for Web App) to avoid SSL certificate issues.

### 🏃‍♂️ Running the Application

#### Option 1: Using Startup Scripts (Recommended)

**Windows (Batch):**
```batch
start-dev.bat
```

**Windows (PowerShell):**
```powershell
.\start-dev.ps1
```

#### Option 2: Manual Start

**1. Start Backend API:**
```bash
cd WebApplication1
dotnet restore
dotnet run
```

**2. Start Frontend Web App (in new terminal):**
```bash
cd WebApplicationFrontend
dotnet restore
dotnet run
```

### 🔐 Default Login Credentials

| Role | Email | Password |
|------|-------|----------|
| **Admin/Backoffice** | admin@evcharging.com | Admin123! |

### 🌐 Application Access

1. **Web Application (Admin Interface)**: http://localhost:5000
2. **API Documentation**: http://localhost:5001/swagger
3. **Health Check**: http://localhost:5001/api/system-test

### 📱 API Endpoints

#### Core Booking APIs
- `GET /api/bookings` - Get all bookings with filtering
- `POST /api/bookings` - Create new booking
- `GET /api/bookings/{id}` - Get booking by ID
- `PUT /api/bookings/{id}` - Update booking
- `PATCH /api/bookings/{id}/status` - Update booking status
- `DELETE /api/bookings/{id}` - Cancel booking

#### Management APIs
- `POST /api/bookings/{id}/approve` - Approve booking
- `POST /api/bookings/{id}/cancel` - Cancel booking
- `GET /api/bookings/statistics` - Get booking statistics
- `GET /api/bookings/{id}/qrcode` - Get QR code for booking

#### User Management
- `GET /api/users` - Get all users
- `POST /api/users` - Create user
- `GET /api/webusers` - Get web users
- `POST /api/webusers` - Create web user

### 🔄 Real-time Features

The application uses SignalR for real-time updates:
- **Booking status changes**
- **New booking notifications**
- **Bulk operation updates**
- **QR code generation alerts**

### 🗃️ Database Configuration

MongoDB connection is configured in `appsettings.json`:
```json
{
  "MongoDB": {
    "ConnectionString": "your-mongodb-connection-string",
    "DatabaseName": "EVChargingDB"
  }
}
```

### 🔧 Development Configuration

#### Backend (WebApplication1)
- **Port**: 7001 (HTTPS), 5001 (HTTP)
- **Environment**: Development
- **Swagger**: Enabled at http://localhost:5001/swagger
- **CORS**: Configured for frontend origins

#### Frontend (WebApplicationFrontend)
- **Port**: 7000 (HTTPS), 5000 (HTTP)
- **API Base URL**: http://localhost:5001/api (Updated for development)
- **Session Timeout**: 30 minutes

### 🏗️ Architecture

```
┌─────────────────┐    HTTPS/7000    ┌──────────────────┐
│   Frontend      │◄─────────────────┤    Browser       │
│   (MVC Web App) │                  │                  │
└─────────────────┘                  └──────────────────┘
         │
         │ API Calls (HTTPS/7001/api)
         │ SignalR (HTTPS/7001/bookingNotificationHub)
         ▼
┌─────────────────┐                  ┌──────────────────┐
│   Backend       │◄─────────────────┤    MongoDB       │
│   (Web API)     │                  │    Database      │
└─────────────────┘                  └──────────────────┘
```

### 🧪 Testing

#### Test Mobile App Registration
```bash
POST https://localhost:7001/api/MobileTest/test-register
```

#### Test Login
```bash
POST https://localhost:7001/api/MobileTest/test-login
```

### 📝 Features

#### ✅ Backend Features
- ✅ Complete CRUD operations for bookings
- ✅ Business rule validation (7-day booking window, 12-hour modification limit)
- ✅ QR code generation for approved bookings
- ✅ Real-time notifications with SignalR
- ✅ MongoDB integration with proper indexing
- ✅ Comprehensive API documentation
- ✅ Statistics and reporting

#### ✅ Frontend Features
- ✅ Admin dashboard with booking management
- ✅ Real-time updates for booking status changes
- ✅ Bulk operations (approve/reject multiple bookings)
- ✅ Advanced filtering and search
- ✅ Statistics dashboard with charts
- ✅ CSV export functionality
- ✅ Responsive design

### 🚨 Troubleshooting

#### Port Conflicts
If ports 7000, 7001, 5000, or 5001 are already in use:
1. Check running processes: `netstat -ano | findstr :7000`
2. Kill the process: `taskkill /PID <process-id> /F`
3. Or modify ports in `launchSettings.json`

#### MongoDB Connection Issues
1. Verify MongoDB connection string in `appsettings.json`
2. Check MongoDB service is running
3. Verify network connectivity

#### CORS Issues
Ensure frontend URL is allowed in backend CORS policy in `Program.cs`

### 📞 Support

For development issues:
1. Check console logs in both applications
2. Verify all services are running on correct ports
3. Check MongoDB connectivity
4. Validate API responses in Swagger UI