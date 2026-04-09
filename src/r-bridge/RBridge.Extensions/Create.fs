namespace RBridge.Extensions

open System
open System.Runtime.InteropServices
open RBridge
open RBridge.SymbolicExpression

/// An environment in R.
type REnvironment =
    private
    | REnvironment of RBridge.NativeApi.sexp
    member this.Pointer = this |> fun (REnvironment p) -> p

module Create =

    let stringVector (engine: NativeApi.RunningEngine) (strings: string seq) : SymbolicExpression =
        let vec =
            engine.Api.allocVector.Invoke(typeAsInt CharacterVector, Seq.length strings)

        for i = 0 to Seq.length strings - 1 do
            let charPtr =
                engine.Api.symbol.mkChar.Invoke(Seq.item i strings)

            SymbolicExpression.setVectorElement engine { ptr = vec } i { ptr = charPtr }

        { ptr = vec }

    let intVector (engine: NativeApi.RunningEngine) ints : SymbolicExpression =
        let vec =
            engine.Api.allocVector.Invoke(typeAsInt IntegerVector, Seq.length ints)

        let ptr = engine.Api.pointers.integerPointer vec

        for i = 0 to Seq.length ints - 1 do
            Marshal.WriteInt32(ptr, i * sizeof<int>, Seq.item i ints)

        { ptr = vec }

    let realVector (engine: NativeApi.RunningEngine) floats : SymbolicExpression =
        let vec =
            engine.Api.allocVector.Invoke(typeAsInt RealVector, Seq.length floats)

        let ptr = engine.Api.pointers.integerPointer vec

        for i = 0 to Seq.length floats - 1 do
            let bits =
                System.BitConverter.DoubleToInt64Bits(Seq.item i floats)

            Marshal.WriteInt64(ptr, i * sizeof<int64>, bits)

        { ptr = vec }

    let logicalVector (engine: NativeApi.RunningEngine) (bools: bool seq) : SymbolicExpression =
        let vec =
            engine.Api.allocVector.Invoke(typeAsInt LogicalVector, Seq.length bools)

        let ptr = engine.Api.pointers.integerPointer vec

        for i = 0 to Seq.length bools - 1 do
            let v = if Seq.item i bools then 1 else 0
            Marshal.WriteInt32(ptr, i * sizeof<int>, v)

        { ptr = vec }

    let complexVector engine strings : SymbolicExpression = failwith "not implemented"


module PairList =

    let rec build (engine: NativeApi.RunningEngine) (args: (string option * SymbolicExpression) list) =
        match args with
        | [] -> NativeApi.nilValue engine
        | (name, value) :: rest ->
            let tail = build engine rest

            let node =
                engine.Api.construct.cons.Invoke(value.ptr, tail)

            match name with
            | Some name ->
                let sym = engine.Api.symbol.install.Invoke(name)
                engine.Api.setTag node sym
            | None -> engine.Api.setTag node engine.Api.nilValue

            node


module Evaluate =

    /// Evaluate raw R code in the R engine. The code must contain
    /// only a single expression, not multiple expressions.
    let eval (code: string) (env: REnvironment) (engine: NativeApi.RunningEngine) =

        let strVec = Create.stringVector engine [| code |]

        let mutable status =
            NativeApi.Evaluate.ParseStatus.PARSE_NULL

        let exprVec =
            engine.Api.eval.parseVector.Invoke(strVec.ptr, -1, &status, NativeApi.nilValue engine)

        printfn "str is %A" (SymbolicExpression.getType engine strVec)

        printfn
            "parseVector.Invoke = %A"
            (engine
                .Api
                .eval
                .parseVector
                .GetType()
                .GetMethod("Invoke"))

        if status <> NativeApi.Evaluate.ParseStatus.PARSE_OK then
            failwithf "Parse error (%A) in expression: %s" status code

        if NativeApi.length exprVec engine <> 1 then
            failwith "The code contained multiple expressions, when only one is permitted here."

        // Extract the first (and only) parsed expression from the VECSXP,
        // as a EXPRSXP:
        let firstExpr =
            SymbolicExpression.getVectorElement engine { ptr = exprVec } 0

        let result =
            NativeApi.eval firstExpr.ptr env.Pointer engine.Api

        { ptr = result }

    /// Call a function with named and / or unnamed arguments, formatted as a
    /// paired list.
    let call
        (rEnv: REnvironment)
        (fn: SymbolicExpression)
        (args: (string option * SymbolicExpression) list)
        (engine: NativeApi.RunningEngine)
        : SymbolicExpression =
        let pairlist = PairList.build engine args

        let callPtr =
            engine.Api.construct.allocLang.Invoke(List.length args + 1)

        engine.Api.setCar callPtr fn.ptr
        engine.Api.setCdr callPtr pairlist

        let result =
            NativeApi.eval callPtr rEnv.Pointer engine.Api

        { ptr = result }


