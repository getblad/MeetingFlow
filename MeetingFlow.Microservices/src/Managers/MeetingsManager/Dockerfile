FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Managers/MeetingsManager/MeetingsManager.csproj src/Managers/MeetingsManager/
RUN dotnet restore src/Managers/MeetingsManager/MeetingsManager.csproj
COPY src/Managers/MeetingsManager/ src/Managers/MeetingsManager/
RUN dotnet publish src/Managers/MeetingsManager/MeetingsManager.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
EXPOSE 5030
ENTRYPOINT ["dotnet", "MeetingsManager.dll"]
