# Steward — API Contract Reference

> All endpoints from the actual C# controllers. Base URL from environment: `process.env.VITE_API_BASE_URL ?? '/api'`

---

## Auth

```
POST /api/auth/register
  Request: RegisterRequest { name: string, email: string, password: string }
  Response: AuthResponse { token: string, user: { id, email, name, avatarUrl } }
  Status: 201

POST /api/auth/login
  Request: LoginRequest { email: string, password: string }
  Response: AuthResponse { token: string, user: { id, email, name, avatarUrl } }
  Status: 200

GET /api/auth/oauth/{provider}/login     → OAuth provider-specific login flow
GET /api/auth/oauth/{provider}/callback  → Auth callback with exchange code
POST /api/auth/oauth/exchange
  Request: { code: string }
  Response: AuthResponse { token, user }
  Status: 200

GET /api/auth/me
  Auth: Bearer token
  Response: { id: string, email: string, name: string, avatarUrl: string | null }
  Status: 200

POST /api/auth/invites/{code}/accept    (requires auth)
  → 200
```

---

## Households

```
GET /api/households                              → HouseholdSummary[]
POST /api/households                             
  Request: { name: string }
  Response: HouseholdSummary { id, name, ownerId }
  Status: 201

GET /api/households/{id:guid}                   
  → HouseholdSummary { id, name, ownerId: string }
  
PUT /api/households/{id:guid}
  Request: { name: string }
  Response: HouseholdSummary { id, name, ownerId: string }
  Status: 200

DELETE /api/households/{id:guid}                 
  → 204
```

---

## Household Memberships

```
GET /api/households/{householdId}/members         
  → MemberSummary[] { userId, name, email, role }
  Role values: "owner" | "contributor" | "viewer"

POST /api/households/{householdId}/members/invite 
  Request: { email: string, role: "contributor" | "viewer" }
  Response: Invitation { code: string, email: string, role: string, createdAt }
  Status: 201

DELETE /api/households/{householdId}/invitations/{code}
  → 204

DELETE /api/households/{householdId}/members/{userId}
  → 204
```

---

## Assets

```
GET /api/households/{householdId}/assets?assetType={type?}
  → AssetSummary[]
  AssetType query param filters by: Boat, Car, Truck, Snowmobile, Utv,
    SnowmobileTrailer, EnclosedTrailer, RidingMower, PowerWasher, SmallEngine
  Each item:
    {
      id: string (guid),
      name: string,
      assetType: string,
      year: number,
      vinHint: string | null,          // VIN or HIN
      manufacturer: string | null,
      length: number | null,           // boats/engines only
      capacity: number | null,         // boats only
      engineCount: number,
      registrationCount: number | null,
      warrantyCount: number | null,
      createdAt: string (ISO date)
    }

GET /api/households/{householdId}/assets/{assetId}
  → Full AssetDetail (all type-specific fields included):
  {
    id, name, assetType, year, vinHint, manufacturer,
    // boat-specific
    length: number | null,
    capacity: number | null,
    // engine-specific  
    engineModel: string | null,
    engineHp: number | null,
    // trailer-specific
    maxWeight: number | null,
    axleCount: number | null,
    // etc.
    currentEngineCount: number,
    activeEngineCount: number,
    registrationCount: 5,
    warrantyCount: 3,
    createdAt
  }

POST /api/households/{householdId}/assets
  Request: CreateAssetRequest
    {
      name: string,
      assetType: "Boat" | "Car" | "Truck" | ... (see AssetType enum above)
      year: number,
      vinHint: string,                   // VIN or HIN
      manufacturer: string | null,
      // type-specific extras (engineered/typed)
      length: number | null,
      capacity: number | null,
      engineModel: string | null,
      engineHp: number | null,
      maxWeight: number | null,
      axleCount: number | null
    }
  Response: AssetDetail (full)
  Status: 201

PUT /api/households/{householdId}/assets/{assetId}
  Request: UpdateAssetRequest (same shape as CreateAssetRequest)
  Response: AssetDetail (full)
  Status: 200

DELETE /api/households/{householdId}/assets/{assetId}
  → 204
```

