# Use official .NET SDK (Build Stage)
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

# Copy and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the application files and build
COPY . ./
RUN dotnet publish -c Release -o /out

# Use official ASP.NET Core runtime image (Runtime Stage)
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app

# Copy built application from previous stage
COPY --from=build /out .

# Expose port and define the entry point
EXPOSE 80
CMD ["dotnet", "YourApp.dll"]
