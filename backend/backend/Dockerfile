# Build aşaması
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Tüm backend dosyalarını kopyala
COPY . ./

# Publish
RUN dotnet publish -c Release -o out

# Runtime aşaması
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Build edilmiş dosyaları kopyala
COPY --from=build /app/out .

EXPOSE 5000

# DLL adını kesinlikle backend.dll olarak ayarla
ENTRYPOINT ["dotnet", "backend.dll"]

