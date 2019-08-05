FROM mcr.microsoft.com/dotnet/core/sdk:3.0

EXPOSE 80

COPY . .

WORKDIR "/src/URU/"

RUN dotnet publish "URU.csproj" -c Release

ENTRYPOINT ["dotnet", "bin/Release/netcoreapp3.0/publish/URU.dll"]