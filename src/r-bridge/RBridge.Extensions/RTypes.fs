namespace RBridge.Extensions

/// An environment in R.
type REnvironment =
    private
    | REnvironment of RBridge.NativeApi.sexp
    member this.Pointer = this |> fun (REnvironment p) -> p

/// Represents an R complex number, which is
/// composed of a real and imaginary component.
[<Struct>]
type RComplex =
    { Real: float
      Imag: float }
    static member Create(real, imaginary) = { Real = real; Imag = imaginary }

/// Represents a date in R, which is based
/// on the unix 1970-01-01 baseline.
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
/// on the unix 1970-01-01 baseline. Internally,
/// R stores date-times as the seconds since the
/// unix baseline, and includes time zone metadata.
[<Struct>]
type RDateTime =
    { SecondsSinceEpoch: float
      TimeZone: string option }

module RDateTime =

    open System

    let unixEpoch = DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)

    let toDateTimeUtc d =
        unixEpoch.AddSeconds d.SecondsSinceEpoch

    /// Create an R date-time from a .NET DateTime, storing
    /// the timezone as metadata on the R representation.
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

    /// Create an RDateTime from the number of seconds elapsed
    /// since 1970-01-01 00:00:00.
    let fromSeconds (seconds: float) (tz: string option) =
        { SecondsSinceEpoch = seconds
          TimeZone = tz }