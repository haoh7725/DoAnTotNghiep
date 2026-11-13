FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY backend/ ./backend/
COPY infra/database/15_conversion_sharing.sql ./infra/database/15_conversion_sharing.sql
COPY infra/database/17_product_business_rules.sql ./infra/database/17_product_business_rules.sql
COPY infra/database/18_project_requirements.sql ./infra/database/18_project_requirements.sql
COPY infra/database/19_target_consistency.sql ./infra/database/19_target_consistency.sql
COPY infra/database/20_reference_research_taxonomy.sql ./infra/database/20_reference_research_taxonomy.sql
COPY infra/database/21_target_product_types.sql ./infra/database/21_target_product_types.sql
RUN dotnet restore backend/ResearchManagement.slnx
RUN dotnet publish backend/src/ResearchManagement.Api/ResearchManagement.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ResearchManagement.Api.dll"]
