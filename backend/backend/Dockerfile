# Build aşaması
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

# Tüm dosyaları kopyala
COPY . ./

# Publish
RUN dotnet publish -c Release -o out

# Runtime aşaması
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build /app/out .

# Port aç
EXPOSE 5000

# Uygulamayı çalıştır
ENTRYPOINT ["dotnet", "chat-backend.dll"]
