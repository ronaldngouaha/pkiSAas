FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Certificates.Lifecycle/Certificates.Lifecycle.csproj", "src/Certificates.Lifecycle/"]
COPY ["src/Shared.Contracts/Shared.Contracts.csproj", "src/Shared.Contracts/"]
RUN dotnet restore "src/Certificates.Lifecycle/Certificates.Lifecycle.csproj"

COPY . .
WORKDIR "/src/src/Certificates.Lifecycle"
RUN dotnet publish "Certificates.Lifecycle.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 80

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Certificates.Lifecycle.dll"]
