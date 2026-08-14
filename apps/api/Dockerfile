FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY AssignmentSubmissionSystem.slnx ./
COPY apps/api/AssignmentSubmissionSystem.Api/AssignmentSubmissionSystem.Api.csproj apps/api/AssignmentSubmissionSystem.Api/
COPY src/AssignmentSubmissionSystem.Application/AssignmentSubmissionSystem.Application.csproj src/AssignmentSubmissionSystem.Application/
COPY src/AssignmentSubmissionSystem.Domain/AssignmentSubmissionSystem.Domain.csproj src/AssignmentSubmissionSystem.Domain/
COPY src/AssignmentSubmissionSystem.Infrastructure/AssignmentSubmissionSystem.Infrastructure.csproj src/AssignmentSubmissionSystem.Infrastructure/
RUN dotnet restore apps/api/AssignmentSubmissionSystem.Api/AssignmentSubmissionSystem.Api.csproj

COPY . ./
RUN dotnet publish apps/api/AssignmentSubmissionSystem.Api/AssignmentSubmissionSystem.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "AssignmentSubmissionSystem.Api.dll"]
