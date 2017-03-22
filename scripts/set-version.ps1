If($env:APPVEYOR_BUILD_VERSION -ne $null -and $ReleaseVersionNumber -ne $null) {
  $ReleaseVersionNumber = $ReleaseVersionNumber + '.' + $env:APPVEYOR_BUILD_VERSION
} Else {
  $ReleaseVersionNumber='3.3.0'
}

If($env:APPVEYOR_PULL_REQUEST_NUMBER -ne $null) {
  $PreReleaseName = '-PR-' + $env:APPVEYOR_PULL_REQUEST_NUMBER
} ElseIf($env:APPVEYOR_REPO_BRANCH -ne 'master' -and $env:APPVEYOR_PULL_REQUEST_NUMBER -ne $null -and -not $env:APPVEYOR_REPO_BRANCH.StartsWith('release')) {
  $PreReleaseName = '-' + $env:APPVEYOR_REPO_BRANCH
} Else {
  $PreReleaseName = '-CI'
}

$PSScriptFilePath = (Get-Item $MyInvocation.MyCommand.Path).FullName
$ScriptDir = Split-Path -Path $PSScriptFilePath -Parent
$SolutionRoot = Split-Path -Path $ScriptDir -Parent

write-host "APPVEYOR_PULL_REQUEST_NUMBER=$env:APPVEYOR_PULL_REQUEST_NUMBER"
write-host "APPVEYOR_REPO_BRANCH=$env:APPVEYOR_REPO_BRANCH"
write-host "ReleaseVersionNumber=$ReleaseVersionNumber"
write-host "PreReleaseName=$PreReleaseName"

$ProjectJsonPath = Join-Path -Path $SolutionRoot -ChildPath "src\dotnet-test-nunit-adapter\package.nuspec"
 $re = [regex]"(.*<version>)(.*)(<.+)"
# write-host ($re.Replace([string]::Join("`n", (Get-Content -Path $ProjectJsonPath)), "`$1 $ReleaseVersionNumber$PreReleaseName `$3",1))
	$re.Replace([string]::Join("`n", (Get-Content -Path $ProjectJsonPath)), "`$1 $ReleaseVersionNumber$PreReleaseName `$3",1) |
 	Set-Content -Path $ProjectJsonPath -Encoding UTF8

$ProjectJsonPath = Join-Path -Path $SolutionRoot -ChildPath "src\dotnet-test-nunit\dotnet-test-nunit.csproj"
 $re = [regex]"(.*<VersionPrefix>)(.*)(<.+)"
# write-host ($re.Replace([string]::Join("`n", (Get-Content -Path $ProjectJsonPath)), "`$1 $ReleaseVersionNumber$PreReleaseName `$3",1))
	$re.Replace([string]::Join("`n", (Get-Content -Path $ProjectJsonPath)), "`$1 $ReleaseVersionNumber$PreReleaseName `$3",1) |
	Set-Content -Path $ProjectJsonPath -Encoding UTF8
