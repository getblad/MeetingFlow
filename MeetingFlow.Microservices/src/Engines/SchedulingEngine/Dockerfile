FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Engines/SchedulingEngine/SchedulingEngine.csproj src/Engines/SchedulingEngine/
RUN dotnet restore src/Engines/SchedulingEngine/SchedulingEngine.csproj
COPY src/Engines/SchedulingEngine/ src/Engines/SchedulingEngine/
RUN dotnet publish src/Engines/SchedulingEngine/SchedulingEngine.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
EXPOSE 5020
ENTRYPOINT ["dotnet", "SchedulingEngine.dll"]
