# Change Log

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

### Test Suite Updates
- Renamed `AuthServiceTests.cs` to `OldAuthServiceTests.cs` and `JwtServiceTests.cs` to `OldJwtServiceTests.cs` to deprecate outdated tests.
- Created `AuthenticationServiceTests.cs` to thoroughly test the new `AuthenticationService.Services.AuthenticationService` implementation, covering:
    - User authentication with valid/invalid credentials and inactive users.
    - JWT generation and validation.
    - Refresh token generation, validation, retrieval, and invalidation.
    - Password change functionality including refresh token invalidation.
- Updated `UserService.Tests.csproj` to include necessary project references for the new `AuthenticationService` and `Shared` projects.
- Modified `UserServiceTests.cs` to align with `UserService` changes, including:
    - Injecting `IEventStore` into the test setup.
    - Adding tests for user creation verifying `PasswordSalt` and `IsActive` defaults, and `UserRegisteredEvent` publishing.
    - Enhancing password update tests to verify `PasswordSalt` usage and `PasswordChangedEvent` publishing.
    - Updating user retrieval tests to confirm soft deletion logic (only active users by default).
    - Modifying user deletion tests to verify soft deletion (setting `IsActive` to `false`) and `UserDeletedEvent` publishing.
    - Adding tests for `UpdateUser` to confirm updates only on active users and `UserUpdatedEvent` publishing.

### Multi-Tenancy (ApplicationDbContext) Enhancements
- Modified `ApplicationDbContext` to inject `IHttpContextAccessor` for proper tenant ID resolution.
- Implemented `GetCurrentTenantId()` to retrieve the `TenantId` from the authenticated user's claims in the HTTP context.
- **Note**: The application's `Startup.cs` or `Program.cs` file needs to register `IHttpContextAccessor` with the dependency injection container (e.g., `services.AddHttpContextAccessor();`).

### Build Fixes
- Resolved `CS1503` errors by explicitly casting event objects to `Shared.Contracts.IEvent` in `UserService.Services.UserService`.
- Corrected `ApplicationDbContext` instantiation in `backend/services/UserService/DesignTimeDbContextFactory.cs` and `tests/NotificationService.Tests/NotificationServiceTests.cs` to provide `IHttpContextAccessor` (or a mock/null) to the constructor.