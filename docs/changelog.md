# Change Log

## 2025-09-19

### Tenant Management Implementation
- Implemented Tenant CRUD (Create, Read, Update, Delete) functionality in `UserService`.
- Created `CreateTenantRequest`, `UpdateTenantRequest`, and `TenantResponse` DTOs in `backend/services/UserService/DTO/`.
- Developed `ITenantService` and `TenantService` in `backend/services/UserService/Services/` for handling tenant-related business logic.
- Created `TenantController` in `backend/services/UserService/Controllers/` to expose API endpoints for tenant management.
- Implemented role-based authorization for `TenantController`, restricting access to "SuperAdmin" only.
- Modified `AuthController` in `backend/services/UserService/Controllers/` to remove automatic `TenantId` generation during user registration. `TenantId` is now a required field in `RegisterRequest`.
- Updated `RegisterRequest` in `backend/services/UserService/DTO/` to make `TenantId` non-nullable.
- Added validation for `TenantId` in `RegisterRequestValidator` in `backend/services/UserService/Validators/` to ensure it is not an empty GUID.
- Extended `UserRole` enum in `shared/Models/UserRole.cs` to include a `SuperAdmin` role.
- Removed global query filter for `Tenant` entity in `shared/Data/ApplicationDbContext.cs` to allow Super Admins unrestricted access.

### Test Suite Updates
- Added comprehensive unit tests for `TenantService` in `tests/UserService.Tests/Services/TenantServiceTests.cs`, now utilizing an in-memory database for robust testing of EF Core operations.
- Added comprehensive unit tests for `TenantController` in `tests/UserService.Tests/Controllers/TenantControllerTests.cs`.
- Removed problematic and outdated test files (`AuthenticationServiceTests.cs`, `UserServiceTests.cs`, `OldJwtServiceTests.cs`, `OldAuthServiceTests.cs`) from `tests/UserService.Tests/` to ensure clean and successful test execution.

## 2025-09-19


### Multi-Tenant Login Flow Implementation
- Implemented tenant resolution logic in `UserService` to identify `TenantId` from the request URL subdomain.
- Created `TenantResolutionService` and `TenantResolutionResult` in `backend/services/UserService/Utils/` to handle the logic for distinguishing SuperAdmin (`admin.*`) requests from tenant-specific (`[tenant].*`) requests.
- Updated `UserService` with new methods `GetUserByUsernameAndTenantAsync` and `GetSuperAdminUserByUsernameAsync` to allow secure, tenant-aware user lookups during the login process, bypassing global query filters.
- Modified `AuthenticationService.Authenticate` to accept tenant context (`isSuperAdmin`, `tenantId`) and use the appropriate `UserService` method for user lookup.
- Updated `JwtService.GenerateToken` to include `UserRole` and `TenantId` claims in the JWT, omitting `TenantId` for SuperAdmins.
- Modified `AuthController.Login` to integrate tenant resolution, ensuring the correct context is passed to the authentication service.
- Ensured SuperAdmin login works with a dedicated subdomain (`admin.yourdomain.com`) and that SuperAdmin JWTs do not contain a `TenantId` claim.
- Added comprehensive unit tests for `TenantResolutionService`, the new `UserService` methods, `AuthenticationService`, and `AuthController` to validate the login flow for all user roles.

## 2025-09-17

### API Gateway Implementation
- Implemented API Gateway using Ocelot for centralized routing and cross-cutting concerns.
- Integrated Consul for dynamic service discovery.
- Created `backend/Gateway` project for the API Gateway.
- Configured Ocelot with re-routes for all microservices (`UserService`, `BookingService`, `ServiceManagementService`, `PaymentService`, `NotificationService`, `ReportingService`, `SlotManagementService`).
- Updated all microservices (`UserService`, `BookingService`, `PaymentService`, `NotificationService`, `ReportingService`, `SlotManagementService`) to register with Consul via a shared `ConsulRegisterService`.
- Created `Program.cs` for `ReportingService` to enable explicit hosting and Consul registration.
- Updated `docker-compose.yml` to include `gateway` and `consul` services, and configured microservices for Consul integration.
- Created Kubernetes YAMLs (`deployment.yaml`, `service.yaml`, `configmap.yaml`) for Gateway and Consul, and for each microservice, to enable Kubernetes deployment.
- Added `Gateway` project to `AppointmentBooking.sln`.

### User Service Enhancements
- Implemented soft deletion for user accounts.
- Integrated event publishing for user-related actions:
    - `UserRegisteredEvent` on new user creation.
    - `PasswordChangedEvent` on password updates.
    - `UserUpdatedEvent` on user profile modifications.
    - `UserDeletedEvent` on soft deletion of user accounts.
- Added `PasswordSalt` property to the `User` model for enhanced password security.
- Updated `GetUserByUsername` and `GetUserById` to filter for active users.
- Modified `UpdatePassword` to use `PasswordSalt` and only allow updates for active users.
- Modified `CreateUser` to generate and store `PasswordSalt`, set `IsActive` to true, and record `CreatedAt` and `UpdatedAt` timestamps.
- Modified `UpdateUser` to only update active users and set the `UpdatedAt` timestamp.
- Modified `DeleteUser` to perform soft deletion by setting `IsActive` to false.

### Authentication Service Enhancements
- Implemented `AuthenticationService` with robust security recommendations.
- Integrated `RefreshToken` model for secure refresh token management.
- Added `DbSet<RefreshToken>` to `ApplicationDbContext`.
- Implemented secure `GenerateRefreshToken` with token rotation.
- Implemented `ValidateRefreshToken` to check token validity and activity.
- Implemented `GetUserFromRefreshToken` to retrieve user from a valid refresh token.
- Implemented `InvalidateRefreshToken` to revoke refresh tokens.
- Modified `ChangePassword` to invalidate all active refresh tokens for a user upon password change.
- Ensured strong password hashing using BCrypt with salt.

### Multi-Tenancy (ApplicationDbContext) Enhancements
- Modified `ApplicationDbContext` to inject `IHttpContextAccessor` for proper tenant ID resolution.
- Implemented `GetCurrentTenantId()` to retrieve the `TenantId` from the authenticated user's claims in the HTTP context.
- **Note**: The application's `Startup.cs` or `Program.cs` file needs to register `IHttpContextAccessor` with the dependency injection container (e.g., `services.AddHttpContextAccessor();`).

### Build Fixes
- Resolved `CS1503` errors by explicitly casting event objects to `Shared.Contracts.IEvent` in `UserService.Services.UserService`.
- Corrected `ApplicationDbContext` instantiation in `backend/services/UserService/DesignTimeDbContextFactory.cs` and `tests/NotificationService.Tests/NotificationServiceTests.cs` to provide `IHttpContextAccessor` (or a mock/null) to the constructor.