FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["App4/App4.csproj", "App4/"]
RUN dotnet restore "App4/App4.csproj"
COPY . .
WORKDIR "/src/App4"
RUN dotnet build "App4.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "App4.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "App4.dll"]