---

## Engines (nested under asset)

```
GET /api/households/{householdId}/assets/{assetId}/engines
  → EngineResponse[]
  Each:
    {
      id, name, manufacturer, model: string,
      hp: number | null,
      status: "Active" | "Retired",
      createdAt,
      retiredAt: Date | null
    }

POST /api/households/{householdId}/assets/{assetId}/engines
  Request:
    {
      name: string,
      manufacturer: string,
      model: string,
      hp: number | null
    }
  Response: EngineResponse
  Status: 201

PUT /api/households/{householdId}/assets/{assetId}/engines/{engineId}
  Request:
    { name, manufacturer, model, hp }
  Response: EngineResponse
  Status: 200

POST /api/households/{householdId}/assets/{assetId}/engines/{engineId}/retire
  → EngineResponse

POST /api/households/{householdId}/assets/{assetId}/engines/{engineId}/reactivate
  → EngineResponse

DELETE /api/households/{householdId}/assets/{assetId}/engines/{engineId}
  → 204
```

---

## Service Records (tracking)

```
GET /api/households/{householdId}/assets/{assetId}/service-records
  ?from={date}&to={date}                // optional date range filter
  → ServiceRecordResponse[]
  Each:
    {
      id, date, description, cost: number,
      serviceType: string | null,         // oil-change, engine-repair, winterization, etc.
      providerName: string | null,
      engineId: string | null,
      odometerHours: number | null,
      notes: string | null,
      createdAt
    }

POST /api/households/{householdId}/assets/{assetId}/service-records
  → ServiceRecordResponse (same shape as above)
  Status: 201

PUT /api/households/{householdId}/assets/{assetId}/service-records/{serviceRecordId}
  → ServiceRecordResponse
  Status: 200

DELETE /api/households/{householdId}/assets/{assetId}/service-records/{serviceRecordId}
  → 204
```

---

## Fuel Logs (tracking)

```
GET /api/households/{householdId}/assets/{assetId}/fuel-logs
  ?from={date}&to={date}
  → FuelLogResponse[]
  Each:
    {
      id, date, logType: "fillup" | "consumption",
      volume: number, volumeUnit: "gallons" | "liters",
      pricePerUnit: number | null,
      totalCost: number | null,
      fuelGrade: string | null,
      engineId: string | null,
      milesAtLog: number | null,
      hoursAtLog: number | null,
      notes: string | null,
      createdAt
    }

POST /api/households/{householdId}/assets/{assetId}/fuel-logs
  → FuelLogResponse
  Status: 201

PUT /api/households/{householdId}/assets/{assetId}/fuel-logs/{fuelLogId}
  → FuelLogResponse
  Status: 200

DELETE /api/households/{householdId}/assets/{assetId}/fuel-logs/{fuelLogId}
  → 204
```

---

## Mileage Logs (tracking)

```
GET /api/households/{householdId}/assets/{assetId}/mileage-logs
  ?from={date}&to={date}
  → MileageLogResponse[]
  Each:
    {
      id, date, logType: "odometer" | "trip-miles",
      reading: number,
      purpose: string | null,              // personal, business, charity, commute
      notes: string | null,
      createdAt
    }

POST /api/households/{householdId}/assets/{assetId}/mileage-logs
  → MileageLogResponse
  Status: 201

PUT /api/households/{householdId}/assets/{assetId}/mileage-logs/{mileageLogId}
  → MileageLogResponse
  Status: 200

DELETE /api/households/{householdId}/assets/{assetId}/mileage-logs/{mileageLogId}
  → 204
```

---

## Engine Hours Logs (tracking)

