namespace RBridge.Extensions

module Extract =

    open RBridge
    open RBridge.SymbolicExpression
    open System.Runtime.InteropServices
    open System

    let extractSymbol (engine: NativeApi.RunningEngine) (sexp: SymbolicExpression) : string =
        let charsPtr = engine.Api.symbol.getPrintName sexp.ptr
        let utf8Ptr = engine.Api.translateUtf8.Invoke charsPtr
        Marshal.PtrToStringUTF8 utf8Ptr

    let extractChar (engine: NativeApi.RunningEngine) (sexp: SymbolicExpression) : string =
        let ptr = engine.Api.translateUtf8.Invoke sexp.ptr
        Marshal.PtrToStringUTF8 ptr

    let extractIntArray (engine: NativeApi.RunningEngine) (sexp: SymbolicExpression) : int [] =
        let len = NativeApi.length sexp.ptr engine

        let ptr =
            engine.Api.pointers.integerPointer sexp.ptr

        let arr = Array.zeroCreate<int> len
        Marshal.Copy(ptr, arr, 0, len)
        arr

    let extractFloatArray engine sexp : float [] =
        let len = NativeApi.length sexp.ptr engine
        let ptr = engine.Api.pointers.realPointer sexp.ptr
        let arr = Array.zeroCreate<float> len
        Marshal.Copy(ptr, arr, 0, len)
        arr

    let extractLogicalArray engine sexp : bool option [] =
        extractIntArray engine sexp
        |> Array.map
            (function
            | 0 -> Some false
            | 1 -> Some true
            | _ -> None)

    let extractComplexArray engine sexp : RComplex [] =
        let len = NativeApi.length sexp.ptr engine
        let ptr = engine.Api.pointers.complexPointer sexp.ptr
        Array.init len (fun i ->
            let offset = i * 2 * sizeof<double>
            let realBits = Marshal.ReadInt64(ptr, offset)
            let imagBits = Marshal.ReadInt64(ptr, offset + sizeof<double>)
            { Real = BitConverter.Int64BitsToDouble realBits
              Imag = BitConverter.Int64BitsToDouble imagBits }
        )

    let extractRawArray engine sexp : SymbolicExpression [] =
        let len = NativeApi.length sexp.ptr engine
        let ptr = engine.Api.pointers.vectorPointer sexp.ptr
        Array.init len (fun i ->
            let elemPtr = Marshal.ReadIntPtr(ptr, i * IntPtr.Size)
            { ptr = elemPtr } )
    
    let extractStringArray (engine: NativeApi.RunningEngine) (sexp: SymbolicExpression) : string [] =
        let len = NativeApi.length sexp.ptr engine

        let ptr =
            engine.Api.pointers.stringPointer sexp.ptr // pointer to SEXP*

        Array.init
            len
            (fun i ->
                let elemPtr = Marshal.ReadIntPtr(ptr, i * IntPtr.Size)
                let utf8Ptr = engine.Api.translateUtf8.Invoke elemPtr
                Marshal.PtrToStringUTF8 utf8Ptr )

    let extractList (engine: NativeApi.RunningEngine) (sexp: SymbolicExpression) : SymbolicExpression [] =
        let len = NativeApi.length sexp.ptr engine

        let ptr =
            engine.Api.pointers.vectorPointer sexp.ptr // pointer to SEXP*

        Array.init
            len
            (fun i ->
                let elemPtr = Marshal.ReadIntPtr(ptr, i * IntPtr.Size)
                { ptr = elemPtr })

    let private buildMatrix rows cols get =
        Array2D.init rows cols (fun r c -> get (c * rows + r))

    let private extractMatrix engine sexp (extract: NativeApi.RunningEngine -> SymbolicExpression -> 'a array) =
        let rows = NativeApi.nrows sexp.ptr engine
        let cols = NativeApi.ncols sexp.ptr engine
        let flat = extract engine sexp
        buildMatrix rows cols (fun i -> flat.[i])


    let extractDoubleMatrix engine sexp : float [,] = extractMatrix engine sexp extractFloatArray

    let extractStringMatrix engine sexp : string [,] = extractMatrix engine sexp extractStringArray

    let extractLogicalMatrix engine sexp : bool option [,] = extractMatrix engine sexp extractLogicalArray

    let extractIntMatrix engine sexp : int [,] = extractMatrix engine sexp extractIntArray

    let extractComplexMatrix engine sexp : RComplex [,] = extractMatrix engine sexp extractComplexArray

    let extractRawMatrix engine sexp : SymbolicExpression [,] = extractMatrix engine sexp extractRawArray

    let getDimension engine sexp =
        match tryGetAttribute sexp "dim" engine with
        | Some dimSexp -> extractIntArray engine dimSexp |> Array.length
        | None -> 1
