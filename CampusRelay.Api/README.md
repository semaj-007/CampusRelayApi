# CampusRelay API

ASP.NET Core 8 Web API backend for CampusRelay (PROG7314 POE), built from Part 1's
"REST Api and Backend Architecture" and "Data Models and Schema Definitions" sections.

## Getting started

1. Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) if you don't have it.
2. From `CampusRelay-Backend/`:
   ```
   dotnet restore
   dotnet run --project CampusRelay.Api
   ```
3. Open the Swagger UI it launches (`https://localhost:5001/swagger`).
4. Call `POST /api/v1/auth/dev-login` with `{ "email": "you@example.com", "fullName": "Your Name" }`
   to get a bearer token (see "Auth" below) and a real `User` row to attach delivery
   requests to.
5. Click **Authorize** in Swagger, paste `Bearer <token>`, and try
   `POST /api/v1/deliveries`.

No database setup needed for local dev - it uses a SQLite file (`campusrelay-dev.db`,
created automatically next to the project) so `dotnet run` works immediately.

### Pointing the Android app at this

The Android app now communicates with this backend using Firebase Authentication.
The app's `debug` build type targets `https://10.0.2.2:5001/` (10.0.2.2 is
how the Android emulator reaches your machine's localhost). Run this API with the
`https` launch profile and the emulator should be able to reach it as-is.

## Auth

REQ-AUTH-1 now uses Firebase Authentication for SSO login instead of Azure AD.
The backend validates Firebase ID tokens and issues its own JWT tokens for API access.

- **Firebase Authentication**: Users sign in via Firebase (Google, Microsoft, etc.)
- **Token Validation**: The backend validates Firebase ID tokens using Firebase Admin SDK
- **JWT Issuance**: After Firebase validation, the backend issues its own JWT tokens for API access

**For production deployment:**
1. Set up a Firebase project
2. Download the service account JSON file
3. Place it in the project root as `firebase-service-account.json`
4. Set **Copy to Output Directory** = **Copy if newer** in file properties
5. Ensure the Android app uses the same Firebase project

## Firebase Setup

### Step 1: Create Firebase Project
1. Go to [Firebase Console](https://console.firebase.google.com/)
2. Create a new project or use an existing one
3. Note your **Project ID** (found in project settings)

### Step 2: Get Service Account JSON
1. Go to **Project Settings** > **Service Accounts**
2. Click **"Generate new private key"**
3. Save the downloaded JSON file as `firebase-service-account.json` in your project root
4. In Visual Studio, set the file property **Copy to Output Directory** = **Copy if newer**

### Step 3: Configure appsettings.Development.json
```json
{
  "Firebase": {
    "ProjectId": "your-firebase-project-id",
    "ServiceAccountFilePath": "firebase-service-account.json"
  }
}
```

### Step 4: Enable Providers
- Go to **Authentication** > **Sign-in method**
- Enable **Google** and **Microsoft** providers
- For Google: Add your Android package name and SHA-1 fingerprint

### Step 5: Android Configuration
1. Download `google-services.json` from Firebase Console
2. Place it in `app/` folder of your Android project
3. Replace `YOUR_WEB_CLIENT_ID` in `AuthRepository.kt` with your Firebase Web Client ID

## Database

- **Development**: SQLite, auto-created via `Database.EnsureCreated()` on startup -
  zero setup, but not migration-based (fine for a prototype, not for production).
- **Production**: Azure SQL Database via `ConnectionStrings:AzureSql` in
  `appsettings.json` - matches Part 1's documented stack (EF Core Code-First against
  Azure SQL, hosted on Azure App Services).

Once you're ready to deploy for real and have the .NET SDK + a real Azure SQL
connection string:
```
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate --project CampusRelay.Api
dotnet ef database update --project CampusRelay.Api
```
(No `Migrations/` folder is checked in yet - this sandbox doesn't have the .NET SDK
to generate one, so that's a one-time step for whoever runs this first with real tooling.)

## Endpoints implemented

| Method | Route | Matches |
|---|---|---|
| POST | `/api/v1/deliveries` | Part 1 "Endpoint 1: Create Delivery Request" |
| GET | `/api/v1/deliveries/{id}` | not in Part 1 - added so the create endpoint's 201 `Location` header points somewhere real |
| POST | `/api/v1/deliveries/sync-offline` | Part 1 "Endpoint 2: Synchronize Offline Handoff Transactions" (REQ-OFF-2) |
| POST | `/api/v1/auth/sso` | Firebase SSO login - validates Firebase ID token and returns API JWT |
| POST | `/api/v1/auth/dev-login` | prototype-only, see "Auth" above |

Every other entity from Part 1's schema (MarketplaceListing, RideOffer, Transaction,
Message, Review, BlockedUser, ModerationReport) has an EF Core model and a `DbSet` in
`CampusRelayDbContext`, ready for their own controllers - just not built out yet,
since only the Delivery flow has a documented endpoint contract to match against.

## Design decisions worth knowing about

Part 1's document describes the delivery flow slightly differently in two places, so
building a real implementation meant picking one interpretation:

- **Status values**: the schema section lists `ACTIVE/MATCHED/FULFILLED/CANCELLED`;
  the endpoint's example response shows `"PendingCourier"`. This implementation uses
  the schema section's four values everywhere (`DeliveryStatus` enum here, and the
  Android app's local enum) so the two ends of the wire agree. A freshly created
  request is `Active`.
- **Weight**: the endpoint payload sends a `weightCategory` string
  (`Small`/`Medium`/`Large`); the schema section instead has a raw `weight_kg`
  double. This API stores both - `WeightCategory` as the source of truth (it's what
  the client actually sends), with `WeightKg` derived from it via the same
  Small=0.5/Medium=3.0/Large=8.0 mapping the Android app uses.
- **Pickup/dropoff**: the endpoint sends building names; the schema section has
  `pickup_lat`/`pickup_lng` points. Building names are stored as the primary fields;
  the lat/lng columns are nullable until a building-name -> coordinate lookup exists
  for REQ-DEL-3's geospatial matching.

One thing to double check on the Android side: `DeliveryRepositoryImpl.createDeliveryRequest`
currently reads `body.deliveryId` from this API's response but not `body.status` -
worth wiring that up now that `status` is a real, meaningful value.

## Firebase Service Account File

The Firebase service account JSON file (`firebase-service-account.json`) should be placed in the project root and configured with:
- **Copy to Output Directory**: Copy if newer
- **Do NOT commit this file to source control** (add to .gitignore)

This approach avoids JSON escaping issues and allows for easier credential management.
