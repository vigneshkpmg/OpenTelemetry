FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["API1/API1.csproj", "API1/"]
RUN dotnet restore "API1/API1.csproj"
COPY . .
WORKDIR "/src/API1"
RUN dotnet build "API1.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "API1.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "API1.dll"]
