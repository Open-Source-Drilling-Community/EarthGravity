FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Service/Service.csproj", "Service/"]
COPY ["Model/Model.csproj", "Model/"]
RUN dotnet restore "Service/Service.csproj"
COPY . .
WORKDIR /src/Service
RUN dotnet publish "Service.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENV EarthGravity__UsageStatisticsFile=/home/EarthGravity.UsageStatistics.json
USER root
RUN mkdir -p /home && chown app:app /home
VOLUME ["/home"]
USER app
ENTRYPOINT ["dotnet", "Service.dll"]
