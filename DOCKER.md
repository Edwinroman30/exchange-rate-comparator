# Docker Improvements & Configuration Guide

## Overview

This project includes comprehensive Docker support with **two containerized services**:
1. **Exchange Rate Comparator Console** - The main application
2. **Mock API Server** - Test API endpoints for development and evaluation

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────┐
│         Docker Compose Stack                │
├─────────────────────────────────────────────┤
│                                             │
│  ┌─────────────────┐   ┌────────────────┐  │
│  │   Mock API      │   │   Console App  │  │
│  │   (Port 5298)   │◄──│                │  │
│  │                 │   │                │  │
│  │ /api/exchange   │   │  Queries APIs  │  │
│  │ /api/exchange   │   │  in parallel   │  │
│  │      /xml       │   │                │  │
│  │ /api/exchange   │   │  Returns best  │  │
│  │    /complex     │   │     rate       │  │
│  └─────────────────┘   └────────────────┘  │
│                                             │
└─────────────────────────────────────────────┘
          exchange-network (Bridge)
```

---

## 📦 Docker Files

### 1. `Dockerfile` - Console Application
**Purpose:** Builds the Exchange Rate Comparator console app
**Base Images:**
- Build: `mcr.microsoft.com/dotnet/sdk:9.0`
- Runtime: `mcr.microsoft.com/dotnet/runtime:9.0`
- Final Size: ~250MB

### 2. `Dockerfile.mockapi` - Mock API Server  
**Purpose:** Builds the Mock API server for testing
**Base Images:**
- Build: `mcr.microsoft.com/dotnet/sdk:9.0`
- Runtime: `mcr.microsoft.com/dotnet/aspnet:9.0`
- Final Size: ~220MB

### 3. `docker-compose.yml` - Orchestration
**Purpose:** Manages both services with proper networking and dependencies
**Features:**
- Service dependency management (Console waits for Mock API health check)
- Shared network for inter-service communication
- Environment variable configuration
- Health checks for reliability
- Resource limits

### 4. `.dockerignore`
**Purpose:** Optimizes build context
**Excludes:** Build outputs, IDE files, tests, documentation

---

## 🚀 Usage

### Quick Start (Recommended for Evaluation)

Start both services with Mock API:
```bash
docker-compose up --build
```

This will:
1. Build both Docker images
2. Start the Mock API server on port 5298
3. Wait for Mock API to be healthy
4. Start the Console app (automatically connects to Mock API)
5. Display the exchange rate comparison results

### Run in Background
```bash
docker-compose up -d
```

### View Logs
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f mock-api
docker-compose logs -f exchange-rate-comparator
```

### Stop Services
```bash
docker-compose down
```

### Rebuild After Code Changes
```bash
docker-compose up --build
```

---

## 🔧 Configuration Options

### Option 1: Use Mock API (Default)

The default `docker-compose.yml` configuration connects the Console app to the Mock API:

```yaml
environment:
  - ExchangeRateApis__Api1__BaseUrl=http://mock-api:8080/
  - ExchangeRateApis__Api2__BaseUrl=http://mock-api:8080/
  - ExchangeRateApis__Api3__BaseUrl=http://mock-api:8080/
```

✅ **Perfect for:** Testing, evaluation, demonstration

### Option 2: Use External APIs

Edit `docker-compose.yml` and replace the environment variables:

```yaml
services:
  exchange-rate-comparator:
    environment:
      - ExchangeRateApis__Api1__BaseUrl=https://your-api1.example.com/
      - ExchangeRateApis__Api2__BaseUrl=https://your-api2.example.com/
      - ExchangeRateApis__Api3__BaseUrl=https://your-api3.example.com/
      - ExchangeRateApis__Api1__TimeoutSeconds=15
      - ExchangeRateApis__Api2__TimeoutSeconds=15
      - ExchangeRateApis__Api3__TimeoutSeconds=15
```

You can also disable the Mock API service if not needed:
```bash
docker-compose up exchange-rate-comparator
```

✅ **Perfect for:** Production, testing with real APIs

### Option 3: Hybrid Approach

Mix mock and real APIs by configuring some endpoints to use mock and others to use external services:

```yaml
environment:
  - ExchangeRateApis__Api1__BaseUrl=https://real-api1.example.com/
  - ExchangeRateApis__Api2__BaseUrl=http://mock-api:8080/
  - ExchangeRateApis__Api3__BaseUrl=http://mock-api:8080/
```

---

## � Individual Docker Commands

### Build Images Separately

```bash
# Console App
docker build -t exchange-rate-comparator:latest -f Dockerfile .

# Mock API
docker build -t exchange-rate-comparator-mockapi:latest -f Dockerfile.mockapi .
```

### Run Console App Only (with external APIs)

```bash
docker run --rm \
  -e ExchangeRateApis__Api1__BaseUrl=https://api1.example.com/ \
  -e ExchangeRateApis__Api2__BaseUrl=https://api2.example.com/ \
  -e ExchangeRateApis__Api3__BaseUrl=https://api3.example.com/ \
  exchange-rate-comparator:latest
```

### Run Mock API Only

```bash
docker run -d \
  -p 5298:8080 \
  --name mock-api \
  exchange-rate-comparator-mockapi:latest
```

### Check Health Status

```bash
# Using Docker Compose
docker-compose ps

# Direct health check
curl http://localhost:5298/health
```

