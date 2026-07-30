FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Contracts/ src/Contracts/
COPY src/Engines/AiChatEngine/AiChatEngine.csproj src/Engines/AiChatEngine/
RUN dotnet restore src/Engines/AiChatEngine/AiChatEngine.csproj
COPY src/Engines/AiChatEngine/ src/Engines/AiChatEngine/
RUN dotnet publish src/Engines/AiChatEngine/AiChatEngine.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
EXPOSE 5040
ENTRYPOINT ["dotnet", "AiChatEngine.dll"]
