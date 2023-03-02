FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["App2/App2.csproj", "App2/"]
RUN dotnet restore "App2/App2.csproj"
COPY . .
WORKDIR "/src/App2"
RUN dotnet build "App2.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "App2.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "App2.dll"]
