FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Accessors/NotificationsAccessor/NotificationsAccessor.csproj src/Accessors/NotificationsAccessor/
RUN dotnet restore src/Accessors/NotificationsAccessor/NotificationsAccessor.csproj
COPY src/Accessors/NotificationsAccessor/ src/Accessors/NotificationsAccessor/
RUN dotnet publish src/Accessors/NotificationsAccessor/NotificationsAccessor.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
EXPOSE 5011
ENTRYPOINT ["dotnet", "NotificationsAccessor.dll"]
