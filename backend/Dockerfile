FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src
COPY . .
RUN dotnet publish backend/Services/UserIdentity/Services.UserIdentity.csproj -c Release -o /publish/identity --no-self-contained \
 && dotnet publish backend/Services/UserProfile/Services.UserProfile.csproj -c Release -o /publish/profile --no-self-contained \
 && dotnet publish backend/Services/Notifications/Services.Notifications.csproj -c Release -o /publish/notifications --no-self-contained \
 && dotnet publish backend/Deployment/BackendLauncher/HelpDesk.BackendLauncher.csproj -c Release -o /publish/launcher --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled
WORKDIR /app
COPY --from=build --chown=1654:1654 /publish/identity ./identity
COPY --from=build --chown=1654:1654 /publish/profile ./profile
COPY --from=build --chown=1654:1654 /publish/notifications ./notifications
COPY --from=build --chown=1654:1654 /publish/launcher ./launcher
COPY --chown=1654:1654 backend/Deployment/profile-pictures/ /data/profile-pictures/
EXPOSE 8080 8081
ENTRYPOINT ["dotnet", "/app/launcher/HelpDesk.BackendLauncher.dll", "/app/identity/Services.UserIdentity.dll", "/app/profile/Services.UserProfile.dll", "/app/notifications/Services.Notifications.dll"]
