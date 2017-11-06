FROM microsoft/dotnet:2.0-sdk AS build-env
WORKDIR /app

# from https://github.com/dotnet/dotnet-docker-samples/blob/master/dotnetapp-selfcontained/Dockerfile

# copy csproj and restore as distinct layers
# COPY nuget.config ./
COPY RealtimeTester/*.csproj ./
RUN dotnet restore

# copy everything else and build
COPY RealtimeTester/ ./
RUN dotnet publish -c Release -r linux-x64 -o out

# build runtime image
FROM microsoft/dotnet:2.0-runtime-deps
WORKDIR /app
COPY --from=build-env /app/out ./
ENTRYPOINT ["./dotnetapp"]