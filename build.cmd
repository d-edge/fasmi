@echo off
dotnet tool restore
dotnet pack -c release .\src\fasm\ -o bin/nuget