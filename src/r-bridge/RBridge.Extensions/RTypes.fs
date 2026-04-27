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
    
    let fromDaysSinceEpoch daysSinceREpoch =
        { DaysSinceEpoch = daysSinceREpoch }

    let fromDateOnly(d: System.DateOnly) =
        { DaysSinceEpoch = d.DayNumber - unixEpochDayNumber }


/// Represents a time in R, which is based
/// on a 1970 baseline.
[<Struct>]
type RDateTime =
    { SecondsSinceEpoch: float
      TimeZone: string option }

module RDateTime =

    open System

    let unixEpoch = DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)

    let toDateTimeUtc d =
        unixEpoch.AddSeconds d.SecondsSinceEpoch

    let fromDateTime(d: DateTime) =
        match d.Kind with
        | DateTimeKind.Utc ->
            { SecondsSinceEpoch = (d - unixEpoch).TotalSeconds
              TimeZone = Some "UTC" }

        | DateTimeKind.Local ->
            let utc = d.ToUniversalTime()
            { SecondsSinceEpoch = (utc - unixEpoch).TotalSeconds
              TimeZone = Some TimeZoneInfo.Local.Id }

        | DateTimeKind.Unspecified ->
            let utc = DateTime.SpecifyKind(d, DateTimeKind.Utc)
            { SecondsSinceEpoch = (utc - unixEpoch).TotalSeconds
              TimeZone = Some "UTC" }

        | _ ->
            failwithf "Unexpected DateTimeKind %A" d.Kind

    let fromSeconds (seconds: float) (tz: string option) =
        { SecondsSinceEpoch = seconds
          TimeZone = tz }