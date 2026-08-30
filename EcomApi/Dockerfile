FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["EcomApi/EcomApi.csproj", "EcomApi/"]
RUN dotnet restore "EcomApi/EcomApi.csproj"

COPY . .
WORKDIR "/src/EcomApi"
RUN dotnet publish "EcomApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EcomApi.dll"]