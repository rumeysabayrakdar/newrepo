# syntax=docker/dockerfile:1
# .NET 10 pilot: trusted auth-gate identity and filtered read-only catalogue.
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
COPY . /src/
RUN --mount=type=secret,id=proxy_ca \
    if [ -f /run/secrets/proxy_ca ]; then export SSL_CERT_FILE=/run/secrets/proxy_ca; fi; \
    dotnet publish /src/src/AppStore.Web/AppStore.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
COPY --from=build --chown=app:app /app/publish/ ./
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Test \
    AppStore__Mode=Demo \
    AppStore__EnableDemo=true
EXPOSE 8080
# Health endpoint: GET /healthz. Configure HTTP probes in the hosting platform.
# Demo requires explicit mode/opt-in. AuthGate requires trusted gateway configuration.
RUN mkdir -p /app/data && chown app:app /app/data
USER app
ENTRYPOINT ["dotnet", "AppStore.Web.dll"]
