.\CleanVisualStudioProject.ps1

$version = (Get-Content "Version.txt")

$filesToEnumerate = get-childitem *.csproj -recurse
foreach($file in $filesToEnumerate) {
	(Get-Content $file.FullName) `
		-replace '<Version>.*</Version>', "<Version>$version</Version>" `
	| Out-File $file.FullName
}

dotnet build -c Release
