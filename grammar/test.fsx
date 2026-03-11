#load "structural.fsx"

open FsSci
open FsSci.ConcreteTypesExample

let dfMini =
    { Columns =
        [ "x", { Data = [1;2;3] }
          "y", { Data = [2;3;4] } ]
        |> Map.ofList }

let df =
    dfMini |> MiniFrame.SpecialMiniFn |> Base.as_data_frame

let logLikA =StatsLibraryExample.Likelihoods.ssr df "x" "y"


let ll f = StatsLibraryExample.Likelihoods.logLikelihood f
let y = StatsLibraryExample.metropolis 1000 (fun _ -> df) ll (Base.DataFrame df)

let nb : Base.NumericBackend<Vector> = Base.NumericBackend.create
