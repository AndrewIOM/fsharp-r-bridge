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

    let complexVector (engine: NativeApi.RunningEngine) values : SymbolicExpression =

        let n = Seq.length values
        let vec =
            engine.Api.allocVector.Invoke(typeAsInt ComplexVector, n)

        let ptr = engine.Api.pointers.complexPointer vec

        values
        |> Seq.fold (fun offset c ->
            Marshal.WriteInt64(ptr, offset, BitConverter.DoubleToInt64Bits c.Real)
            Marshal.WriteInt64(ptr, offset + sizeof<double>, BitConverter.DoubleToInt64Bits c.Imag)
            offset + 2 * sizeof<double>
        ) 0 |> ignore

        { ptr = vec }


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
                engine.Api.linkedLists.setTag node sym
            | None -> engine.Api.linkedLists.setTag node engine.Api.nilValue

            node

    let rec read (engine: NativeApi.RunningEngine) (sexp: SymbolicExpression) : (string option * SymbolicExpression) list =

        if sexp.ptr = engine.Api.nilValue then
            []
        else
            let tag = engine.Api.linkedLists.getTag sexp.ptr
            let nameOpt =
                match { ptr = tag } with
                | t when t.ptr = engine.Api.nilValue -> None
                | t when SymbolicExpression.getType engine t = Symbol ->
                    Some <| Extract.extractSymbol engine t
                | _ -> None

            let carPtr = engine.Api.linkedLists.getCar sexp.ptr
            let cdrPtr = engine.Api.linkedLists.getCdr sexp.ptr
            let value = { ptr = carPtr }
            (nameOpt, value) :: read engine { ptr = cdrPtr }

module Promise =

    /// Forces a promise to evaluate. If the expression is
    /// not a promise, passes through unmodified.
    let force (engine: NativeApi.RunningEngine) sexp =
        match SymbolicExpression.getType engine sexp with
        | Promise ->
            if sexp.ptr = engine.Api.missingArg then
                failwith "Cannot force a missing argument"

            let forcedPtr =
                NativeApi.tryEval sexp.ptr engine.Api.globalEnv engine
                |> Result.defaultWith (fun _ -> failwith "Could not force expression")

            { ptr = forcedPtr }
        | _ -> sexp


module Evaluate =

    /// Evaluate raw R code in the R engine. The code must contain
    /// only a single expression, not multiple expressions.
    let tryEval (code: string) (env: REnvironment) (engine: NativeApi.RunningEngine) =

        let strVec = Create.stringVector engine [| code |]

        let mutable status =
            NativeApi.Evaluate.ParseStatus.PARSE_NULL

        let exprVec =
            engine.Api.eval.parseVector.Invoke(strVec.ptr, -1, &status, NativeApi.nilValue engine)

        if status <> NativeApi.Evaluate.ParseStatus.PARSE_OK then
            failwithf "Parse error (%A) in expression: %s" status code

        if NativeApi.length exprVec engine <> 1 then
            failwith "The code contained multiple expressions, when only one is permitted here."

        // Extract the first (and only) parsed expression from the VECSXP,
        // as a EXPRSXP:
        let firstExpr =
            SymbolicExpression.getVectorElement engine { ptr = exprVec } 0

        match NativeApi.tryEval firstExpr.ptr env.Pointer engine with
        | Ok ptr -> Ok { ptr = ptr }
        | Error errPtr -> Error "R evaluation error"

    /// Call a function with named and / or unnamed arguments, formatted as a
    /// paired list.
    let tryCall
        (rEnv: REnvironment)
        (fn: SymbolicExpression)
        (args: (string option * SymbolicExpression) list)
        (engine: NativeApi.RunningEngine)
        : Result<SymbolicExpression, string> =
        let pairlist = PairList.build engine args

        let callPtr =
            engine.Api.construct.allocLang.Invoke(List.length args + 1)

        engine.Api.linkedLists.setCar callPtr fn.ptr
        engine.Api.linkedLists.setCdr callPtr pairlist

        match NativeApi.tryEval callPtr rEnv.Pointer engine with
        | Ok ptr -> Ok { ptr = ptr }
        | Error errPtr -> Error "R evaluation error"


module REnvironment =

    /// Return a reference to the R global environment.
    let globalEnv engine =
        REnvironment <| NativeApi.globalEnv engine

    let ofNamespace (engine: NativeApi.RunningEngine) namespaceName =
        let name =
            NativeApi.mkString namespaceName engine.Api

        let nsPtr = engine.Api.findNamespace.Invoke name
        REnvironment nsPtr

    /// Convert a symbolic expression pointer to an environment, if it is one.
    let ofSExp engine sexp =
        match SymbolicExpression.getType engine sexp with
        | Environment -> REnvironment sexp.ptr |> Some
        | _ -> None

    let ofPackage (engine: NativeApi.RunningEngine) (pkgName: string) =
        let code = sprintf "as.environment('package:%s')" pkgName
        Evaluate.tryEval code (globalEnv engine) engine
        |> Result.toOption
        |> Option.bind (ofSExp engine)
        |> Option.defaultWith (fun _ -> failwith "Error making new environment")

    let createEmpty (engine: NativeApi.RunningEngine) =
        Evaluate.tryEval "new.env()" (globalEnv engine) engine
        |> Result.toOption
        |> Option.bind (ofSExp engine)
        |> Option.defaultWith (fun _ -> failwith "Error making new environment")

    /// Looks up a symbol by name within the specified environment.
    /// Returns None if not bound.
    /// Returns an R promise (Some) if bound. To obtain the true value,
    /// force the promise.
    let tryGetValue (engine: NativeApi.RunningEngine) (env: REnvironment) (name: string) =
        let sym = NativeApi.install name engine.Api

        let valuePtr =
            NativeApi.getVarEx sym env.Pointer false engine.Api.unboundVal engine.Api

        if valuePtr = engine.Api.unboundVal then
            None
        else
            Some { ptr = valuePtr }

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

