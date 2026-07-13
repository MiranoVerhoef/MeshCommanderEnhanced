FROM node:22-bookworm-slim AS web
WORKDIR /src
COPY . .
RUN npm run test:web && npm run build:web

FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build
WORKDIR /src
COPY . .
COPY --from=web /src/src/MeshCommander.Server/wwwroot ./src/MeshCommander.Server/wwwroot
RUN dotnet publish src/MeshCommander.Server/MeshCommander.Server.csproj -c Release -o /app -p:SkipWebBuild=true

FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS runtime
WORKDIR /app
RUN adduser --system --group --home /app meshcommander
COPY --from=build /app .
USER meshcommander
ENV ASPNETCORE_URLS=http://0.0.0.0:3000
ENV MCE_ALLOWED_TARGETS=private
EXPOSE 3000
ENTRYPOINT ["dotnet", "MeshCommander.Server.dll"]
