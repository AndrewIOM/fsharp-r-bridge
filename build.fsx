// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#r "nuget: FAKE.Core.Target"
#r "nuget: FAKE.Core.ReleaseNotes"
#r "nuget: FAKE.DotNet.Cli"
#r "nuget: FAKE.DotNet.Fsi"
#r "nuget: FAKE.DotNet.AssemblyInfoFile"
#r "nuget: FAKE.Tools.Git"
#r "nuget: FAKE.DotNet.Testing.XUnit2"
#r "nuget: System.Reactive"
#r "nuget: MSBuild.StructuredLogger, 2.3.71"

let execContext = Fake.Core.Context.FakeExecutionContext.Create false "build.fsx" []
Fake.Core.Context.setExecutionContext (Fake.Core.Context.RuntimeContext.Fake execContext)

open Fake.Core
open Fake.Core.TargetOperators
open Fake.IO.FileSystemOperators
open Fake.DotNet

// --------------------------------------------------------------------------------------
// Information about the project to be used at NuGet and in AssemblyInfo files
// --------------------------------------------------------------------------------------

let projectName = "RBridge"

let projectSummary =
    "A slim F# bridge for R."

let projectDescription =
    """A bridging layer to interop between the R statistical language and F#."""

let authors = "Andrew Martin"
let companyName = "University of Cambridge"
let tags = "F# fsharp R interop"
let license = "MIT"
let copyright = "(C) 2026 Andrew Martin (University of Cambridge)"
let packageProjectUrl = "https://github.com/AndrewIOM/fsharp-r-bridge/"
let repositoryType = "git"
let repositoryUrl = "https://github.com/AndrewIOM/fsharp-r-bridge/"
let repositoryContentUrl = "https://raw.githubusercontent.com/AndrewIOM/fsharp-r-bridge"


// --------------------------------------------------------------------------------------
// The rest of the code is standard F# build script
// --------------------------------------------------------------------------------------

// Read release notes & version info from RELEASE_NOTES.md
System.Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
let binDir = __SOURCE_DIRECTORY__ @@ "bin"

let release =
    System.IO.File.ReadLines "RELEASE_NOTES.MD" |> Fake.Core.ReleaseNotes.parse

// Generate assembly info files with the right version & up-to-date information
Target.create "AssemblyInfo" (fun _ ->
    let fileName = "src/r-bridge/Common/AssemblyInfo.fs"

    AssemblyInfoFile.createFSharpWithConfig
        fileName
        [ Fake.DotNet.AssemblyInfo.Title projectName
          Fake.DotNet.AssemblyInfo.Company companyName
          Fake.DotNet.AssemblyInfo.Product projectName
          Fake.DotNet.AssemblyInfo.Description projectSummary
          Fake.DotNet.AssemblyInfo.Version release.AssemblyVersion
          Fake.DotNet.AssemblyInfo.FileVersion release.AssemblyVersion ]
        (AssemblyInfoFileConfig(false)))

// --------------------------------------------------------------------------------------
// Clean build results & restore NuGet packages

Target.create "Clean" (fun _ ->
    Fake.IO.Shell.cleanDirs [ "bin" ]
    Fake.IO.Shell.cleanDirs [ "tests/RBridge.Tests/bin"; "tests/RBridge.Tests/obj" ]
    Fake.IO.Shell.cleanDirs [ "tests/RBridge.Extensions.Tests/bin"; "tests/RBridge.Extensions.Tests/obj" ])

// --------------------------------------------------------------------------------------
// Check formatting with Fantomas

Target.create "CheckFormat" (fun _ ->
    let result = DotNet.exec id "fantomas" "./src --check"

    if result.ExitCode = 0 then
        Trace.log "No files need formatting"
    elif result.ExitCode = 99 then
        failwith "Some files need formatting, run \"dotnet fantomas  ./src\" to resolve this."
    elif result.ExitCode = 99 then
        failwith "Some files need formatting, run \"dotnet fantomas  ./docs\" to resolve this."
    else
        Trace.logf "Errors while formatting: %A" result.Errors)

