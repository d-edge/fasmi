dotnet tool restore
dotnet test
dotnet pack -c release ./src/fasm/ -o bin/nuget