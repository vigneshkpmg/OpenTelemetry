FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["App3/App3.csproj", "App3/"]
RUN dotnet restore "App3/App3.csproj"
COPY . .
WORKDIR "/src/App3"
RUN dotnet build "App3.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "App3.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "App3.dll"]
