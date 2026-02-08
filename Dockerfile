# Use the official ASP.NET Core runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["UserManagementApp.csproj", "./"]
RUN dotnet restore "./UserManagementApp.csproj"

# Copy the rest of the code
COPY . .
WORKDIR "/src/."
RUN dotnet publish "UserManagementApp.csproj" -c Release -o /app/publish

# Build runtime image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# SQLite requires write permissions for the database file
RUN mkdir -p /app/Data
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Set environment to Production
ENV DOTNET_ENVIRONMENT=Production

# Run the app
ENTRYPOINT ["dotnet", "UserManagementApp.dll"]
