FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["apisem.csproj", "."]
RUN dotnet restore "apisem.csproj"

COPY . .
RUN dotnet build "apisem.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "apisem.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "apisem.dll"]