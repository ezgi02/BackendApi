# ---------- build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Proje dosyasını klasör adıyla kopyala ve restore et
COPY BackendApi/*.csproj BackendApi/
RUN dotnet restore BackendApi/BackendApi.csproj

# Tüm kodu al ve publish et
COPY . .
RUN dotnet publish BackendApi/BackendApi.csproj -c Release -o /app/out

# ---------- runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .
ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000
ENTRYPOINT ["dotnet", "BackendApi.dll"]
