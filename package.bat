@echo off
echo packaging...
vpk pack -u CroomsBellSchedule --packTitle "Crooms Bell Schedule" --packAuthors MikhailSoftware -i Assets/croomsBellSchedule.ico -v 5.0.0 -p "CroomsBellSchedule\bin\Release\win-x64\finalpublish" -e "Crooms Bell Schedule.exe" --splashImage Assets/splash.png
pause