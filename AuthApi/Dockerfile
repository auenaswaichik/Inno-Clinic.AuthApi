# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore 
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8081
EXPOSE 8081
# Use environment variables for connection strings
ENV ConnectionStrings__sqlConnection=""
ENV Serilog__WriteTo__0__Args__databaseUrl=""
ENTRYPOINT ["dotnet", "AuthApi.dll"] 