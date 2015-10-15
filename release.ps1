$7z = "$env:ProgramFiles\7-Zip\7z.exe"
if (-not (test-path $7z)) {throw $7z + " needed"} 

$CI = $env:APPVEYOR -eq "True"
if($CI)
{
	$BuildFolder = $env:APPVEYOR_BUILD_FOLDER + "\GzsTool\bin\Release\"
	
	if ($env:APPVEYOR_REPO_TAG -eq "True")
	{
		$Version = ".$env:APPVEYOR_REPO_TAG_NAME"
	}
	else
	{
		$Version = ".$env:APPVEYOR_BUILD_VERSION"
	}
}
else
{
	$BuildFolder = ".\GzsTool\bin\Release\"
	
    $AssemblyInfoFile = ".\GzsTool\Properties\AssemblyInfo.cs"
    $Version = select-string -Path $AssemblyInfoFile -Pattern "AssemblyVersion\(`"(?<AssemblyVersion>.*)`"\)"| Select -Expand Matches | Foreach { $_.Groups["AssemblyVersion"] }
	if($Version)
	{
	    $Version = "." + $Version
	}	
}

$ReleaseFileName = "GzsTool$Version.zip"
& $7z a $ReleaseFileName $BuildFolder*.dll $BuildFolder*.txt $BuildFolder*.exe $BuildFolder*.config "-x!*.vshost.*"

if($CI)
{
    Push-AppveyorArtifact $ReleaseFileName
}
else
{
    Move-Item $ReleaseFileName ".\Releases\$ReleaseFileName" -force
}
