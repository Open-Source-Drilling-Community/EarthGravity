FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["WebApp/WebApp.csproj", "WebApp/"]
COPY ["WebPages/WebPages.csproj", "WebPages/"]
COPY ["ModelSharedOut/EarthGravityMergedModel.cs", "ModelSharedOut/"]
COPY ["ModelSharedOut/PseudoConstructors.cs", "ModelSharedOut/"]
RUN dotnet restore "WebApp/WebApp.csproj"
COPY . .
WORKDIR /src/WebApp
RUN dotnet publish "WebApp.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
USER app
ENTRYPOINT ["dotnet", "WebApp.dll"]
