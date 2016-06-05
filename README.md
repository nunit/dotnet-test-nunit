# NUnit 3 Test Runner for .NET Core

[![Build status](https://ci.appveyor.com/api/projects/status/yg7dawcy1106g1li/branch/master?svg=true)](https://ci.appveyor.com/project/CharliePoole/dotnet-test-nunit/branch/master)

`dotnet-test-nunit` is the unit test runner for .NET Core for running unit tests with NUnit 3.

## Usage

`dotnet-test-nunit` is still under development, so you will need to add a `NuGet.Config` file to your solution to download NuGet packages from the NUnit CI NuGet feeds.

### NuGet.Config

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear/>
    <add key="NUnit CI Builds (AppVeyor)" value="https://ci.appveyor.com/nuget/nunit" />
    <add key="dotnet-test-nunit CI Builds (AppVeyor)" value="https://ci.appveyor.com/nuget/dotnet-test-nunit" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
```

Your `project.json` in your test project should look like the following;

### project.json

```json
{
    "version": "1.0.0-*",

    "dependencies": {
        "NUnitWithDotNetCoreRC2": "1.0.0-*",
        "NETStandard.Library": "1.5.0-rc2-24027",
        "NUnit": "3.2.1",
        "dotnet-test-nunit": "3.3.0.49-CI"
    },
    "testRunner": "nunit",

    "frameworks": {
        "netstandard1.5": {
            "imports": [
                "dnxcore50",
                "netcoreapp1.0",
                "portable-net45+win8"
            ]
        }
    },

    "runtimes": {
        "win10-x86": { },
        "win10-x64": { }
    }
}
```

The lines of interest here are the dependency on `dotnet-test-nunit`. Feel free to use the newest pre-release version that ends in `-CI`, that is latest from the master branch. Note that the `NUnitWithDotNetCoreRC2` dependency is the project under test.

I have added `"testRunner": "nunit"` to specify NUnit 3 as the test adapter. I also had to add to the imports for both the test adapter and NUnit to resolve. Lastly, I had to add the `runtimes`.

You can now run your tests using the Visual Studio Test Explorer, or by running `dotnet test` from the command line.

```
# Restore the NuGet packages
dotnet restore

# Run the unit tests in the current directory
dotnet test

# Run the unit tests in a different directory
dotnet test .\test\NUnitWithDotNetCoreRC2.Test\
```

### Notes

Double clicking on a test in Visual Studio does not currently work. That is being tracked in #11.

Also note that the `dotnet` command line swallows blank lines and does not work with color.
The NUnit test runner's output is in color, but you won't see it. These are known issues with the `dotnet` cli.