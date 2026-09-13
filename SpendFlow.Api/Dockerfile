FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY SpendFlow.Api/SpendFlow.Api.csproj SpendFlow.Api/
RUN dotnet restore SpendFlow.Api/SpendFlow.Api.csproj

COPY SpendFlow.Api/ SpendFlow.Api/
RUN dotnet publish SpendFlow.Api/SpendFlow.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
USER app
ENTRYPOINT ["dotnet", "SpendFlow.Api.dll"]
