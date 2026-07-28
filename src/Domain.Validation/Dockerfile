FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Domain.Validation/Domain.Validation.csproj", "src/Domain.Validation/"]
COPY ["src/Shared.Contracts/Shared.Contracts.csproj", "src/Shared.Contracts/"]
RUN dotnet restore "src/Domain.Validation/Domain.Validation.csproj"

COPY . .
WORKDIR "/src/src/Domain.Validation"
RUN dotnet publish "Domain.Validation.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 80

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Domain.Validation.dll"]
