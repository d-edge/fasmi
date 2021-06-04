dotnet tool restore
dotnet test || exit 1
dotnet pack -c release ./src/fasm/ -o bin/nuget