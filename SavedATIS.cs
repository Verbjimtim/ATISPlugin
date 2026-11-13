using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ATISPlugin
{
    // The last ATIS saved for an airport in VATPAC airspace, from ozserver, so a controller taking over can carry on
    // from it. Each section of its content is one ATIS field, named as in the editor (RWY, APCH, OPR INFO...).
    public class SavedATIS
    {
        internal const string Url = "https://api.ozserver.org/api/v1/vatsim/atis/";

        // Long enough for the API to answer, short enough that a slow one doesn't hold up the runway mode.
        internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

        // Older than this, the runways and procedures are likely to have changed, so it isn't offered.
        public static readonly TimeSpan MaxAge = TimeSpan.FromHours(3);

        internal static readonly HttpClient Client = new HttpClient();

        public string ICAO { get; private set; }
        public char? Letter { get; private set; }
        public Dictionary<string, string> Content { get; private set; } = new Dictionary<string, string>();
        public DateTime? LastUpdatedUtc { get; private set; }

        // The whole text of a saved Zulu, which has no fields; null for a normal ATIS.
        public string Text { get; private set; }

        // Fields marked as changed (+) on the saved ATIS, by name.
        public HashSet<string> Changed { get; private set; } = new HashSet<string>();

        public bool IsCurrent(DateTime utcNow) => LastUpdatedUtc != null && utcNow - LastUpdatedUtc.Value <= MaxAge;

        // A record with no letter or no text (e.g. one left behind with every section null) has nothing to carry on.
        public bool IsUsable => Letter != null && Content.Values.Any(x => !string.IsNullOrWhiteSpace(x));

        // A field's saved value, or empty when the section is blank or missing.
        public string Get(string field) => Content.TryGetValue(field, out var value) ? value ?? string.Empty : string.Empty;

        // ozserver holds the live network's ATISs, so off it (on Sweatbox, a local server or a LAN) nothing is read
        // from or sent to it: a training session mustn't be offered a real ATIS, or leave its own behind. Anything
        // added here that sends to ozserver has to check this too.
        public static bool Available => Plugin.OnLiveNetwork;

        // Null when there's no saved ATIS, the API can't be reached in time, or it isn't available off the live network.
        public static async Task<SavedATIS> Get(string icao, CancellationToken cancel = default)
        {
            if (string.IsNullOrWhiteSpace(icao) || !Available) return null;

            // However ozserver fails (refused, unresolvable, hanging, an error, junk), there's simply no saved ATIS.
            // The request runs off the calling thread, which is the GUI's: the framework resolves the host before
            // the timeout applies, so a dead DNS would otherwise freeze vatSys. And it's given up on at the timeout
            // whatever it's stuck on. Fetch catches everything itself, so one left running can't fault unobserved.
            try
            {
                var timeout = new CancellationTokenSource(Timeout);

                var request = Task.Run(() => Fetch(icao, CancellationTokenSource.CreateLinkedTokenSource(cancel, timeout.Token).Token));

                var done = await Task.WhenAny(request, Task.Delay(Timeout, cancel)).ConfigureAwait(false);

                return done == request ? await request.ConfigureAwait(false) : null;
            }
            catch
            {
                return null;
            }
        }

        private static async Task<SavedATIS> Fetch(string icao, CancellationToken cancel)
        {
            try
            {
                using (var response = await Client.GetAsync(Url + Uri.EscapeDataString(icao), cancel).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode) return null;

                    return Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false), icao);
                }
            }
            catch
            {
                // No saved ATIS to offer; the editor works as it does without one.
                return null;
            }
        }

        // The API answers with the airport's record, or a list of records (empty when nothing is saved for it).
        public static SavedATIS Parse(string json, string icao, DateTime? utcNow = null)
        {
            var token = JToken.Parse(json);

            var items = token is JArray list ? list.OfType<JObject>() : token is JObject single ? new[] { single } : Enumerable.Empty<JObject>();

            var item = items.FirstOrDefault(x => string.Equals((string)x["icao"], icao, StringComparison.OrdinalIgnoreCase));

            if (item == null) return null;

            var output = new SavedATIS { ICAO = icao.ToUpper() };

            var letter = ((string)item["atis_letter"] ?? string.Empty).Trim().ToUpper();

            if (letter.Length == 1 && char.IsLetter(letter[0])) output.Letter = letter[0];

            if (item["content"] is JObject content)
            {
                foreach (var section in content.Properties())
                {
                    var name = section.Name.Trim().ToUpper();

                    var value = section.Value.Type == JTokenType.Null ? null : section.Value.ToString().Trim().ToUpper();

                    // A section name or value starting with + is marked as changed, as vatSys shows it.
                    if (name.StartsWith("+")) { name = name.Substring(1).Trim(); output.Changed.Add(name); }

                    if (value != null && value.StartsWith("+")) { value = value.Substring(1).Trim(); output.Changed.Add(name); }

                    output.Content[name] = value;
                }
            }
            else if (item["content"] is JValue text && text.Type == JTokenType.String)
            {
                // A Zulu has no fields, only its whole text.
                output.Text = ((string)text).Trim();
            }

            // The fields marked as changed, as a list of section names: "changed": ["RWY", "OPR INFO"].
            if (item["changed"] is JArray changed)
            {
                foreach (var name in changed.Select(x => ((string)x ?? string.Empty).Trim().ToUpper()).Where(x => x.Length > 0))
                {
                    output.Changed.Add(name);
                }
            }

            // Called last_updated before, issued now. Either a DDHHMM(Z) time group or a full date.
            var updated = new[] { item["last_updated"], item["issued"] }.FirstOrDefault(x => x != null && x.Type != JTokenType.Null);

            if (updated != null && updated.Type == JTokenType.Date)
            {
                output.LastUpdatedUtc = updated.Value<DateTime>().ToUniversalTime();
            }
            else if (ParseTimeGroup((string)updated, utcNow ?? DateTime.UtcNow) is DateTime group)
            {
                output.LastUpdatedUtc = group;
            }
            else if (DateTime.TryParse((string)updated, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var parsed))
            {
                output.LastUpdatedUtc = parsed;
            }

            return output;
        }

        // A DDHHMMz time group, as on the ATIS ("110750z"), in the latest month it can be in without being ahead of now.
        public static DateTime? ParseTimeGroup(string value, DateTime utcNow)
        {
            value = (value ?? string.Empty).Trim().ToUpper();

            if (value.EndsWith("Z")) value = value.Substring(0, value.Length - 1);

            if (value.Length != 6 || !value.All(char.IsDigit)) return null;

            var day = int.Parse(value.Substring(0, 2));
            var hour = int.Parse(value.Substring(2, 2));
            var minute = int.Parse(value.Substring(4, 2));

            if (day < 1 || day > 31 || hour > 23 || minute > 59) return null;

            // A few minutes ahead is clock difference, not last month.
            var latest = utcNow.AddMinutes(10);

            for (var months = 0; months <= 2; months++)
            {
                var month = new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-months);

                if (day > DateTime.DaysInMonth(month.Year, month.Month)) continue;

                var time = new DateTime(month.Year, month.Month, day, hour, minute, 0, DateTimeKind.Utc);

                if (time <= latest) return time;
            }

            return null;
        }
    }
}
