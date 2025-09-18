FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SubEmailSender.csproj", "./"]
RUN dotnet restore "SubEmailSender.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "./SubEmailSender.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SubEmailSender.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final

ARG INFISICAL_CLIENT_ID
ARG INFISICAL_CLIENT_SECRET
ARG INFISICAL_PROJECT_ID

ENV INFISICAL_CLIENT_ID=$INFISICAL_CLIENT_ID
ENV INFISICAL_CLIENT_SECRET=$INFISICAL_CLIENT_SECRET
ENV INFISICAL_PROJECT_ID=$INFISICAL_PROJECT_ID

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SubEmailSender.dll"]
