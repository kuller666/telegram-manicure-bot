FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore "./телеграм бот.csproj"
RUN dotnet publish "./телеграм бот.csproj" -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/out ./

ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "TelegramManicureBot.dll"]
