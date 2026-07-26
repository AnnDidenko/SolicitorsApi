# syntax=docker/dockerfile:1

FROM node:24-alpine AS client-build
WORKDIR /src/ClientApp
COPY ClientApp/package*.json ./
RUN npm ci
COPY ClientApp/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api-build
WORKDIR /src
COPY SolicitorsApi.slnx ./
COPY SolicitorsApi/SolicitorsApi.csproj SolicitorsApi/
RUN dotnet restore SolicitorsApi/SolicitorsApi.csproj
COPY SolicitorsApi/ SolicitorsApi/
RUN dotnet publish SolicitorsApi/SolicitorsApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=api-build /app/publish ./
COPY --from=client-build /src/ClientApp/dist ./wwwroot
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "SolicitorsApi.dll"]
