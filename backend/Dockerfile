# ============================================
# DKPT Backend — .NET 9 Multi-Stage Dockerfile
# ============================================

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files first (better cache)
COPY Dkpt.sln ./
COPY src/Dkpt.Domain/Dkpt.Domain.csproj src/Dkpt.Domain/
COPY src/Dkpt.Application/Dkpt.Application.csproj src/Dkpt.Application/
COPY src/Dkpt.Infrastructure/Dkpt.Infrastructure.csproj src/Dkpt.Infrastructure/
COPY src/Dkpt.Api/Dkpt.Api.csproj src/Dkpt.Api/

# Restore dependencies (API project only, not tests)
RUN dotnet restore src/Dkpt.Api/Dkpt.Api.csproj

# Copy everything else and publish
COPY . .
RUN dotnet publish src/Dkpt.Api/Dkpt.Api.csproj -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# .NET 9 default port
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Dkpt.Api.dll"]
