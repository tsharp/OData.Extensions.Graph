#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["GraphQLSample.Api/GraphQLSample.Api.csproj", "GraphQLSample.Api/"]
COPY ["ODataGraphQL/Microsoft.AspNetCore.OData.Extensions.GraphQL.csproj", "ODataGraphQL/"]
RUN dotnet restore "GraphQLSample.Api/GraphQLSample.Api.csproj"
COPY . .
WORKDIR "/src/GraphQLSample.Api"
RUN dotnet build "GraphQLSample.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GraphQLSample.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GraphQLSample.Api.dll"]