namespace RBridge

open System
open System.IO

/// Simple helpers for locating an R installation on the host machine.
module EngineHost =

    /// Result of searching for R.
    type RLocation =
        { DllPath: string
          RHome: string
          RBin: string }

    let libFolder () =
        match Environment.OSVersion.Platform with
        | PlatformID.Win32NT -> "bin" // Path.Combine(location, "bin", (if Environment.Is64BitProcess then "x64" else "i386"))?
        | _ -> "lib"

    let tryFromRHome home =
        let homePath =
            if Directory.Exists home then
                Some(Path.GetFullPath home)
            else
                None

        match homePath with
        | Some homePath ->
            let bin = Path.Combine(homePath, libFolder ())

            let dll =
                Directory.EnumerateFiles(bin, "libR.*", SearchOption.TopDirectoryOnly)
                |> Seq.tryHead

            match dll with
            | Some file ->
                Some
                    { DllPath = file
                      RHome = home
                      RBin = bin }
            | None -> None
        | None -> None

    let tryFromEnvironment () =
        match Environment.GetEnvironmentVariable "R_HOME" with
        | null
        | "" -> None
        | home -> tryFromRHome home

    let tryDefaultPaths () =

        let standardLocations =
            [ if Environment.OSVersion.Platform = PlatformID.Win32NT then
                  yield
                      Path.Combine(
                          Environment.SpecialFolder.ProgramFiles
                          |> Environment.GetFolderPath,
                          "R"
                      )
              else
                  yield "/usr/local/lib/R"
                  yield "/usr/lib/R" ]

        standardLocations
        |> List.tryPick
            (fun baseDir ->
                if Directory.Exists(baseDir) then
                    let libFolder = libFolder ()
                    let path = Path.Combine(baseDir, libFolder)

                    let dll =
                        Directory.EnumerateFiles(path, "libR.*", SearchOption.TopDirectoryOnly)
                        |> Seq.tryHead

                    match dll with
                    | Some file ->
                        Some
                            { DllPath = file
                              RHome = baseDir
                              RBin = path }
                    | None -> None
                else
                    None)

    /// Try to find R by inspecting environment variables or common
    /// installation locations.
    let tryFindSystemR () : RLocation option =
        tryFromEnvironment ()
        |> Option.orElseWith tryDefaultPaths

    /// Finds the system's R installation from the R_HOME environment
    /// variable, and from
    let findSystemR () : RLocation =
        match tryFindSystemR () with
        | Some loc -> loc
        | None ->
            let msg =
                "Could not locate a system R installation. Ensure R is installed and R_HOME is set or accessible."

            raise (Exception(msg))
