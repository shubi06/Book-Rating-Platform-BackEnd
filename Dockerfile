# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY BookRatingAPI/BookRatingAPI.csproj BookRatingAPI/
RUN dotnet restore "BookRatingAPI/BookRatingAPI.csproj"

# Copy everything else and build
COPY BookRatingAPI/ BookRatingAPI/
WORKDIR /src/BookRatingAPI
RUN dotnet build "BookRatingAPI.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "BookRatingAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copy published app
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "BookRatingAPI.dll"]
