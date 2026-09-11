FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/CarRental.Api/CarRental.Api.csproj", "src/CarRental.Api/"]
COPY ["src/CarRental.Application/CarRental.Application.csproj", "src/CarRental.Application/"]
COPY ["src/CarRental.Domain/CarRental.Domain.csproj", "src/CarRental.Domain/"]
COPY ["src/CarRental.Infrastructure/CarRental.Infrastructure.csproj", "src/CarRental.Infrastructure/"]
RUN dotnet restore "src/CarRental.Api/CarRental.Api.csproj"

COPY . .
RUN dotnet publish "src/CarRental.Api/CarRental.Api.csproj" --configuration Release --output /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CarRental.Api.dll"]
