FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY OldMuSite.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app
COPY --from=build /app .
# The registration log (per-IP limits) lives in /app/data, which the compose file mounts as a volume.
RUN mkdir -p /app/data && chown $APP_UID /app/data
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080 \
    Site__DataDirectory=/app/data
EXPOSE 8080
ENTRYPOINT ["dotnet", "OldMuSite.dll"]
