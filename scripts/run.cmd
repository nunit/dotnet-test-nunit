rem set APPVEYOR_PULL_REQUEST_NUMBER=112
rem set APPVEYOR_REPO_BRANCH=master
rem set APPVEYOR_BUILD_VERSION=3.3.0.269

pushd ..
powershell -executionpolicy bypass -File scripts\set-version.ps1
popd