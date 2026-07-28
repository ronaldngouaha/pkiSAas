FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Audit.Events/Audit.Events.csproj", "src/Audit.Events/"]
COPY ["src/Shared.Contracts/Shared.Contracts.csproj", "src/Shared.Contracts/"]
RUN dotnet restore "src/Audit.Events/Audit.Events.csproj"

COPY . .
WORKDIR "/src/src/Audit.Events"
RUN dotnet publish "Audit.Events.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 80

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Audit.Events.dll"]
