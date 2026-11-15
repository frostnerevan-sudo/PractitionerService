FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["PractitionerService/PractitionerService.Api.csproj", "PractitionerService/"]
COPY ["PractitionerService.Application/PractitionerService.Application.csproj", "PractitionerService.Application/"]
COPY ["PractitionerService.Domain/PractitionerService.Domain.csproj", "PractitionerService.Domain/"]
COPY ["PractitionerService.Infrastructure/PractitionerService.Infrastructure.csproj", "PractitionerService.Infrastructure/"]

RUN dotnet restore "PractitionerService/PractitionerService.Api.csproj"

COPY . .

WORKDIR "/src/PractitionerService"
RUN dotnet build "PractitionerService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "PractitionerService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PractitionerService.Api.dll"]