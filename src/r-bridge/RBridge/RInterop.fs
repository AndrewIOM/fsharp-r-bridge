namespace RBridge

open System
open System.Threading
open System.Threading.Tasks
open System.Collections.Concurrent

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


    /// initialise the engine (e.g. load DLL, set environment etc.)
    let internal initialiseAt' (loc: EngineHost.RLocation) (logger: Logging.Logger) : NativeApi.REngine =

        // Force .NET to initialise globalisation before R corrupts it.
        let _ = Globalization.CultureInfo.CurrentCulture
        let _ = Globalization.CultureInfo.CurrentUICulture
        let _ = TimeZoneInfo.Local

        // set R_HOME if not already
        if String.IsNullOrEmpty(Environment.GetEnvironmentVariable "R_HOME") then
            logger.info "Setting R_HOME as it was not set."
            Environment.SetEnvironmentVariable("R_HOME", loc.RHome)

        // ensure the library/bin directory is on PATH
        let oldPath =
            Environment.GetEnvironmentVariable "PATH"

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
        logger.debug <| sprintf "loading native R library at %s" loc.DllPath
        let engine = NativeApi.loadApi loc.DllPath

        // initialise the embedded R runtime (argc/argv may be empty)
        // follow rdotnet's sequence: set start time then call Rf_initialize_R.
        NativeApi.setStartTime engine

        logger.debug <| sprintf  "R_HOME=%s" (Environment.GetEnvironmentVariable "R_HOME")
        logger.debug <| sprintf  "PATH=%s" (Environment.GetEnvironmentVariable "PATH")
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
        logger.debug <| sprintf  "Saved locale: %A" savedLocale
        logger.debug <| sprintf "Culture before restore = %A" System.Globalization.CultureInfo.CurrentCulture
        logger.debug <| sprintf  "Starting embedded R"
        NativeApi.startEmbeddedR args engine
        logger.debug <| sprintf "Culture after restore = %A" System.Globalization.CultureInfo.CurrentCulture
        Locale.setEnvironment savedLocale

        // update nilValue, globalEnv and other global vars now that R has been initialised;
        // previous reads returned 0 because the globals hadn't been set yet.
        logger.debug "refreshing environment values"

        NativeApi.refreshEnvironmentValues engine
        
    /// An R instance running on it's own dedicated thread.
    /// May be called from multiple threads.
    type RInstance =
        { invoke : (NativeApi.REngine -> nativeint) -> nativeint
          invokeUnit : (NativeApi.REngine -> unit) -> unit
          invokeInt : (NativeApi.REngine -> int) -> int
          invokeFloat : (NativeApi.REngine -> float) -> float
          invokeIntPtr : (NativeApi.REngine -> IntPtr) -> IntPtr
          shutdown : unit -> unit }

    type RCommand =
        | Invoke      of (NativeApi.REngine -> nativeint) * TaskCompletionSource<nativeint>
        | InvokeU     of (NativeApi.REngine -> unit)      * TaskCompletionSource<unit>
        | InvokeI     of (NativeApi.REngine -> int)       * TaskCompletionSource<int>
        | InvokeF     of (NativeApi.REngine -> float)     * TaskCompletionSource<float>
        | InvokeIP    of (NativeApi.REngine -> IntPtr)    * TaskCompletionSource<IntPtr>
        | Shutdown    of TaskCompletionSource<unit>

    let startR (rLocation: EngineHost.RLocation) (logger: Logging.Logger) : BlockingCollection<RCommand> =

        let queue = new BlockingCollection<RCommand>()

        let thread = Thread(ThreadStart(fun () ->            

            logger.debug <| sprintf "Starting R on dedicated background thread %i." Thread.CurrentThread.ManagedThreadId
            let engine = initialiseAt' rLocation logger
            logger.debug <| sprintf "R started on thread %i." Thread.CurrentThread.ManagedThreadId

            let rec pump () =
                logger.debug <| sprintf "Started listening for R commands on thread %i." Thread.CurrentThread.ManagedThreadId

                let mutable cmd = Unchecked.defaultof<RCommand>
                while queue.TryTake(&cmd, Timeout.Infinite) do
                    match cmd with
                    | Invoke(work, tcs) ->
                        try
                            let res = work engine
                            tcs.SetResult res
                        with ex ->
                            logger.debug ex.Message
                            tcs.SetException ex

                    | InvokeU(work, tcs) ->
                        try
                            work engine
                            tcs.SetResult ()
                        with ex ->
                            tcs.SetException ex

                    | InvokeI(work, tcs) ->
                        try
                            let res = work engine
                            tcs.SetResult res
                        with ex ->
                            tcs.SetException ex

                    | InvokeF(work, tcs) ->
                        try
                            let res = work engine
                            tcs.SetResult res
                        with ex ->
                            tcs.SetException ex

                    | InvokeIP(work, tcs) ->
                        try
                            let res = work engine
                            tcs.SetResult res
                        with ex ->
                            tcs.SetException ex

                    | Shutdown tcs ->
                        try
                            NativeApi.endEmbeddedR 0 engine
                            tcs.SetResult ()
                        with ex ->
                            tcs.SetException ex
                        queue.CompleteAdding()

            pump ()

        ), 16 * 1024 * 1024)
        thread.IsBackground <- true
        thread.Start()

        queue

    let mkInstance (queue: BlockingCollection<RCommand>) : RInstance =
        let invoke work =
            let tcs = TaskCompletionSource<nativeint>()
            queue.Add(Invoke(work, tcs))
            tcs.Task.Result

        let invokeUnit work =
            let tcs = TaskCompletionSource<unit>()
            queue.Add(InvokeU(work, tcs))
            tcs.Task.Result

        let invokeInt work =
            let tcs = TaskCompletionSource<int>()
            queue.Add(InvokeI(work, tcs))
            tcs.Task.Result

        let invokeFloat work =
            let tcs = TaskCompletionSource<float>()
            queue.Add(InvokeF(work, tcs))
            tcs.Task.Result

        let invokeIntPtr work =
            let tcs = TaskCompletionSource<IntPtr>()
            queue.Add(InvokeIP(work, tcs))
            tcs.Task.Result

        let shutdown () =
            let tcs = TaskCompletionSource<unit>()
            queue.Add(Shutdown tcs)
            tcs.Task.Result

        { invoke      = invoke
          invokeUnit  = invokeUnit
          invokeInt   = invokeInt
          invokeFloat = invokeFloat
          invokeIntPtr= invokeIntPtr
          shutdown    = shutdown }


    /// Initialise R on it's own dedicated thread.
    /// If a location is not specified, will look for
    /// system R.
    let initialiseAt rLocation logger =
        let queue = startR rLocation logger
        mkInstance queue
        
    let initialise (logger: Logging.Logger) =
        let loc = EngineHost.findSystemR ()
        logger.info <| sprintf "found R install: %A" loc
        initialiseAt loc logger
