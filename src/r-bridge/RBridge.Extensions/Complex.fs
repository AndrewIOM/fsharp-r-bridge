namespace RBridge.Extensions

/// R has a 'complex' type, which is two floats
/// stored together. There is no built-in .NET
/// equivalent.
[<Struct>]
type RComplex =
    { Real : float
      Imag : float }
