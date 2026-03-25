namespace RBridge

open System

/// Utilities for converting between F# values and RValue.  The bridge
/// exposes a small registration API so higher layers (including the RProvider)
/// can plug in their own conversions.
module Converters =

    /// function that converts a .NET object to an RValue
    type ToRConverter = obj -> RValue
    /// function that converts an RValue to a .NET object (or fails)
    type FromRConverter = RValue -> obj option

    let private toRTable = System.Collections.Concurrent.ConcurrentDictionary<Type,ToRConverter>()
    let private fromRTable = System.Collections.Concurrent.ConcurrentDictionary<Type,FromRConverter>()

    let registerToR<'T> (f: 'T -> RValue) =
        toRTable.[typeof<'T>] <- (fun o -> f (o :?> 'T))

    let registerFromR<'T> (f: RValue -> 'T option) =
        fromRTable.[typeof<'T>] <- (fun v -> f v |> Option.map box)

    let tryConvertToR (o: obj) : RValue option =
        if isNull o then Some RNull
        else
            let t = o.GetType()
            match toRTable.TryGetValue t with
            | true, conv -> Some(conv o)
            | _ -> None

    let tryConvertFromR<'T> (v:RValue) : 'T option =
        let t = typeof<'T>
        match fromRTable.TryGetValue t with
        | true, conv -> conv v |> Option.map unbox<'T>
        | _ -> None
