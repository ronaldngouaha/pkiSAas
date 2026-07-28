FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Crl.Publish/Crl.Publish.csproj", "src/Crl.Publish/"]
COPY ["src/Shared.Contracts/Shared.Contracts.csproj", "src/Shared.Contracts/"]
RUN dotnet restore "src/Crl.Publish/Crl.Publish.csproj"

COPY . .
WORKDIR "/src/src/Crl.Publish"
RUN dotnet publish "Crl.Publish.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 80

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Crl.Publish.dll"]
