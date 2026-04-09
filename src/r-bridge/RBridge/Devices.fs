namespace RBridge

module Devices =

    /// Represents an R device that is character-based
    /// (not graphics-based). Reimplementation of
    /// R.NET's ICharacterDevice.
    type CharacterDevice =
        { ReadConsole: string -> int -> bool -> string
          WriteConsole: string -> unit
          ShowMessage: string -> unit
          Busy: bool -> unit }


    module Console =

        open System
        open System.Runtime.InteropServices

        module Native =

            [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
            type WriteConsoleCallback = delegate of IntPtr * int -> unit

            [<DllImport("rbridge-native", CallingConvention = CallingConvention.Cdecl)>]
            extern void rbridge_set_write_console(IntPtr cb)

        let register (device: CharacterDevice) =
            let handler =
                Native.WriteConsoleCallback
                    (fun ptr len ->
                        let text = Marshal.PtrToStringAnsi(ptr, len)
                        device.WriteConsole text)

            let fp =
                Marshal.GetFunctionPointerForDelegate handler

            Native.rbridge_set_write_console fp

        let defaultDevice: CharacterDevice =
            { ReadConsole =
                  fun prompt len addToHistory ->
                      System.Console.Write prompt
                      System.Console.ReadLine()

              WriteConsole = fun text -> System.Console.Write text

              ShowMessage = fun msg -> System.Console.WriteLine msg

              Busy =
                  fun isBusy ->
                      if isBusy then
                          System.Console.WriteLine "[R] Busy..."
                      else
                          System.Console.WriteLine "[R] Ready." }
