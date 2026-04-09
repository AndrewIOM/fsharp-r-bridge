namespace RBridge

open System

/// A module that represnts the interop layer with the R API.
module RInterop =

    /// To avoid issues parsing locale within R initialisation,
    /// save the locale into environment variables first, then
    /// reload it afterwards.
    module Locale =

        let internal saveLocaleEnv () =
            [ "LANG"
              "LC_ALL"
              "LC_CTYPE"
              "LC_MESSAGES" ]
            |> List.map (fun k -> k, Environment.GetEnvironmentVariable k)

        let internal setEnvironment (saved: (string * string) list) =
            for (k, v) in saved do
                if isNull v then
                    Environment.SetEnvironmentVariable(k, null)
                else
                    Environment.SetEnvironmentVariable(k, v)

    open System.Runtime.InteropServices

    [<DllImport("libc")>]
    extern nativeint signal(int signum, nativeint handler)

    let SIGABRT = 6

    let dumpSignal label =
        let current = signal (SIGABRT, nativeint -1) // query mode
        printfn "%s: SIGABRT handler = %A" label current
        ignore (signal (SIGABRT, current)) // restore

    /// initialise the engine (e.g. load DLL, set environment etc.)
    let initialiseAt (loc: EngineHost.RLocation) : NativeApi.REngine =

        // set R_HOME if not already
        if String.IsNullOrEmpty(Environment.GetEnvironmentVariable("R_HOME")) then
            Environment.SetEnvironmentVariable("R_HOME", loc.RHome)

        // ensure the library/bin directory is on PATH
        let oldPath =
            Environment.GetEnvironmentVariable("PATH")

        let sep =
            if Environment.OSVersion.Platform = PlatformID.Win32NT then
                ";"
            else
                ":"

        let newPath =
            loc.RBin
            + sep
            + (if isNull oldPath then "" else oldPath)

        Environment.SetEnvironmentVariable("PATH", newPath)

        // load the R API and cache delegates
        Logging.debug "loading native R library at %s" loc.DllPath
        let engine = NativeApi.loadApi loc.DllPath

        // initialise the embedded R runtime (argc/argv may be empty)
        // follow rdotnet's sequence: set start time then call Rf_initialize_R.
        NativeApi.setStartTime engine

        Logging.debug "R_HOME=%s" (Environment.GetEnvironmentVariable "R_HOME")
        Logging.debug "PATH=%s" (Environment.GetEnvironmentVariable "PATH")
        Environment.SetEnvironmentVariable("LC_NUMERIC", "C")

        // R expects argv[0] to be program name; supply a dummy value
        // build a minimal argv similar to rdotnet's BuildRArgv
        let args: string [] =
            [| "REmbeddedFSharpBridge"
               "--quiet"
               "--gui=none"
               "--no-save"
               "--no-restore-data"
               "--no-site-file"
               "--no-init-file" |]

        let savedLocale = Locale.saveLocaleEnv ()
        Logging.debug "Saved locale: %A" savedLocale
        printfn "Culture before restore = %A" System.Globalization.CultureInfo.CurrentCulture
        dumpSignal "Before R"
        Logging.debug "Starting embedded R"
        let status = NativeApi.startEmbeddedR args engine
        Logging.debug "Rf_initEmbeddedR returned %d" status
        printfn "Culture after restore = %A" System.Globalization.CultureInfo.CurrentCulture
        Locale.setEnvironment savedLocale
        dumpSignal "After R"
        // if status <> 0 then
        //     failwithf "R initialization failed with status %d" status

        // update nilValue and globalEnv now that R has been initialised;
        // previous reads returned 0 because the globals hadn't been set yet.
        Logging.debug "refreshing environment values"

        NativeApi.refreshEnvironmentValues engine
        |> NativeApi.Running

    let initialise () =
        let loc = EngineHost.findSystemR ()
        Logging.debug "found R install: %A" loc
        initialiseAt loc

    let shutdown engine : unit = NativeApi.endEmbeddedR 0 engine
