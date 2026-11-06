FROM mcr.microsoft.com/dotnet/sdk:10.0.403 AS build
WORKDIR /src
COPY ILearn/ILearn.csproj ILearn/
COPY ILearn.Domain/ ILearn.Domain/
COPY ILearn.Application/ ILearn.Application/
COPY ILearn.Infrastructure/ ILearn.Infrastructure/
COPY ILearn.Infrastructure.Server/ ILearn.Infrastructure.Server/
COPY ILearn.Presentation/ ILearn.Presentation/
RUN dotnet restore ILearn/ILearn.csproj
COPY ILearn/ ILearn/
RUN dotnet publish ILearn/ILearn.csproj -c Release -o /out --no-restore /p:UseAppHost=false
RUN test -f /out/wwwroot/_framework/blazor.web.js && test -f /out/Content/catalog.txt && test -s /out/Content/csharp-v2.json && test -s /out/Content/code-room.json && test -s /out/Content/interviews.json && test -f /out/Content/VerificationEmail.html

FROM mcr.microsoft.com/dotnet/aspnet:10.0.14-alpine3.24-extra AS final
RUN apk upgrade --no-cache
WORKDIR /app
COPY --from=build /out/ ./
RUN mkdir -p App_Data/keys && chown -R "$APP_UID:$APP_UID" App_Data
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ILearn.dll"]
