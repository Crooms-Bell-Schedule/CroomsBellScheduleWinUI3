@echo off
echo packaging...
set APP_VERSION=6.0.1

dotnet publish CBSApp.Desktop/CBSApp.Desktop.csproj /p:PublishProfile=CBSApp.Desktop/Properties/PublishProfiles/Profile-WinX64.pubxml
dotnet publish CBSApp.Desktop/CBSApp.Desktop.csproj /p:PublishProfile=CBSApp.Desktop/Properties/PublishProfiles/Profile-Linux-X64.pubxml

vpk pack -u CroomsBellSchedule --packTitle "Crooms Bell Schedule" --packAuthors MikhailSoftware -i CBSApp/Assets/croomsBellSchedule.ico -v %APP_VERSION% -p "CBSApp.Desktop\bin\Release\Publish" -e "CBSApp.Desktop.exe" --splashImage CBSApp/Assets/splash.png --outputDir Releases
REM vpk pack -u CroomsBellSchedule -r linux-x64 --packTitle "Crooms Bell Schedule" --packAuthors MikhailSoftware -i CBSApp/Assets/croomsBellSchedule.ico -v %APP_VERSION% -p "CBSApp.Desktop\bin\Release\PublishLinux" -e "CBSApp.Desktop" --splashImage CBSApp/Assets/splash.png --outputDir Releases
pause
