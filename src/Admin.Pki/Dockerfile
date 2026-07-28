FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Admin.Pki/Admin.Pki.csproj", "src/Admin.Pki/"]
COPY ["src/Shared.Contracts/Shared.Contracts.csproj", "src/Shared.Contracts/"]
RUN dotnet restore "src/Admin.Pki/Admin.Pki.csproj"

COPY . .
WORKDIR "/src/src/Admin.Pki"
RUN dotnet publish "Admin.Pki.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 80

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Admin.Pki.dll"]
