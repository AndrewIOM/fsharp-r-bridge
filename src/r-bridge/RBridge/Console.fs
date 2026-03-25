namespace RBridge

/// Represents an R device. Reimplementation of
/// R.NET's 
type Device =
    { ReadConsole  : string -> int -> bool -> string
      WriteConsole : string -> unit
      ShowMessage  : string -> unit
      Busy         : bool -> unit }

