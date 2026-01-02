# Production Deployment Considerations

## Security Best Practices

### 1. Password Management

For production deployments, **never** include user passwords in the realm export. Instead:

#### Option A: Use Temporary Passwords
```json
{
  "credentials": [
    {
      "type": "password",
      "value": "ChangeMe123!",
      "temporary": true
    }
  ]
}
```

This forces users to change their password on first login.

#### Option B: Remove Users from Export
Export the realm without users and create them manually or via API:

```bash
# Export realm without users
docker exec urbanx-keycloak /opt/keycloak/bin/kc.sh export \
  --dir /opt/keycloak/data/export \
  --realm urbanx \
  --users skip
```

#### Option C: Use User Federation
Connect Keycloak to your existing LDAP, Active Directory, or other user stores.

### 2. Environment-Specific Configuration

Use environment variables for sensitive configuration:

```yaml
# docker-compose.yml for production
keycloak:
  environment:
    KEYCLOAK_ADMIN: ${KC_ADMIN_USER}
    KEYCLOAK_ADMIN_PASSWORD: ${KC_ADMIN_PASSWORD}
    KC_DB_PASSWORD: ${KC_DB_PASSWORD}
    KC_HOSTNAME: ${KC_HOSTNAME}
    KC_HOSTNAME_STRICT_HTTPS: true
```

Create a `.env` file (and add it to .gitignore):
```env
KC_ADMIN_USER=admin
KC_ADMIN_PASSWORD=<strong-random-password>
KC_DB_PASSWORD=<strong-random-password>
KC_HOSTNAME=auth.yourdomain.com
```

### 3. HTTPS/TLS Configuration

**Always use HTTPS in production:**

```yaml
keycloak:
  environment:
    KC_HTTPS_CERTIFICATE_FILE: /opt/keycloak/conf/server.crt
    KC_HTTPS_CERTIFICATE_KEY_FILE: /opt/keycloak/conf/server.key
    KC_HTTP_ENABLED: false
    KC_HOSTNAME_STRICT_HTTPS: true
  volumes:
    - ./certs:/opt/keycloak/conf
```

### 4. Client Secrets

For confidential clients (backend services), use strong, randomly generated secrets:

```bash
# Generate a strong client secret
openssl rand -hex 32
```

Store in environment variables or a secrets management system like:
- Azure Key Vault
- AWS Secrets Manager
- HashiCorp Vault

### 5. Database Security

- Use strong, unique passwords for database connections
- Enable SSL/TLS for database connections
- Restrict database network access
- Regular backups with encryption

Example production database configuration:
```yaml
KC_DB_URL_HOST: db.internal.domain.com
KC_DB_URL_PORT: 5432
KC_DB_URL_DATABASE: keycloak_prod
KC_DB_USERNAME: keycloak_user
KC_DB_PASSWORD: ${KC_DB_PASSWORD}  # From environment or secrets manager
KC_DB_POOL_INITIAL_SIZE: 5
KC_DB_POOL_MAX_SIZE: 20
```

### 6. Admin Console Security

- Change default admin username from "admin"
- Use a strong, unique password
- Enable Two-Factor Authentication (2FA) for admin accounts
- Limit admin console access by IP address
- Use audit logging to track admin actions

### 7. Realm Configuration Review

Before production deployment, review and update:

#### Required Actions
Enable mandatory actions for new users:
- Verify Email
- Update Password
- Configure OTP

#### Password Policies
Set strong password requirements:
- Minimum length: 12 characters
- Require uppercase, lowercase, numbers, special characters
- Password history: prevent reuse of last 5 passwords
- Password expiration: 90 days
- Failed login attempts lockout

#### Session Management
- Access token lifespan: 5 minutes (short-lived)
- Refresh token lifespan: 30 minutes
- SSO session idle timeout: 30 minutes
- SSO session max lifespan: 10 hours

#### Brute Force Detection
Enable and configure:
- Max login failures: 5
- Wait increment: 60 seconds
- Quick login check millis: 1000
- Minimum quick login wait: 60 seconds

### 8. Monitoring and Logging

Enable comprehensive logging:

```yaml
keycloak:
  environment:
    KC_LOG_LEVEL: INFO
    KC_LOG_CONSOLE_COLOR: false
    KC_METRICS_ENABLED: true
    KC_HEALTH_ENABLED: true
```

Monitor:
- Failed login attempts
- Token issuance rates
- Session creation/destruction
- Admin console access
- Configuration changes

### 9. Backup and Disaster Recovery

Regular backups of:
- Keycloak database
- Realm configuration
- Custom themes and providers

Test recovery procedures regularly.

### 10. Network Security

- Place Keycloak behind a reverse proxy (nginx, Traefik, etc.)
- Implement rate limiting
- Use Web Application Firewall (WAF)
- Restrict network access to management ports
- Use internal networks for service-to-service communication

## Production Deployment Checklist

- [ ] Remove or mask all hard-coded passwords
- [ ] Configure HTTPS/TLS with valid certificates
- [ ] Set strong password policies
- [ ] Enable Two-Factor Authentication for admins
- [ ] Configure brute force detection
- [ ] Set appropriate token lifespans
- [ ] Enable email verification
- [ ] Configure email server for notifications
- [ ] Set up monitoring and alerting
- [ ] Configure database backups
- [ ] Document admin procedures
- [ ] Test disaster recovery procedures
- [ ] Review and update redirect URIs for production domains
- [ ] Configure CORS for production domains
- [ ] Enable audit logging
- [ ] Set up log aggregation and analysis
- [ ] Perform security audit/penetration testing
- [ ] Document incident response procedures

## Example Production Deployment

### Using Docker Compose with Secrets

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_USER: keycloak
      POSTGRES_PASSWORD_FILE: /run/secrets/db_password
    secrets:
      - db_password
    volumes:
      - postgres_data:/var/lib/postgresql/data
    networks:
      - internal

  keycloak:
    image: quay.io/keycloak/keycloak:23.0.6
    command: start
    environment:
      KC_DB: postgres
      KC_DB_URL: jdbc:postgresql://postgres:5432/keycloak
      KC_DB_USERNAME: keycloak
      KC_DB_PASSWORD_FILE: /run/secrets/db_password
      KEYCLOAK_ADMIN_FILE: /run/secrets/admin_user
      KEYCLOAK_ADMIN_PASSWORD_FILE: /run/secrets/admin_password
      KC_HOSTNAME: auth.yourdomain.com
      KC_HOSTNAME_STRICT: true
      KC_HOSTNAME_STRICT_HTTPS: true
      KC_HTTPS_CERTIFICATE_FILE: /opt/keycloak/conf/server.crt
      KC_HTTPS_CERTIFICATE_KEY_FILE: /opt/keycloak/conf/server.key
      KC_HTTP_ENABLED: false
      KC_PROXY: edge
      KC_METRICS_ENABLED: true
      KC_HEALTH_ENABLED: true
    secrets:
      - db_password
      - admin_user
      - admin_password
    volumes:
      - ./certs:/opt/keycloak/conf
    depends_on:
      - postgres
    networks:
      - internal
      - external

secrets:
  db_password:
    file: ./secrets/db_password.txt
  admin_user:
    file: ./secrets/admin_user.txt
  admin_password:
    file: ./secrets/admin_password.txt

volumes:
  postgres_data:

networks:
  internal:
    driver: bridge
  external:
    driver: bridge
```

## Resources

- [Keycloak Server Administration Guide](https://www.keycloak.org/docs/latest/server_admin/)
- [Keycloak Security Guide](https://www.keycloak.org/docs/latest/server_installation/#_security_hardening)
- [OWASP Authentication Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html)