module Classes =

    let tryGetClass engine sexp =
        SymbolicExpression.tryGetAttribute sexp "class" engine

    let getClasses (engine: NativeApi.RunningEngine) sexp =
        match SymbolicExpression.tryGetAttribute sexp "class" engine with
        | None -> []
        | Some cl ->
            if cl.ptr = engine.Api.nilValue then []
            else
                match SymbolicExpression.getType engine cl with
                | SymbolicExpression.CharacterVector ->
                    Extract.extractStringArray engine cl
                    |> Array.toList
                | _ -> []


module S4 =

    let isS4 engine sexp =
        Classes.tryGetClass engine sexp |> Option.isSome
        && SymbolicExpression.tryGetAttribute sexp "package" engine
           |> Option.isSome

    let tryGetSlotTypes engine sexpS4 =
        let globalEnv = REnvironment.globalEnv engine

        Classes.tryGetClass engine sexpS4
        |> Option.bind
            (fun className ->
                match getType engine className with
                | CharacterVector ->
                    let classNames =
                        Extract.extractStringArray engine className

                    classNames |> Seq.tryHead
                | _ -> None)
        |> Option.map (fun mainClass -> Evaluate.tryEval (sprintf "getClass('%s')" mainClass) globalEnv engine)
        |> Option.bind Result.toOption
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

    let isPosixDateTime engine sexp : bool =
        match SymbolicExpression.getType engine sexp with
        | RealVector ->
            let classes = Classes.getClasses engine sexp
            List.contains "POSIXct" classes
        | _ -> false

    let isDate engine sexp =
        match SymbolicExpression.getType engine sexp with
        | RealVector ->
            let classes = Classes.getClasses engine sexp
            classes = ["Date"] || List.contains "Date" classes
        | _ -> false

module Factor =

    let trylevels engine sexp =
        match SymbolicExpression.tryGetAttribute sexp "levels" engine with
        | Some levelSexp ->
            match SymbolicExpression.getType engine levelSexp with
            | CharacterVector -> Extract.extractStringArray engine levelSexp |> Array.toList |> Some
            | _ -> None
        | None -> None

    let isFactor engine sexp =
        if SymbolicExpression.getType engine sexp <> SymbolicExpression.IntegerVector then false
        else
            match trylevels engine sexp with
            | None -> false
            | Some _ ->
                let classes = Classes.getClasses engine sexp
                List.contains "factor" classes

module Symbol =

    /// Assign a value to a named symbol in R.
    let setSymbol
        (name: string)
        (value: SymbolicExpression)
        (env: REnvironment)
        (engine: NativeApi.RunningEngine)
        : unit =
        let sym = NativeApi.install name engine.Api
        NativeApi.defineVar sym 0n env.Pointer |> ignore

    /// Capture the value of a symbol.
    let getSymbol (name: string) (env: REnvironment) (engine: NativeApi.RunningEngine) : SymbolicExpression option =
        let sym = NativeApi.install name engine.Api

        let v =
            NativeApi.getVar sym env.Pointer engine.Api

        if v = 0n then
            None
        else
            Some { ptr = v }

module Closures =

    type DefaultValue =
        | Missing
        | Null
        | Literal of SymbolicExpression
        | Expression of SymbolicExpression

    type ArgKind =
        | Normal
        | VarArgs
        | Optional

    /// An argument to a closure in R
    type Formal = {
            Name: string
            Default: DefaultValue
            Kind: ArgKind }

    let classifyKind (name: string) (def: DefaultValue) =
        if name = "..." then
            VarArgs
        else
            match def with
            | Missing -> Normal
            | _ -> Optional

    /// Try and retrieve formals for a closure.
    let tryFormals (engine: NativeApi.RunningEngine) closure =
        match SymbolicExpression.getType engine closure with
        | SymbolicExpression.Closure ->

            let formalsPtr = engine.Api.closures.getFormals.Invoke closure.ptr
            if formalsPtr = engine.Api.nilValue
            then None
            else
                let raw = PairList.read engine { ptr = formalsPtr }
                raw
                |> List.choose (fun (nameOpt, defaultExpr) ->
                    nameOpt |> Option.map (fun name ->

                        let def =
                            if defaultExpr.ptr = engine.Api.missingArg then
                                Missing
                            else
                                match SymbolicExpression.getType engine defaultExpr with
                                | SymbolicExpression.Nil -> Null
                                | SymbolicExpression.IntegerVector
                                | SymbolicExpression.RealVector
                                | SymbolicExpression.LogicalVector
                                | SymbolicExpression.CharacterVector ->
                                    Literal defaultExpr
                                | _ ->
                                    Expression defaultExpr

                        let kind = classifyKind name def

                        { Name = name
                          Kind = kind
                          Default = def }))
                |> Some
        | _ -> None


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
