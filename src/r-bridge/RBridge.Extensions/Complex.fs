namespace RBridge.Extensions

/// R has a 'complex' type, which is two floats
/// stored together. There is no built-in .NET
/// equivalent.
[<Struct>]
type RComplex =
    { Real: float
      Imag: float }
    static member Create(real, imaginary) = { Real = real; Imag = imaginary }

/// Represents a date in R, which is based
/// on a 1970 baseline.
[<Struct>]
type RDate =
    { DaysSinceEpoch: int }

module RDate =

    let unixEpochDayNumber = System.DateOnly(1970,1,1).DayNumber

    let toDateOnly d =
        System.DateOnly.FromDayNumber(unixEpochDayNumber + d.DaysSinceEpoch)
    
    let create daysSinceREpoch =
        { DaysSinceEpoch = daysSinceREpoch }


/// Represents a time in R, which is based
/// on a 1970 baseline.
[<Struct>]
type RDateTime =
    { SecondsSinceEpoch: float
      TimeZone: string option }

module RDateTime =

    let toDateTimeUtc d =
        System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc).AddSeconds d.SecondsSinceEpoch

    let fromSeconds (seconds: float) (tz: string option) =
        { SecondsSinceEpoch = seconds
          TimeZone = tz }