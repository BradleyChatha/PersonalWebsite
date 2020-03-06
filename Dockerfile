FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build-env

COPY . /app/
WORKDIR /app/
RUN dotnet restore
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:2.2
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "PersonalWebsite.dll"]
