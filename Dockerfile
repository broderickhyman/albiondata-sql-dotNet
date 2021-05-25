FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /app

# copy csproj and restore as distinct layers
COPY albiondata-sql-dotNet/*.csproj ./albiondata-sql-dotNet/
WORKDIR /app/albiondata-sql-dotNet
RUN dotnet restore

# copy and publish app and libraries
WORKDIR /app/
COPY albiondata-sql-dotNet/. ./albiondata-sql-dotNet/
WORKDIR /app/albiondata-sql-dotNet
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/core/runtime:3.1 AS runtime
WORKDIR /app
COPY --from=build /app/albiondata-sql-dotNet/out ./
ENTRYPOINT ["dotnet", "albiondata-sql-dotNet.dll"]
