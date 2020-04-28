#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

# FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
# WORKDIR /app

# FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
# WORKDIR /src
# COPY ["Veracity.EventHub.Splitter.DataFabric/Veracity.EventHub.Splitter.DataFabric.csproj", "Veracity.EventHub.Splitter.DataFabric/"]
# RUN dotnet restore "Veracity.EventHub.Splitter.DataFabric/Veracity.EventHub.Splitter.DataFabric.csproj"
# COPY . .
# WORKDIR "/src/Veracity.EventHub.Splitter.DataFabric"
# RUN dotnet build "Veracity.EventHub.Splitter.DataFabric.csproj" -c Release -o /app/build

# FROM build AS publish
# RUN dotnet publish "Veracity.EventHub.Splitter.DataFabric.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine as build
WORKDIR /app

# copy csproj and restore
COPY /. ./dotnet/
RUN cd ./dotnet/ && dotnet restore

# copy all files and build
#COPY ./. ./dotnet/
WORKDIR /app/dotnet
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine as runtime
WORKDIR /app
COPY --from=build /app/dotnet/out ./
ENTRYPOINT ["dotnet", "Veracity.EventHub.Splitter.DataFabric.dll"]