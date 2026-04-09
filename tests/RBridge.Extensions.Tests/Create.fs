module SExpTests

open Expecto
open RBridge
open RBridge.Extensions

let engine =
    lazy(
        match RInterop.initialise() with
        | NativeApi.Running r -> r
        | _ -> failwith "Could not start R instance" )

[<Tests>]
let create =
    testList "Creating and extracting R vectors" [

        testProperty "Integer vector" <| fun (ints: int array) ->
            let v = Create.intVector engine.Value ints
            let roundTrip = Extract.extractIntArray engine.Value v
            Expect.equal roundTrip ints "integer list was changed in R"

        // TODO equality of nan
        // testProperty "Real vector" <| fun (ints: float array) ->
        //     let v = Create.realVector engine.Value ints
        //     let roundTrip = Extract.extractFloatArray engine.Value v
        //     Expect.equal roundTrip ints "float list was changed in R"

        // testProperty "Logical vector" <| fun (bools: bool array) ->
        //     let v = Create.logicalVector engine.Value bools
        //     let roundTrip = Extract.extractLogicalArray engine.Value v
        //     Expect.equal roundTrip bools "float list was changed in R"

    ]


[<EntryPoint>]
let main argv =
    Tests.runTestsInAssemblyWithCLIArgs [ Sequenced ] argv
