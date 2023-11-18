FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build

EXPOSE 80

COPY . .

WORKDIR "/src/URU/"

RUN dotnet publish "URU.csproj" -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine

RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

COPY --from=build "/src/URU/out" .

ARG versionNumber
ENV versionNumber=$versionNumber

ENTRYPOINT ["dotnet", "URU.dll"]