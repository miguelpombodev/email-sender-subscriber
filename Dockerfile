FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY nuget.config .
COPY ["SubEmailSender.csproj", "./"]

RUN --mount=type=secret,id=GITHUB_TOKEN \
    --mount=type=cache,target=/root/.nuget/packages \
    export GITHUB_TOKEN="$(cat /run/secrets/GITHUB_TOKEN)" && \
    dotnet restore "SubEmailSender.csproj" --verbosity normal --configfile nuget.config

COPY . .

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "SubEmailSender.csproj" \
    --configuration $BUILD_CONFIGURATION \
    --no-restore \
    -o /app/publish \
    /p:UseAppHost=false
    
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
ARG APP_USER=app
ARG APP_UID=1000

RUN adduser -S -u ${APP_UID:-1000} -G ${APP_USER} -h /app ${APP_USER} || true

WORKDIR /app

ENV DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 8080
COPY --from=build --chown=${APP_USER}:${APP_USER} /app/publish .

USER ${APP_USER}

ENTRYPOINT ["dotnet", "SubEmailSender.dll"]
