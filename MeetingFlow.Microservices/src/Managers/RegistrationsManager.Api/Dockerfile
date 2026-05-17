FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Managers/RegistrationsManager.Api/RegistrationsManager.Api.csproj src/Managers/RegistrationsManager.Api/
RUN dotnet restore src/Managers/RegistrationsManager.Api/RegistrationsManager.Api.csproj
COPY src/Managers/RegistrationsManager.Api/ src/Managers/RegistrationsManager.Api/
RUN dotnet publish src/Managers/RegistrationsManager.Api/RegistrationsManager.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
EXPOSE 5031
ENTRYPOINT ["dotnet", "RegistrationsManager.Api.dll"]
