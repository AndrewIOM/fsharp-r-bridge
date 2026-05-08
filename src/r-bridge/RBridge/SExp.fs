namespace RBridge

open System.Runtime.InteropServices

type SymbolicExpression = { ptr: nativeint }

/// Functions for working with R symbolic expressions.
module SymbolicExpression =

    type SexpType =
        | Nil
        | Symbol
        | Pairlist
        | Closure
        | Environment
        | Promise
        | Language
        | Special
        | Builtin
        | Char
        | LogicalVector
        | IntegerVector
        | RealVector
        | ComplexVector
        | CharacterVector
        | DotDotDot
        | Any
        | RawVector
        | List

    let typeAsInt =
        function
        | Nil -> 0
        | Symbol -> 1
        | Pairlist -> 2
        | Closure -> 3
        | Environment -> 4
        | Promise -> 5
        | Language -> 6
        | Special -> 7
        | Builtin -> 8
        | Char -> 9
        | LogicalVector -> 10
        | IntegerVector -> 13
        | RealVector -> 14
        | ComplexVector -> 15
        | CharacterVector -> 16
        | DotDotDot -> 17
        | Any -> 18
        | RawVector -> 24
        | List -> 19

    let isPromise (api: NativeApi.Types.TypesApi) sexp =
        let p = sexp.ptr

        not (
            api.isNull.Invoke p <> 0
            || api.isSymbol.Invoke p <> 0
            || api.isEnvironment.Invoke p <> 0
            || api.isFunction.Invoke p <> 0
            || api.isLanguage.Invoke p <> 0
            || api.isPairList.Invoke p <> 0
            || api.isExpression.Invoke p <> 0
            || api.isVector.Invoke p <> 0
        )

    let getType (engine: RBridge.RInterop.RInstance) (sexp: SymbolicExpression) : SexpType =
        if engine.invokeInt (fun e -> e.Api.typeof.isNull.Invoke sexp.ptr) <> 0 then
            Nil
        elif engine.invokeInt (fun e -> e.Api.typeof.isSymbol.Invoke sexp.ptr) <> 0 then
            Symbol
        elif engine.invokeInt (fun e -> e.Api.typeof.isLanguage.Invoke sexp.ptr) <> 0 then
            Language
        elif engine.invokeInt (fun e -> e.Api.typeof.isPairList.Invoke sexp.ptr) <> 0 then
            Pairlist
        elif engine.invokeInt (fun e -> e.Api.typeof.isFunction.Invoke sexp.ptr) <> 0 then
            if engine.invokeInt (fun e -> e.Api.typeof.isPrimitive.Invoke sexp.ptr) <> 0 then
                let t = engine.invokeInt (NativeApi.typeOf sexp.ptr)

                if t = 7 then Special
                elif t = 8 then Builtin
                else Any // Shouldn't happen
            else
                Closure
        elif engine.invokeInt(fun e-> e.Api.typeof.isEnvironment.Invoke sexp.ptr)
             <> 0 then
            Environment
        elif engine.invokeInt(fun e-> e.Api.typeof.isLogical.Invoke sexp.ptr) <> 0 then
            LogicalVector
        elif engine.invokeInt(fun e-> e.Api.typeof.isInteger.Invoke sexp.ptr) <> 0 then
            IntegerVector
        elif engine.invokeInt(fun e-> e.Api.typeof.isReal.Invoke sexp.ptr) <> 0 then
            RealVector
        elif engine.invokeInt(fun e-> e.Api.typeof.isComplex.Invoke sexp.ptr) <> 0 then
            ComplexVector
        elif engine.invokeInt(fun e-> e.Api.typeof.isString.Invoke sexp.ptr) <> 0 then
            CharacterVector
        elif engine.invokeInt(fun e-> e.Api.typeof.isList.Invoke sexp.ptr) <> 0 then
            Pairlist
        elif engine.invokeInt(fun e-> e.Api.typeof.isExpression.Invoke sexp.ptr) <> 0 then
            List
        elif engine.invokeInt (NativeApi.typeOf sexp.ptr) = 9 then
            Char
        elif engine.invokeInt(fun e -> if isPromise e.Api.typeof sexp then 1 else 0) = 1 then
            Promise
        else
            let t = engine.invokeInt (NativeApi.typeOf sexp.ptr)
            match t with
            | 13 -> IntegerVector // for factors.
            | 19 -> List
            | _ -> Any

    let isVector engine sexp =
        match getType engine sexp with
        | IntegerVector
        | RealVector
        | LogicalVector
        | CharacterVector
        | ComplexVector
        | RawVector -> true
        | _ -> false

    let length engine (sexp: SymbolicExpression) = NativeApi.length sexp.ptr engine

    let tryGetAttribute
        (sexp: SymbolicExpression)
        (name: string)
        (engine: RInterop.RInstance)
        : SymbolicExpression option =
        let sym = NativeApi.install name |> engine.invoke

        let attrPtr =
            NativeApi.getAttribute sexp.ptr sym |> engine.invoke

        if attrPtr = engine.invoke(fun e -> e.Api.nilValue) then
            None
        else
            Some { ptr = attrPtr }

    let setAttribute
        (engine: RInterop.RInstance)
        (sexp: SymbolicExpression)
        (name: string)
        (value: SymbolicExpression)
        : unit =

        let sym = NativeApi.install name |> engine.invoke
        NativeApi.setAttribute sexp.ptr sym value.ptr |> engine.invokeUnit

    let print (engine: RInterop.RInstance) (sexp: SymbolicExpression) =
        engine.invokeUnit(NativeApi.printVal sexp.ptr)

    let getVectorElement
        (engine: RInterop.RInstance)
        (sexp: SymbolicExpression)
        (index: int)
        : SymbolicExpression =
        match getType engine sexp with
        | LogicalVector ->
            let ptr =
                engine.invoke(fun e -> e.Api.pointers.logicalPointer sexp.ptr)

            let value =
                Marshal.ReadInt32(ptr, index * sizeof<int>)

            let out =
                engine.invoke(fun e -> e.Api.allocVector.Invoke(typeAsInt LogicalVector, 1))

            let outPtr = engine.invoke(fun e -> e.Api.pointers.logicalPointer out)
            Marshal.WriteInt32(outPtr, 0, value)
            { ptr = out }

        | IntegerVector ->
            let ptr =
                engine.invoke(fun e -> e.Api.pointers.integerPointer sexp.ptr)

            let value =
                Marshal.ReadInt32(ptr, index * sizeof<int>)

            let out =
                NativeApi.allocVector (typeAsInt IntegerVector) 1 |> engine.invoke

            let outPtr = engine.invoke(fun e -> e.Api.pointers.integerPointer out)
            Marshal.WriteInt32(outPtr, 0, value)
            { ptr = out }

        | RealVector ->
            let ptr = engine.invoke(fun e -> e.Api.pointers.realPointer sexp.ptr)

            let bits =
                Marshal.ReadInt64(ptr, index * sizeof<int64>)

            let value =
                System.BitConverter.Int64BitsToDouble bits

            let out =
                engine.invoke(fun e -> e.Api.allocVector.Invoke(typeAsInt RealVector, 1))

            let outPtr = engine.invoke(fun e -> e.Api.pointers.realPointer out)
            Marshal.WriteInt64(outPtr, 0, System.BitConverter.DoubleToInt64Bits value)
            { ptr = out }

        | ComplexVector ->
            let ptr = engine.invoke(fun e -> e.Api.pointers.realPointer sexp.ptr) // complexPointer would be nicer, but realPointer works
            let realBits = Marshal.ReadInt64(ptr, index * 16)
            let imagBits = Marshal.ReadInt64(ptr, index * 16 + 8)

            let out =
                engine.invoke(fun e -> e.Api.allocVector.Invoke(typeAsInt ComplexVector, 1))

            let outPtr = engine.invoke(fun e -> e.Api.pointers.realPointer out)
            Marshal.WriteInt64(outPtr, 0, realBits)
            Marshal.WriteInt64(outPtr, 8, imagBits)
            { ptr = out }

        | CharacterVector ->
            let ptr =
                engine.invoke(fun e -> e.Api.pointers.stringPointer sexp.ptr)

            { ptr = Marshal.ReadIntPtr(ptr, index * System.IntPtr.Size) }

        | RawVector ->
            let ptr = engine.invoke(fun e -> e.Api.pointers.charPointer sexp.ptr)
            let value = Marshal.ReadByte(ptr, index)

            let out =
                engine.invoke(fun e -> e.Api.allocVector.Invoke(typeAsInt RawVector, 1))

            let outPtr = engine.invoke(fun e -> e.Api.pointers.charPointer out)
            Marshal.WriteByte(outPtr, 0, value)
            { ptr = out }

        // Expression vector:
        | List ->
            let ptr =
                engine.invoke(fun e -> e.Api.pointers.vectorPointer sexp.ptr)

            { ptr = Marshal.ReadIntPtr(ptr, index * System.IntPtr.Size) }

        | s -> failwithf "Could not get vector element of type: %A" s

    let setVectorElement
        (engine: RInterop.RInstance)
        (vector: SymbolicExpression)
        (index: int)
        (value: SymbolicExpression)
        : unit =

        match getType engine vector with

        // Logical vector
        | LogicalVector ->
            let ptr =
                engine.invoke(fun e -> e.Api.pointers.logicalPointer vector.ptr)

            let vptr =
                engine.invoke(fun e -> e.Api.pointers.logicalPointer value.ptr)

            let v = Marshal.ReadInt32(vptr, 0)
            Marshal.WriteInt32(ptr, index * sizeof<int>, v)

        // Integer vector
        | IntegerVector ->
            let ptr =
                engine.invoke(fun e -> e.Api.pointers.integerPointer vector.ptr)

            let vptr =
                engine.invoke(fun e -> e.Api.pointers.integerPointer value.ptr)

            let v = Marshal.ReadInt32(vptr, 0)
            Marshal.WriteInt32(ptr, index * sizeof<int>, v)

        // Real vector
        | RealVector ->
            let ptr =
                engine.invoke(fun e -> e.Api.pointers.realPointer vector.ptr)

            let vptr =
                engine.invoke(fun e -> e.Api.pointers.realPointer value.ptr)

            let bits = Marshal.ReadInt64(vptr, 0)
            Marshal.WriteInt64(ptr, index * sizeof<int64>, bits)

        // Complex vector
        | ComplexVector ->
            let ptr =
                engine.invoke(fun e -> e.Api.pointers.realPointer vector.ptr)

            let vptr =
                engine.invoke(fun e -> e.Api.pointers.realPointer value.ptr)

            let realBits = Marshal.ReadInt64(vptr, 0)
            let imagBits = Marshal.ReadInt64(vptr, 8)
            Marshal.WriteInt64(ptr, index * 16, realBits)
            Marshal.WriteInt64(ptr, index * 16 + 8, imagBits)

        // Character vector (STRSXP)
        | CharacterVector ->
            let ptr =
                engine.invoke(fun e -> e.Api.pointers.stringPointer vector.ptr)
            // value.ptr is a CHARSXP
            Marshal.WriteIntPtr(ptr, index * System.IntPtr.Size, value.ptr)

        // Raw vector
        | RawVector ->
            let ptr =
                engine.invoke(fun e -> e.Api.pointers.charPointer vector.ptr)

            let vptr =
                engine.invoke(fun e -> e.Api.pointers.charPointer value.ptr)

            let b = Marshal.ReadByte(vptr, 0)
            Marshal.WriteByte(ptr, index, b)

        // VECSXP / EXPRSXP
        | List ->
            let ptr =
                engine.invoke(fun e -> e.Api.pointers.vectorPointer vector.ptr)

            Marshal.WriteIntPtr(ptr, index * System.IntPtr.Size, value.ptr)

        | t -> failwithf "Cannot set element of type: %A" t
