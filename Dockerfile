# syntax=docker/dockerfile:1
# .NET 10 LTS host for the existing React DEMO. No database/API backend yet.
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY src/AppStore.Web/ src/AppStore.Web/
# Frontend is precompiled. A Node toolchain is not required to build this image.
COPY dist/ dist/
RUN dotnet publish src/AppStore.Web/AppStore.Web.csproj -c Release \
    -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
COPY --from=build --chown=app:app /app/publish/ ./
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Test \
    AppStore__EnableDemo=false
EXPOSE 8080
# Health endpoint: GET /healthz. Configure HTTP probes in the hosting platform.
# Explicitly enable the demo at runtime. Production startup is rejected.
USER app
ENTRYPOINT ["dotnet", "AppStore.Web.dll"]
