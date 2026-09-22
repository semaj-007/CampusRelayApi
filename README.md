# CampusRelay API

ASP.NET Core 8 Web API backend for CampusRelay (PROG7314 POE), built from Part 1's
"REST Api and Backend Architecture" and "Data Models and Schema Definitions" sections.

## 🚀 Getting Started

### Local Development (SQLite)

1. Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
2. From the project root:
   ```bash
   cd CampusRelay.Api
   dotnet restore
   dotnet run
   ```
3. Open Swagger UI at `https://localhost:5001/swagger`
4. Test with dev login:
   ```bash
   curl -X POST https://localhost:5001/api/v1/auth/dev-login \
     -H "Content-Type: application/json" \
     -d '{"email":"test@example.com","fullName":"Test User"}'
   ```

**No database setup needed** - SQLite file (`campusrelay-dev.db`) is auto-created.

### Pointing Android App at Local Backend

The Android emulator can reach your localhost via `10.0.2.2`:
```kotlin
// In Constants.kt
const val BASE_URL = "https://10.0.2.2:5001/"
```

## 🌐 Deployment Options

### Option A: Render (Recommended for Students)

**✅ Free tier available, PostgreSQL included, easy deployment**

#### Prerequisites
- [Render account](https://render.com) (free)
- Firebase project with service account JSON

#### Deployment Steps

1. **Create PostgreSQL Database on Render**
   - Go to [Render Dashboard](https://dashboard.render.com)
   - New → PostgreSQL
   - Name: `campusrelay-db`
   - Region: `Frankfurt` (or nearest)
   - Plan: Free
   - Create

2. **Create Web Service**
   - New → Web Service
   - Connect GitHub repository: `semaj-007/CampusRelayApi`
   - Name: `campusrelay-api`
   - Region: Same as your database
   - Branch: `main`
   - Root Directory: `CampusRelay.Api`
   - Build Command: `dotnet publish -c Release -o ./publish`
   - Start Command: `dotnet CampusRelay.Api.dll`
   - Publish Directory: `./publish`

3. **Add Environment Variables**

   | Key | Value | Notes |
   |-----|-------|-------|
   | `DATABASE_URL` | From your PostgreSQL database | Auto-populated if linked |
   | `ASPNETCORE_ENVIRONMENT` | `Production` | Required |
   | `Jwt__Issuer` | `campusrelay-api` | JWT issuer |
   | `Jwt__Audience` | `campusrelay-app` | JWT audience |
   | `Jwt__Key` | 32+ character secret | Generate with: `openssl rand -base64 32` |
   | `FIREBASE_PROJECT_ID` | Your Firebase project ID | From Firebase Console |
   | `FIREBASE_SERVICE_ACCOUNT_B64` | Base64 encoded JSON | `base64 firebase-service-account.json` |

4. **Link Database to Service**
   - In Web Service settings → Environment
   - Add DATABASE_URL from your PostgreSQL database
   - Or use `fromDatabase` in render.yaml

5. **Deploy!**
   - Click "Deploy" and wait 2-5 minutes
   - Your API will be live at: `https://campusrelay-api.onrender.com`

6. **Test Deployment**
   ```bash
   curl -X POST https://campusrelay-api.onrender.com/api/v1/auth/dev-login \
     -H "Content-Type: application/json" \
     -d '{"email":"test@example.com","fullName":"Test User"}'
   ```

#### render.yaml Configuration

```yaml
services:
  - type: web
    name: campusrelay-api
    runtime: dotnet
    rootDir: CampusRelay.Api
    buildCommand: dotnet publish -c Release -o ./publish
    startCommand: dotnet CampusRelay.Api.dll
    publishDir: ./publish
    envVars:
      - key: ASPNETCORE_ENVIRONMENT
        value: Production
      - key: Jwt__Issuer
        value: campusrelay-api
      - key: Jwt__Audience
        value: campusrelay-app
      - key: Jwt__Key
        value: YOUR_32_CHARACTER_SECRET
      - key: Firebase__ProjectId
        value: YOUR_FIREBASE_PROJECT_ID
      - key: FIREBASE_SERVICE_ACCOUNT_B64
        value: YOUR_BASE64_ENCODED_JSON
      - key: DATABASE_URL
        fromDatabase:
          name: campusrelay-db
          property: connectionString

databases:
  - type: pgsql
    name: campusrelay-db
    plan: free
    region: frankfurt
```

### Option B: Azure App Service

**For Azure for Students users**

1. Create Resource Group in **East US** (required for Azure for Students)
2. Create Azure SQL Database
3. Update connection string in `appsettings.json`
4. Deploy via Visual Studio or Azure CLI

**Note**: Azure for Students has **region restrictions** - South Africa regions are NOT allowed.

### Option C: Local/Other Hosting

The API supports:
- **SQLite** (Development)
- **PostgreSQL** (Render, production)
- **Azure SQL** (Azure deployments)

Update `Program.cs` and connection strings accordingly.

## 🔐 Authentication

The API now uses **Firebase Authentication** for SSO login (replacing Azure AD).

### How It Works

1. **Android App** → Firebase Auth → Firebase ID Token
2. **Android App** → API (`POST /api/v1/auth/sso`) with Firebase ID Token
3. **API** → Validates Firebase token → Creates/finds user → Returns JWT
4. **Android App** → Uses JWT for all subsequent API calls

### Backend Endpoints

| Method | Route | Purpose | Auth Required |
|---|---|---|---|
| POST | `/api/v1/auth/sso` | Firebase SSO login | ❌ |
| POST | `/api/v1/auth/dev-login` | Dev/testing only | ❌ |
| POST | `/api/v1/deliveries` | Create delivery request | ✅ |
| GET | `/api/v1/deliveries/{id}` | Get delivery details | ✅ |
| POST | `/api/v1/deliveries/sync-offline` | Sync offline transactions | ✅ |

### JWT Token Flow

1. Client sends Firebase ID token to `/api/v1/auth/sso`
2. Backend validates token with Firebase Admin SDK
3. Backend creates/finds user in database
4. Backend returns JWT token for API access
5. Client includes JWT in `Authorization: Bearer <token>` header

## 🗄 Database Configuration

### Supported Databases

| Environment | Database | Connection String |
|-------------|----------|-------------------|
| Development | SQLite | `Data Source=campusrelay-dev.db` |
| Render | PostgreSQL | From `DATABASE_URL` env var |
| Azure | Azure SQL | From `ConnectionStrings:AzureSql` |

### Database Setup

#### For Local Development
- **No setup needed** - SQLite auto-creates on first run
- File: `campusrelay-dev.db` in project directory

#### For Render (PostgreSQL)
1. Create PostgreSQL database on Render
2. Copy connection string to `DATABASE_URL` environment variable
3. The API automatically detects and uses PostgreSQL

#### For Azure SQL
1. Create Azure SQL Database
2. Update `appsettings.json` with connection string
3. Deploy to Azure App Service

### Entity Framework Migrations

For production deployments with migrations:
```bash
# Install EF Core tools
dotnet tool install --global dotnet-ef

# Create migration
dotnet ef migrations add InitialCreate --project CampusRelay.Api

# Apply migration
dotnet ef database update --project CampusRelay.Api
```

Note: SQLite doesn't support migrations in this configuration - use for development only.

## 📡 API Endpoints

### Authentication

| Method | Route | Description |
|---|---|---|
| POST | `/api/v1/auth/sso` | Validate Firebase token, return JWT |
| POST | `/api/v1/auth/dev-login` | Create dev user (testing only) |

### Deliveries

| Method | Route | Description |
|---|---|---|
| POST | `/api/v1/deliveries` | Create delivery request (Part 1 Endpoint 1) |
| GET | `/api/v1/deliveries/{id}` | Get delivery details |
| POST | `/api/v1/deliveries/sync-offline` | Sync offline transactions (REQ-OFF-2) |

### Other Entities (Ready for Implementation)

All entities from Part 1 have EF Core models and DbSets:
- MarketplaceListing
- RideOffer
- Transaction
- Message
- Review
- BlockedUser
- ModerationReport

## 🔧 Firebase Setup

### For Development (Optional)

1. Create Firebase project at [Firebase Console](https://console.firebase.google.com/)
2. Go to **Project Settings** > **Service Accounts**
3. Click **"Generate new private key"**
4. Save as `firebase-service-account.json` in project root
5. Set **Copy to Output Directory** = **Copy if newer** (Visual Studio)

### For Render Deployment

1. Base64 encode your service account JSON:
   ```bash
   base64 firebase-service-account.json
   ```
2. Add to Render environment variables:
   - `FIREBASE_SERVICE_ACCOUNT_B64`: [base64 encoded JSON]
   - `FIREBASE_PROJECT_ID`: [your project ID]

### For Android App

1. Add `google-services.json` to your Android app
2. Update `AuthRepository.kt` with your Firebase Web Client ID
3. Enable Google/Microsoft sign-in providers in Firebase Console

## 🛠 Design Decisions

### Status Values
- Uses `ACTIVE/MATCHED/FULFILLED/CANCELLED` (from schema section)
- Not `"PendingCourier"` (from endpoint example)
- Fresh requests start as `Active`

### Weight Categories
- Stores both `WeightCategory` (string: Small/Medium/Large) and `WeightKg` (double)
- Mapping: Small=0.5, Medium=3.0, Large=8.0

### Pickup/Dropoff
- Stores building names as primary fields
- Lat/lng columns are nullable until geospatial lookup is implemented

## 📝 Important Notes

### Security
- **Never commit** `firebase-service-account.json` to GitHub
- **Never commit** production connection strings with passwords
- Use environment variables for all secrets
- Enable HTTPS in production

### Development
- The `/api/v1/auth/dev-login` endpoint is for **testing only**
- Use `/api/v1/auth/sso` with real Firebase tokens in production
- SQLite is for **local development only**

### Production
- Use PostgreSQL (Render) or Azure SQL for production
- Configure proper CORS settings
- Set up monitoring/logging
- Use HTTPS everywhere

## 🤝 Android Integration

### Update Constants.kt
```kotlin
// For local development
const val BASE_URL = "https://10.0.2.2:5001/"

// For Render deployment
const val BASE_URL = "https://campusrelay-api.onrender.com/"

// For Azure deployment
const val BASE_URL = "https://YOUR-APP-SERVICE.azurewebsites.net/"
```

### Retrofit Configuration
The Android app already sends `Authorization: Bearer <JWT>` headers automatically via the `authInterceptor` in `RetrofitProvider.kt`.

## 🆘 Troubleshooting

### Common Issues

| Issue | Solution |
|-------|----------|
| Database connection fails | Verify connection string format |
| Firebase not initialized | Check FIREBASE_SERVICE_ACCOUNT_B64 env var |
| 404 on endpoints | Verify URL has `/api/v1/` prefix |
| CORS errors | Check CORS settings in Program.cs |
| 500 errors | Check logs for detailed error |

### Render-Specific Issues

| Issue | Solution |
|-------|----------|
| Build fails | Ensure `rootDir: CampusRelay.Api` in render.yaml |
| DATABASE_URL not found | Link database to service in Render |
| Port issues | Render auto-sets PORT, no need to configure |

### Check Logs
- **Render**: Go to Web Service → Logs tab
- **Local**: `dotnet run` shows console output

## 📚 Resources

- [.NET 8 Documentation](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [Firebase Admin SDK](https://firebase.google.com/docs/admin/setup)
- [Render Documentation](https://render.com/docs)
- [PostgreSQL with EF Core](https://www.npgsql.org/efcore/)
