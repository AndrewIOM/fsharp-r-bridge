namespace RBridge.Extensions

module Real =

    open System
    open RBridge

    let isNA (engine: NativeApi.RunningEngine) (x: float) =
        Double.IsNaN x &&
        BitConverter.DoubleToInt64Bits x =
            BitConverter.DoubleToInt64Bits engine.Api.naReal


module Extract =

    open RBridge
    open RBridge.SymbolicExpression
    open System.Runtime.InteropServices
    open System

    /// NA values used by R in its atomic types.
    module NAs =

        let internal intNa = -2147483648


    let extractSymbol (engine: NativeApi.RunningEngine) (sexp: SymbolicExpression) : string =
        let charsPtr = engine.Api.symbol.getPrintName sexp.ptr
        let utf8Ptr = engine.Api.translateUtf8.Invoke charsPtr
        Marshal.PtrToStringUTF8 utf8Ptr

    let extractChar (engine: NativeApi.RunningEngine) (sexp: SymbolicExpression) : string =
        let ptr = engine.Api.translateUtf8.Invoke sexp.ptr
        Marshal.PtrToStringUTF8 ptr

    /// Extracts an integer array from an R vector. Returns
    /// an option value where NA = None.
    let extractIntArray (engine: NativeApi.RunningEngine) (sexp: SymbolicExpression) : int option [] =
        let len = NativeApi.length sexp.ptr engine

        let ptr =
            engine.Api.pointers.integerPointer sexp.ptr

        let arr = Array.zeroCreate<int> len
        Marshal.Copy(ptr, arr, 0, len)
        arr |> Array.map(fun i -> if i = NAs.intNa then None else Some i)

    /// Extracts an F# float (double) array from an R real vector. Returns
    /// an option value where NA = None.
    let extractFloatArray engine sexp : float option [] =
        let len = NativeApi.length sexp.ptr engine
        let ptr = engine.Api.pointers.realPointer sexp.ptr
        let arr = Array.zeroCreate<float> len
        Marshal.Copy(ptr, arr, 0, len)
        arr |> Array.map(fun i -> if Real.isNA engine i then None else Some i )

    let extractStringArray (engine: NativeApi.RunningEngine) (sexp: SymbolicExpression) : string option [] =
        let len = NativeApi.length sexp.ptr engine

        let ptr =
            engine.Api.pointers.stringPointer sexp.ptr // pointer to SEXP*

        Array.init
            len
            (fun i ->
                let elemPtr = Marshal.ReadIntPtr(ptr, i * IntPtr.Size)
                if elemPtr = engine.Api.naString then None
                else
                    let utf8Ptr = engine.Api.translateUtf8.Invoke elemPtr
                    Some <| Marshal.PtrToStringUTF8 utf8Ptr )

    let extractDateArray engine sexp : RDate option [] =
        extractFloatArray engine sexp
        |> Array.map(Option.map (int >> RDate.fromDaysSinceEpoch))

    let extractDateTimeArray engine sexp : RDateTime option[] =
        
        let timeZone =
            tryGetAttribute sexp "tzone" engine
            |> Option.bind(fun tz ->
                match getType engine tz with
                | CharacterVector -> extractStringArray engine tz |> Array.tryHead |> Option.flatten
                | _ -> None )
        
        extractFloatArray engine sexp
        |> Array.map(Option.map(fun s -> RDateTime.fromSeconds s timeZone))

    let extractLogicalArray engine sexp : bool option [] =
        extractIntArray engine sexp
        |> Array.map
            (function
            | Some 0 -> Some false
            | Some 1 -> Some true
            | _ -> None)

    let extractComplexArray engine sexp : RComplex option [] =
        let len = NativeApi.length sexp.ptr engine
        let ptr = engine.Api.pointers.complexPointer sexp.ptr
        Array.init len (fun i ->
            let offset = i * 2 * sizeof<double>
            let realBits = Marshal.ReadInt64(ptr, offset)
            let imagBits = Marshal.ReadInt64(ptr, offset + sizeof<double>)
            let c = { Real = BitConverter.Int64BitsToDouble realBits; Imag = BitConverter.Int64BitsToDouble imagBits }
            if Real.isNA engine c.Real && Real.isNA engine c.Imag then
                None
            else
                Some c
        )

    let extractRawArray engine sexp : SymbolicExpression [] =
        let len = NativeApi.length sexp.ptr engine
        let ptr = engine.Api.pointers.vectorPointer sexp.ptr
        Array.init len (fun i ->
            let elemPtr = Marshal.ReadIntPtr(ptr, i * IntPtr.Size)
            { ptr = elemPtr } )
    
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


    let extractDoubleMatrix engine sexp : float option [,] = extractMatrix engine sexp extractFloatArray

    let extractStringMatrix engine sexp : string option [,] = extractMatrix engine sexp extractStringArray

    let extractLogicalMatrix engine sexp : bool option [,] = extractMatrix engine sexp extractLogicalArray

    let extractIntMatrix engine sexp : int option [,] = extractMatrix engine sexp extractIntArray

    let extractComplexMatrix engine sexp : RComplex option [,] = extractMatrix engine sexp extractComplexArray

    let extractRawMatrix engine sexp : SymbolicExpression [,] = extractMatrix engine sexp extractRawArray

    let getDimension engine sexp =
        match tryGetAttribute sexp "dim" engine with
        | Some dimSexp -> extractIntArray engine dimSexp |> Array.length
        | None -> 1
