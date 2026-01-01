# Keycloak Integration Testing Guide

This document provides a comprehensive guide to test the Keycloak integration in the UrbanX application.

## Prerequisites

1. Docker and Docker Compose installed
2. .NET 10 SDK installed
3. Keycloak and PostgreSQL running via Docker Compose

## Step 1: Start Infrastructure Services

```bash
cd /path/to/urbanx
docker compose up -d
```

Verify services are running:

```bash
docker compose ps
```

You should see:
- `urbanx-postgres` - PostgreSQL database
- `urbanx-keycloak` - Keycloak identity provider
- `urbanx-zookeeper` - Zookeeper for Kafka
- `urbanx-kafka` - Kafka message broker

## Step 2: Verify Keycloak Setup

### Access Keycloak Admin Console

1. Open browser to: http://localhost:8080
2. Click on "Administration Console"
3. Login with:
   - Username: `admin`
   - Password: `admin`

### Verify Realm Configuration

1. Select "urbanx" realm from the dropdown (top-left)
2. Verify the following:

#### Clients
Go to **Clients** menu:
- `urbanx-spa` should exist with:
  - Access Type: public
  - Valid Redirect URIs: `http://localhost:5173/*`, `https://localhost:5173/*`, etc.
  - Web Origins: `http://localhost:5173`, `https://localhost:5173`

#### Roles
Go to **Realm Roles** menu:
- `admin` - Administrator with full access
- `merchant-editor` - Merchant user who can edit products
- `merchant-order-manager` - Merchant user who can manage orders
- `buyer` - Customer/buyer role

#### Users
Go to **Users** menu and verify the following test users exist:

| Email | Password | Roles |
|-------|----------|-------|
| admin@urbanx.com | Admin123! | admin |
| merchant1@urbanx.com | Merchant123! | merchant-editor, merchant-order-manager |
| merchant2@urbanx.com | Merchant123! | merchant-editor |
| buyer1@urbanx.com | Buyer123! | buyer |
| buyer2@urbanx.com | Buyer123! | buyer |

## Step 3: Build and Run the Application

### Option A: Using .NET Aspire (Recommended)

```bash
cd src/AppHost/UrbanX.AppHost
dotnet run
```

Access:
- Aspire Dashboard: http://localhost:15260
- Frontend: Check Aspire Dashboard for the dynamically assigned port

### Option B: Manual Setup

1. **Build the solution:**
```bash
dotnet build UrbanX.sln
```

2. **Start the backend services** (each in a separate terminal):

```bash
# Gateway
cd src/Gateway/UrbanX.Gateway
dotnet run

# Catalog Service
cd src/Services/Catalog/UrbanX.Services.Catalog
dotnet run

# Order Service
cd src/Services/Order/UrbanX.Services.Order
dotnet run

# Merchant Service
cd src/Services/Merchant/UrbanX.Services.Merchant
dotnet run

# Payment Service
cd src/Services/Payment/UrbanX.Services.Payment
dotnet run

# Identity Service
cd src/Services/Identity/UrbanX.Services.Identity
dotnet run
```

3. **Start the frontend:**
```bash
cd src/Frontend/UrbanX.Frontend/UrbanX.Frontend
dotnet run
```

## Step 4: Test Authentication Flow

### Test Login Flow

