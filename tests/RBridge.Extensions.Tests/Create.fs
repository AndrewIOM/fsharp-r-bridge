module SExpTests

open Expecto
open RBridge
open RBridge.Extensions
open FsCheck

/// Guards from symbol names that R will reject
/// from being passed into R.
let isSafeSymbolName (s: string) =
    s
    |> Seq.forall
        (fun c ->
            c >= 'A' && c <= 'Z'
            || c >= 'a' && c <= 'z'
            || c >= '0' && c <= '9'
            || c = '_')

let engine =
    lazy
        (match RInterop.initialise Logging.console with
         | NativeApi.Running r -> r
         | _ -> failwith "Could not start R instance")

let removeNans seq =
    seq |> Seq.filter(fun i ->
        match i with
        | Some i -> System.Double.IsNaN i |> not
        | None -> true )    

[<Tests>]
let create =
    testList
        "Creating and extracting R vectors"
        [

          testProperty "Integer vector"
          <| fun (ints: int option array) ->
              let v = Create.intVector engine.Value ints
              let roundTrip = Extract.extractIntArray engine.Value v
              Expect.sequenceEqual roundTrip ints "integer list was changed in R"

          testProperty "Real vector"
          <| fun (ints: float option array) ->
              let v = Create.realVector engine.Value ints
              let roundTrip = Extract.extractFloatArray engine.Value v

              Expect.sequenceEqual
                  (roundTrip |> removeNans)
                  (ints |> removeNans)
                  "float list was changed in R"

          testCase "Infinity and negative infinity round-trip" <| fun _ ->
            let v = [ Some infinity; Some -infinity]
            let v2 = Create.realVector engine.Value v
            let roundTrip = Extract.extractFloatArray engine.Value v2
            Expect.sequenceEqual roundTrip v "Inf values were transformed"

          testProperty "Logical vector"
          <| fun (bools: bool option array) ->
              let v = Create.logicalVector engine.Value bools

              let roundTrip =
                  Extract.extractLogicalArray engine.Value v

              Expect.equal roundTrip bools "float list was changed in R"

          testProperty "Date only vector"
          <| fun (daysSince1970: int option array) ->
            let dates = daysSince1970 |> Array.map (Option.map RDate.fromDaysSinceEpoch)
            let v = Create.dateVector engine.Value dates
            let roundTrip = Extract.extractDateArray engine.Value v
            Expect.sequenceEqual roundTrip dates "dates were changed in R"

          testProperty "Date-time vector"
          <| fun secondsSince1970 ->
            let dates = secondsSince1970 |> Array.map (Option.map(fun d -> RDateTime.fromSeconds d None))
            let v = Create.dateTimeVector engine.Value dates
            let roundTrip = Extract.extractDateTimeArray engine.Value v
            Expect.sequenceEqual
                (dates |> Array.filter(fun d -> match d with | Some d -> not <| System.Double.IsNaN d.SecondsSinceEpoch | None -> true))
                (roundTrip |> Array.filter(fun d -> match d with | Some d -> not <| System.Double.IsNaN d.SecondsSinceEpoch | None -> true))
                "dates were changed in R"

          ]

[<Tests>]
let stress =
    testList
        "Stress tests for Create functions"
        [

          testCase "10,000 string vectors"
          <| fun _ ->
              let mutable ok = true

              for i in 1 .. 10000 do
                  let v =
                      Create.stringVector engine.Value [| Some "a"; Some "b"; Some "c" |]

                  let t =
                      SymbolicExpression.getType engine.Value v

                  if t <> SymbolicExpression.CharacterVector then
                      printfn "STRSXP misclassified at iteration %d: %A" i t
                      ok <- false

                  let arr =
                      Extract.extractStringArray engine.Value v

                  if arr.Length <> 3 then
                      printfn "Wrong length at iteration %d" i
                      ok <- false

              Expect.isTrue ok "STRSXP should classify and extract correctly"

          testCase "10,000 characters"
          <| fun _ ->
              let mutable ok = true

              for i in 1 .. 100000 do
                  let ptr =
                      engine.Value.Api.symbol.mkChar.Invoke "x"

                  let sexp = { ptr = ptr }
                  let t = Extract.extractChar engine.Value sexp
                  if t <> "x" then ok <- false

              Expect.isTrue ok "CHARSXP should always classify as Char"

          ]

[<Tests>]
let closureTests =
    testList "Closures" [

        testCase "Gets correct closures for mean" <| fun _ ->

            // Retrieve the built-in closure "mean"
            let baseEnv = Environment.ofNamespace engine.Value "base"
            let mean = Environment.tryGetValue engine.Value baseEnv "mean"
            Expect.isSome mean "Could not find 'mean' in base."

            let formals = Closures.tryFormals engine.Value mean.Value
            Expect.isSome mean "Could not get formals for 'mean'."

            let names = formals.Value |> List.map (fun f -> f.Name)
            Expect.sequenceEqual names ["x"; "..."] "mean should have formals x and ..."

            let kinds = formals.Value |> List.map (fun f -> f.Kind)
            Expect.sequenceEqual kinds [Closures.Normal; Closures.VarArgs] "Kinds should be Normal and VarArgs"

            let defaults = formals.Value |> List.map (fun f -> f.Default)
            Expect.isTrue (defaults.[0] = Closures.Missing) "x should be Missing"
            Expect.isTrue (defaults.[1] = Closures.Missing) "... should be Missing"

    ]

let testFactor engine code expected =
    let glob = Environment.globalEnv engine
    let sexp =
        Evaluate.tryEval code glob engine
        |> Result.toOption
        |> Option.get
    Expect.isTrue (Factor.isFactor engine sexp) "Did not detect factor"
    Expect.equal (Factor.trylevels engine sexp) expected ""


[<Tests>]
let factorTests =
    testList "Factors" [

        testCase "Gets factor levels" <| fun _ ->
            testFactor engine.Value """factor(c("a", "b", "a"))""" (Some [Some "a"; Some "b"])

        testCase "Gets factor levels (alt order)" <| fun _ ->
            testFactor engine.Value """factor(c("b", "b", "a"))""" (Some [Some "a"; Some "b"])

        testCase "Gets factor levels (empty factor)" <| fun _ ->
            testFactor engine.Value """factor(character(0))""" (Some [])

    ]