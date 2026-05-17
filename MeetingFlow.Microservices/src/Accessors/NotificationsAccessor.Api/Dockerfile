FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Accessors/NotificationsAccessor.Api/NotificationsAccessor.Api.csproj src/Accessors/NotificationsAccessor.Api/
RUN dotnet restore src/Accessors/NotificationsAccessor.Api/NotificationsAccessor.Api.csproj
COPY src/Accessors/NotificationsAccessor.Api/ src/Accessors/NotificationsAccessor.Api/
RUN dotnet publish src/Accessors/NotificationsAccessor.Api/NotificationsAccessor.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
EXPOSE 5011
ENTRYPOINT ["dotnet", "NotificationsAccessor.Api.dll"]
