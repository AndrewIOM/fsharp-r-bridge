module SExpTests

open Expecto
open RBridge

let engine =
    lazy(
        match RInterop.initialise() with
        | NativeApi.Running r -> r
        | _ -> failwith "Could not start R instance" )

[<Tests>]
let exprTypes =
    testList "get expression type" [

        testCase "String" <| fun _ ->

            let str = NativeApi.mkString "Hello string" engine.Value.Api
            let t = SymbolicExpression.getType engine.Value { ptr = str }

            Expect.equal t SymbolicExpression.CharacterVector
                "String was not inferred as string by R"
    ]


[<EntryPoint>]
let main argv =
    Tests.runTestsInAssemblyWithCLIArgs [ Sequenced ] argv
