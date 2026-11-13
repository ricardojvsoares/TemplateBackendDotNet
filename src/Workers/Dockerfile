FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG WORKER            # e.g. FileProcessor
WORKDIR /src
COPY . .
RUN dotnet publish "src/Workers/${WORKER}/${WORKER}.csproj" \
    -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
ARG WORKER
ENV WORKER_DLL=${WORKER}.dll
COPY --from=build /app/publish .
ENTRYPOINT ["sh", "-c", "dotnet $WORKER_DLL"]