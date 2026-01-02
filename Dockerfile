FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PurchaseService/PurchaseService.csproj", "PurchaseService/"]
RUN dotnet restore "PurchaseService/PurchaseService.csproj"
COPY . .
WORKDIR "/src/PurchaseService"
RUN dotnet build "PurchaseService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PurchaseService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PurchaseService.dll"]