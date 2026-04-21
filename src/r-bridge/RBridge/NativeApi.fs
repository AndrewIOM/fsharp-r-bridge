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

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_allocLang = delegate of int -> sexp

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type R_NewEnv = delegate of IntPtr * IntPtr * IntPtr -> IntPtr

        type ConstructionApi =
            { cons: Rf_cons
              lcons: Rf_lcons
              lang1: Rf_lang1
              lang2: Rf_lang2
              lang3: Rf_lang3
              allocList: Rf_allocList
              allocLang: Rf_allocLang
              newEnv: R_NewEnv }

    module Evaluate =

        type ParseStatus =
            | PARSE_NULL = 0
            | PARSE_OK = 1
            | PARSE_INCOMPLETE = 2
            | PARSE_ERROR = 3
            | PARSE_EOF = 4

        /// Returns a NULL pointer if evaluating the expression results in a jump to top level.
        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type R_tryEvalSilent = delegate of sexp * sexp * byref<int> -> sexp

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type R_ParseVector = delegate of sexp * int * byref<ParseStatus> * sexp -> sexp

        type EvaluateApi =
            { tryEval: R_tryEvalSilent
              parseVector: R_ParseVector }

    module Memory =

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_protect = delegate of sexp -> sexp

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_unprotect = delegate of int -> unit

        type MemoryApi =
            { protect: Rf_protect
              unprotect: Rf_unprotect }

    module Symbols =

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_install = delegate of string -> sexp

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_mkString = delegate of string -> sexp

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_mkChar = delegate of string -> sexp

        type SymbolApi =
            { install: Rf_install
              mkString: Rf_mkString
              mkChar: Rf_mkChar
              getPrintName: nativeint -> nativeint }

    module Attributes =

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_getAttrib = delegate of sexp * sexp -> sexp

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_setAttrib = delegate of sexp * sexp * sexp -> unit

        type AttributeApi =
            { setAttrib: Rf_setAttrib
              getAttrib: Rf_getAttrib }

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

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_isFunction = delegate of nativeint -> int

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_isPrimitive = delegate of nativeint -> int

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_isLanguage = delegate of nativeint -> int

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_isPairList = delegate of nativeint -> int

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type Rf_isVector = delegate of nativeint -> int

        type TypesApi =
            { getType: sexp -> int
              isNull: Rf_isNull
              isSymbol: Rf_isSymbol
              isLogical: Rf_isLogical
              isInteger: Rf_isInteger
              isReal: Rf_isReal
              isComplex: Rf_isComplex
              isString: Rf_isString
              isList: Rf_isList
              isExpression: Rf_isExpression
              isEnvironment: Rf_isEnvironment
              isFunction: Rf_isFunction
              isPrimitive: Rf_isPrimitive
              isLanguage: Rf_isLanguage
              isPairList: Rf_isPairList
              isVector: Rf_isVector }

    module ClosuresApi =

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type R_ClosureFormals = delegate of sexp -> sexp

        type ClosuresApi = {
            getFormals: R_ClosureFormals
        }

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type R_MissingArg = delegate of sexp -> sexp

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type R_FindNamespace = delegate of sexp -> sexp

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type Rf_translateCharUTF8 = delegate of sexp -> sexp

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type R_getVar = delegate of sexp * sexp * int -> sexp

    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type R_getVarEx = delegate of sexp * sexp * int * sexp -> sexp

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

    /// Custom methods implemented in the C shim, which generally
    /// wrap R macros.
    module Custom =

        [<DllImport("rbridge-native", CallingConvention = CallingConvention.Cdecl)>]
        extern void rbridge_set_tag(sexp node, sexp tag)

        [<DllImport("rbridge-native", CallingConvention = CallingConvention.Cdecl)>]
        extern sexp rbridge_get_tag(sexp node)

        [<DllImport("rbridge-native", CallingConvention = CallingConvention.Cdecl)>]
        extern void rbridge_set_car(sexp node, sexp value)

        [<DllImport("rbridge-native", CallingConvention = CallingConvention.Cdecl)>]
        extern sexp rbridge_get_car(sexp node)

        [<DllImport("rbridge-native", CallingConvention = CallingConvention.Cdecl)>]
        extern void rbridge_set_cdr(sexp node, sexp next)

        [<DllImport("rbridge-native", CallingConvention = CallingConvention.Cdecl)>]
        extern sexp rbridge_get_cdr(sexp node)

        [<DllImport("rbridge-native", CallingConvention = CallingConvention.Cdecl)>]
        extern sexp rbridge_get_printname(sexp node)

        [<DllImport("rbridge-native", CallingConvention = CallingConvention.Cdecl)>]
        extern int rbridge_typeof(nativeint sexp)


    type Api =
        { construct: Construction.ConstructionApi
          eval: Evaluate.EvaluateApi
          memory: Memory.MemoryApi
          symbol: Symbols.SymbolApi
          attribute: Attributes.AttributeApi
          typeof: Types.TypesApi
          pointers: PointerAccess
          closures: ClosuresApi.ClosuresApi
          findNamespace: R_FindNamespace
          translateUtf8: Rf_translateCharUTF8
          getVar: R_getVar
          getVarEx: R_getVarEx
          defineVar: Rf_defineVar
          length: Rf_length
          nrows: Rf_nrows
          ncols: Rf_ncols
          linkedLists: LinkedListsApi
          allocVector: Rf_allocVector
          allocMatrix: Rf_allocMatrix
          initEmbedded: Rf_initEmbeddedR
          setStartTime: R_setStartTime
          replDllInit: R_ReplDLLinit
          endEmbeddedR: Rf_endEmbeddedR
          printR: Rf_PrintValue
          missingArg: sexp
          globalEnv: sexp
          emptyEnv: sexp
          unboundVal: sexp
          nilValue: sexp }

    and PointerAccess =
        { integerPointer: sexp -> sexp // INTEGER(x)
          realPointer: sexp -> sexp // REAL(x)
          logicalPointer: sexp -> sexp // LOGICAL(x)
          stringPointer: sexp -> sexp // STRING_PTR(x)
          vectorPointer: sexp -> sexp // VECTOR_PTR(x)
          complexPointer: sexp -> sexp 
          charPointer: sexp -> sexp } // CHAR(x)

    and LinkedListsApi = {
          setTag: nativeint -> nativeint -> unit
          setCdr: nativeint -> nativeint -> unit
          setCar: nativeint -> nativeint -> unit
          getTag: nativeint -> nativeint
          getCdr: nativeint -> nativeint
          getCar: nativeint -> nativeint
    }

    type REngine =
        | Running of RunningEngine
        | NotRunning of error: string

    and RunningEngine = { Api: Api; LibHandle: nativeint }

    let loadApi (dllPath: string) =
        let handle = NativeLibrary.Load(dllPath)

        let get (name: string) : 'T =
            let ptr = NativeLibrary.GetExport(handle, name)
            Marshal.GetDelegateForFunctionPointer(ptr, typeof<'T>) :?> 'T

        let nilVal = 0n
        let dataptr: Rf_dataptr = get "DATAPTR"

        { Api =
              { construct =
                    { cons = get "Rf_cons"
                      lcons = get "Rf_lcons"
                      lang1 = get "Rf_lang1"
                      lang2 = get "Rf_lang2"
                      lang3 = get "Rf_lang3"
                      allocList = get "Rf_allocList"
                      allocLang = get "Rf_allocLang"
                      newEnv = get "R_NewEnv" }
                eval =
                    { tryEval = get "R_tryEvalSilent"
                      parseVector = get "R_ParseVector" }
                memory =
                    { protect = get "Rf_protect"
                      unprotect = get "Rf_unprotect" }
                symbol =
                    { install = get "Rf_install"
                      mkString = get "Rf_mkString"
                      mkChar = get "Rf_mkChar"
                      getPrintName = Custom.rbridge_get_printname }
                attribute =
                    { getAttrib = get "Rf_getAttrib"
                      setAttrib = get "Rf_setAttrib" }
                typeof =
                    { getType = Custom.rbridge_typeof
                      isNull = get "Rf_isNull"
                      isSymbol = get "Rf_isSymbol"
                      isLogical = get "Rf_isLogical"
                      isInteger = get "Rf_isInteger"
                      isReal = get "Rf_isReal"
                      isComplex = get "Rf_isComplex"
                      isString = get "Rf_isString"
                      isList = get "Rf_isList"
                      isExpression = get "Rf_isExpression"
                      isEnvironment = get "Rf_isEnvironment"
                      isFunction = get "Rf_isFunction"
                      isPrimitive = get "Rf_isPrimitive"
                      isLanguage = get "Rf_isLanguage"
                      isPairList = get "Rf_isPairList"
                      isVector = get "Rf_isVector" }
                closures = {
                    getFormals = get "R_ClosureFormals"
                }
                linkedLists = {
                    setTag = fun node tag -> Custom.rbridge_set_tag (node, tag)
                    setCar = fun node value -> Custom.rbridge_set_car (node, value)
                    setCdr = fun node next -> Custom.rbridge_set_cdr (node, next)
                    getTag = Custom.rbridge_get_tag
                    getCar = Custom.rbridge_get_car
                    getCdr = Custom.rbridge_get_cdr
                }
                missingArg = 0n
                findNamespace = get "R_FindNamespace"
                translateUtf8 = get "Rf_translateCharUTF8"
                getVar = get "R_getVar"
                getVarEx = get "R_getVarEx"
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
                globalEnv = nilVal
                emptyEnv = nilVal
                unboundVal = nilVal
                nilValue = nilVal
                pointers =
                    { integerPointer = dataptr.Invoke
                      realPointer = dataptr.Invoke
                      logicalPointer = dataptr.Invoke
                      stringPointer = dataptr.Invoke
                      vectorPointer = dataptr.Invoke
                      complexPointer = dataptr.Invoke
                      charPointer = dataptr.Invoke } }
          LibHandle = handle }

    let private getApi engine =
        match engine with
        | Running api -> Ok api.Api
        | NotRunning e -> Error(sprintf "Native API not loaded: %s" e)

    /// Convert .NET string array of arguments into a native
    /// C char* array.
    let withArgv (args: string []) (f: int * nativeint -> 'a) =
        let ptrs =
            args
            |> Array.map (fun s -> Marshal.StringToHGlobalAnsi s)

        let argv =
            Marshal.AllocHGlobal(nativeint (ptrs.Length * sizeof<nativeint>))

        ptrs
        |> Array.iteri (fun i p -> Marshal.WriteIntPtr(argv, i * sizeof<nativeint>, p))

        try
            f (args.Length, argv)
        finally
            ptrs |> Array.iter Marshal.FreeHGlobal
            Marshal.FreeHGlobal argv

    /// Evaluate an EXPRSXP in a given environment
    let tryEval expr env =
        fun engine ->
            let mutable err = 0

            let resultPtr =
                engine.Api.eval.tryEval.Invoke(expr, env, &err)

            if err <> 0 then
                Error resultPtr
            else
                Ok resultPtr

    let protect expr =
        fun engine -> engine.memory.protect.Invoke expr

    let unprotect n =
        fun engine -> engine.memory.unprotect.Invoke n

    let install name =
        fun engine -> engine.symbol.install.Invoke name

    /// Get a variable without environment inheritance
    let getVar sym env =
        fun engine -> engine.getVar.Invoke(sym, env, 0)

    let getVarEx sym (env: sexp) inherits ifNotFound =
        fun engine ->
            let inherits = if inherits = true then 1 else 0
            engine.getVarEx.Invoke(sym, env, inherits, ifNotFound)

    let defineVar sym value env =
        fun engine -> engine.defineVar.Invoke(sym, value, env)

    let setAttribute sexp sym newVal =
        fun engine -> engine.attribute.setAttrib.Invoke(sexp, sym, newVal)

    let getAttribute sexp sym =
        fun engine -> engine.attribute.getAttrib.Invoke(sexp, sym)

    let nilValue = fun engine -> engine.Api.nilValue
    let globalEnv = fun engine -> engine.Api.globalEnv


    /// refresh the cached R_NilValue and R_GlobalEnv after R has been initialised.
    let refreshEnvironmentValues (run: RunningEngine) : RunningEngine =
        let nilPtrLoc =
            NativeLibrary.GetExport(run.LibHandle, "R_NilValue")

        let nilVal = Marshal.ReadIntPtr nilPtrLoc

        let envPtrLoc =
            NativeLibrary.GetExport(run.LibHandle, "R_GlobalEnv")

        let envVal = Marshal.ReadIntPtr envPtrLoc

        let missingPtrLoc =
            NativeLibrary.GetExport(run.LibHandle, "R_MissingArg")

        let missingVal = Marshal.ReadIntPtr missingPtrLoc

        let emptyEnvPtrLoc =
            NativeLibrary.GetExport(run.LibHandle, "R_EmptyEnv")

        let emptyEnv = Marshal.ReadIntPtr emptyEnvPtrLoc

        let unboundPtrLoc =
            NativeLibrary.GetExport(run.LibHandle, "R_UnboundValue")

        let unboundValue = Marshal.ReadIntPtr unboundPtrLoc

        { run with
              Api =
                  { run.Api with
                        nilValue = nilVal
                        globalEnv = envVal
                        emptyEnv = emptyEnv
                        unboundVal = unboundValue
                        missingArg = missingVal } }

    /// allocate an UTF8 null-terminated string and call Rf_mkString.
    /// Returns both the resulting sexp and the native pointer so that the
    /// caller can free the memory once it has been protected by R.
    let mkString (s: string) api : sexp = api.symbol.mkString.Invoke s

    /// create a CHARSXP directly from a managed string using Rf_mkChar
    let mkChar (s: string) api : sexp = api.symbol.mkChar.Invoke s

    /// parseVector takes an expression and returns a vector of parsed
    /// expressions.  `status` is a byref int that receives any parser status.
    let parseVector (expr: sexp) (n: int) (status: byref<Evaluate.ParseStatus>) (env: sexp) engine =
        engine.Api.eval.parseVector.Invoke(expr, n, &status, env)

    let length v =
        fun running -> running.Api.length.Invoke(v)

    let typeOf v =
        fun running -> running.Api.typeof.getType v

    let nrows m =
        fun running -> running.Api.nrows.Invoke(m)

    let ncols m =
        fun running -> running.Api.ncols.Invoke(m)

    let allocVector t len =
        fun running -> running.Api.allocVector.Invoke(t, len)

    let allocMatrix t r c =
        fun running -> running.Api.allocMatrix.Invoke(t, r, c)

    let setStartTime api = api.Api.setStartTime.Invoke()
    /// optional hook used by RInside / R.NET, safe to call unconditionally
    let replDllInit api = api.Api.replDllInit.Invoke()
    let printVal m api = api.Api.printR.Invoke m

    let startEmbeddedR argv =
        fun running -> withArgv argv running.Api.initEmbedded.Invoke

    let endEmbeddedR status =
        fun running -> running.Api.endEmbeddedR.Invoke(status)
