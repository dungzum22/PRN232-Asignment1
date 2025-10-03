# Multi-stage Dockerfile targeting ARM64 builds
ARG TARGETPLATFORM=linux/arm64
ARG BUILDPLATFORM=linux/arm64

FROM --platform=${BUILDPLATFORM} mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ShopNew/ShopNew.csproj ShopNew/
RUN dotnet restore ShopNew/ShopNew.csproj

# Copy everything and publish
COPY . .
RUN dotnet publish ShopNew/ShopNew.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM --platform=${TARGETPLATFORM} mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ShopNew.dll"]


