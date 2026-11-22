# Voice coverage setup

1. Run `001_ServiceCities.sql` in your SQL Server application database using SSMS or sqlcmd. It creates tables and `dbo.ServiceCities_Select`; rerunning preserves data. No database is created and no real coverage decisions are seeded.
2. Populate `dbo.ServiceCities` with the full list of cities you want to recognize, including cities outside coverage (`IsCovered = 0`). Set `IsCovered = 1` only for actual supported cities. `Province` disambiguates duplicate names; multiple matches require clarification. Add spelling alternatives in `dbo.ServiceCityAliases` using the actual city ID.
3. Supply `ConnectionStrings__MainConnection` securely via the process environment. SQL Server must be reachable over an encrypted connection with a trusted server certificate. The runtime SQL user needs EXECUTE on `dbo.ServiceCities_Select`.
4. Start the [local faster-whisper service](../../SpeechService/README.md) and check that `GET /health` returns `{"status":"ready"}`. The API sends audio to this service on loopback by default. Both applications can buffer multipart uploads in temporary files during processing.
5. Start with `dotnet run --project CallCenter --launch-profile http`. Open the `/swagger` path on the running API, select `POST /api/voice-coverage`, click Try it out, choose `audio`, then Execute. Swagger is enabled only in Development.

Accepted extensions: wav, mp3, m4a, mp4, mpeg, mpga, webm; maximum file size 20 MiB. The local Python service decodes the audio; an allowed extension alone does not establish a valid recording. Prefer a short recording containing one explicit city name. React Native clients can send the same multipart field `audio`.

Example successful response (illustrative):

```json
{
  "status": "covered",
  "transcript": "آیا تهران را پوشش می‌دهید؟",
  "isCovered": true,
  "message": "بله، شهر تهران تحت پوشش خدمات ما است.",
  "cities": [{ "id": 1, "name": "تهران", "province": "تهران", "isCovered": true }]
}
```

`covered` / `not_covered` mean one catalog city matched. `city_not_found`, `multiple_cities`, and `no_speech` return `isCovered: null`, never false. Invalid uploads return 400/413. Configuration, database or transcription service errors return an error response, never a coverage decision.

City matching normalizes Persian/Arabic letter variants, diacritics, whitespace and punctuation; it uses whole phrases, aliases and longest matches. It is deterministic catalog matching after local Whisper transcription, not an inference of user intent. A transcription mistake, negated city, or colloquial name absent from aliases can need manual clarification. Unknown cities are not assumed to be outside coverage.

Checks: `dotnet run --project tests/VoiceCoverageChecks` exercises nine matching/service cases with fake transcription and database adapters. For an end-to-end check, run the Python service and API, apply the SQL script, populate the city catalog and upload a Persian recording through Swagger. Compare the returned `transcript` and city match against the expected text and city.
