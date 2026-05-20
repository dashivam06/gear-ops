
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first for better layer caching
COPY gearOps.sln ./
COPY gearOps/gearOps.csproj gearOps/
COPY gearOps.Application/gearOps.Application.csproj gearOps.Application/
COPY gearOps.Domain/gearOps.Domain.csproj gearOps.Domain/
COPY gearOps.Infrastructure/gearOps.Infrastructure.csproj gearOps.Infrastructure/

RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet publish gearOps/gearOps.csproj -c Release -o /app/publish --no-restore

# ── Stage 2: Runtime ──────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Security: run as non-root user
RUN groupadd -r gearops && useradd -r -g gearops -s /bin/false gearops

COPY --from=build /app/publish .

# Default environment variables to run on port 7777
ENV ASPNETCORE_URLS=http://+:7777
ENV ASPNETCORE_ENVIRONMENT=Production
ENV SERVER_PORT=7777

EXPOSE 7777

USER gearops

ENTRYPOINT ["dotnet", "gearOps.dll"]
