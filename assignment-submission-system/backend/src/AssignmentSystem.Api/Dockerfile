# Build context is the repository root: docker build -f backend/src/AssignmentSystem.Api/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY backend/ ./
RUN dotnet restore src/AssignmentSystem.Api/AssignmentSystem.Api.csproj
RUN dotnet publish src/AssignmentSystem.Api/AssignmentSystem.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

COPY --from=build /app/publish ./

EXPOSE 8080
ENTRYPOINT ["dotnet", "AssignmentSystem.Api.dll"]
