module EnvironmentTests

open Expecto
open RBridge
open RBridge.Extensions
open SExpTests

[<Tests>]
let environmentTests =
    testList
        "REnvironment"
        [

          testCase "globalEnv is an environment"
          <| fun _ ->
              let env = REnvironment.globalEnv engine.Value
              let sexp = { ptr = env.Pointer }

              let t =
                  SymbolicExpression.getType engine.Value sexp

              Expect.equal t SymbolicExpression.Environment "globalEnv should be an environment"

          testCase "ofNamespace loads base namespace"
          <| fun _ ->
              let baseNs =
                  REnvironment.ofNamespace engine.Value "base"

              let sexp = { ptr = baseNs.Pointer }

              let t =
                  SymbolicExpression.getType engine.Value sexp

              Expect.equal t SymbolicExpression.Environment "getNamespace('base') should return an environment"

          testCase "ofPackage loads correct environment"
          <| fun _ ->
              let dsEnv =
                  REnvironment.ofPackage engine.Value "datasets"

              let sexp = { ptr = dsEnv.Pointer }

              let t =
                  SymbolicExpression.getType engine.Value sexp

              Expect.equal t SymbolicExpression.Environment "getNamespace('base') should return an environment"

              let mtCars =
                REnvironment.tryGetValue engine.Value dsEnv "mtcars"
                |> fun m -> Expect.wantSome m "Could not find mtcars"
              
              SymbolicExpression.print engine.Value mtCars
              let carType = SymbolicExpression.getType engine.Value mtCars
              Expect.equal carType SymbolicExpression.List "mtcars should have sexp type list"


          testCase "createEmpty creates an environment"
          <| fun _ ->
              let env = REnvironment.createEmpty engine.Value
              let sexp = { ptr = env.Pointer }

              let t =
                  SymbolicExpression.getType engine.Value sexp

              Expect.equal t SymbolicExpression.Environment "new.env() should return an environment"

          testCase "createEmpty environments are unique"
          <| fun _ ->
              let a = REnvironment.createEmpty engine.Value
              let b = REnvironment.createEmpty engine.Value
              Expect.notEqual a.Pointer b.Pointer "The environments were the same"

          testCase "ofSExp recognises environments"
          <| fun _ ->
              let env = REnvironment.globalEnv engine.Value
              let sexp = { ptr = env.Pointer }

              match REnvironment.ofSExp engine.Value sexp with
              | Some _ -> () // OK
              | None -> failtest "ofSExp should return Some for environment"

          testCase "ofSExp rejects non-environments"
          <| fun _ ->
              let str =
                  Create.stringVector engine.Value [ Some "hello" ]

              match REnvironment.ofSExp engine.Value str with
              | None -> ()
              | Some _ -> failtest "ofSExp should return None for non-environment"

          testProperty "Lookup of bound symbol always returns Some"
          <| fun value ->
              let env = REnvironment.createEmpty engine.Value
              let eSym = NativeApi.install "env" engine.Value.Api

              NativeApi.defineVar eSym env.Pointer engine.Value.Api.globalEnv engine.Value.Api
              |> ignore

              Evaluate.tryEval
                  (sprintf "assign('x', %i, envir = env)" value)
                  (REnvironment.globalEnv engine.Value)
                  engine.Value
              |> ignore

              let found =
                  REnvironment.tryGetValue engine.Value env "x"

              match found with
              | None -> false
              | Some sexp ->
                  SymbolicExpression.getType engine.Value sexp
                  <> SymbolicExpression.Nil

          testProperty "Lookup of unknown symbol returns None"
          <| fun sym ->
              if
                  System.String.IsNullOrEmpty sym
                  || not (isSafeSymbolName sym)
              then
                  true
              else
                  printfn "Name = '%s'" sym
                  let emptyEnv = REnvironment.globalEnv engine.Value
                  REnvironment.tryGetValue engine.Value emptyEnv sym = None

          ]

[<Tests>]
let evalTests =
    testList
        "Evaluation of expressions"
        [

          testCase "eval returns numeric type for 1+1"
          <| fun _ ->
              let glob = REnvironment.globalEnv engine.Value

              let sexp =
                  Evaluate.tryEval "1+1" glob engine.Value
                  |> Result.toOption
                  |> Option.get

              let t =
                  SymbolicExpression.getType engine.Value sexp

              Expect.equal t SymbolicExpression.RealVector "eval should not return a promise"

              let r =
                  sexp |> Extract.extractFloatArray engine.Value

              Expect.equal r [| Some 2. |] "1+1 = 2"

         ]
