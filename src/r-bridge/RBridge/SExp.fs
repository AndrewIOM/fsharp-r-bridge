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

    let getType (engine: RBridge.NativeApi.RunningEngine) (sexp: SymbolicExpression) : SexpType =
        if engine.Api.typeof.isNull.Invoke sexp.ptr <> 0 then
            Nil
        elif engine.Api.typeof.isSymbol.Invoke sexp.ptr <> 0 then
            Symbol
        elif engine.Api.typeof.isEnvironment.Invoke sexp.ptr
             <> 0 then
            Environment
        elif engine.Api.typeof.isLogical.Invoke sexp.ptr <> 0 then
            LogicalVector
        elif engine.Api.typeof.isInteger.Invoke sexp.ptr <> 0 then
            IntegerVector
        elif engine.Api.typeof.isReal.Invoke sexp.ptr <> 0 then
            RealVector
        elif engine.Api.typeof.isComplex.Invoke sexp.ptr <> 0 then
            ComplexVector
        elif engine.Api.typeof.isString.Invoke sexp.ptr <> 0 then
            CharacterVector
        elif engine.Api.typeof.isList.Invoke sexp.ptr <> 0 then
            List
        elif engine.Api.typeof.isExpression.Invoke sexp.ptr
             <> 0 then
            List
        else
            Any

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
        (engine: NativeApi.RunningEngine)
        : SymbolicExpression option =
        let sym = NativeApi.install name engine.Api

        let attrPtr =
            NativeApi.getAttribute sexp.ptr sym engine.Api

        if attrPtr = engine.Api.nilValue then
            None
        else
            Some { ptr = attrPtr }

    let setAttribute
        (engine: NativeApi.RunningEngine)
        (sexp: SymbolicExpression)
        (name: string)
        (value: SymbolicExpression)
        : unit =

        let sym = NativeApi.install name engine.Api
        NativeApi.setAttribute sexp.ptr sym value.ptr engine.Api

    let print (engine: NativeApi.RunningEngine) (sexp: SymbolicExpression) = NativeApi.printVal sexp.ptr engine

    let getVectorElement
        (engine: NativeApi.RunningEngine)
        (sexp: SymbolicExpression)
        (index: int)
        : SymbolicExpression =
        match getType engine sexp with
        | LogicalVector ->
            let ptr =
                engine.Api.pointers.logicalPointer sexp.ptr

            let value =
                Marshal.ReadInt32(ptr, index * sizeof<int>)

            let out =
                engine.Api.allocVector.Invoke(typeAsInt LogicalVector, 1)

            let outPtr = engine.Api.pointers.logicalPointer out
            Marshal.WriteInt32(outPtr, 0, value)
            { ptr = out }

        | IntegerVector ->
            let ptr =
                engine.Api.pointers.integerPointer sexp.ptr

            let value =
                Marshal.ReadInt32(ptr, index * sizeof<int>)

            let out =
                NativeApi.allocVector (typeAsInt IntegerVector) 1 engine

            let outPtr = engine.Api.pointers.integerPointer out
            Marshal.WriteInt32(outPtr, 0, value)
            { ptr = out }

        | RealVector ->
            let ptr = engine.Api.pointers.realPointer sexp.ptr

            let bits =
                Marshal.ReadInt64(ptr, index * sizeof<int64>)

            let value =
                System.BitConverter.Int64BitsToDouble bits

            let out =
                engine.Api.allocVector.Invoke(typeAsInt RealVector, 1)

            let outPtr = engine.Api.pointers.realPointer out
            Marshal.WriteInt64(outPtr, 0, System.BitConverter.DoubleToInt64Bits value)
            { ptr = out }

        | ComplexVector ->
            let ptr = engine.Api.pointers.realPointer sexp.ptr // complexPointer would be nicer, but realPointer works
            let realBits = Marshal.ReadInt64(ptr, index * 16)
            let imagBits = Marshal.ReadInt64(ptr, index * 16 + 8)

            let out =
                engine.Api.allocVector.Invoke(typeAsInt ComplexVector, 1)

            let outPtr = engine.Api.pointers.realPointer out
            Marshal.WriteInt64(outPtr, 0, realBits)
            Marshal.WriteInt64(outPtr, 8, imagBits)
            { ptr = out }

        | CharacterVector ->
            let ptr =
                engine.Api.pointers.stringPointer sexp.ptr

            { ptr = Marshal.ReadIntPtr(ptr, index * System.IntPtr.Size) }

        | RawVector ->
            let ptr = engine.Api.pointers.charPointer sexp.ptr
            let value = Marshal.ReadByte(ptr, index)

            let out =
                engine.Api.allocVector.Invoke(typeAsInt RawVector, 1)

            let outPtr = engine.Api.pointers.charPointer out
            Marshal.WriteByte(outPtr, 0, value)
            { ptr = out }

        // Expression vector:
        | List ->
            let ptr =
                engine.Api.pointers.vectorPointer sexp.ptr

            { ptr = Marshal.ReadIntPtr(ptr, index * System.IntPtr.Size) }

        | s -> failwithf "Could not get vector element of type: %A" s

    let setVectorElement
        (engine: NativeApi.RunningEngine)
        (vector: SymbolicExpression)
        (index: int)
        (value: SymbolicExpression)
        : unit =

        match getType engine vector with

        // Logical vector
        | LogicalVector ->
            let ptr =
                engine.Api.pointers.logicalPointer vector.ptr

            let vptr =
                engine.Api.pointers.logicalPointer value.ptr

            let v = Marshal.ReadInt32(vptr, 0)
            Marshal.WriteInt32(ptr, index * sizeof<int>, v)

        // Integer vector
        | IntegerVector ->
            let ptr =
                engine.Api.pointers.integerPointer vector.ptr

            let vptr =
                engine.Api.pointers.integerPointer value.ptr

            let v = Marshal.ReadInt32(vptr, 0)
            Marshal.WriteInt32(ptr, index * sizeof<int>, v)

        // Real vector
        | RealVector ->
            let ptr =
                engine.Api.pointers.realPointer vector.ptr

            let vptr =
                engine.Api.pointers.realPointer value.ptr

            let bits = Marshal.ReadInt64(vptr, 0)
            Marshal.WriteInt64(ptr, index * sizeof<int64>, bits)

        // Complex vector
        | ComplexVector ->
            let ptr =
                engine.Api.pointers.realPointer vector.ptr

            let vptr =
                engine.Api.pointers.realPointer value.ptr

            let realBits = Marshal.ReadInt64(vptr, 0)
            let imagBits = Marshal.ReadInt64(vptr, 8)
            Marshal.WriteInt64(ptr, index * 16, realBits)
            Marshal.WriteInt64(ptr, index * 16 + 8, imagBits)

        // Character vector (STRSXP)
        | CharacterVector ->
            let ptr =
                engine.Api.pointers.stringPointer vector.ptr
            // value.ptr is a CHARSXP
            Marshal.WriteIntPtr(ptr, index * System.IntPtr.Size, value.ptr)

        // Raw vector
        | RawVector ->
            let ptr =
                engine.Api.pointers.charPointer vector.ptr

            let vptr =
                engine.Api.pointers.charPointer value.ptr

            let b = Marshal.ReadByte(vptr, 0)
            Marshal.WriteByte(ptr, index, b)

        // VECSXP / EXPRSXP
        | List ->
            let ptr =
                engine.Api.pointers.vectorPointer vector.ptr

            Marshal.WriteIntPtr(ptr, index * System.IntPtr.Size, value.ptr)

        | t -> failwithf "Cannot set element of type: %A" t
