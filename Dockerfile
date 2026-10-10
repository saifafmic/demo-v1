# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files for caching restore layers
COPY Directory.Build.props ./
COPY src/VideoConference.Domain/VideoConference.Domain.csproj src/VideoConference.Domain/
COPY src/VideoConference.Application/VideoConference.Application.csproj src/VideoConference.Application/
COPY src/VideoConference.Infrastructure/VideoConference.Infrastructure.csproj src/VideoConference.Infrastructure/
COPY src/VideoConference.API/VideoConference.API.csproj src/VideoConference.API/

RUN dotnet restore src/VideoConference.API/VideoConference.API.csproj

# Copy source and publish
COPY src/ src/
RUN dotnet publish src/VideoConference.API/VideoConference.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "VideoConference.API.dll"]
