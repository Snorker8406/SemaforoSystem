# Consulte https://aka.ms/customizecontainer para aprender a personalizar su contenedor de depuración y cómo Visual Studio usa este Dockerfile para compilar sus imágenes para una depuración más rápida.

# Esta fase se usa cuando se ejecuta desde VS en modo rápido (valor predeterminado para la configuración de depuración)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080


# Esta fase se usa para compilar el proyecto de servicio
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS with-node
RUN apt-get update && apt-get install -y curl && \
    curl -sL https://deb.nodesource.com/setup_20.x | bash && \
    apt-get -y install nodejs


FROM with-node AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SemaforoSystem.Server/SemaforoSystem.Server.csproj", "SemaforoSystem.Server/"]
COPY ["semaforosystem.client/semaforosystem.client.esproj", "semaforosystem.client/"]
COPY ["semaforosystem.client/package.json", "semaforosystem.client/"]
COPY ["semaforosystem.client/package-lock.json", "semaforosystem.client/"]
RUN dotnet restore "./SemaforoSystem.Server/SemaforoSystem.Server.csproj"
COPY . .

# Build del cliente React/Vite
WORKDIR /src/semaforosystem.client
RUN npm ci
RUN npm run build

# Build del servidor .NET
WORKDIR "/src/SemaforoSystem.Server"
RUN dotnet build "./SemaforoSystem.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Esta fase se usa para publicar el proyecto de servicio que se copiará en la fase final.
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SemaforoSystem.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Esta fase se usa en producción o cuando se ejecuta desde VS en modo normal (valor predeterminado cuando no se usa la configuración de depuración)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SemaforoSystem.Server.dll"]
