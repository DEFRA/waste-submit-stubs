# Build stage image
FROM mcr.microsoft.com/dotnet/sdk@sha256:de4fe90a98845d75910b9769662465c84de1a934665c431279d1382b2bddc02b AS build
WORKDIR /src
COPY . .
RUN dotnet build tests/WasteSubmitStubs.Test --warnaserror && \
    dotnet test --test-modules tests/WasteSubmitStubs.Test/bin/Debug/net10.0/WasteSubmitStubs.Test.dll --no-build
RUN dotnet publish src/WasteSubmitStubs -c Release -o /app/publish /p:UseAppHost=false

# Final production image
FROM mcr.microsoft.com/dotnet/aspnet@sha256:de0ee358cdd672f7017a003bed21e9ac9ed48a27c922a59b18175b6e82fa7fee
WORKDIR /app

# curl is needed for the CDP platform health check
RUN apt update && \
    apt install curl -y && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
EXPOSE 8085
ENTRYPOINT ["dotnet", "WasteSubmitStubs.dll"]
