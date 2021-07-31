FROM mcr.microsoft.com/dotnet/runtime:5.0.8-buster-slim-arm32v7 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["AutoRetweeter/AutoRetweeter.csproj", "AutoRetweeter/"]
RUN dotnet restore "AutoRetweeter/AutoRetweeter.csproj"
COPY . .
WORKDIR "/src/AutoRetweeter"
RUN dotnet build "AutoRetweeter.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AutoRetweeter.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AutoRetweeter.dll"]
