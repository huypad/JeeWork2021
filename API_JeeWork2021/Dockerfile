#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["./API_JeeWork2021.csproj", "API_JeeWork2021/"]
COPY packages packages/
RUN dotnet restore "API_JeeWork2021/API_JeeWork2021.csproj"
COPY . API_JeeWork2021/
WORKDIR "/src/API_JeeWork2021"
RUN dotnet build "API_JeeWork2021.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "API_JeeWork2021.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "API_JeeWork2021.dll"]