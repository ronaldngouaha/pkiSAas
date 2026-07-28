FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Tenants.Identity/Tenants.Identity.csproj", "src/Tenants.Identity/"]
COPY ["src/Shared.Contracts/Shared.Contracts.csproj", "src/Shared.Contracts/"]
RUN dotnet restore "src/Tenants.Identity/Tenants.Identity.csproj"
COPY . .
WORKDIR "/src/src/Tenants.Identity"
RUN dotnet publish "Tenants.Identity.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
ENTRYPOINT ["dotnet", "Tenants.Identity.dll"]