module REnvironment =

    /// Return a reference to the R global environment.
    let globalEnv engine =
        REnvironment <| NativeApi.globalEnv engine

    let ofNamespace (engine: NativeApi.RunningEngine) namespaceName =
        let code =
            sprintf "getNamespace(\"%s\")" namespaceName

        Evaluate.eval code (globalEnv engine) engine
        |> fun s -> s.ptr
        |> REnvironment

    let createEmpty (engine: NativeApi.RunningEngine) =
        Evaluate.eval "new.env" (globalEnv engine) engine
        |> fun s -> s.ptr
        |> REnvironment

    /// Convert a symbolic expression pointer to an environment, if it is one.
    let ofSExp engine sexp =
        match SymbolicExpression.getType engine sexp with
        | Environment -> REnvironment sexp.ptr |> Some
        | _ -> None

    /// Looks up a symbol by name within the specified environment.
    /// Returns None if not bound.
    let tryGetValue (engine: NativeApi.RunningEngine) (env: REnvironment) (name: string) =
        let sym = NativeApi.install name engine.Api

        let valuePtr =
            NativeApi.findVar sym env.Pointer engine.Api

        let foundEx = { ptr = valuePtr }

        match SymbolicExpression.getType engine foundEx with
        | Nil -> None
        | _ -> Some foundEx


module Attributes =

    /// Gets the names of a named vector, otherwise returns None.
    let tryNames engine sexp : string [] option =
        match tryGetAttribute sexp "names" engine with
        | None -> None
        | Some namesSexp ->
            match getType engine namesSexp with
            | CharacterVector ->
                Some
                <| Extract.extractStringArray engine namesSexp
            | _ -> None

module Vector =

    let isNamedVector engine sexp =
        if isVector engine sexp then
            match tryGetAttribute sexp "names" engine with
            | Some _ -> true
            | None -> false
        else
            false

    let tryNames engine sexp = failwith "not finished"


module S4 =

    let tryGetClass engine sexpS4 =
        SymbolicExpression.tryGetAttribute sexpS4 "class" engine

    let isS4 engine sexp =
        tryGetClass engine sexp |> Option.isSome
        && SymbolicExpression.tryGetAttribute sexp "package" engine
           |> Option.isSome

    let tryGetSlotTypes engine sexpS4 =
        let globalEnv = REnvironment.globalEnv engine

        tryGetClass engine sexpS4
        |> Option.bind
            (fun className ->
                match getType engine className with
                | CharacterVector ->
                    let classNames =
                        Extract.extractStringArray engine className

                    classNames |> Seq.tryHead
                | _ -> None)
        |> Option.map (fun mainClass -> Evaluate.eval (sprintf "getClass('%s')" mainClass) globalEnv engine)
        |> Option.bind (fun classDef -> SymbolicExpression.tryGetAttribute classDef "slots" engine)
        |> Option.bind
            (fun slotsSexp ->
                match getType engine slotsSexp with
                | CharacterVector ->
                    let names =
                        Vector.tryNames engine slotsSexp
                        |> Option.defaultValue [||]

                    let types =
                        Extract.extractStringArray engine slotsSexp

                    Some(Array.zip names types |> Map.ofArray)
                | _ -> None)

    let tryGetSlot (engine: NativeApi.RunningEngine) sexp slotName =
        let sym = NativeApi.install slotName engine.Api

        let ptr =
            NativeApi.getAttribute sexp.ptr sym engine.Api

        if ptr = engine.Api.nilValue then
            None
        else
            Some { ptr = ptr }

module Dates =

    let isPosixDateTime sexp : bool =
        // Should be a RealVector
        // if has class POSIXct, then should have attribute tzone.
        failwith "not implemented"

    let isDate sexp : bool =
        // Should be a RealVector
        // if has class Date...
        failwith "not implemented"


module Factor =

    let isFactor engine sexp = failwith "not implemented"

    let trylevels engine sexp : string list = failwith "not implemented"


module Symbol =

    /// Assign a value to a named symbol in R.
    let setSymbol
        (name: string)
        (value: SymbolicExpression)
        (env: REnvironment)
        (engine: NativeApi.RunningEngine)
        : unit =
        Logging.debug "setSymbol %s = %A" name value
        let sym = NativeApi.install name engine.Api
        NativeApi.defineVar sym 0n env.Pointer |> ignore

    /// Capture the value of a symbol.
    let getSymbol (name: string) (env: REnvironment) (engine: NativeApi.RunningEngine) : SymbolicExpression option =
        Logging.debug "getSymbol %s" name
        let sym = NativeApi.install name engine.Api

        let v =
            NativeApi.findVar sym env.Pointer engine.Api

        if v = 0n then
            None
        else
            Some { ptr = v }

module Function =

    let getFormals engine closure = failwith "not implemented"



    [<Literal>]
    let RDateOffset = 25569.

    let dateVector engine (dates: seq<DateTime>) =
        let values =
            dates
            |> Seq.map (fun dt -> dt.ToOADate() - RDateOffset)
            |> Seq.toArray

        let sexpPtr =
            let t = typeAsInt RealVector
            NativeApi.allocVector (int t) values.Length engine

        let sexp = { ptr = sexpPtr }

        let dataPtr = engine.Api.pointers.realPointer sexp.ptr
        Marshal.Copy(values, 0, dataPtr, values.Length)

        let classPtr =
            let t = typeAsInt CharacterVector
            NativeApi.allocVector (int t) 1 engine

        let classSexp = { ptr = classPtr }

        let charPtr = NativeApi.mkChar "Date" engine.Api

        let vecPtr =
            engine.Api.pointers.stringPointer classSexp.ptr

        Marshal.WriteIntPtr(vecPtr, 0, charPtr)

        // Attach attribute
        let classSym = NativeApi.install "class" engine.Api
        engine.Api.attribute.setAttrib.Invoke(sexp.ptr, classSym, classSexp.ptr)

        sexp
