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
                  Environment.ofNamespace engine.Value "base"

              let meanOpt =
                  Environment.tryGetValue engine.Value baseNs "mean"

              match meanOpt with
              | None -> failtest "mean not found in base namespace"
              | Some mean ->
                  let t =
                      SymbolicExpression.getType engine.Value mean

                  Expect.equal t SymbolicExpression.Closure "mean should be a closure"

          testCase "as.data.frame is a closure"
          <| fun _ ->
              let baseNs =
                  Environment.ofNamespace engine.Value "base"

              let fnOpt =
                  Environment.tryGetValue engine.Value baseNs "as.data.frame"

              match fnOpt with
              | None -> failtest "as.data.frame not found in base namespace"
              | Some fn ->
                  let t =
                      SymbolicExpression.getType engine.Value fn

                  Expect.equal t SymbolicExpression.Closure "as.data.frame should be a closure"

          testCase "sin is a builtin"
          <| fun _ ->
              let baseNs =
                  Environment.ofNamespace engine.Value "base"

              let fnOpt =
                  Environment.tryGetValue engine.Value baseNs "sin"

              match fnOpt with
              | None -> failtest "sin not found in base namespace"
              | Some fn -> Expect.equal (SymbolicExpression.getType engine.Value fn) SymbolicExpression.Builtin ""

          ]

[<Tests>]
let symExTests =
    testList "Symbolic expression extensions" [

        testCase "Returns empty list for objects with no class" <| fun _ ->
            let xR = Evaluate.tryEval "42" (Environment.globalEnv engine.Value) engine.Value
            let x = Expect.wantOk xR "Could not eval 42"
            let classes = SymbolicExpression.getClasses engine.Value x
            Expect.sequenceEqual classes [] "Numeric scalar should have no class"

        testCase "Gets multiple classes in correct inheritance order" <| fun _ ->
            let xR = Evaluate.tryEval "as.POSIXlt(Sys.time())" (Environment.globalEnv engine.Value) engine.Value
            let x = Expect.wantOk xR "Could not eval code"
            let classes = SymbolicExpression.getClasses engine.Value x
            Expect.sequenceEqual classes [ Some "POSIXlt"; Some "POSIXt" ] "Did not have classes in correct order"

    ]


[<EntryPoint>]
let main argv =
    Tests.runTestsInAssemblyWithCLIArgs [ Sequenced ] argv
