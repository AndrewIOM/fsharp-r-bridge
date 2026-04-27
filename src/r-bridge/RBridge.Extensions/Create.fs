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


/// NA values used by R in its atomic types.
module NAs =

    let intNa = -2147483648


module Create =

    let stringVector (engine: NativeApi.RunningEngine) (strings: string option seq) : SymbolicExpression =
        let vec =
            engine.Api.allocVector.Invoke(typeAsInt CharacterVector, Seq.length strings)

        for i = 0 to Seq.length strings - 1 do
            let charPtr =
                match Seq.item i strings with
                | Some s -> engine.Api.symbol.mkChar.Invoke s
                | None -> engine.Api.naString

            SymbolicExpression.setVectorElement engine { ptr = vec } i { ptr = charPtr }

        { ptr = vec }

    /// Create an integer vector in R, where F# None values correspond
    /// to R's internal representation of NA.
    let intVector (engine: NativeApi.RunningEngine) ints : SymbolicExpression =
        let xs = Seq.toArray ints
        let n = Array.length xs
        let vec = engine.Api.allocVector.Invoke(typeAsInt IntegerVector, n)

        let ptr = engine.Api.pointers.integerPointer vec

        for idx = 0 to n - 1 do
            let value =
                match xs[idx] with
                | None -> NAs.intNa
                | Some v -> v
            Marshal.WriteInt32(ptr, idx * sizeof<int>, value)

        { ptr = vec }

    /// Create a real numeric vector in R, where F# None values correspond
    /// to NAs in R.
    let realVector (engine: NativeApi.RunningEngine) floats : SymbolicExpression =
        let vec =
            engine.Api.allocVector.Invoke(typeAsInt RealVector, Seq.length floats)

        let ptr = engine.Api.pointers.realPointer vec

        for i = 0 to Seq.length floats - 1 do
            let bits =
                match Seq.item i floats with
                | None -> BitConverter.DoubleToInt64Bits engine.Api.naReal
                | Some x ->  BitConverter.DoubleToInt64Bits x

            Marshal.WriteInt64(ptr, i * sizeof<int64>, bits)

        { ptr = vec }

    let dateVector (engine: NativeApi.RunningEngine) (dates: RDate option seq) =
        let dayOffsets =
            dates |> Seq.map (Option.map (fun d -> float d.DaysSinceEpoch))

        let vec = realVector engine dayOffsets
        let cls = NativeApi.install "class" engine.Api
        let dateClass = NativeApi.mkString "Date" engine.Api
        NativeApi.setAttribute vec.ptr cls dateClass engine.Api
        vec

    let dateTimeVector (engine: NativeApi.RunningEngine) (values: RDateTime option seq) : SymbolicExpression =
        let seconds = values |> Seq.map (Option.map (fun d -> d.SecondsSinceEpoch))
        let timezones =
            values
            |> Seq.choose id
            |> Seq.map (fun d -> d.TimeZone)
            |> Seq.distinct
            |> Seq.toArray

        let timezone =
            match timezones with
            | [||] -> None
            | [| tz |] -> tz
            | _ -> failwith "Cannot mix timezones in an R POSIXct vector."
        
        let vec = realVector engine seconds
        let classes = [ Some "POSIXct"; Some "POSIXt" ]
        let classVec = stringVector engine classes
        let cls = NativeApi.install "class" engine.Api
        NativeApi.setAttribute vec.ptr cls classVec.ptr engine.Api
        match timezone with
        | Some tz ->
            let tzVec = NativeApi.mkString tz engine.Api
            let tzSym = NativeApi.install "tzone" engine.Api
            NativeApi.setAttribute vec.ptr tzSym tzVec engine.Api
        | None -> ()
        vec

    let logicalVector (engine: NativeApi.RunningEngine) (bools: bool option seq) : SymbolicExpression =
        let vec =
            engine.Api.allocVector.Invoke(typeAsInt LogicalVector, Seq.length bools)

        let ptr = engine.Api.pointers.logicalPointer vec

        for i = 0 to Seq.length bools - 1 do
            match Seq.item i bools with
            | Some true -> 1
            | Some false -> 0
            | None -> NAs.intNa
            |> fun v -> Marshal.WriteInt32(ptr, i * sizeof<int>, v)

        { ptr = vec }

    let complexVector (engine: NativeApi.RunningEngine) values : SymbolicExpression =

        let n = Seq.length values
        let vec =
            engine.Api.allocVector.Invoke(typeAsInt ComplexVector, n)

        let ptr = engine.Api.pointers.complexPointer vec

        values
        |> Seq.fold (fun offset c ->
            let c = c |> Option.defaultValue { Real = engine.Api.naReal; Imag = engine.Api.naReal }
            Marshal.WriteInt64(ptr, offset, BitConverter.DoubleToInt64Bits c.Real)
            Marshal.WriteInt64(ptr, offset + sizeof<double>, BitConverter.DoubleToInt64Bits c.Imag)
            offset + 2 * sizeof<double>
        ) 0 |> ignore

        { ptr = vec }
