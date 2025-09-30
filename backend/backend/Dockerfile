# Build aşaması (9.0 SDK)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Proje dosyalarını kopyala
COPY . ./

# Publish
RUN dotnet publish -c Release -o out

# Runtime aşaması (9.0 runtime)
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .

EXPOSE 5000

ENTRYPOINT ["dotnet", "chat-backend.dll"]

