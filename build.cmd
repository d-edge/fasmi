@echo off
dotnet tool restore
dotnet test
IF %ERRORLEVEL% NEQ 0 (
  exit %ERRORLEVEL%
)
dotnet pack -c release .\src\fasmi\ -o bin/nuget