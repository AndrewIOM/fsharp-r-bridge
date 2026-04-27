module ExtractTests

open Expecto
open RBridge.Extensions
open SExpTests


let dateTimeTrip timezone =
    let globalEnv = REnvironment.globalEnv engine.Value
    let d = Expect.wantOk (Evaluate.tryEval (sprintf "as.POSIXct('2000-03-12 15:30:45', tz='%s')" timezone) globalEnv engine.Value) "Could not make date in R"
    let extr = Extract.extractDateTimeArray engine.Value d
    let expectedNet =
        System.DateTime(2000, 3, 12, 15, 30, 45, System.DateTimeKind.Utc)

    let expected =
        let seconds = (expectedNet - System.DateTime(1970,1,1,0,0,0, System.DateTimeKind.Utc)).TotalSeconds
        RDateTime.fromSeconds seconds (Some timezone)

    let extr0 = Expect.wantSome extr.[0] "Date time was NA"
    Expect.equal extr0.SecondsSinceEpoch expected.SecondsSinceEpoch "Seconds since epoch mismatch"
    Expect.equal extr0.TimeZone expected.TimeZone "Timezone mismatch"

    let netDate = extr0 |> RDateTime.toDateTimeUtc
    Expect.equal netDate expectedNet "DateTime conversion to .NET was not correct"


[<Tests>]
let dates =
    testList "Dates and times" [

        testCase "Date round trip" <| fun _ ->
            let globalEnv = REnvironment.globalEnv engine.Value
            let d = Expect.wantOk (Evaluate.tryEval "as.Date('2000-03-12')" globalEnv engine.Value) "Could not make date in R"
            let extr = Extract.extractDateArray engine.Value d
            let expectedNet = System.DateOnly(2000,3,12)
            let expected =
                let epoch = System.DateOnly(1970,1,1)
                let days = expectedNet.DayNumber - epoch.DayNumber
                RDate.fromDaysSinceEpoch days

            Expect.hasLength extr 1 "Should only be one date"
            Expect.equal extr.[0] (Some expected) ""

            let netDate = extr.[0] |> Option.map RDate.toDateOnly
            Expect.equal netDate (Some <| System.DateOnly(2000,03,12)) "Date conversion to .NET was not correct"

        testCase "Date time round trip" <| fun _ ->
            dateTimeTrip "UTC"

        testCase "Date time round trip (non-UTC)" <| fun _ ->
            dateTimeTrip "Europe/London"

        testCase "Round-trip DateOnly to RDate" <| fun _ ->
            let d = System.DateOnly(1999, 12, 31)
            let r = RDate.fromDateOnly d
            let back = RDate.toDateOnly r
            Expect.equal r.DaysSinceEpoch (d.DayNumber - RDate.unixEpochDayNumber) "Day offset incorrect"
            Expect.equal back d "Round-trip failed"

    ]