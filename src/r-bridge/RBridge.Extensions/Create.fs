namespace RBridge.Extensions

open System
open System.Runtime.InteropServices
open RBridge
open RBridge.SymbolicExpression

module Create =

    [<Literal>]
    let RDateOffset = 25569.

    let dateVector engine (dates: seq<DateTime>) =
        let values =
            dates
            |> Seq.map (fun dt -> dt.ToOADate() - RDateOffset)
            |> Seq.toArray

        let sexpPtr =
            let t = typeAsByte RealVector
            NativeApi.allocVector (int t) values.Length engine

        let sexp = { ptr = sexpPtr }

        let dataPtr = engine.Api.pointers.realPointer sexp.ptr
        Marshal.Copy(values, 0, dataPtr, values.Length)

        let classPtr =
            let t = typeAsByte StringVector
            NativeApi.allocVector (int t) 1 engine

        let classSexp = { ptr = classPtr }

        let charPtr = NativeApi.mkChar "Date" engine.Api
        let vecPtr = engine.Api.pointers.stringPointer classSexp.ptr
        Marshal.WriteIntPtr(vecPtr, 0, charPtr)

        // Attach attribute
        let classSym =  NativeApi.install "class" engine.Api
        engine.Api.setAttrib.Invoke(sexp.ptr, classSym, classSexp.ptr)

        sexp
