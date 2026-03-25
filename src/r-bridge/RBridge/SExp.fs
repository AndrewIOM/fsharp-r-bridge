namespace RBridge

open System.Runtime.InteropServices

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
        | StringVector
        | DotDotDot
        | Any
        | RawVector
        | List

    let typeAsByte = function
        | Nil -> 0uy  
        | Symbol -> 1uy  
        | Pairlist -> 2uy  
        | Closure -> 3uy  
        | Environment -> 4uy  
        | Promise -> 5uy  
        | Language -> 6uy  
        | Special -> 7uy  
        | Builtin -> 8uy  
        | Char -> 9uy  
        | LogicalVector -> 10uy 
        | IntegerVector -> 13uy 
        | RealVector -> 14uy 
        | ComplexVector -> 15uy 
        | StringVector -> 16uy 
        | DotDotDot -> 17uy 
        | Any -> 18uy 
        | RawVector -> 24uy 
        | List -> 19uy


    type SymbolicExpression =
        { ptr: nativeint }

    let getType (engine: RBridge.NativeApi.RunningEngine) (sexp: SymbolicExpression) : SexpType =
        let typeCode = Marshal.ReadByte(sexp.ptr)

        match typeCode with
        | 0uy  -> Nil
        | 1uy  -> Symbol
        | 2uy  -> Pairlist
        | 3uy  -> Closure
        | 4uy  -> Environment
        | 5uy  -> Promise
        | 6uy  -> Language
        | 7uy  -> Special
        | 8uy  -> Builtin
        | 9uy  -> Char
        | 10uy -> LogicalVector
        | 13uy -> IntegerVector
        | 14uy -> RealVector
        | 15uy -> ComplexVector
        | 16uy -> StringVector
        | 17uy -> DotDotDot
        | 18uy -> Any
        | 24uy -> RawVector
        | 19uy -> List
        | other ->
            failwithf "Unknown SEXP type code: %d" other

    let isVector engine sexp =
        match getType engine sexp with
        | IntegerVector
        | RealVector
        | LogicalVector
        | StringVector
        | ComplexVector
        | RawVector -> true
        | _ -> false

    let getLength engine (sexp:SymbolicExpression) =
        NativeApi.length sexp.ptr engine

    let getAttribute (sexp: SymbolicExpression) (name: string) (engine: NativeApi.RunningEngine) : SymbolicExpression option =
        let sym = NativeApi.install name engine.Api
        let attrPtr = NativeApi.getAttribute sexp.ptr sym engine.Api
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
