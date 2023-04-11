#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Notification.NotificationServer/Notification.NotificationServer.csproj", "Notification.NotificationServer/"]
COPY ["Notification.NotificationServer.Backplane/Notification.NotificationServer.Backplane.csproj", "Notification.NotificationServer.Backplane/"]
COPY ["Notification.NotificationServer.Backplane.RabbitMq/Notification.NotificationServer.Backplane.RabbitMq.csproj", "Notification.NotificationServer.Backplane.RabbitMq/"]
COPY ["Notification.NotifierLibrary/Notification.NotifierLibrary.csproj", "Notification.NotifierLibrary/"]
RUN dotnet restore "Notification.NotificationServer/Notification.NotificationServer.csproj"
COPY . .
WORKDIR "/src/Notification.NotificationServer"
RUN dotnet build "Notification.NotificationServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Notification.NotificationServer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Notification.NotificationServer.dll"]