FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY DataShare.sln .
COPY DataShare.Domain/*.csproj DataShare.Domain/
COPY DataShare.Application/*.csproj DataShare.Application/
COPY DataShare.Infrastructure/*.csproj DataShare.Infrastructure/
COPY DataShare.Api/*.csproj DataShare.Api/
COPY DataShare.Tests/*.csproj DataShare.Tests/
RUN dotnet restore

COPY . .
RUN dotnet publish DataShare.Api -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

RUN mkdir -p /var/datashare/files

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "DataShare.Api.dll"]
