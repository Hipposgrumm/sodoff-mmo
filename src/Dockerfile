# Use the official .NET SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the source code
COPY . .

# Restore dependencies and build the application
RUN dotnet build -c Release -o /app

# Create clean run environment (without source and sdk)
# FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
# WORKDIR /app
# COPY --from=build /app .

# Run the application
WORKDIR /app
ENTRYPOINT ["./sodoffmmo"]
