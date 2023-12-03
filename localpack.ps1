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
	nuget add $package.FullName -source ..\packages
}