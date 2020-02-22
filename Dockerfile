FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

EXPOSE 80

COPY . .

WORKDIR "/src/URU/"

RUN dotnet publish "URU.csproj" -c Release -o out

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine3.11

COPY --from=build "/src/URU/out" .

ARG versionNumber
ENV versionNumber=$versionNumber

ENTRYPOINT ["dotnet", "URU.dll"]