Searched codebase for "give me a readme file for this app", 12 results

# EV Charging Booking Application

A comprehensive electric vehicle charging station booking and management system with web admin interface and mobile application for EV owners and station operators.

---

## 📋 Table of Contents

- Overview
- Features
- Technology Stack
- Architecture
- Prerequisites
- Installation
- Running the Application
- User Roles & Permissions
- API Documentation
- Mobile App Setup
- Deployment
- Testing
- Troubleshooting
- Team Members & Responsibilities
- License

---

## 🎯 Overview

The EV Charging Booking Application is an enterprise-grade solution for managing electric vehicle charging station reservations. It provides a seamless experience for EV owners to book charging slots, station operators to manage charging sessions, and administrators to oversee the entire system.

### Key Highlights

- **Real-time booking management** with instant status updates
- **QR code-based verification** for secure check-in
- **Multi-platform support** (Web + Android Mobile)
- **Role-based access control** (Admin, Station Operator, EV Owner)
- **Business rule enforcement** (7-day booking window, 12-hour modification limit)
- **Comprehensive analytics** and reporting dashboards

---

## ✨ Features

### 🌐 Web Application (Admin & Backoffice)

#### Dashboard & Analytics
- Real-time booking statistics and revenue tracking
- Interactive charts showing booking trends and patterns
- Station utilization metrics
- Recent activity timeline

#### Booking Management
- Advanced filtering (status, date range, station, user)
- Bulk operations (approve/reject multiple bookings)
- QR code generation for approved bookings
- Export functionality (CSV with custom date ranges)
- Booking modification approval workflow
- Real-time status updates via SignalR WebSocket

#### Station Management
- Create and configure charging stations
- Set pricing, connector types, and availability
- Location-based station mapping
- Station activation/deactivation with validation
- Operating hours and amenities management

#### User Management
- EV owner registration approvals
- Account activation/deactivation
- User profile management
- Search and filter capabilities

### 📱 Mobile Application (EV Owners & Station Operators)

#### For EV Owners
- **Browse Charging Stations** - View nearby stations with filters
- **Book Charging Slots** - Reserve time slots up to 7 days in advance
- **Modify Bookings** - Request changes up to 12 hours before booking
- **View History** - Track past and upcoming reservations
- **QR Code Display** - Show booking QR code for check-in
- **Push Notifications** - Receive booking status updates
- **Offline Mode** - SQLite local database for offline access

#### For Station Operators
- **QR Code Scanner** - Scan and verify customer bookings
- **Complete Sessions** - Mark charging complete with energy consumed
- **Manage Queue** - View and process pending bookings
- **Real-time Updates** - Instant notification of new bookings

### 🔒 Security Features

- JWT token-based authentication
- Role-based authorization
- QR code encryption and validation
- Station-specific access control for operators
- Input validation and sanitization
- SQL injection prevention (NoSQL with MongoDB)

---

## 🛠️ Technology Stack

### Backend API
| Technology | Version | Purpose |
|------------|---------|---------|
| **ASP.NET Core** | 8.0 | Web API framework |
| **C#** | 12.0 | Programming language |
| **MongoDB** | 7.0 | NoSQL database |
| **SignalR** | 8.0 | Real-time communications |
| **Swagger/OpenAPI** | 3.0 | API documentation |

### Frontend Web
| Technology | Version | Purpose |
|------------|---------|---------|
| **React** | 18.2.0 | UI framework |
| **TypeScript** | 5.2.2 | Type safety |
| **Vite** | 4.5.0 | Build tool |
| **Tailwind CSS** | 3.3.5 | Styling |
| **shadcn/ui** | Latest | Component library |
| **React Router** | 6.18.0 | Routing |
| **React Query** | 5.8.4 | Server state management |
| **Axios** | 1.6.0 | HTTP client |
| **Recharts** | 2.8.0 | Data visualization |

### Mobile App
| Technology | Version | Purpose |
|------------|---------|---------|
| **Kotlin** | 1.9.0 | Programming language |
| **Jetpack Compose** | 1.5.4 | UI framework |
| **Android SDK** | 34 | Platform SDK |
| **Room** | 2.6.0 | SQLite database |
| **Retrofit** | 2.9.0 | API client |
| **Google Maps SDK** | 18.2.0 | Location services |
| **ZXing** | 3.5.2 | QR code scanning |

