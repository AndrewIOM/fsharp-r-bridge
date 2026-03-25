namespace RBridge

open System
open System.Runtime.InteropServices

/// Low-level bindings to the R C API.  We load the native library
/// dynamically (so we can point at a local/embedded copy) and then
/// create delegates for the functions we actually need.
module NativeApi =

    type sexp = nativeint

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type Rf_eval = delegate of sexp * sexp -> sexp

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type Rf_protect = delegate of sexp -> sexp

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type Rf_unprotect = delegate of int -> unit

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type Rf_install = delegate of string -> sexp

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type Rf_findVar = delegate of sexp * sexp -> sexp

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type Rf_defineVar = delegate of sexp * sexp * sexp -> sexp

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type Rf_mkString = delegate of sexp -> sexp

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type Rf_mkChar = delegate of string -> sexp

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type R_ParseVector = delegate of sexp * int * byref<int> * sexp -> sexp

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type Rf_length = delegate of sexp -> int

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type Rf_nrows = delegate of sexp -> int

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type Rf_ncols = delegate of sexp -> int

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type Rf_getAttrib = delegate of sexp * sexp -> sexp

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type Rf_setAttrib = delegate of sexp * sexp * sexp -> unit

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type Rf_allocVector = delegate of int * int -> sexp

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type Rf_allocMatrix = delegate of int * int * int -> sexp

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type Rf_initialize_R = delegate of int * string[] -> int

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type R_setStartTime = delegate of unit -> unit

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type Rf_endEmbeddedR = delegate of int -> unit

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type Rf_initEmbeddedR = delegate of int * string[] -> int

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type Rf_dataptr = delegate of nativeint -> nativeint

    /// called after Rf_initialize_R on Unix to complete setup of the main loop
    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type setup_Rmainloop = delegate of unit -> unit

    /// optional initialization helper exported by R; mostly used by RInside etc.
    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type R_ReplDLLinit = delegate of unit -> unit

    type Api =
        { eval : Rf_eval
          protect : Rf_protect
          unprotect : Rf_unprotect
          install : Rf_install
          findVar : Rf_findVar
          defineVar : Rf_defineVar
          mkString : Rf_mkString
          mkChar : Rf_mkChar
          parseVector : R_ParseVector
          length : Rf_length
          nrows : Rf_nrows
          ncols : Rf_ncols
          getAttrib : Rf_getAttrib
          setAttrib: Rf_setAttrib
          allocVector : Rf_allocVector
          allocMatrix : Rf_allocMatrix
          initializeR : Rf_initialize_R
          setStartTime : R_setStartTime
          setupMainloop : setup_Rmainloop
          replDllInit : R_ReplDLLinit
          endEmbeddedR : Rf_endEmbeddedR
          globalEnv: sexp
          nilValue : sexp
          pointers: PointerAccess }

    and PointerAccess = {
        integerPointer : sexp -> sexp   // INTEGER(x)
        realPointer    : sexp -> sexp   // REAL(x)
        logicalPointer : sexp -> sexp   // LOGICAL(x)
        stringPointer  : sexp -> sexp   // STRING_PTR(x)
        vectorPointer  : sexp -> sexp   // VECTOR_PTR(x)
        charPointer    : sexp -> sexp   // CHAR(x)
    }

    type REngine =
        | Running of RunningEngine
        | NotRunning of error:string

    and RunningEngine = {
        Api: Api
        LibHandle: nativeint
    }

    let loadApi (dllPath:string) =
        // load the shared library and bind the small set of symbols we need
        System.Console.WriteLine(sprintf "NativeApi: loading library '%s'" dllPath)
        let handle = NativeLibrary.Load(dllPath)
        // libHandle <- Some handle
        System.Console.WriteLine(sprintf "NativeApi: library loaded, handle=%A" handle)
        let get (name:string) : 'T =
            System.Console.WriteLine(sprintf "NativeApi: resolving %s" name)
            let ptr = NativeLibrary.GetExport(handle, name)
            System.Console.WriteLine(sprintf "NativeApi: got ptr for %s = %A" name ptr)
            Marshal.GetDelegateForFunctionPointer(ptr, typeof<'T>) :?> 'T
        // initial nilValue will be read after R is initialized; for now store
        // zero so we can update later via refreshNilValue.
        let nilVal = 0n
        let dataptr : Rf_dataptr = get "DATAPTR"
        System.Console.WriteLine(sprintf "NativeApi: initial nilValue placeholder = %A" nilVal)
        {
            Api = {
                eval = get "Rf_eval"
                protect = get "Rf_protect"
                unprotect = get "Rf_unprotect"
                install = get "Rf_install"
                findVar = get "Rf_findVar"
                defineVar = get "Rf_defineVar"
                mkString = get "Rf_mkString"
                mkChar = get "Rf_mkChar"
                parseVector = get "R_ParseVector"
                length = get "Rf_length"
                nrows = get "Rf_nrows"
                ncols = get "Rf_ncols"
                getAttrib = get "Rf_getAttrib"
                setAttrib = get "Rf_setAttrib"
                allocVector = get "Rf_allocVector"
                allocMatrix = get "Rf_allocMatrix"
                initializeR = get "Rf_initialize_R"
                setStartTime = get "R_setStartTime"
                setupMainloop = get "setup_Rmainloop"
                replDllInit = get "R_ReplDLLinit"
                endEmbeddedR = get "Rf_endEmbeddedR"
                globalEnv = 0n
                nilValue = nilVal
                pointers = {
                    integerPointer = dataptr.Invoke
                    realPointer    = dataptr.Invoke
                    logicalPointer = dataptr.Invoke
                    stringPointer  = dataptr.Invoke
                    vectorPointer  = dataptr.Invoke
                    charPointer    = dataptr.Invoke
                } }
            LibHandle = handle
        }

    let private getApi engine =
        match engine with
        | Running api -> Ok api.Api
        | NotRunning e -> Error (sprintf "Native API not loaded: %s" e)

    let eval expr env = fun engine -> engine.eval.Invoke(expr, env)
    let protect expr = fun engine -> engine.protect.Invoke expr
    let unprotect n = fun engine -> engine.unprotect.Invoke n
    let install name = fun engine -> engine.install.Invoke name
    let findVar sym env = fun engine -> engine.findVar.Invoke(sym, env)
    let defineVar sym value env = fun engine -> engine.defineVar.Invoke(sym, value, env)

    let setAttribute sexp sym newVal = fun engine -> engine.setAttrib.Invoke(sexp, sym, newVal)
    let getAttribute sexp sym = fun engine -> engine.getAttrib.Invoke(sexp, sym)

    let nilValue = fun engine -> engine.Api.nilValue
    let globalEnv = fun engine -> engine.Api.globalEnv


    /// refresh the cached R_NilValue and R_GlobalEnv after R has been initialised.
    let refreshEnvironmentValues (run:RunningEngine) : RunningEngine =
        let nilPtrLoc = NativeLibrary.GetExport(run.LibHandle, "R_NilValue")
        System.Console.WriteLine(sprintf "NativeApi: R_NilValue address = %A" nilPtrLoc)
        let nilVal = Marshal.ReadIntPtr(nilPtrLoc)
        System.Console.WriteLine(sprintf "NativeApi: refreshed nilValue = %A" nilVal)

        let envPtrLoc = NativeLibrary.GetExport(run.LibHandle, "R_GlobalEnv")
        System.Console.WriteLine(sprintf "NativeApi: R_GlobalEnv address = %A" envPtrLoc)
        let envVal = Marshal.ReadIntPtr(envPtrLoc)
        System.Console.WriteLine(sprintf "NativeApi: refreshed globalEnv = %A" envVal)

        { run with Api = { run.Api with nilValue = nilVal; globalEnv = envVal }}

    /// allocate an UTF8 null-terminated string and call Rf_mkString.
    /// Returns both the resulting sexp and the native pointer so that the
    /// caller can free the memory once it has been protected by R.
    let mkStringPtr (s:string) api : sexp * nativeint =
        System.Console.WriteLine(sprintf "NativeApi.mkStringPtr converting '%s'" s)
        let bytes = System.Text.Encoding.UTF8.GetBytes(s + "\0")
        let ptr = Marshal.AllocHGlobal(bytes.Length)
        Marshal.Copy(bytes, 0, ptr, bytes.Length)
        System.Console.WriteLine(sprintf "NativeApi.mkStringPtr calling Rf_mkString")
        let sexp = api.mkString.Invoke ptr
        System.Console.WriteLine(sprintf "NativeApi.mkStringPtr returned sexp=%A" sexp)
        sexp, ptr

    /// create a CHARSXP directly from a managed string using Rf_mkChar
    let mkChar (s:string) api : sexp =
        api.mkChar.Invoke s

    /// parseVector takes an expression and returns a vector of parsed
    /// expressions.  `status` is a byref int that receives any parser status.
    let parseVector (expr:sexp) (n:int) (status:byref<int>) (env:sexp) engine =
        engine.Api.parseVector.Invoke(expr, n, &status, env)

    let length v = fun running -> running.Api.length.Invoke(v)
    let nrows m = fun running -> running.Api.nrows.Invoke(m)
    let ncols m = fun running -> running.Api.ncols.Invoke(m)
    let allocVector t len = fun running -> running.Api.allocVector.Invoke(t, len)
    let allocMatrix t r c = fun running -> running.Api.allocMatrix.Invoke(t, r, c)
    let initialize_R argc argv = fun running -> running.Api.initializeR.Invoke(argc, argv)
    let setStartTime api = api.Api.setStartTime.Invoke()
    /// call after initialization on Unix-like platforms
    let setupMainloop api = api.Api.setupMainloop.Invoke()
    /// optional hook used by RInside / R.NET, safe to call unconditionally
    let replDllInit api = api.Api.replDllInit.Invoke()
    let endEmbeddedR status = fun running -> running.Api.endEmbeddedR.Invoke(status)
