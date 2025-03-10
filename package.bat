@echo off
dotnet tool restore
vpk pack -u CroomsBellSchedule --packTitle "Crooms Bell Schedule" --packAuthors thealmightyderpybird -i Assets/croomsBellSchedule.ico -v 3.0.2 -p "bin\Release\win-x64\finalpublish" -e "Crooms Bell Schedule.exe"
pause