param (
	[Parameter(Mandatory=$true)]
	[ValidatePattern("^\d+\.\d+\.(?:\d+\.\d+$|\d+$)")]
	[string]
	$ReleaseVersionNumber,
	[Parameter(Mandatory=$true)]
	[string]
	[AllowEmptyString()]
	$PreReleaseName
)

$PSScriptFilePath = (Get-Item $MyInvocation.MyCommand.Path).FullName
$ScriptDir = Split-Path -Path $PSScriptFilePath -Parent
$SolutionRoot = Split-Path -Path $ScriptDir -Parent

 $ProjectJsonPath = Join-Path -Path $SolutionRoot -ChildPath "src\dotnet-test-nunit\project.json"
 (gc -Path $ProjectJsonPath) `
 	-replace "(?<=`"version`":\s`")[.\w-]*(?=`",)", "$ReleaseVersionNumber$PreReleaseName" |
 	sc -Path $ProjectJsonPath -Encoding UTF8