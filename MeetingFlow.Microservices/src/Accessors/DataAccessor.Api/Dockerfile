FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Accessors/DataAccessor.Api/DataAccessor.Api.csproj src/Accessors/DataAccessor.Api/
RUN dotnet restore src/Accessors/DataAccessor.Api/DataAccessor.Api.csproj
COPY src/Accessors/DataAccessor.Api/ src/Accessors/DataAccessor.Api/
RUN dotnet publish src/Accessors/DataAccessor.Api/DataAccessor.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
EXPOSE 5010
ENTRYPOINT ["dotnet", "DataAccessor.Api.dll"]
