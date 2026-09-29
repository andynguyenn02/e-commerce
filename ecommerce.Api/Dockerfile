# Build context is the repo root (the API needs all four projects):
#   docker build -f ecommerce.Api/Dockerfile .

# ---- restore (cached until a .csproj changes) ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src
COPY ecommerce.Domain/ecommerce.Domain.csproj ecommerce.Domain/
COPY ecommerce.Application/ecommerce.Application.csproj ecommerce.Application/
COPY ecommerce.Infrastructure/ecommerce.Infrastructure.csproj ecommerce.Infrastructure/
COPY ecommerce.Api/ecommerce.Api.csproj ecommerce.Api/
RUN dotnet restore ecommerce.Api/ecommerce.Api.csproj

# ---- build + publish ----
FROM restore AS publish
COPY ecommerce.Domain/ ecommerce.Domain/
COPY ecommerce.Application/ ecommerce.Application/
COPY ecommerce.Infrastructure/ ecommerce.Infrastructure/
COPY ecommerce.Api/ ecommerce.Api/
RUN dotnet publish ecommerce.Api/ecommerce.Api.csproj -c Release -o /out --no-restore

# ---- EF Core migrations bundle: a single executable that applies migrations ----
FROM publish AS migrations-build
RUN dotnet tool install --global dotnet-ef --version 10.0.12
ENV PATH="$PATH:/root/.dotnet/tools"
# AddInfrastructure throws without a connection string, even at design time.
# The bundle never connects while building; the real one comes via --connection.
RUN ConnectionStrings__DefaultConnection="Server=build-placeholder" \
    dotnet ef migrations bundle \
      --project ecommerce.Infrastructure \
      --startup-project ecommerce.Api \
      --self-contained -r linux-x64 \
      -o /out/efbundle

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS migrator
WORKDIR /app
COPY --from=migrations-build /out/efbundle .
# Needs ConnectionStrings__DefaultConnection at run time (see docker-compose.full.yml).
ENTRYPOINT ["./efbundle"]

# ---- API runtime (default target) ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app
COPY --from=publish /out .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ecommerce.Api.dll"]
