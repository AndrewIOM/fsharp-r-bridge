namespace RBridge

open System
open System.Runtime.InteropServices

/// Low-level bindings to the R C API.  We load the native library
/// dynamically (so we can point at a local/embedded copy) and then
/// create delegates for the functions we actually need.
module NativeApi =

    type sexp = nativeint

    module Construction =

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_cons = delegate of sexp * sexp -> sexp

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_lcons = delegate of sexp * sexp -> sexp

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_lang1 = delegate of sexp -> sexp

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_lang2 = delegate of sexp * sexp -> sexp

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_lang3 = delegate of sexp * sexp * sexp -> sexp

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_allocList = delegate of int -> sexp

        type ConstructionApi = 
            { cons : Rf_cons
              lcons : Rf_lcons
              lang1 : Rf_lang1
              lang2 : Rf_lang2
              lang3 : Rf_lang3
              allocList : Rf_allocList }

    module Evaluate =

        type ParseStatus =
            | PARSE_NULL = 0
            | PARSE_OK = 1
            | PARSE_INCOMPLETE = 2
            | PARSE_ERROR = 3
            | PARSE_EOF = 4

        /// TODO Replace with R_tryEval
        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_eval = delegate of sexp * sexp -> sexp

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type R_ParseVector = delegate of sexp * int * byref<ParseStatus> * sexp -> sexp

        type EvaluateApi = {
            eval: Rf_eval
            parseVector : R_ParseVector
        }

    module Memory =

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_protect = delegate of sexp -> sexp

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_unprotect = delegate of int -> unit

        type MemoryApi = {
          protect : Rf_protect
          unprotect : Rf_unprotect
        }

    module Symbols =

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_install = delegate of string -> sexp

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_mkString = delegate of string -> sexp

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_mkChar = delegate of string -> sexp

        type SymbolApi = {
          install : Rf_install
          mkString : Rf_mkString
          mkChar : Rf_mkChar

        }

    module Attributes =

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_getAttrib = delegate of sexp * sexp -> sexp

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_setAttrib = delegate of sexp * sexp * sexp -> unit

        type AttributeApi = {
            setAttrib: Rf_setAttrib
            getAttrib: Rf_getAttrib
        }

    module Types =

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_isNull = delegate of nativeint -> int

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_isSymbol = delegate of nativeint -> int

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_isLogical = delegate of nativeint -> int

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_isInteger = delegate of nativeint -> int

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_isReal = delegate of nativeint -> int

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_isComplex = delegate of nativeint -> int

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_isString = delegate of nativeint -> int

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_isList = delegate of nativeint -> int

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_isExpression = delegate of nativeint -> int

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_isEnvironment = delegate of nativeint -> int

        type TypesApi =
            { isNull        : Rf_isNull
              isSymbol      : Rf_isSymbol
              isLogical     : Rf_isLogical
              isInteger     : Rf_isInteger
              isReal        : Rf_isReal
              isComplex     : Rf_isComplex
              isString      : Rf_isString
              isList        : Rf_isList
              isExpression  : Rf_isExpression
              isEnvironment : Rf_isEnvironment }


    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type Rf_findVar = delegate of sexp * sexp -> sexp

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type Rf_defineVar = delegate of sexp * sexp * sexp -> sexp

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type Rf_length = delegate of sexp -> int

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type Rf_nrows = delegate of sexp -> int

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type Rf_ncols = delegate of sexp -> int

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type Rf_allocVector = delegate of int * int -> sexp

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type Rf_allocMatrix = delegate of int * int * int -> sexp

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type R_setStartTime = delegate of unit -> unit

    // We are not using string[] arguments here, because
    // R mutates them, so we need them to be C char* instead.
    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type Rf_initEmbeddedR = delegate of int * nativeint -> int

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type Rf_endEmbeddedR = delegate of int -> unit

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type Rf_dataptr = delegate of nativeint -> nativeint

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type Rf_PrintValue = delegate of nativeint -> unit

    /// optional initialization helper exported by R; mostly used by RInside etc.
    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type R_ReplDLLinit = delegate of unit -> unit

    type Api =
        { construct: Construction.ConstructionApi
          eval: Evaluate.EvaluateApi
          memory: Memory.MemoryApi
          symbol: Symbols.SymbolApi
          attribute: Attributes.AttributeApi
          typeof: Types.TypesApi
          pointers: PointerAccess
          findVar : Rf_findVar
          defineVar : Rf_defineVar
          length : Rf_length
          nrows : Rf_nrows
          ncols : Rf_ncols
          allocVector : Rf_allocVector
          allocMatrix : Rf_allocMatrix
          initEmbedded: Rf_initEmbeddedR
          setStartTime : R_setStartTime
          replDllInit : R_ReplDLLinit
          endEmbeddedR : Rf_endEmbeddedR
          printR: Rf_PrintValue
          globalEnv: sexp
          nilValue : sexp }

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
                construct = { 
                    cons = get "Rf_cons"
                    lcons = get "Rf_lcons"
                    lang1 = get "Rf_lang1"
                    lang2 = get "Rf_lang2"
                    lang3 = get "Rf_lang3"
                    allocList = get "Rf_allocList"
                }
                eval = {
                    eval = get "Rf_eval"
                    parseVector = get "R_ParseVector"
                }
                memory = {
                    protect = get "Rf_protect"
                    unprotect = get "Rf_unprotect"
                }
                symbol = {
                    install = get "Rf_install"
                    mkString = get "Rf_mkString"
                    mkChar = get "Rf_mkChar"
                }
                attribute = {
                    getAttrib = get "Rf_getAttrib"
                    setAttrib = get "Rf_setAttrib"
                }
                typeof = {
                    isNull        = get "Rf_isNull"
                    isSymbol      = get "Rf_isSymbol"
                    isLogical     = get "Rf_isLogical"
                    isInteger     = get "Rf_isInteger"
                    isReal        = get "Rf_isReal"
                    isComplex     = get "Rf_isComplex"
                    isString      = get "Rf_isString"
                    isList        = get "Rf_isList"
                    isExpression  = get "Rf_isExpression"
                    isEnvironment = get "Rf_isEnvironment" 
                }
                findVar = get "Rf_findVar"
                defineVar = get "Rf_defineVar"
                length = get "Rf_length"
                nrows = get "Rf_nrows"
                ncols = get "Rf_ncols"
                allocVector = get "Rf_allocVector"
                allocMatrix = get "Rf_allocMatrix"
                initEmbedded = get "Rf_initEmbeddedR"
                setStartTime = get "R_setStartTime"
                replDllInit = get "R_ReplDLLinit"
                endEmbeddedR = get "Rf_endEmbeddedR"
                printR = get "Rf_PrintValue"
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

    /// Convert .NET string array of arguments into a native
    /// C char* array.
    let withArgv (args: string[]) (f: int * nativeint -> 'a) =
        let ptrs =
            args
            |> Array.map (fun s -> Marshal.StringToHGlobalAnsi s)

        let argv = Marshal.AllocHGlobal (nativeint (ptrs.Length * sizeof<nativeint>))

        ptrs
        |> Array.iteri (fun i p ->
            Marshal.WriteIntPtr(argv, i * sizeof<nativeint>, p)
        )

        try
            f (args.Length, argv)
        finally
            ptrs |> Array.iter Marshal.FreeHGlobal
            Marshal.FreeHGlobal argv

    let eval expr env = fun engine -> engine.eval.eval.Invoke(expr, env)
    let protect expr = fun engine -> engine.memory.protect.Invoke expr
    let unprotect n = fun engine -> engine.memory.unprotect.Invoke n
    let install name = fun engine -> engine.symbol.install.Invoke name
    let findVar sym env = fun engine -> engine.findVar.Invoke(sym, env)
    let defineVar sym value env = fun engine -> engine.defineVar.Invoke(sym, value, env)

    let setAttribute sexp sym newVal = fun engine -> engine.attribute.setAttrib.Invoke(sexp, sym, newVal)
    let getAttribute sexp sym = fun engine -> engine.attribute.getAttrib.Invoke(sexp, sym)

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
    let mkString (s:string) api : sexp =
        api.symbol.mkString.Invoke s

    /// create a CHARSXP directly from a managed string using Rf_mkChar
    let mkChar (s:string) api : sexp =
        api.symbol.mkChar.Invoke s

    /// parseVector takes an expression and returns a vector of parsed
    /// expressions.  `status` is a byref int that receives any parser status.
    let parseVector (expr:sexp) (n:int) (status:byref<Evaluate.ParseStatus>) (env:sexp) engine =
        engine.Api.eval.parseVector.Invoke(expr, n, &status, env)

    let length v = fun running -> running.Api.length.Invoke(v)
    let nrows m = fun running -> running.Api.nrows.Invoke(m)
    let ncols m = fun running -> running.Api.ncols.Invoke(m)
    let allocVector t len = fun running -> running.Api.allocVector.Invoke(t, len)
    let allocMatrix t r c = fun running -> running.Api.allocMatrix.Invoke(t, r, c)
    let setStartTime api = api.Api.setStartTime.Invoke()
    /// optional hook used by RInside / R.NET, safe to call unconditionally
    let replDllInit api = api.Api.replDllInit.Invoke()
    let printVal m api = api.Api.printR.Invoke m
    let startEmbeddedR argv = fun running -> withArgv argv running.Api.initEmbedded.Invoke
    let endEmbeddedR status = fun running -> running.Api.endEmbeddedR.Invoke(status)