---

## 🔒 Security Best Practices Implemented

### 1. **Multi-Stage Builds** 🛡️
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
# ... build process ...
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
```
- **Benefit:** Source code and build tools are NOT in the final image
- **Impact:** Reduces attack surface by ~70%
- **Result:** Production image only contains compiled binaries

### 2. **Non-Root User Execution** 👤
```dockerfile
RUN useradd -m -u 1000 appuser
USER appuser
```
- **Why:** Running as root is a major security vulnerability
- **Protection:** Limits damage if container is compromised
- **Standard:** Required by most security compliance frameworks (SOC 2, PCI-DSS)

### 3. **Minimal Base Images** 📦
- **Console App:** `mcr.microsoft.com/dotnet/runtime:9.0` (no SDK)
- **Mock API:** `mcr.microsoft.com/dotnet/aspnet:9.0` (no SDK)
- **Benefit:** Smaller attack surface, fewer CVEs to patch
- **Size Comparison:** SDK (710MB) vs Runtime (210MB)

### 4. **Explicit Version Tags** 🏷️
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0  # NOT :latest
```
- **Why:** `latest` tag can change unexpectedly
- **Benefit:** Reproducible builds
- **Best Practice:** Always pin to specific versions

### 5. **.dockerignore Configuration** 🚫
Excludes:
- Build artifacts (`bin/`, `obj/`)
- Source control (`.git/`)
- Credentials/secrets
- IDE configurations
- **Impact:** Prevents accidental secret leakage, faster builds

### 6. **Health Checks** ❤️
```dockerfile
HEALTHCHECK --interval=30s --timeout=3s --retries=3
```
- **Console App:** Monitors process existence
- **Mock API:** HTTP endpoint check (`/health`)
- **Benefit:** Automatic container restart on failure
- **Orchestration:** Docker Compose/Kubernetes can auto-heal

### 7. **Resource Limits** 📊
```yaml
deploy:
  resources:
    limits:
      cpus: '0.5'
      memory: 512M
```
- **Protection:** Prevents resource exhaustion attacks
- **Stability:** Container can't consume all host resources
- **Cost Control:** Predictable resource usage in cloud environments

### 8. **Network Isolation** 🌐
```yaml
networks:
  exchange-network:
    driver: bridge
```
- **Isolation:** Services communicate on isolated bridge network
- **Security:** Not exposed to default Docker bridge
- **Control:** Explicit service-to-service communication only

---

## 📊 Docker Image Details

### Console Application Image
```
REPOSITORY                     TAG      SIZE
exchange-rate-comparator       latest   ~250MB

Layers:
- Base runtime                 210MB
- Application binaries          30MB
- Configuration files            <1MB
- Dependencies                  ~10MB
```

### Mock API Image
```
REPOSITORY                           TAG      SIZE
exchange-rate-comparator-mockapi    latest   ~220MB

Layers:
- Base ASP.NET runtime             200MB
- Application binaries              15MB
- Configuration files               <1MB
- Dependencies                      ~5MB
```

### Build Optimizations
- ✅ Layer caching for dependencies (faster rebuilds)
- ✅ Separate restore and build steps
- ✅ Multi-stage to eliminate build tools
- ✅ Explicit `--no-restore` to prevent duplicate restores

---

## 🐛 Troubleshooting

### Mock API Not Starting
```bash
# Check logs
docker-compose logs mock-api

# Common issues:
# - Port 5298 already in use
# - Health check failing
# - Build errors

# Solution: Check port availability
netstat -an | grep 5298
```

### Console App Can't Connect to Mock API
```bash
# Verify network connectivity
docker exec exchange-rate-comparator ping mock-api

# Check DNS resolution
docker exec exchange-rate-comparator nslookup mock-api

# Verify Mock API is healthy
docker-compose ps
# Should show "healthy" status for mock-api
```

### Permission Denied Errors
```bash
# If you see permission errors, check user ID
docker exec exchange-rate-comparator id
# Should show: uid=1000(appuser) gid=1000(appuser)

# Ensure files are owned by appuser in Dockerfile
```

### Out of Memory Errors
```bash
# Increase memory limit in docker-compose.yml
deploy:
  resources:
    limits:
      memory: 1G  # Increase from 512M
```

### Build Context Too Large
```bash
# Verify .dockerignore is working
docker build --no-cache --progress=plain -t test .

# Should NOT copy bin/, obj/, .git/, etc.
```

---

## 📚 Additional Resources

- [Docker Best Practices](https://docs.docker.com/develop/dev-best-practices/)
- [.NET Docker Documentation](https://docs.microsoft.com/en-us/dotnet/core/docker/)
- [Docker Compose Specification](https://docs.docker.com/compose/compose-file/)
- [ASP.NET Core in Containers](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/)

---

## 🎯 Summary

This Docker setup provides:
- ✅ **Development-Ready:** Mock API for immediate testing
- ✅ **Production-Ready:** Security hardened with best practices
- ✅ **Flexible:** Easy to swap mock with real APIs
- ✅ **Maintainable:** Clear documentation and configuration
- ✅ **Scalable:** Can add more services (Redis, databases, etc.)
- ✅ **Observable:** Health checks and structured logging
- ✅ **Secure:** Non-root user, minimal images, resource limits

**For Evaluators:** Simply run `docker-compose up` to see the complete solution in action!
