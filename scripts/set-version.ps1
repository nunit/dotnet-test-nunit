$ReleaseVersionNumber = $env:APPVEYOR_BUILD_VERSION
$PreReleaseName = ''

If($env:APPVEYOR_PULL_REQUEST_NUMBER -ne $null) {
  $PreReleaseName = '-PR-' + $env:APPVEYOR_PULL_REQUEST_NUMBER
} ElseIf($env:APPVEYOR_REPO_BRANCH -ne 'master' -and -not $env:APPVEYOR_REPO_BRANCH.StartsWith('release')) {
  $PreReleaseName = '-' + $env:APPVEYOR_REPO_BRANCH
} Else {
  $PreReleaseName = '-CI'
}

$PSScriptFilePath = (Get-Item $MyInvocation.MyCommand.Path).FullName
$ScriptDir = Split-Path -Path $PSScriptFilePath -Parent
$SolutionRoot = Split-Path -Path $ScriptDir -Parent

 $ProjectJsonPath = Join-Path -Path $SolutionRoot -ChildPath "src\dotnet-test-nunit\project.json"
 $re = [regex]"(?<=`"version`":\s`")[.\w-]*(?=`",)"
 $re.Replace([string]::Join("`n", (Get-Content -Path $ProjectJsonPath)), "$ReleaseVersionNumber$PreReleaseName", 1) |
 	Set-Content -Path $ProjectJsonPath -Encoding UTF8