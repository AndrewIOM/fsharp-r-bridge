namespace RBridge.Extensions

open System
open System.Runtime.InteropServices
open RBridge

module SymbolicExpression =

    let getListItem (engine: NativeApi.RunningEngine) index sexp =
        let ptr =
            engine.Api.pointers.vectorPointer sexp.ptr

        let elemPtr =
            Marshal.ReadIntPtr(ptr, index * IntPtr.Size)

        { ptr = elemPtr }

    let getListItemByName engine name sexp =
        let names =
            Attributes.tryNames engine sexp
            |> Option.defaultValue [||]

        let idx = names |> Array.findIndex ((=) name)
        getListItem engine idx sexp

    /// Get the classes associated with a symbolic expression.
    /// If inheritence is active, the child class will appear earlier
    /// than the parent class.
    let getClasses (engine: NativeApi.RunningEngine) sexp =
        Classes.getClasses engine sexp