1. Open the frontend application in your browser (http://localhost:7001 or check Aspire Dashboard)
2. You should see the UrbanX Shop homepage
3. Click the "Log in" button in the header
4. You will be redirected to Keycloak login page
5. Try logging in with one of the test users:
   - **As Buyer:** Use `buyer1@urbanx.com` / `Buyer123!`
   - **As Merchant:** Use `merchant1@urbanx.com` / `Merchant123!`
   - **As Admin:** Use `admin@urbanx.com` / `Admin123!`

6. After successful login:
   - You should be redirected back to the UrbanX homepage
   - The header should display "Hello, [User's First Name]!" instead of "Log in"
   - You should see a "Log out" button

### Test Logout Flow

1. While logged in, click the "Log out" button
2. You will be redirected to Keycloak logout page
3. After logout, you should be redirected back to the homepage
4. The header should show "Log in" button again

### Test Protected Routes

1. Try accessing protected pages:
   - Cart page
   - Orders page
   - Product details page

2. Verify that:
   - When not logged in, you're redirected to login
   - After login, you can access the protected pages
   - User information is displayed correctly

## Step 5: Test Different User Roles

### Test as Buyer

Login as `buyer1@urbanx.com`:

1. Verify you can:
   - Browse products
   - View product details
   - Add items to cart
   - View cart
   - Place orders (if implemented)

### Test as Merchant

Login as `merchant1@urbanx.com`:

1. Verify you can:
   - Access all buyer features
   - See merchant-specific menu items (if implemented)
   - Manage products (if implemented)
   - Manage orders (if implemented)

### Test as Admin

Login as `admin@urbanx.com`:

1. Verify you can:
   - Access all features
   - See admin-specific menu items (if implemented)

## Step 6: Verify Token Claims

### Using Browser DevTools

1. Open browser DevTools (F12)
2. Go to Application/Storage → Session Storage or Local Storage
3. Look for OIDC tokens
4. Decode the ID token (JWT) using jwt.io
5. Verify claims include:
   - `preferred_username` or `email`
   - `name`
   - `realm_access.roles` with correct roles

### Using Keycloak Admin Console

1. Go to Keycloak Admin Console
2. Select urbanx realm
3. Go to Users → Select a user → Sessions
4. Verify active sessions appear after login

## Troubleshooting

### Keycloak Not Starting

```bash
# Check Keycloak logs
docker compose logs keycloak

# Common issue: Database not created
docker exec urbanx-postgres psql -U postgres -c "CREATE DATABASE keycloak;"
docker compose restart keycloak
```

### Login Redirects Not Working

1. Verify redirect URIs in Keycloak client configuration match your frontend URL
2. Check browser console for CORS errors
3. Ensure Web Origins are configured correctly in Keycloak

### Token Validation Errors

1. Check system time synchronization (JWT tokens are time-sensitive)
2. Verify the `Authority` configuration in frontend matches Keycloak URL
3. Check network connectivity between services

### User Can't Login

1. Verify user exists in Keycloak Admin Console
2. Check user is enabled
3. Verify email is verified (if required)
4. Check user has correct roles assigned
5. Try resetting user password in Keycloak Admin Console

## Advanced Testing

### Test Token Refresh

1. Login and wait for token expiration (default: 5 minutes)
2. Try navigating to a protected page
3. Verify token is automatically refreshed

### Test Session Timeout

1. Login and remain idle
2. After session timeout, try accessing a protected resource
3. Verify you're redirected to login

### Test Concurrent Sessions

1. Login from multiple browsers/devices with the same user
2. Logout from one session
3. Verify other sessions remain active (or are terminated based on configuration)

## Security Verification

### PKCE Flow

1. Open browser DevTools → Network tab
2. Click "Log in"
3. Verify the authorization request includes:
   - `code_challenge`
   - `code_challenge_method=S256`

### HTTPS in Production

For production deployments:
1. Ensure all URLs use HTTPS
2. Update Keycloak redirect URIs to use HTTPS
3. Configure Keycloak to require SSL
4. Update frontend configuration to use HTTPS URLs

## Performance Testing

### Token Caching

1. Login and verify token is stored
2. Refresh page and verify no new token request is made
3. Check network tab to confirm cached token is used

### Realm Configuration Load Time

1. Measure time from Keycloak startup to realm import completion
2. Check logs for import time: `grep "imported" docker compose logs keycloak`

## Cleanup

To stop and remove all containers:

```bash
docker compose down
```

To also remove volumes (will delete all data):

```bash
docker compose down -v
```

## Next Steps

After successful testing:

1. Configure additional user attributes as needed
2. Add more fine-grained permissions using Keycloak authorization services
3. Set up user registration flow if needed
4. Configure password policies and account security settings
5. Set up email server for password reset and verification
6. Configure social login providers (Google, Facebook, etc.) if needed
7. Set up proper SSL certificates for production
8. Configure backup strategy for Keycloak database

## Support

For issues or questions:
- Check Keycloak documentation: https://www.keycloak.org/documentation
- Review application logs in Aspire Dashboard
- Check Docker Compose logs: `docker compose logs`
