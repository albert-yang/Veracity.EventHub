#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["Veracity.EventHub.DataFabricIngester/Veracity.EventHub.DataFabricIngester.csproj", "Veracity.EventHub.DataFabricIngester/"]
RUN dotnet restore "Veracity.EventHub.DataFabricIngester/Veracity.EventHub.DataFabricIngester.csproj"
COPY . .
WORKDIR "/src/Veracity.EventHub.DataFabricIngester"
RUN dotnet build "Veracity.EventHub.DataFabricIngester.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Veracity.EventHub.DataFabricIngester.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Veracity.EventHub.DataFabricIngester.dll"]