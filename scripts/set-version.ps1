$ReleaseVersionNumber = $env:APPVEYOR_BUILD_VERSION
$PreReleaseName = ''

If($env:APPVEYOR_PULL_REQUEST_NUMBER -ne $null) {
  $PreReleaseName = '-PR-' + $env:APPVEYOR_PULL_REQUEST_NUMBER
} ElseIf($env:APPVEYOR_REPO_BRANCH -ne 'master') {
  $PreReleaseName = '-' + $env:APPVEYOR_REPO_BRANCH
} Else {
  $PreReleaseName = '-CI'
}

$PSScriptFilePath = (Get-Item $MyInvocation.MyCommand.Path).FullName
$ScriptDir = Split-Path -Path $PSScriptFilePath -Parent
$SolutionRoot = Split-Path -Path $ScriptDir -Parent

 $ProjectJsonPath = Join-Path -Path $SolutionRoot -ChildPath "src\dotnet-test-nunit\project.json"
 (gc -Path $ProjectJsonPath) `
 	-replace "(?<=`"version`":\s`")[.\w-]*(?=`",)", "$ReleaseVersionNumber$PreReleaseName" |
 	sc -Path $ProjectJsonPath -Encoding UTF8