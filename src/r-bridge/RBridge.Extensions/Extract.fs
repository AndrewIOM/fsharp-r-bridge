namespace RBridge.Extensions

module Extract =

    open RBridge
    open RBridge.SymbolicExpression
    open System.Runtime.InteropServices
    open System

    let extractIntArray (engine: NativeApi.RunningEngine) (sexp: SymbolicExpression) : int[] =
        let len = NativeApi.length sexp.ptr engine
        let ptr = engine.Api.pointers.integerPointer sexp.ptr
        let arr = Array.zeroCreate<int> len
        Marshal.Copy(ptr, arr, 0, len)
        arr

    let extractDoubleArray engine sexp : float[] =
        let len = NativeApi.length sexp.ptr engine
        let ptr = engine.Api.pointers.realPointer sexp.ptr
        let arr = Array.zeroCreate<float> len
        Marshal.Copy(ptr, arr, 0, len)
        arr

    let extractLogicalArray engine sexp : bool option[] =
        extractIntArray engine sexp
        |> Array.map (function
            | 0 -> Some false
            | 1 -> Some true
            | _ -> None)

    let extractStringArray (engine: NativeApi.RunningEngine) (sexp: SymbolicExpression) : string[] =
        let len = NativeApi.length sexp.ptr engine
        let ptr = engine.Api.pointers.stringPointer sexp.ptr   // pointer to SEXP*
        Array.init len (fun i ->
            let elemPtr = Marshal.ReadIntPtr(ptr, i * IntPtr.Size)
            let charPtr = engine.Api.pointers.charPointer elemPtr
            Marshal.PtrToStringAnsi(charPtr))

    let extractList (engine: NativeApi.RunningEngine) (sexp: SymbolicExpression) : SymbolicExpression[] =
        let len = NativeApi.length sexp.ptr engine
        let ptr = engine.Api.pointers.vectorPointer sexp.ptr   // pointer to SEXP*
        Array.init len (fun i ->
            let elemPtr = Marshal.ReadIntPtr(ptr, i * IntPtr.Size)
            { ptr = elemPtr })

    let extractDoubleMatrix engine sexp : float[,] =
        let rows = NativeApi.nrows sexp.ptr engine
        let cols = NativeApi.ncols sexp.ptr engine
        let flat = extractDoubleArray engine sexp
        let m = Array2D.zeroCreate<float> rows cols
        for c in 0 .. cols - 1 do
            for r in 0 .. rows - 1 do
                m[r, c] <- flat.[c * rows + r]
        m

    let getDimension engine sexp =
        match getAttribute sexp "dim" engine with
        | Some dimSexp ->
            extractIntArray engine dimSexp |> Array.length
        | None -> 1
