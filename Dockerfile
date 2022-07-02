FROM mcr.microsoft.com/dotnet/framework/runtime:4.8 AS runtime
WORKDIR /src/bin/x64/Release
ENTRYPOINT ["HomeMailHub.exe"]
