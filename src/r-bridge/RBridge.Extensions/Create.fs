namespace RBridge.Extensions

open System
open System.Runtime.InteropServices
open RBridge
open RBridge.SymbolicExpression

module REnvironment =

    type REnvironment = {
        pointer: RBridge.NativeApi.sexp
    }

    /// Return a reference to the R global environment.
    let globalEnv engine = { pointer = NativeApi.globalEnv engine }


module Evaluate =

    /// Evaluate raw R code in the R engine.
    let eval (expr:string) env (engine: NativeApi.RunningEngine) =
        let exprPtr = NativeApi.mkChar expr engine.Api
        let result = NativeApi.eval exprPtr env engine.Api
        { ptr = result }


module S4 =

    let tryGetClass engine sexpS4 =
        SymbolicExpression.tryGetAttribute sexpS4 "class" engine

    let isS4 engine sexp =
        tryGetClass engine sexp |> Option.isSome &&
        SymbolicExpression.tryGetAttribute sexp "package" engine |> Option.isSome

    let tryGetSlot (engine: NativeApi.RunningEngine) sexp slotName =
        let sym = NativeApi.install slotName engine.Api
        let ptr = NativeApi.getAttribute sexp.ptr sym engine.Api
        if ptr = engine.Api.nilValue then None else Some { ptr = ptr }


module Factor =

    let isFactor engine sexp =
        failwith "not implemented"

    let trylevels engine sexp =
        failwith "not implemented"


module Symbol =

    /// Assign a value to a named symbol in R.
    let setSymbol (name:string) (value:SymbolicExpression) (env: REnvironment.REnvironment) (engine: NativeApi.RunningEngine) : unit =
        Logging.debug "setSymbol %s = %A" name value
        let sym = NativeApi.install name engine.Api
        NativeApi.defineVar sym 0n env.pointer |> ignore

    /// Capture the value of a symbol.
    let getSymbol (name:string) (env: REnvironment.REnvironment) (engine: NativeApi.RunningEngine) : SymbolicExpression option =
        Logging.debug "getSymbol %s" name
        let sym = NativeApi.install name engine.Api
        let v = NativeApi.findVar sym env.pointer engine.Api
        if v = 0n then None else Some { ptr = v }


module Vector =

    let isNamedVector engine sexp =
        if isVector engine sexp
        then
            match tryGetAttribute sexp "names" engine with
            | Some _ -> true
            | None -> false
        else false

    /// Gets the names of a named vector, otherwise returns None.
    let tryNames engine sexp : string[] option =
        match tryGetAttribute sexp "names" engine with
        | None -> None
        | Some namesSexp ->
            match getType engine namesSexp with
            | CharacterVector -> Some <| Extract.extractStringArray engine namesSexp
            | _ -> None


module Create =

    let stringVector engine strings  : SymbolicExpression =
        failwith "not implemented"

    [<Literal>]
    let RDateOffset = 25569.

    let dateVector engine (dates: seq<DateTime>) =
        let values =
            dates
            |> Seq.map (fun dt -> dt.ToOADate() - RDateOffset)
            |> Seq.toArray

        let sexpPtr =
            let t = typeAsByte RealVector
            NativeApi.allocVector (int t) values.Length engine

        let sexp = { ptr = sexpPtr }

        let dataPtr = engine.Api.pointers.realPointer sexp.ptr
        Marshal.Copy(values, 0, dataPtr, values.Length)

        let classPtr =
            let t = typeAsByte CharacterVector
            NativeApi.allocVector (int t) 1 engine

        let classSexp = { ptr = classPtr }

        let charPtr = NativeApi.mkChar "Date" engine.Api
        let vecPtr = engine.Api.pointers.stringPointer classSexp.ptr
        Marshal.WriteIntPtr(vecPtr, 0, charPtr)

        // Attach attribute
        let classSym =  NativeApi.install "class" engine.Api
        engine.Api.setAttrib.Invoke(sexp.ptr, classSym, classSexp.ptr)

        sexp
