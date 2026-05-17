FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Engines/SchedulingEngine.Api/SchedulingEngine.Api.csproj src/Engines/SchedulingEngine.Api/
RUN dotnet restore src/Engines/SchedulingEngine.Api/SchedulingEngine.Api.csproj
COPY src/Engines/SchedulingEngine.Api/ src/Engines/SchedulingEngine.Api/
RUN dotnet publish src/Engines/SchedulingEngine.Api/SchedulingEngine.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
EXPOSE 5020
ENTRYPOINT ["dotnet", "SchedulingEngine.Api.dll"]
