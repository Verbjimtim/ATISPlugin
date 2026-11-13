using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ATISPlugin
{
    // Another airport's current ATIS, for presets that depend on it (e.g. YPJT's wake turbulence caution while YPPH
    // uses runway 03). It comes from this plugin when the controller holds that ATIS too, otherwise from ozserver,
    // where every plugin ATIS is pushed as it goes live. When neither has it, it's unknown, and whatever depends on
    // it falls back (to its hours) or stays as it is: nothing is changed on a guess.
    public class LinkedATIS
    {
        public string ICAO { get; private set; }
        public char? Letter { get; private set; }
        public Dictionary<string, string> Fields { get; private set; } = new Dictionary<string, string>();

        // Where it came from, to say why something was suggested.
        public string Source { get; private set; }

        public string Get(string field) => field != null && Fields.TryGetValue(field.Trim().ToUpper(), out var value) ? value ?? string.Empty : string.Empty;

        // From ozserver, by airport: the last answer (null when it had nothing current), until the next refresh.
        private static readonly ConcurrentDictionary<string, LinkedATIS> Fetched = new ConcurrentDictionary<string, LinkedATIS>(StringComparer.OrdinalIgnoreCase);

        private static readonly ConcurrentDictionary<string, bool> Fetching = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        // Set by the plugin: the ATISs it holds, to read before asking ozserver.
        public static Func<IEnumerable<ATISControl>> Held { get; set; } = () => Enumerable.Empty<ATISControl>();

        // The airport's ATIS as it stands, or null when it isn't known.
        public static LinkedATIS Find(string icao)
        {
            if (string.IsNullOrWhiteSpace(icao)) return null;

            icao = icao.Trim().ToUpper();

            try
            {
                var held = Held().FirstOrDefault(x => string.Equals(x.ICAO, icao, StringComparison.OrdinalIgnoreCase) && !x.IsZulu);

                var fromPlugin = held == null ? null : FromControl(held);

                if (fromPlugin != null) return fromPlugin;
            }
            catch
            {
                // Read from ozserver instead.
            }

            return OnLiveNetwork() && Fetched.TryGetValue(icao, out var fetched) ? fetched : null;
        }

        private static bool OnLiveNetwork()
        {
            try
            {
                return SavedATIS.Available;
            }
            catch
            {
                return false;
            }
        }

        private static LinkedATIS FromControl(ATISControl control)
        {
            // Only what's been saved: an edit or suggestion not yet saved isn't the ATIS.
            var fields = control.Lines.Where(x => !string.IsNullOrWhiteSpace(x.Name) && x.Name != "ZULU")
                .GroupBy(x => x.Name.Trim().ToUpper())
                .ToDictionary(x => x.Key, x => x.First().Value ?? string.Empty);

            if (!fields.Values.Any(x => !string.IsNullOrWhiteSpace(x))) return null;

            return new LinkedATIS { ICAO = control.ICAO.ToUpper(), Letter = control.ID, Fields = fields, Source = "this plugin" };
        }

        public static LinkedATIS FromSaved(SavedATIS saved, DateTime utcNow)
        {
            // A Zulu has no fields to go by, and an old one may not be what's in use now.
            if (saved == null || !saved.IsCurrent(utcNow) || !saved.IsUsable) return null;

            return new LinkedATIS
            {
                ICAO = saved.ICAO,
                Letter = saved.Letter,
                Fields = saved.Content.ToDictionary(x => x.Key, x => x.Value ?? string.Empty),
                Source = "ozserver"
            };
        }

        // Asks ozserver for each airport not held here. Silent, as with every ozserver request: one that fails
        // leaves the airport unknown. Calls back (off the GUI thread) once any answer has changed.
        public static void Refresh(IEnumerable<string> airports, Action changed)
        {
            if (!OnLiveNetwork()) return;

            foreach (var icao in airports.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim().ToUpper()).Distinct())
            {
                bool heldHere;

                try
                {
                    heldHere = Held().Any(x => string.Equals(x.ICAO, icao, StringComparison.OrdinalIgnoreCase) && !x.IsZulu);
                }
                catch
                {
                    heldHere = false;
                }

                if (heldHere || !Fetching.TryAdd(icao, true)) continue;

                Task.Run(async () =>
                {
                    try
                    {
                        var linked = FromSaved(await SavedATIS.Get(icao).ConfigureAwait(false), DateTime.UtcNow);

                        Fetched.TryGetValue(icao, out var previous);

                        Fetched[icao] = linked;

                        if (Describe(previous) != Describe(linked)) changed?.Invoke();
                    }
                    catch
                    {
                        // Unknown until the next refresh.
                    }
                    finally
                    {
                        Fetching.TryRemove(icao, out _);
                    }
                });
            }
        }

        private static string Describe(LinkedATIS atis) => atis == null ? string.Empty
            : $"{atis.Letter}|{string.Join("|", atis.Fields.OrderBy(x => x.Key).Select(x => $"{x.Key}={x.Value}"))}";

        // For tests.
        public static void SetFetched(string icao, LinkedATIS atis) => Fetched[icao] = atis;

        public static LinkedATIS Create(string icao, char? letter, Dictionary<string, string> fields, string source = "test") =>
            new LinkedATIS { ICAO = icao, Letter = letter, Fields = fields, Source = source };
    }

    // A preset condition on another airport's ATIS: Airport="YPPH" AirportField="RWY" Contains="03".
    public static class AirportCondition
    {
        public static bool IsSet(string airport) => !string.IsNullOrWhiteSpace(airport);

        // True or false when the airport's ATIS is known, null when it isn't (or the condition is incomplete).
        // Contains lists alternatives separated by |, each matched as whole words, so "03" isn't found in "1030".
        public static bool? Check(string airport, string field, string contains)
        {
            if (!IsSet(airport) || string.IsNullOrWhiteSpace(field) || string.IsNullOrWhiteSpace(contains)) return null;

            var atis = LinkedATIS.Find(airport);

            if (atis == null) return null;

            return Matches(atis.Get(field), contains);
        }

        public static bool Matches(string value, string contains)
        {
            value = (value ?? string.Empty).ToUpper();

            return (contains ?? string.Empty).Split('|').Select(x => x.Trim().ToUpper()).Where(x => x.Length > 0)
                .Any(x => Regex.IsMatch(value, $"(?<![A-Z0-9]){Regex.Escape(x)}(?![A-Z0-9])"));
        }

        // e.g. "YPPH ATIS K (ozserver): RWY 03 FOR ARR"
        public static string Reason(string airport, string field)
        {
            var atis = LinkedATIS.Find(airport);

            if (atis == null) return null;

            return $"{atis.ICAO} ATIS {atis.Letter} ({atis.Source}): {field.Trim().ToUpper()} {atis.Get(field)}".Trim();
        }
    }
}
