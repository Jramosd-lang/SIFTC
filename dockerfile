FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["SIFTC.API/SIFTC.Api.csproj", "SIFTC.API/"]
COPY ["SIFTC.Application/SIFTC.Application.csproj", "SIFTC.Application/"]
COPY ["SIFTC.Domain/SIFTC.Domain.csproj", "SIFTC.Domain/"]
COPY ["SIFTC.Infrastructure/SIFTC.Infrastructure.csproj", "SIFTC.Infrastructure/"]

RUN dotnet restore "SIFTC.API/SIFTC.Api.csproj"

COPY . .
WORKDIR "/src/SIFTC.API"
RUN dotnet publish "SIFTC.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SIFTC.Api.dll"]