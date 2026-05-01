namespace RBridge

open System
open System.IO
open System.Text.RegularExpressions

/// Simple helpers for locating an R installation on the host machine.
module EngineHost =

    /// Result of searching for R.
    type RLocation =
        { DllPath: string
          RHome: string
          RBin: string }

    /// Platform-specific folder within an R install where
    /// the R library is located.
    let libFolder () =
        match Environment.OSVersion.Platform with
        | PlatformID.Win32NT ->
            let arch = if Environment.Is64BitProcess then "x64" else "i386"
            Path.Combine("bin", arch)
        | _ -> "lib"

    /// Platform-specific naming pattern for the R library,
    /// for use as IO patterns.
    let libFileName () =
        match Environment.OSVersion.Platform with
        | PlatformID.Win32NT -> "R.dll"
        | _ -> "libR.*"

    /// On windows, R is stored in versioned folder (e.g. R-4.5.0).
    /// Try extract the R version from a directory.
    let tryParseRVersion (dirName: string) =
        let m = Regex.Match(dirName, @"R-(\d+)\.(\d+)\.(\d+)")
        if m.Success then
            let major = int m.Groups.[1].Value
            let minor = int m.Groups.[2].Value
            let patch = int m.Groups.[3].Value
            Some (major, minor, patch)
        else None

    /// Sort a list of R version directories (as on windows)
    /// with the latest version first.
    let sortByRVersionDescending (dirs: string list) =
        dirs
        |> List.choose (fun d ->
            let name = Path.GetFileName d
            match tryParseRVersion name with
            | Some v -> Some (v, d)
            | None -> None)
        |> List.sortByDescending fst
        |> List.map snd

    let tryLoadR homePath =
        let bin = Path.Combine(homePath, libFolder ())

        let dll =
            Directory.EnumerateFiles(bin, libFileName(), SearchOption.TopDirectoryOnly)
            |> Seq.tryHead

        match dll with
        | Some file ->
            Some
                { DllPath = file
                  RHome = homePath
                  RBin = bin }
        | None -> None


    let tryFromRHome home =
        let homePath =
            if Directory.Exists home then
                Some(Path.GetFullPath home)
            else
                None

        match homePath with
        | Some homePath -> tryLoadR homePath
        | None -> None

    let tryFromEnvironment () =
        match Environment.GetEnvironmentVariable "R_HOME" with
        | null
        | "" -> None
        | home -> tryFromRHome home

    let tryDefaultPaths () =

        let standardLocations =
            [ if Environment.OSVersion.Platform = PlatformID.Win32NT then
                let rBaseDir =
                    Path.Combine(
                        Environment.SpecialFolder.ProgramFiles
                        |> Environment.GetFolderPath,
                        "R"
                    )
                let rVersionDirs = Directory.EnumerateDirectories rBaseDir
                for d in rVersionDirs do
                    yield d
    
              else
                  yield "/usr/local/lib/R"
                  yield "/usr/lib/R" ]

        standardLocations
        |> List.tryPick
            (fun baseDir ->
                if Directory.Exists baseDir then tryLoadR baseDir
                else None)

    /// Try to find R by inspecting environment variables or common
    /// installation locations.
    let tryFindSystemR () : RLocation option =
        tryFromEnvironment ()
        |> Option.orElseWith tryDefaultPaths

    /// Finds the system's R installation from the R_HOME environment
    /// variable, or from standard install locations if not set.
    let findSystemR () : RLocation =
        match tryFindSystemR () with
        | Some loc -> loc
        | None ->
            let msg =
                "Could not locate a system R installation. Ensure R is installed and R_HOME is set or accessible."

            raise (Exception(msg))
