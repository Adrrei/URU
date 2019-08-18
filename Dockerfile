FROM mcr.microsoft.com/dotnet/core/sdk:3.0 AS build

EXPOSE 80

COPY . .

WORKDIR "/src/URU/"

RUN dotnet publish "URU.csproj" -c Release

FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-alpine3.9

COPY --from=build "/src/URU" .

ENTRYPOINT ["dotnet", "bin/Release/netcoreapp3.0/publish/URU.dll"]