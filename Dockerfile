# ---------- build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Sadece csproj’u kopyalayıp restore (cache için)
COPY BackendApi/*.csproj BackendApi/
RUN dotnet restore BackendApi/BackendApi.csproj

# Tüm kaynak kodu kopyala ve publish et
COPY . .
RUN dotnet publish BackendApi/BackendApi.csproj -c Release -o /app/out

# ---------- runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

# Render Docker servisleri için sabit port 10000 kullan
ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "BackendApi.dll"]
