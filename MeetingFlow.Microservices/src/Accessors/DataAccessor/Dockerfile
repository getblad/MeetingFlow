FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Accessors/DataAccessor/DataAccessor.csproj src/Accessors/DataAccessor/
RUN dotnet restore src/Accessors/DataAccessor/DataAccessor.csproj
COPY src/Accessors/DataAccessor/ src/Accessors/DataAccessor/
RUN dotnet publish src/Accessors/DataAccessor/DataAccessor.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
EXPOSE 5010
ENTRYPOINT ["dotnet", "DataAccessor.dll"]
