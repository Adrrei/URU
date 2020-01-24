FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

COPY . .

WORKDIR "/src/URU/"

RUN dotnet publish "URU.csproj" -c Release -o out

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine3.10

COPY --from=build "/src/URU/out" .

ENTRYPOINT ["dotnet", "URU.dll"]