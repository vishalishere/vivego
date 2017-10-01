param([string]$sourceDirectory = "")

$filesToDelete = @(
	".sdf";
	".sln.docstates";
	".suo";
	".upgradelogxml";
	"TestResult.xml";
	"project.lock.json";
	#"*.vcxproj.filters";
	".sln.old";
	#"*.suo.old";
	#"UpgradeLog.XML";
	#"*.wixproj.vspscc";
	#"*.csproj.vspscc";
	#"*.SCC";
	#"*.ncb";
	#"*.opt";
	#"*.plg";
	#"*.aps";
	#"*.clw"
)

$foldersToDelete = @(
	"_UpgradeReport_Files";
	"_ReSharper.Caches";
	"ipch";
	"bin\Debug";
	"bin\Release";
	"obj";
	"TestResults";
)

$filesToEnumerate = get-childitem $sourceDirectory -recurse
foreach($file in $filesToEnumerate) {
	if($file.PSIsContainer) {
		foreach ($folderToMatch in $foldersToDelete) {
			if ($file.FullName.EndsWith($folderToMatch)) {
				Remove-Item $file.FullName -Force -ErrorAction SilentlyContinue -Confirm:$false -Recurse
				$fileName = $file.FullName
				"Removed: $fileName"
			}
		}
	} else {
		foreach ($fileToDelete in $filesToDelete) {
			if ($file.FullName.EndsWith($fileToDelete)) {
				Remove-Item $file.FullName -Force -ErrorAction SilentlyContinue -Confirm:$false
				$fileName = $file.FullName
				"Removed: $fileName"
			}
		}
	}
}