### Infrastructure
| Component | Technology |
|-----------|------------|
| **Web Server** | IIS 10.0 |
| **Hosting** | Windows Server 2019+ |
| **Database** | MongoDB Atlas (Cloud) |
| **Version Control** | Git + GitHub |

---

## 🏗️ Architecture

```
EVChargingBookingApp/
├── WebApplication1/              # Backend API (.NET 8.0)
│   ├── Controllers/              # API endpoints
│   │   ├── BookingsController.cs
│   │   ├── ChargingStationsController.cs
│   │   ├── UsersController.cs
│   │   └── HealthController.cs
│   ├── Services/                 # Business logic
│   │   ├── BookingService.cs
│   │   ├── QRCodeService.cs
│   │   ├── ChargingStationService.cs
│   │   └── NotificationService.cs
│   ├── Models/                   # Data models
│   ├── DTOs/                     # Data transfer objects
│   ├── Hubs/                     # SignalR hubs
│   └── appsettings.json          # Configuration
│
├── frontend/                     # React Web App
│   ├── src/
│   │   ├── components/           # UI components
│   │   ├── pages/                # Page components
│   │   ├── services/             # API services
│   │   ├── providers/            # Context providers
│   │   └── utils/                # Utilities
│   └── package.json
│
├── app/                          # Android Mobile App
│   ├── src/main/
│   │   ├── java/com/example/evchargingmobile/
│   │   │   ├── ui/screens/       # UI screens
│   │   │   ├── network/          # API client
│   │   │   ├── data/             # Database & repositories
│   │   │   └── utils/            # Utilities
│   │   └── res/                  # Resources
│   └── build.gradle.kts
│
└── deploy/                       # Deployment scripts
    ├── deploy-to-iis.ps1         # IIS deployment automation
    ├── fix-network-access.ps1    # Network configuration
    └── DEPLOYMENT-GUIDE.md       # Deployment documentation
```

---

## 📦 Prerequisites

