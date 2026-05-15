# ── Build stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first (layer-cached when only source changes)
COPY src/AutomowerMcp/AutomowerMcp.csproj AutomowerMcp/
RUN dotnet restore AutomowerMcp/AutomowerMcp.csproj

# Copy source and publish
COPY src/AutomowerMcp/ AutomowerMcp/
RUN dotnet publish AutomowerMcp/AutomowerMcp.csproj \
        --configuration Release \
        --no-restore \
        --output /app/publish \
        /p:UseAppHost=false

# ── Runtime stage ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# The official .NET runtime image ships with a built-in non-root user 'app' (uid 1654).
USER app

COPY --from=build --chown=app /app/publish .

# Port 8080 is only used when MCP_TRANSPORT=http.
# Pass -p 8080:8080 and omit -i when running in HTTP mode.
EXPOSE 8080

# Health check for HTTP transport mode. Has no effect in stdio mode.
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD ["sh", "-c", "[ \"$MCP_TRANSPORT\" = \"http\" ] && wget -qO- http://localhost:8080/health || exit 0"]

ENTRYPOINT ["dotnet", "AutomowerMcp.dll"]
