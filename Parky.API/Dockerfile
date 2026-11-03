FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Parky.API/Parky.API.csproj", "Parky.API/"]
COPY ["Parky.Application/Parky.Application.csproj", "Parky.Application/"]
COPY ["Parky.Domain/Parky.Domain.csproj", "Parky.Domain/"]
COPY ["Parky.Infrastructure/Parky.Infrastructure.csproj", "Parky.Infrastructure/"]

RUN dotnet restore "Parky.API/Parky.API.csproj"

COPY . .

WORKDIR /src/Parky.API
RUN dotnet publish "Parky.API.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Parky.API.dll"]
