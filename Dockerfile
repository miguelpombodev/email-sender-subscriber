FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY ["SubEmailSender.csproj", "./"]

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "SubEmailSender.csproj" --verbosity normal 

COPY . .

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "SubEmailSender.csproj" \
    -c $BUILD_CONFIGURATION -o /app/publish \
    /p:UseAppHost=false
    
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
ARG APP_USER=app
ARG APP_UID=1000

RUN adduser -S -u ${APP_UID:-1000} -G ${APP_USER} -h /app ${APP_USER} || true

WORKDIR /app

EXPOSE 8080
COPY --from=build --chown=${APP_USER}:${APP_USER} /app/publish .

USER ${APP_USER}

ENTRYPOINT ["dotnet", "SubEmailSender.dll"]
