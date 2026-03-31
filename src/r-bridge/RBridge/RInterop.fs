namespace RBridge

open System

/// A module that represnts the interop layer with the R API.
module RInterop =

    /// initialise the engine (e.g. load DLL, set environment etc.)
    let initialiseAt (loc:EngineHost.RLocation) : NativeApi.REngine =

        // set R_HOME if not already
        if String.IsNullOrEmpty(Environment.GetEnvironmentVariable("R_HOME")) then
            Environment.SetEnvironmentVariable("R_HOME", loc.RHome)

        // ensure the library/bin directory is on PATH
        let oldPath = Environment.GetEnvironmentVariable("PATH")
        let sep = if Environment.OSVersion.Platform = PlatformID.Win32NT then ";" else ":"
        let newPath = loc.RBin + sep + (if isNull oldPath then "" else oldPath)
        Environment.SetEnvironmentVariable("PATH", newPath)
        Logging.debug "loading native R library at %s" loc.DllPath

        // load the R API and cache delegates
        let engine = NativeApi.loadApi loc.DllPath

        // initialise the embedded R runtime (argc/argv may be empty)
        // follow rdotnet's sequence: set start time then call Rf_initialize_R.
        NativeApi.setStartTime engine

        // R expects argv[0] to be program name; supply a dummy value
        // build a minimal argv similar to rdotnet's BuildRArgv
        let args : string[] =
            [| "r-bridge"; "--no-save"; "--no-restore-data"; "--no-site-file"; "--no-init-file" |]
        Logging.debug "calling Rf_initialize_R"
        let status = NativeApi.initialize_R args.Length args engine
        Logging.debug "Rf_initialize_R returned %d" status
        if status <> 0 then
            failwithf "R initialization failed with status %d" status

        // the R specification (and rdotnet) indicate that on unix-like
        // platforms the C library exports a setup_Rmainloop function that must
        // be invoked before any evaluations.  it's essentially a no‑op on windows
        // but calling it unconditionally is safe.
        Logging.debug "calling setup_Rmainloop"
        NativeApi.setupMainloop engine

        // RInside also calls R_ReplDLLinit; include it for completeness
        Logging.debug "calling R_ReplDLLinit"
        NativeApi.replDllInit engine

        // update nilValue and globalEnv now that R has been initialised;
        // previous reads returned 0 because the globals hadn't been set yet.
        Logging.debug "refreshing environment values"
        NativeApi.refreshEnvironmentValues engine
        |> NativeApi.Running

    let initialise () =
        let loc = EngineHost.findSystemR()
        Logging.debug "found R install: %A" loc
        initialiseAt loc

    let shutdown engine : unit =
        NativeApi.endEmbeddedR 0 engine