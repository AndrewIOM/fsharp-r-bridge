namespace RBridge

open System
open System.Runtime.InteropServices

/// Representation of a value returned from R.  we keep this abstract here
/// until the engine implementation is sorted out.
type RValue =
    | RNull
    | RNumeric of float []
    | RInteger of int []
    | RLogical of bool []
    | RCharacter of string []
    | RList of RValue list
    | RExpression of string        // unevaluated expression etc.
    // ... extend as needed

/// A module that represnts the interop layer with the R API.
module RInterop =

    /// utility that protects a sexp, executes the function then
    /// automatically unprotects the value.  mirrors the common
    /// ``PROTECT(expr); …; UNPROTECT(1)`` pattern from the R API.
    let private withProtected (expr:NativeApi.sexp) f run : 'a =
        let p = NativeApi.protect expr run
        try f p
        finally NativeApi.unprotect 1 run

    /// convert a piece of R source into a parsed vector and return
    /// the status code along with the vector object.  the supplied
    /// `sexp` must already be a protected CHARSXP (usually from
    /// `mkChar`).
    let private parseExpression (protectedString:NativeApi.sexp) engine : int * NativeApi.sexp =
        let mutable status = 0
        let globalEnv = NativeApi.globalEnv engine
        let vec = NativeApi.parseVector protectedString -1 &status globalEnv engine
        status, vec

    /// Evaluate a string of R code and return a typed value.
    let evaluate (code:string) (engine: NativeApi.RunningEngine) : RValue =
        Logging.debug "evaluating: %s" code
        let cstr = NativeApi.mkChar code engine.Api
        Logging.debug "mkChar returned %A" cstr
        withProtected cstr (fun protectedCstr ->
            Logging.debug "string object created and protected"
            let status, parsed = parseExpression protectedCstr engine
            Logging.debug "parsed expression, status=%d" status
            Logging.debug "calling eval"
            let globalEnv = NativeApi.globalEnv engine
            let res = NativeApi.eval parsed globalEnv engine.Api
            Logging.debug "eval returned %A" res
            // discard the result for now
            ()
        ) engine.Api
        RValue.RExpression code

    /// Execute a string of R code for its side effects.
    let exec (code:string) : unit =
        let _ = evaluate code
        ()

    /// Assign a value to a named symbol in R.
    let setSymbol (name:string) (value:RValue) (engine: NativeApi.RunningEngine) : unit =
        // for now we only support expression values
        Logging.debug "setSymbol %s = %A" name value
        let sym = NativeApi.install name engine.Api
        // TODO convert RValue -> sexp; use global environment
        let globalEnv = NativeApi.globalEnv engine
        NativeApi.defineVar sym 0n globalEnv |> ignore

    /// Capture the value of a symbol.
    let getSymbol (name:string) (engine: NativeApi.RunningEngine) : RValue option =
        Logging.debug "getSymbol %s" name
        let sym = NativeApi.install name engine.Api
        let globalEnv = NativeApi.globalEnv engine
        let v = NativeApi.findVar sym globalEnv engine.Api
        if v = 0n then None else Some (RValue.RExpression name)

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