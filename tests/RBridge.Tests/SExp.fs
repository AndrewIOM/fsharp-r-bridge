module SExpTests

open Expecto
open RBridge

let engine =
    lazy
        (match RInterop.initialise Logging.console with
         | NativeApi.Running r -> r
         | _ -> failwith "Could not start R instance")

[<Tests>]
let exprTypes =
    testList
        "get expression type"
        [

          testCase "String"
          <| fun _ ->

              let str =
                  NativeApi.mkString "Hello string" engine.Value.Api

              let t =
                  SymbolicExpression.getType engine.Value { ptr = str }

              Expect.equal t SymbolicExpression.CharacterVector "String was not inferred as string by R"

          testCase "Character"
          <| fun _ ->
              let ptr =
                  engine.Value.Api.symbol.mkChar.Invoke "x"

              let sexp = { ptr = ptr }

              let t =
                  SymbolicExpression.getType engine.Value sexp

              Expect.equal t SymbolicExpression.Char "mkChar should produce a CHARSXP"

          testCase "Integer vector"
          <| fun _ ->
              let ptr =
                  engine.Value.Api.allocVector.Invoke(SymbolicExpression.typeAsInt SymbolicExpression.IntegerVector, 5)

              let sexp = { ptr = ptr }

              let t =
                  SymbolicExpression.getType engine.Value sexp

              Expect.equal t SymbolicExpression.IntegerVector "allocVector(INTSXP) should produce INTSXP"

          testCase "Real vector"
          <| fun _ ->
              let ptr =
                  engine.Value.Api.allocVector.Invoke(SymbolicExpression.typeAsInt SymbolicExpression.RealVector, 3)

              let sexp = { ptr = ptr }

              let t =
                  SymbolicExpression.getType engine.Value sexp

              Expect.equal t SymbolicExpression.RealVector "allocVector(REALSXP) should produce REALSXP"

          testCase "Pair list"
          <| fun _ ->
              let car =
                  engine.Value.Api.symbol.mkString.Invoke "a"

              let cdr = engine.Value.Api.nilValue

              let ptr =
                  engine.Value.Api.construct.cons.Invoke(car, cdr)

              let sexp = { ptr = ptr }

              let t =
                  SymbolicExpression.getType engine.Value sexp

              Expect.equal t SymbolicExpression.Pairlist "cons() should produce LISTSXP"

          testCase "Language object"
          <| fun _ ->
              let sym =
                  engine.Value.Api.symbol.install.Invoke "+"

              let ptr =
                  engine.Value.Api.construct.lang1.Invoke sym

              let sexp = { ptr = ptr }

              let t =
                  SymbolicExpression.getType engine.Value sexp

              Expect.equal t SymbolicExpression.Language "lang1 should produce LANGSXP"

          ]


[<EntryPoint>]
let main argv =
    Tests.runTestsInAssemblyWithCLIArgs [ Sequenced ] argv