```
GET /api/households/{householdId}/assets/{assetId}/engines/{engineId}/hours-logs
  ?from={date}&to={date}
  → EngineHoursLogResponse[]
  Each:
    {
      id, date, hours: number,
      readingType: "total" | "trip",
      createdAt
    }

POST /api/households/{householdId}/assets/{assetId}/engines/{engineId}/hours-logs
  → EngineHoursLogResponse
  Status: 201

PUT /api/households/{householdId}/assets/{assetId}/engines/{engineId}/hours-logs/{hoursLogId}
  → EngineHoursLogResponse
  Status: 200

DELETE /api/households/{householdId}/assets/{assetId}/engines/{engineId}/hours-logs/{hoursLogId}
  → 204
```

---

## Registrations + Documents

```
GET /api/households/{householdId}/assets/{assetId}/registrations
  → RegistrationResponse[]
  Each:
    {
      id, authority: string,
      number: string | null,
      issueDate: string (ISO date),
      expiryDate: string (ISO date),
      cost: number | null,
      documentUrl: string | null           // signed/proxied download URL
      hasDocument: boolean
    }

POST /api/households/{householdId}/assets/{assetId}/registrations
  Request:
    { authority, number?, issueDate?, expiryDate?, cost? }
  → RegistrationResponse
  Status: 201

PUT /api/households/{householdId}/assets/{assetId}/registrations/{registrationId}
  → RegistrationResponse
  Status: 200

DELETE /api/households/{householdId}/assets/{assetId}/registrations/{registrationId}
  → 204

// Document uploads for registrations
POST /api/households/{householdId}/assets/{assetId}/registrations/{registrationId}/document
  Content-Type: multipart/form-data
  Body: file (PDF, JPEG, PNG)
  → RegistrationResponse (with documentUrl populated)

GET /api/households/{householdId}/assets/{assetId}/registrations/{registrationId}/document
  → file stream (Binary)

DELETE /api/households/{householdId}/assets/{assetId}/registrations/{registrationId}/document
  → 204
```

---

## Warranties + Documents

```
GET /api/households/{householdId}/assets/{assetId}/warranties
  → WarrantyResponse[]
  Each:
    {
      id, provider: string,
      startDate: string, endDate: string,
      cost: number | null,
      coverage: string | null,
      documentUrl: string | null
      hasDocument: boolean
    }

POST /api/households/{householdId}/assets/{assetId}/warranties
  Request:
    { provider, startDate, endDate?, cost?, coverage? }
  → WarrantyResponse
  Status: 201

PUT /api/households/{householdId}/assets/{assetId}/warranties/{warrantyId}
  → WarrantyResponse
  Status: 200

DELETE /api/households/{householdId}/assets/{assetId}/warranties/{warrantyId}
  → 204

// Document uploads for warranties
POST /api/households/{householdId}/assets/{assetId}/warranties/{warrantyId}/document
  → WarrantyResponse (with documentUrl)

GET /api/households/{householdId}/assets/{assetId}/warranties/{warrantyId}/document
  → file stream

DELETE /api/households/{householdId}/assets/{assetId}/warranties/{warrantyId}/document
  → 204
```

---

## Auth & Authorization Notes

All endpoints (except auth routes) require **Bearer token** in `Authorization` header.

```
Authorization: Bearer <token>
```

- JWT tokens have a **15-minute expiry** (no refresh tokens — re-login required)
- All scoped to the household: responses are household-scoped
- Roles: `owner` (full access) | `contributor` (edit) | `viewer` (read-only)
- Household resource authorization enforced on every read/write
- All GUIDs in paths are required

### Common status codes
- **200** — OK (GET, PUT responses)
- **201** — Created (POST responses)
- **204** — No Content (DELETE responses)
- **400** — Validation errors (check `ValidationProblem` response body for details)
- **403** — Forbidden (not enough permission or household mismatch)
- **404** — Not found
- **401** — Unauthorized (expired/missing token)
- **500** — Internal server error
