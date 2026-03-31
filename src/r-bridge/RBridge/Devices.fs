namespace RBridge

module Devices =

    /// Represents an R device that is character-based
    /// (not graphics-based). Reimplementation of
    /// R.NET's ICharacterDevice.
    type CharacterDevice =
        { ReadConsole  : string -> int -> bool -> string
          WriteConsole : string -> unit
          ShowMessage  : string -> unit
          Busy         : bool -> unit }

    module NativeDeviceApi =

        open System.Runtime.InteropServices
        open NativeApi

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type R_WriteConsole = delegate of nativeint * int * int -> unit

        [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
        type R_ReadConsole = delegate of string * nativeint * int * int -> int
    
        let installDevice (engine: RunningEngine) device =

            // Create delegate
            let writeDel =
                R_WriteConsole(fun ptr len otype ->
                    let text = Marshal.PtrToStringAnsi(ptr, len)
                    device.WriteConsole text)

            // Write delegate pointer into R’s global variable
            let fp = Marshal.GetFunctionPointerForDelegate writeDel
            Marshal.WriteIntPtr(engine.Api.ptr_R_WriteConsole, fp)
            // Same pattern for ReadConsole, ShowMessage, Busy, etc.