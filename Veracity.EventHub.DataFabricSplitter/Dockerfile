#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["Veracity.EventHub.DataFabricSplitter/Veracity.EventHub.DataFabricSplitter.csproj", "Veracity.EventHub.DataFabricSplitter/"]
RUN dotnet restore "Veracity.EventHub.DataFabricSplitter/Veracity.EventHub.DataFabricSplitter.csproj"
COPY . .
WORKDIR "/src/Veracity.EventHub.DataFabricSplitter"
RUN dotnet build "Veracity.EventHub.DataFabricSplitter.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Veracity.EventHub.DataFabricSplitter.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Veracity.EventHub.DataFabricSplitter.dll"]