# syntax=docker/dockerfile:1
# Painel de super admin (independente do jogo).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/EduGame.Admin/EduGame.Admin.csproj src/EduGame.Admin/
RUN dotnet restore src/EduGame.Admin/EduGame.Admin.csproj
COPY src/ src/
RUN dotnet publish src/EduGame.Admin/EduGame.Admin.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "EduGame.Admin.dll"]
