namespace RBridge.Extensions

/// R has a 'complex' type, which is two floats
/// stored together. There is no built-in .NET
/// equivalent.
[<Struct>]
type RComplex =
  { Real : float
    Imag : float }

/// Represents a date in R, which is based
/// on a 1970 baseline.
[<Struct>]
type RDate = { DaysSinceEpoch : int }
    with
      member d.ToDateOnly() =
          System.DateOnly.FromDayNumber(0).AddDays(d.DaysSinceEpoch)
      member d.ToDateTime() =
          System.DateTime(1970, 1, 1).AddDays(float d.DaysSinceEpoch)

/// Represents a time in R, which is based
/// on a 1970 baseline.
[<Struct>]
type RDateTime =
    { SecondsSinceEpoch : float
      TimeZone : string option }
    with
      member d.ToDateTime() =
          System.DateTime(1970, 1, 1).AddSeconds d.SecondsSinceEpoch

module RDateTime =

  let private epoch = System.DateTime(1970, 1, 1)

  // TODO Check this function works correctly.
  let fromDays (days: float) timezone =
    let ts = System.TimeSpan.FromDays days
    { SecondsSinceEpoch = ts.TotalSeconds; TimeZone = timezone }