### Development Environment
- **Windows 10/11** or **Windows Server 2019+**
- **.NET 8.0 SDK** ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
- **Node.js 18+** and npm ([Download](https://nodejs.org/))
- **MongoDB Atlas Account** (Free tier available) or local MongoDB
- **Git** for version control
- **Android Studio** (for mobile development)
- **IIS 10.0+** (Windows Feature - for deployment)

### Runtime Requirements
- **8GB RAM** minimum (16GB recommended)
- **10GB free disk space**
- **Internet connection** for MongoDB Atlas

---

## 🚀 Installation

### 1. Clone the Repository

```bash
git clone https://github.com/IT22056320/EVChargingBookingApp.git
cd EVChargingBookingApp
```

### 2. Backend Setup

```bash
cd WebApplication1

# Restore NuGet packages
dotnet restore

# Update MongoDB connection string in appsettings.json
# Edit: WebApplication1/appsettings.json
{
  "MongoDB": {
    "ConnectionString": "your-mongodb-atlas-connection-string",
    "DatabaseName": "EVChargingDB"
  }
}

# Build the project
dotnet build
```

### 3. Frontend Setup

```bash
cd ../frontend

# Install dependencies
npm install

# Configure API base URL (optional - defaults to localhost:5001)
# Create .env file:
VITE_API_BASE_URL=http://localhost:5001/api
```

### 4. Mobile App Setup

```bash
cd ../app

# Open in Android Studio
# Update API base URL in:
# app/src/main/java/com/example/evchargingmobile/network/ApiService.kt

private const val BASE_URL = "http://YOUR_SERVER_IP:5001/api/"

# Sync Gradle files
# Build and run on emulator or device
```

---

## 🏃‍♂️ Running the Application

### Option 1: Automated Startup (Recommended)

**Windows Batch:**
```batch
start-dev.bat
```

**Windows PowerShell:**
```powershell
.\start-dev.ps1
```

This will start both backend and frontend automatically.

### Option 2: Manual Startup

**Terminal 1 - Backend API:**
```bash
cd WebApplication1
dotnet run
```

**Terminal 2 - Frontend Web:**
```bash
cd frontend
npm run dev
```

### Access Points

| Service | URL | Description |
|---------|-----|-------------|
| **Backend API** | http://localhost:5001 | REST API endpoints |
| **API Documentation** | http://localhost:5001/swagger | Interactive API docs |
| **Frontend Web** | http://localhost:3000 | Admin dashboard |
| **Health Check** | http://localhost:5001/api/health | System status |

---

## 👥 User Roles & Permissions

### 🔑 Default Login Credentials

| Role | Email | Password | Access |
|------|-------|----------|--------|
| **Admin/Backoffice** | admin@evcharging.com | Admin123! | Full system access |
| **Station Operator** | operator@test.com | Operator@123 | Assigned station only |
| **EV Owner** | owner@test.com | Owner@123 | Personal bookings |

### Permission Matrix

| Feature | Admin | Station Operator | EV Owner |
|---------|-------|------------------|----------|
| View all bookings | ✅ | ❌ (Station only) | ❌ (Own only) |
| Approve bookings | ✅ | ✅ | ❌ |
| Create stations | ✅ | ❌ | ❌ |
| Deactivate stations | ✅ | ❌ | ❌ |
| Scan QR codes | ✅ | ✅ | ❌ |
| Create bookings | ✅ | ❌ | ✅ |
| Modify bookings | ✅ (Direct) | ❌ | ✅ (Request) |
| View analytics | ✅ | ✅ (Station) | ❌ |

---

## 📚 API Documentation

### Core Endpoints

#### Bookings
```http
POST   /api/Bookings                    # Create booking
GET    /api/Bookings/{id}               # Get booking by ID
GET    /api/Bookings                    # Get all bookings (paginated)
GET    /api/Bookings/user/{userId}      # Get user bookings
PUT    /api/Bookings/{id}               # Update booking
DELETE /api/Bookings/{id}               # Delete booking
POST   /api/Bookings/{id}/approve       # Approve booking
POST   /api/Bookings/{id}/cancel        # Cancel booking
GET    /api/Bookings/{id}/qrcode        # Get QR code image
POST   /api/Bookings/validate-qr        # Validate QR code
GET    /api/Bookings/{id}/verify        # Verify booking (Operator)
POST   /api/Bookings/{id}/complete      # Complete session (Operator)
```

#### Charging Stations
```http
GET    /api/ChargingStations            # Get all stations
GET    /api/ChargingStations/{id}       # Get station by ID
POST   /api/ChargingStations            # Create station
PUT    /api/ChargingStations/{id}       # Update station
DELETE /api/ChargingStations/{id}       # Deactivate station
GET    /api/ChargingStations/nearby     # Get nearby stations
```

#### Users
```http
POST   /api/Users/register              # Register EV owner
POST   /api/Users/login                 # Login
GET    /api/Users/{id}                  # Get user profile
PUT    /api/Users/{id}                  # Update profile
GET    /api/Users/pending-approvals     # Get pending registrations
POST   /api/Users/{id}/approve          # Approve registration
```

#### Health & Monitoring
```http
GET    /api/health                      # Basic health check
GET    /api/health/detailed             # Detailed system info
GET    /api/health/ping                 # Simple ping
```

### Request Examples

**Create Booking:**
```json
POST /api/Bookings
{
  "userId": "user123",
  "chargingStationId": "station456",
  "bookingDate": "2025-10-15",
  "startTime": "2025-10-15T10:00:00Z",
  "endTime": "2025-10-15T12:00:00Z",
  "vehicleNumber": "ABC-1234",
  "vehicleType": "Tesla Model 3",
  "estimatedChargingTimeMinutes": 120
}
```

**Complete Session:**
```json
POST /api/Bookings/{id}/complete
{
  "energyConsumedKWh": 45.5,
  "notes": "Charging completed successfully"
}
```

### Business Rules

1. **7-Day Booking Window** - Bookings can only be made up to 7 days in advance
2. **12-Hour Modification Limit** - Modifications require admin approval if within 12 hours of start time
3. **Station Deactivation** - Stations with active bookings cannot be deactivated
4. **QR Code Security** - QR codes are station-specific and expire after use

Full API documentation available at: **http://localhost:5001/swagger**

---

## 📱 Mobile App Setup

### Building the APK

```bash
cd app

# Build debug APK
./gradlew assembleDebug

# APK location:
# app/build/outputs/apk/debug/app-debug.apk
```

### Network Configuration

For testing on same WiFi network:

1. **Find your PC's IP address:**
```powershell
ipconfig | Select-String "IPv4"
# Example output: 192.168.1.8
```

2. **Update mobile app API URL:**
```kotlin
// app/src/main/java/com/example/evchargingmobile/network/ApiService.kt
private const val BASE_URL = "http://192.168.1.8:5001/api/"
```

3. **Configure IIS for network access:**
```powershell
cd deploy
.\fix-network-access.ps1
```

4. **Rebuild and install APK**

### Testing Checklist

- [ ] Login with test credentials
- [ ] Browse charging stations
- [ ] Create a booking
- [ ] View QR code for approved booking
- [ ] Scan QR code (operator mode)
- [ ] Complete charging session
- [ ] View booking history

---

## 🌐 Deployment

### IIS Deployment (Automated)

**Prerequisites:**
- Windows Server 2019+ or Windows 10/11 Pro
- IIS 10.0+ installed with ASP.NET Core Module V2
- .NET 8.0 Hosting Bundle

**Deploy to IIS:**
```powershell
cd deploy

# Run as Administrator
.\deploy-to-iis.ps1

# Or use batch file
deploy.bat
```

The script will:
1. ✅ Pull latest code from GitHub
2. ✅ Build and publish .NET project
3. ✅ Configure IIS automatically
4. ✅ Create App Pool and Website
5. ✅ Add firewall rules
6. ✅ Test deployment

**Manual Deployment:**

See complete guide: DEPLOYMENT-GUIDE.md

### Production Considerations

- **SSL/TLS** - Configure HTTPS with valid certificate
- **MongoDB Atlas** - Use production connection string
- **CORS** - Update allowed origins for production domains
- **Logging** - Configure Application Insights or ELK stack
- **Backups** - Schedule MongoDB Atlas backups
- **Monitoring** - Set up health check monitoring
- **Rate Limiting** - Implement API rate limiting
- **CDN** - Use CDN for frontend static assets

---

## 🧪 Testing

### Backend API Testing

**Using Swagger UI:**
```
http://localhost:5001/swagger
```

**Using cURL:**
```bash
# Health check
curl http://localhost:5001/api/health

# Create booking (requires auth)
curl -X POST http://localhost:5001/api/Bookings \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "userId": "user123",
    "chargingStationId": "station456",
    "bookingDate": "2025-10-15",
    "startTime": "2025-10-15T10:00:00Z",
    "endTime": "2025-10-15T12:00:00Z",
    "vehicleNumber": "ABC-1234"
  }'
```

### Frontend Testing

```bash
cd frontend

# Run tests
npm run test

# Type checking
npm run type-check

# Lint
npm run lint
```

### Mobile App Testing

```bash
cd app

# Run unit tests
./gradlew test

# Run instrumentation tests
./gradlew connectedAndroidTest
```

### Test Accounts

Test data is automatically seeded. See User Roles for credentials.

---

## 🐛 Troubleshooting

### Common Issues

#### 1. Backend won't start
```bash
# Check if port 5001 is in use
netstat -ano | findstr :5001

# Kill process using port
taskkill /PID <process-id> /F
```

#### 2. MongoDB connection failed
- Verify connection string in `appsettings.json`
- Check MongoDB Atlas network access (whitelist your IP)
- Test connectivity: https://cloud.mongodb.com

#### 3. Frontend API calls fail
- Ensure backend is running on port 5001
- Check CORS configuration in Program.cs
- Verify API base URL in frontend `.env` file

#### 4. Mobile app "Cannot reach site"
- Ensure PC and phone are on same WiFi
- Verify IP address is correct in `ApiService.kt`
- Check Windows Firewall allows port 5001
- Run `fix-network-access.ps1` script
- Test in phone browser: `http://YOUR_IP:5001/api/health`

#### 5. QR code scanning fails
- Grant camera permissions to mobile app
- Ensure booking is in "Approved" status
- Check QR code is generated (not expired)
- Verify operator is assigned to correct station

#### 6. SignalR connection issues
```javascript
// Check browser console for WebSocket errors
// Verify SignalR hub URL: http://localhost:5001/bookingNotificationHub
```

### Debug Mode

**Backend:**
```bash
cd WebApplication1
dotnet run --environment Development
```

**Frontend:**
```bash
cd frontend
npm run dev -- --host
```

### Logs Location

- **Backend:** `WebApplication1/logs/` (if file logging enabled)
- **IIS:** logs
- **Mobile:** Android Studio Logcat

---

## 👨‍💻 Team Members & Responsibilities

### Member 1: Madhini - Web Charging Station Management
**Student ID:** IT22000000

**Responsibilities:**
- Backend: `ChargingStationsController` (CRUD operations)
- Station models with location, connector types
- Business rule: Prevent deactivation with active bookings
- Frontend: Station management pages for Backoffice users
- Role-based access control for station operations

**Key Files:**
- ChargingStationsController.cs
- ChargingStationService.cs
- station-mgt

---

### Member 2: Sandun - Web Booking Management
**Student ID:** IT22056320

**Responsibilities:**
- Backend: `BookingsController` with full CRUD
- Business rules: 7-day window, 12-hour modification limit
- QR code generation for approved bookings
- Booking status workflow (Pending/Approved/Completed/Cancelled)
- Frontend: Booking management interface for Backoffice
- Real-time status updates with SignalR

**Key Files:**
- BookingsController.cs
- BookingService.cs
- QRCodeService.cs
- bookings.tsx

---

### Member 3: Kavindya - Mobile Reservation Management
**Student ID:** IT22000000

**Responsibilities:**
- Mobile booking creation with station selection
- Booking modification/cancellation (12-hour rule enforcement)
- Booking confirmation summaries
- QR code display for approved bookings
- Booking history and upcoming reservations view
- Google Maps integration for station selection

**Key Files:**
- booking
- `app/src/main/java/com/example/evchargingmobile/data/BookingRepository.kt`

---

### Member 4: Udula - Mobile EV Operator Functions
**Student ID:** IT22000000

**Responsibilities:**
- Station Operator login to mobile app
- QR code scanner functionality
- Retrieve and confirm booking data from server
- Mark charging sessions as complete
- Update slot availability in real-time
- Handle booking finalization workflow

**Key Files:**
- operator
- `app/src/main/java/com/example/evchargingmobile/ui/screens/scanner/`

---

## 📄 Project Documentation

| Document | Description |
|----------|-------------|
| DEVELOPMENT-GUIDE.md | Complete development guide |
| DEPLOYMENT-GUIDE.md | Deployment instructions |
| QUICK-START.md | Quick reference guide |
| README.md | Frontend-specific documentation |
| CODE-DOCUMENTATION-GUIDE.md | Code documentation standards |
| `CHALLENGES.md` | Technical challenges & solutions |

---

## 📊 Project Statistics

- **Total Lines of Code:** ~50,000+
- **Backend API Endpoints:** 40+
- **Frontend Pages:** 15+
- **Mobile Screens:** 20+
- **Database Collections:** 8
- **Business Rules:** 15+
- **Team Members:** 4
- **Development Duration:** 3 months

---

## 🚀 Future Enhancements

- [ ] Payment gateway integration (Stripe/PayPal)
- [ ] Push notifications for mobile (FCM)
- [ ] Advanced analytics dashboard with ML predictions
- [ ] Multi-language support (i18n)
- [ ] Mobile app for iOS
- [ ] Live station occupancy tracking
- [ ] Integration with EV manufacturer APIs
- [ ] Carbon footprint calculator
- [ ] Loyalty program and rewards
- [ ] API rate limiting and caching (Redis)

---

## 📞 Support

For technical support or questions:

- **Project Repository:** [GitHub - EVChargingBookingApp](https://github.com/IT22056320/EVChargingBookingApp)
- **API Documentation:** http://localhost:5001/swagger
- **Issue Tracker:** GitHub Issues

---

## 📄 License

This project is developed as part of academic coursework at SLIIT (Sri Lanka Institute of Information Technology) for the Enterprise Application Development module.

**Copyright © 2025 EV Charging Team. All rights reserved.**

---

## ⚡ Quick Links

| Resource | URL |
|----------|-----|
| **Live API** | http://localhost:5001 |
| **API Docs** | http://localhost:5001/swagger |
| **Web Dashboard** | http://localhost:3000 |
| **Health Check** | http://localhost:5001/api/health |
| **GitHub** | https://github.com/IT22056320/EVChargingBookingApp |

---

**Version:** 1.0.0  
**Last Updated:** October 9, 2025  
**Built with ❤️ by EV Charging Team**
