@echo off
cd /d "%~dp0"
dotnet run --project SportsRental.Console -- --data .\data
pause
