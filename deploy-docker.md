# Docker Deployment Guide for Production Tracker

## Build Docker Image
```bash
docker build -t production-tracker .
```

## Run Docker Container
```bash
docker run -d -p 8080:80 --name production-tracker-app production-tracker
```

## Access
Your app will be available at: `http://localhost:8080`

## With Docker Compose
Create a `docker-compose.yml`:

```yaml
version: '3.8'
services:
  production-tracker:
    build: .
    ports:
      - "8080:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    restart: unless-stopped
```

Run with: `docker-compose up -d`
