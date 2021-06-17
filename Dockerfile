ARG runtime=runtime
ARG project

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
ARG project

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

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS runtime
ARG project
WORKDIR /app
COPY --from=build /app/albiondata-sql-dotNet/out ./

# This is a hack to get the project into the entrypoint. Build args
# don't make it into the entrypoint, so you need an env var and a shell
# to evaluate it.
COPY entrypoint.sh /entrypoint.sh
ENV PROJECT=albiondata-sql-dotNet
ENTRYPOINT ["/entrypoint.sh"]
