# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000



# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["HomerLy.Presentation/HomerLy.Presentation.csproj", "HomerLy.Presentation/"]
COPY ["HomerLy.BusinessObject/HomerLy.BusinessObject.csproj", "HomerLy.BusinessObject/"]
COPY ["HomerLy.Business/HomerLy.Business.csproj", "HomerLy.Business/"]
COPY ["HomerLy.DataAccess/HomerLy.DataAccess.csproj", "HomerLy.DataAccess/"]
RUN dotnet restore "./HomerLy.Presentation/HomerLy.Presentation.csproj"
COPY . .
WORKDIR "/src/HomerLy.Presentation"
RUN dotnet build "./HomerLy.Presentation.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./HomerLy.Presentation.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HomerLy.Presentation.dll"]ENTRYPOINT ["dotnet", "HomerLy.Presentation.dll"]