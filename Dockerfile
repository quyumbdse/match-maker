FROM microsoft/dotnet:2.1.0-aspnetcore-runtime

WORKDIR /app

COPY . .

CMD export ASPNETCORE_URLS=http://*:$PORT && dotnet DatingApp.API.dll