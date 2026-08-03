FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["AcikIstihbarat.API.csproj", "./"]
RUN dotnet restore "AcikIstihbarat.API.csproj"

# Copy everything else and build
COPY . .
RUN dotnet publish "AcikIstihbarat.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create the directory for MedyaKutuphanesi
RUN mkdir -p /app/MedyaKutuphanesi

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "AcikIstihbarat.API.dll"]
