@echo off
cd /d "%~dp0"
dotnet run --project SportsRental.Console -- --self-test
pause
