module SymbolicExprTests

open Expecto
open RBridge
open RBridge.Extensions
open SExpTests

[<Tests>]
let create =
    testList
        "Type inference"
        [

          testCase "mean is a closure"
          <| fun _ ->
              let baseNs =
                  REnvironment.ofNamespace engine.Value "base"

              let meanOpt =
                  REnvironment.tryGetValue engine.Value baseNs "mean"

              match meanOpt with
              | None -> failtest "mean not found in base namespace"
              | Some mean ->
                  let t =
                      SymbolicExpression.getType engine.Value mean

                  Expect.equal t SymbolicExpression.Closure "mean should be a closure"

          testCase "as.data.frame is a closure"
          <| fun _ ->
              let baseNs =
                  REnvironment.ofNamespace engine.Value "base"

              let fnOpt =
                  REnvironment.tryGetValue engine.Value baseNs "as.data.frame"

              match fnOpt with
              | None -> failtest "as.data.frame not found in base namespace"
              | Some fn ->
                  let t =
                      SymbolicExpression.getType engine.Value fn

                  Expect.equal t SymbolicExpression.Closure "as.data.frame should be a closure"

          testCase "sin is a builtin"
          <| fun _ ->
              let baseNs =
                  REnvironment.ofNamespace engine.Value "base"

              let fnOpt =
                  REnvironment.tryGetValue engine.Value baseNs "sin"

              match fnOpt with
              | None -> failtest "sin not found in base namespace"
              | Some fn -> Expect.equal (SymbolicExpression.getType engine.Value fn) SymbolicExpression.Builtin ""

          ]


[<EntryPoint>]
let main argv =
    Tests.runTestsInAssemblyWithCLIArgs [ Sequenced ] argv
