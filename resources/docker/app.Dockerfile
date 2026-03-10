FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first for layer caching
COPY PostgresTemplate.slnx .
COPY src/Domain/PostgresTemplate.Domain.csproj src/Domain/
COPY src/Infrastructure/PostgresTemplate.Infrastructure.csproj src/Infrastructure/
COPY src/Api/PostgresTemplate.Api.csproj src/Api/
COPY src/Worker/PostgresTemplate.Worker.csproj src/Worker/

RUN dotnet restore

# Copy everything and build
COPY . .
RUN dotnet publish src/Api/PostgresTemplate.Api.csproj -c Release -o /app/api
RUN dotnet publish src/Worker/PostgresTemplate.Worker.csproj -c Release -o /app/worker

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /app/api ./api
COPY --from=build /app/worker ./worker

EXPOSE 8080

# Default: run the API. Override CMD to run the worker:
# CMD ["dotnet", "/app/worker/PostgresTemplate.Worker.dll"]
CMD ["dotnet", "/app/api/PostgresTemplate.Api.dll"]
