# Use official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["UserManagementApp.csproj", "./"]
RUN dotnet restore "./UserManagementApp.csproj"
COPY . .
RUN dotnet publish "UserManagementApp.csproj" -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
# Create folder for SQLite DB
RUN mkdir -p /app/Data

# Expose the port Render will use
EXPOSE 8080

# Start the app
ENTRYPOINT ["dotnet", "UserManagementApp.dll"]