// --------------------------------------------------------------------------------------
// Move native libraries (compiled by gh runners) into correct location.

let artifactsDir = "native-artifacts"
let runtimesDir = "runtimes"

Target.create "PrepareRuntimes" (fun _ ->
    Fake.IO.Shell.cleanDir runtimesDir

    let copy rid pattern =
        let target = runtimesDir @@ rid @@ "native"
        Fake.IO.Directory.ensure target
        Fake.IO.Shell.copyFileIntoSubFolder target pattern

    copy "linux-x64"   (artifactsDir @@ "native-ubuntu-latest-x64"   @@ "librbridge-native.so")
    copy "linux-arm64" (artifactsDir @@ "native-ubuntu-latest-arm64" @@ "librbridge-native.so")
    copy "osx-x64"     (artifactsDir @@ "native-macos-latest-x64"    @@ "librbridge-native.dylib")
    copy "osx-arm64"   (artifactsDir @@ "native-macos-latest-arm64"  @@ "librbridge-native.dylib")
    copy "win-x64"     (artifactsDir @@ "native-windows-latest-x64"  @@ "rbridge-native.dll")
    copy "win-arm64"   (artifactsDir @@ "native-windows-latest-arm64"@@ "rbridge-native.dll")
)


// --------------------------------------------------------------------------------------
// Build library & test project

Target.create "Build" (fun _ ->
    Trace.log " --- Building the library --- "
    DotNet.build (fun p ->
        { p with
            Configuration = DotNet.BuildConfiguration.Debug
            MSBuildParams =
                { p.MSBuildParams with
                    Properties = [ "Platform", "Any CPU" ] } })
        "rbridge.sln" )

// --------------------------------------------------------------------------------------
// Run the unit tests using test runner & kill test runner when complete

Target.create "RunTests" (fun _ ->
    let rHome = Environment.environVarOrFail "R_HOME"
    Trace.logf "R_HOME is set as %s" rHome

    let result =
        Fake.DotNet.DotNet.exec
            (fun args ->
                { args with
                    Verbosity = Some Fake.DotNet.DotNet.Verbosity.Normal })
            "run"
            ("--project tests/RBridge.Extensions.Tests")

    if result.ExitCode <> 0 then
        failwith "Tests failed")

// --------------------------------------------------------------------------------------
// Build a NuGet package

Target.create "NuGet" (fun _ ->
    // Format the description to fit on a single line (remove \r\n and double-spaces)
    let projectDescription =
        projectDescription.Replace("\r", "").Replace("\n", "").Replace("  ", " ")

    // Format the release notes
    let releaseNotes = release.Notes |> String.concat "\n"

    let properties =
        [ ("Version", release.NugetVersion)
          ("Authors", authors)
          ("PackageProjectUrl", packageProjectUrl)
          ("PackageTags", tags)
          ("RepositoryType", repositoryType)
          ("RepositoryUrl", repositoryUrl)
          ("PackageLicenseExpression", license)
          ("PackageRequireLicenseAcceptance", "false")
          ("PackageReleaseNotes", releaseNotes)
          ("Summary", projectSummary)
          ("PackageDescription", projectDescription)
          ("EnableSourceLink", "true")
          ("PublishRepositoryUrl", "true")
          ("EmbedUntrackedSources", "true")
          ("IncludeSymbols", "true")
          ("IncludeSymbols", "false")
          ("SymbolPackageFormat", "snupkg")
          ("Copyright", copyright) ]

    DotNet.pack
        (fun p ->
            { p with
                Configuration = DotNet.BuildConfiguration.Release
                OutputPath = Some "bin"
                MSBuildParams =
                    { p.MSBuildParams with
                        Properties = properties } })
        ("rbridge.sln"))

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override

Target.create "All" ignore

"Clean" ==> "CheckFormat" ==> "AssemblyInfo" ==> "Build"
"Build" ==> "PrepareRuntimes" ==> "NuGet" ==> "All"
"Build" ==> "RunTests" ==> "All"

Target.runOrDefault "All"

