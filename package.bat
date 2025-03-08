@echo off
dotnet tool restore
vpk pack -u CroomsBellSchedule --packTitle "Crooms Bell Schedule" --packAuthors thealmightyderpybird -i Assets/croomsBellSchedule.ico -v 1.0.0 -p "bin\Release\win-x64\finalpublish" -e "Crooms Bell Schedule.exe"
pause