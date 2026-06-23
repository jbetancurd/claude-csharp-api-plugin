# Docker Setup for Swagger UI & Health Checks

Run your Swagger-enabled API in Docker with proper port configuration and health checks.

## Basic Dockerfile

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MyApi/MyApi.csproj", "MyApi/"]
RUN dotnet restore "MyApi/MyApi.csproj"
COPY . .
WORKDIR "/src/MyApi"
RUN dotnet build "MyApi.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "MyApi.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=publish /app/publish .

# Port configuration
ENV PORT=80
ENV ASPNETCORE_URLS=http://+:${PORT}
ENV ASPNETCORE_ENVIRONMENT=Development

EXPOSE 80
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "MyApi.dll"]
```

## Docker Compose for Local Development

### Basic Setup (Single API)

```yaml
version: '3.9'

services:
  api:
    build:
      context: .
      dockerfile: Dockerfile
    
    ports:
      - "5000:80"
    
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:80
      - PORT=80
      - ConnectionStrings__DefaultConnection=Server=db;Database=MyDb;User=sa;Password=YourPassword123!
    
    depends_on:
      db:
        condition: service_healthy
    
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost/health"]
      interval: 30s
      timeout: 3s
      retries: 3
      start_period: 5s
    
    networks:
      - app-network

  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourPassword123!
    
    ports:
      - "1433:1433"
    
    healthcheck:
      test: /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourPassword123! -Q "SELECT 1" || exit 1
      interval: 10s
      timeout: 3s
      retries: 5
      start_period: 10s
    
    networks:
      - app-network

networks:
  app-network:
    driver: bridge
```

## Usage

### Build and Run
```bash
docker-compose up --build
```

### Access Services
- **Swagger UI**: http://localhost:5000/
- **Health Check**: http://localhost:5000/health
- **Database**: localhost:1433 (SQL Server)

### Stop Services
```bash
docker-compose down
```

## Advanced: Multiple Environments

### docker-compose.override.yml (Local Development)

```yaml
version: '3.9'

services:
  api:
    build:
      context: .
    
    ports:
      - "5000:80"
    
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:80
    
    volumes:
      - .:/app
      - /app/bin
      - /app/obj
```

### docker-compose.prod.yml (Production)

```yaml
version: '3.9'

services:
  api:
    image: myregistry.azurecr.io/myapi:latest
    
    ports:
      - "80:80"
    
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=https://+:443;http://+:80
      - PORT=80
    
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost/health"]
      interval: 60s
      timeout: 5s
      retries: 2
    
    restart: unless-stopped
    
    resources:
      limits:
        cpus: '1'
        memory: 512M
      reservations:
        cpus: '0.5'
        memory: 256M
```

### Run Production Environment
```bash
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

## Port Configuration in Docker

### Port Mapping Syntax
```yaml
ports:
  - "host_port:container_port"
```

### Examples

```yaml
# Standard development (5000 → 80)
ports:
  - "5000:80"

# Custom port (8080 → 80)
ports:
  - "8080:80"

# HTTPS support (443 → 443)
ports:
  - "443:443"

# Multiple ports
ports:
  - "5000:80"      # HTTP
  - "5001:443"     # HTTPS
```

## Health Checks in Docker

### HEALTHCHECK Directive
```dockerfile
# Check every 30 seconds, timeout after 3 seconds
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost/health || exit 1
```

### View Health Status
```bash
# Check if container is healthy
docker ps

# Output example:
# STATUS: Up 2 minutes (healthy)
# STATUS: Up 1 minute (unhealthy)
```

### Custom Health Check
```dockerfile
HEALTHCHECK --interval=30s --timeout=3s \
    CMD ["curl", "-f", "http://localhost:80/health"]
```

## Docker Compose Health Checks

### Health Check for Database
```yaml
db:
  image: mcr.microsoft.com/mssql/server:2022-latest
  
  healthcheck:
    test: /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P ${SA_PASSWORD} -Q "SELECT 1" || exit 1
    interval: 10s
    timeout: 3s
    retries: 5
    start_period: 10s
```

### API Depends on Database
```yaml
api:
  depends_on:
    db:
      condition: service_healthy
```

## Environment Variables

### Container Environment Configuration

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Development
  - ASPNETCORE_URLS=http://+:80
  - PORT=80
  - Logging__LogLevel__Default=Information
  - ConnectionStrings__DefaultConnection=Server=db;Database=MyDb;...
