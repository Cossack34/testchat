# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "ChatApp.sln"
RUN dotnet publish "src/ChatApp.Web/ChatApp.Web.csproj" -c Release -o /app/out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/out .
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "ChatApp.Web.dll"]
