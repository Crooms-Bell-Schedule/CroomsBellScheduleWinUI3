@echo off
echo packaging...
vpk pack -u CroomsBellSchedule --packTitle "Crooms Bell Schedule" --packAuthors MikhailSoftware -i CroomsBellSchedule/Assets/croomsBellSchedule.ico -v 5.0.4 -p "CroomsBellSchedule\bin\Release\win-x64\finalpublish" -e "Crooms Bell Schedule.exe" --splashImage CroomsBellSchedule/Assets/splash.png
pause