```

### .env File

Create `.env` file:
```
PORT=5000
ASPNETCORE_ENVIRONMENT=Development
SA_PASSWORD=YourPassword123!
```

Use in compose:
```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT}
  - PORT=${PORT}
```

Run with env file:
```bash
docker-compose --env-file .env up
```

## SSL/HTTPS in Docker

### Self-Signed Certificate
```bash
# Generate certificate
dotnet dev-certs https -ep $HOME/.aspnet/https/aspnetapp.pfx -p yourpassword

# Trust certificate (macOS)
security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain $HOME/.aspnet/https/aspnetapp.crt
```

### Dockerfile with HTTPS
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=https://+:443;http://+:80
ENV ASPNETCORE_HTTPS_PORT=443

COPY $HOME/.aspnet/https/aspnetapp.pfx /https/aspnetapp.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=yourpassword

EXPOSE 80 443
ENTRYPOINT ["dotnet", "MyApi.dll"]
```

### Docker Compose with HTTPS
```yaml
api:
  volumes:
    - ~/.aspnet/https:/https:ro
  
  ports:
    - "5000:80"
    - "5001:443"
  
  environment:
    - ASPNETCORE_URLS=https://+:443;http://+:80
    - ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx
    - ASPNETCORE_Kestrel__Certificates__Default__Password=yourpassword
```

## Networking

### Service-to-Service Communication

```yaml
services:
  api:
    networks:
      - app-network
    
    environment:
      - DATABASE_URL=Server=db;Database=MyDb;User=sa;Password=...
  
  db:
    networks:
      - app-network

networks:
  app-network:
    driver: bridge
```

### Access Database from API
```csharp
// Connection string uses service name 'db'
var connectionString = "Server=db;Database=MyDb;User=sa;Password=...";
```

## Kubernetes Setup

### Liveness Probe (Check if pod is alive)
```yaml
livenessProbe:
  httpGet:
    path: /health
    port: 80
  initialDelaySeconds: 10
  periodSeconds: 30
```

### Readiness Probe (Check if ready to accept traffic)
```yaml
readinessProbe:
  httpGet:
    path: /health
    port: 80
  initialDelaySeconds: 5
  periodSeconds: 10
```

### Complete Kubernetes Deployment
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: api-deployment
spec:
  replicas: 3
  selector:
    matchLabels:
      app: api
  template:
    metadata:
      labels:
        app: api
    spec:
      containers:
      - name: api
        image: myregistry.azurecr.io/myapi:latest
        ports:
        - containerPort: 80
        
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: PORT
          value: "80"
        
        livenessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 10
          periodSeconds: 30
        
        readinessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 5
          periodSeconds: 10
        
        resources:
          limits:
            cpu: 500m
            memory: 256Mi
          requests:
            cpu: 250m
            memory: 128Mi
```

## Troubleshooting

### Container Exits Immediately
```bash
# Check logs
docker-compose logs api

# Check build errors
docker-compose build --no-cache api
```

### Health Check Failing
```bash
# Test health endpoint in running container
docker-compose exec api curl http://localhost/health

# View container health
docker ps | grep api
```

### Port Conflicts
```bash
# Find process using port
lsof -i :5000

# Kill process
kill -9 <PID>

# Or change port in docker-compose
ports:
  - "8080:80"  # Change 5000 to 8080
```

### Database Connection Issues
```bash
# Verify database is healthy
docker-compose ps

# Test connection from API container
docker-compose exec api sqlcmd -S db -U sa -P YourPassword123! -Q "SELECT 1"
```

## Best Practices

1. **Use .dockerignore**
   ```
   bin/
   obj/
   .git/
   .gitignore
   .vs/
   .vscode/
   *.user
   ```

2. **Multi-stage builds** - Reduce final image size
   ```dockerfile
   FROM sdk AS build    # Build stage
   FROM runtime         # Final stage
   ```

3. **Health checks** - Enable Docker to manage container lifecycle
   ```dockerfile
   HEALTHCHECK --interval=30s ...
   ```

4. **Secrets management** - Don't hardcode credentials
   ```bash
   docker secret create db_password -
   # Use in compose via secrets
   ```

5. **Resource limits** - Prevent runaway containers
   ```yaml
   resources:
     limits:
       memory: 512M
       cpus: '1'
   ```

---

**See Also**:
- [Swagger Health Checks Guide](swagger-health-checks.md)
- [Docker Documentation](https://docs.docker.com/)
- [Docker Compose Specification](https://docs.docker.com/compose/compose-file/)
- [Kubernetes Health Checks](https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-startup-probes/)
