FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine3.14 AS build

EXPOSE 80

COPY . .

WORKDIR "/src/URU/"

RUN dotnet publish "URU.csproj" -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine3.14

COPY --from=build "/src/URU/out" .

ARG versionNumber
ENV versionNumber=$versionNumber

ENTRYPOINT ["dotnet", "URU.dll"]