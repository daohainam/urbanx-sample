# Keycloak Configuration for UrbanX

This directory contains the Keycloak realm configuration for the UrbanX application.

## Overview

Keycloak is integrated as the authentication and authorization provider for UrbanX, replacing the previous Duende IdentityServer setup.

## Access Keycloak Admin Console

- **URL:** http://localhost:8080
- **Admin Username:** admin
- **Admin Password:** admin

## Realm Information

- **Realm Name:** urbanx
- **Realm Display Name:** UrbanX Realm

## Clients

### urbanx-spa
- **Client ID:** urbanx-spa
- **Type:** Public client (OIDC Authorization Code Flow with PKCE)
- **Redirect URIs:** 
  - http://localhost:5173/*
  - https://localhost:5173/*
  - http://localhost:7001/*
  - https://localhost:7002/*
- **Purpose:** Frontend Blazor WebAssembly application

### urbanx-merchant-spa
- **Client ID:** urbanx-merchant-spa
- **Type:** Public client (OIDC Authorization Code Flow with PKCE)
- **Redirect URIs:**
  - http://localhost:5174/*
  - https://localhost:5174/*
- **Purpose:** Merchant portal application

## Roles

The following realm roles are configured:

1. **admin** - Administrator with full access to all features
2. **merchant-editor** - Merchant user who can edit products and categories
3. **merchant-order-manager** - Merchant user who can manage orders
4. **buyer** - Customer/buyer who can browse and purchase products

## Sample Users

### Admin User
- **Username:** admin@urbanx.com
- **Password:** Admin123!
- **Roles:** admin
- **Purpose:** Full system administration

### Merchant Users

#### Merchant 1 (Full Access)
- **Username:** merchant1@urbanx.com
- **Password:** Merchant123!
- **Roles:** merchant-editor, merchant-order-manager
- **Merchant ID:** merchant-001
- **Purpose:** Merchant with full product and order management access

#### Merchant 2 (Editor Only)
- **Username:** merchant2@urbanx.com
- **Password:** Merchant123!
- **Roles:** merchant-editor
- **Merchant ID:** merchant-002
- **Purpose:** Merchant with product editing access only

### Buyer Users

#### Buyer 1
- **Username:** buyer1@urbanx.com
- **Password:** Buyer123!
- **Roles:** buyer
- **Purpose:** Regular customer account

#### Buyer 2
- **Username:** buyer2@urbanx.com
- **Password:** Buyer123!
- **Roles:** buyer
- **Purpose:** Regular customer account

## Starting Keycloak

Keycloak is included in the docker-compose.yml file and will start automatically:

```bash
docker-compose up -d keycloak
```

The realm configuration is automatically imported on first startup.

## Configuration Details

- **Database:** PostgreSQL (shared with other services)
- **Database Name:** keycloak
- **Port:** 8080
- **Import:** Realm configuration is automatically imported from `realm-export.json`

## OIDC Endpoints

Once Keycloak is running, the OpenID Connect configuration can be accessed at:

```
http://localhost:8080/realms/urbanx/.well-known/openid-configuration
```

Key endpoints:
- **Authorization:** http://localhost:8080/realms/urbanx/protocol/openid-connect/auth
- **Token:** http://localhost:8080/realms/urbanx/protocol/openid-connect/token
- **UserInfo:** http://localhost:8080/realms/urbanx/protocol/openid-connect/userinfo
- **End Session:** http://localhost:8080/realms/urbanx/protocol/openid-connect/logout

## Customization

To modify the realm configuration:

1. Make changes through the Keycloak Admin Console
2. Export the realm: Realm Settings → Action → Partial export
3. Save the exported JSON to `realm-export.json`
4. Restart Keycloak to apply changes

## Troubleshooting

### Keycloak not starting
- Check if PostgreSQL is running: `docker-compose ps postgres`
- Check Keycloak logs: `docker-compose logs keycloak`

### Realm not imported
- Ensure `realm-export.json` exists in the keycloak directory
- Check Keycloak logs for import errors
- Verify the JSON is valid

### Cannot login
- Verify user exists in the Keycloak Admin Console
- Check user is enabled and email is verified
- Ensure password is correct (case-sensitive)
- Verify the user has the appropriate realm roles
