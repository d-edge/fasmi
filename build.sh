dotnet tool restore
dotnet test || exit 1
dotnet pack -c release ./src/fasmi/ -o bin/nuget