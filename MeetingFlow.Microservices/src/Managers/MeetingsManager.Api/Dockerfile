FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Managers/MeetingsManager.Api/MeetingsManager.Api.csproj src/Managers/MeetingsManager.Api/
RUN dotnet restore src/Managers/MeetingsManager.Api/MeetingsManager.Api.csproj
COPY src/Managers/MeetingsManager.Api/ src/Managers/MeetingsManager.Api/
RUN dotnet publish src/Managers/MeetingsManager.Api/MeetingsManager.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
EXPOSE 5030
ENTRYPOINT ["dotnet", "MeetingsManager.Api.dll"]
