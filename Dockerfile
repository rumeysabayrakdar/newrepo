# syntax=docker/dockerfile:1
# .NET 10 + React application. PostgreSQL is mandatory.
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
COPY . /src/
RUN --mount=type=secret,id=proxy_ca \
    if [ -f /run/secrets/proxy_ca ]; then export SSL_CERT_FILE=/run/secrets/proxy_ca; fi; \
    dotnet publish /src/src/AppStore.Web/AppStore.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
COPY --from=build --chown=app:app /app/publish/ ./
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
# Health endpoint: GET /healthz. Configure HTTP probes in the hosting platform.
# Test/Production is selected only by AppStore:Environment in appsettings.json.
RUN mkdir -p /app/data && chown app:app /app/data
USER app
ENTRYPOINT ["dotnet", "AppStore.Web.dll"]
