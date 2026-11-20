# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props EduGame.slnx ./
COPY src/EduGame.Domain/EduGame.Domain.csproj src/EduGame.Domain/
COPY src/EduGame.Problems/EduGame.Problems.csproj src/EduGame.Problems/
COPY src/EduGame.Infrastructure/EduGame.Infrastructure.csproj src/EduGame.Infrastructure/
COPY src/EduGame.Api/EduGame.Api.csproj src/EduGame.Api/
RUN dotnet restore src/EduGame.Api/EduGame.Api.csproj
COPY src/ src/
RUN dotnet publish src/EduGame.Api/EduGame.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "EduGame.Api.dll"]
