rmdir /s /Q TestResults
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:".\TestResults\*\coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html