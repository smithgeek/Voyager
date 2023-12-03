$oldpackages = Get-ChildItem -Recurse src/**/*.nupkg
foreach ($package in $oldpackages) {
	Remove-Item $package
}

$ticks = (Get-Date).ticks
$version = gitversion /output json /showvariable NuGetVersion
$version = "${version}-${ticks}"

$files = Get-ChildItem -Recurse src/**/*.csproj
foreach ($file in $files) {
	dotnet pack -p:PackageVersion=$version $file.FullName
}

$newpackages = Get-ChildItem -Recurse src/**/*.nupkg
foreach ($package in $newpackages) {
	nuget push $package.FullName -source http://localhost:8081/repository/cassini-nuget/ -ApiKey 7673bbd0-219c-3c00-a615-360e7b7d5ffa
}