FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Managers/RegistrationsManager/RegistrationsManager.csproj src/Managers/RegistrationsManager/
RUN dotnet restore src/Managers/RegistrationsManager/RegistrationsManager.csproj
COPY src/Managers/RegistrationsManager/ src/Managers/RegistrationsManager/
RUN dotnet publish src/Managers/RegistrationsManager/RegistrationsManager.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
EXPOSE 5031
ENTRYPOINT ["dotnet", "RegistrationsManager.dll"]
