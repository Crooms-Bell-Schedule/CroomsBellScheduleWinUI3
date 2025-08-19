@echo off
dotnet tool restore
vpk pack -u CroomsBellSchedule --packTitle "Crooms Bell Schedule" --packAuthors MikhailSoftware -i Assets/croomsBellSchedule.ico -v 4.3.1 -p "bin\Release\win-x64\finalpublish" -e "Crooms Bell Schedule.exe" --splashImage Assets/splash.png
pause