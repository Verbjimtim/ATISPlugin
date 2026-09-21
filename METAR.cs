using System.Text.RegularExpressions;
using System;

namespace ATISPlugin
{
    public class METAR
    {
        public string ICAO { get; set; }
        public DateTime ProductTime { get; set; } = DateTime.MaxValue;
        public string Wind { get; set; }
        public string Visibility { get; set; }
        public string Cloud { get; set; }
        public string Weather { get; set; }
        public string Temperature { get; set; }
        public string DewPoint { get; set; }
        public string QNH { get; set; }

        public METAR Process(string metar)
        {
            if (metar.Length < 4) return null;

            ICAO = metar.Substring(0, 4);

            string[] components = Regex.Replace(metar.Substring(5), "\\t|\\n|\\r", "").Split(' ');

            foreach (var item in components)
            {
                if (item == "RMK") break;

                if (item == "AUTO") continue;

                if (item == "CAVOK") 
                {
                    Weather = "CAVOK";
                    continue;
                }

                if (item == "//") // weather
                {
                    // Weather = "WEATHER NAVBL";
                    continue;
                }

                if (item == "////") // visibility,
                {
                    Visibility = "NAVBL";
                    continue;
                }

                if (item == "//////") // cloud
                {
                    Cloud = "NAVBL";
                    continue;
                }

                // TIME
                if (Regex.IsMatch(item, "^\\d{6}Z$"))
                {
                    ProductTime = new DateTime(1, 1, int.Parse(item.Substring(0, 2)), int.Parse(item.Substring(2, 2)), int.Parse(item.Substring(4, 2)), 0);
                    continue;
                }

                // WIND
                if (Regex.IsMatch(item, "^(\\d{3}|VRB)\\d{2}(KT|MPS|KPH)$") || Regex.IsMatch(item, "^(\\d{3}|VRB)\\d{2}G\\d{2,3}(KT|MPS|KPH)$"))
                {
                    var wind = item.Replace("KT", "").Replace("MPS", "").Replace("KPH", "");

                    if (wind == "00000")
                    {
                        Wind = "CALM";

                        continue;
                    }

                    if (wind.EndsWith("01") || wind.EndsWith("02") || wind.EndsWith("03"))
                    {
                        Wind = "LIGHT AND VRB";

                        continue;
                    }

                    var direction = wind.Substring(0, 3);

                    var speed = wind.Substring(3, 2).TrimStart('0');

                    Wind = $"{direction}/{speed}";

                    var gust = wind.Split('G');

                    if (gust.Length > 1) Wind += $"-{gust[1]}";

                    continue;
                }

                // VARIABLE WIND
                if (Wind != "CALM" && Regex.IsMatch(item, "^\\d{3}V\\d{3}$"))
                {
                    if (Wind == null) continue;

                    string[] wind = Wind.Split('/');

                    string[] variable = item.Split('V');

                    if (wind.Length == 2 && variable.Length == 2)
                    {
                        Wind = variable[0] + "-" + variable[1] + "/" + wind[1];
                    }
                    else
                    {
                        Wind = Wind + " " + item;
                    }

                    continue;
                }

                // WEATHER
                if (Regex.IsMatch(item, "^(\\+|\\-){0,1}([A-Z]{2}){1,3}$"))
                {
                    if (Weather != null) Weather += " ";

                    Weather += item;

                    continue;
                }

                // VISIBILITY
                if (Regex.IsMatch(item, "^(\\d\\/\\d+|\\d{1,2})(KM|SM)$"))
                {
                    Visibility = item;

                    continue;
                }

                if (Regex.IsMatch(item, "^\\d{4}$"))
                {
                    if (item == "9999")
                    {
                        Visibility = "GT 10KM";
                        continue;
                    }

                    var visOk = int.TryParse(item, out int visibility);

                    if (!visOk) continue;

                    Visibility = visibility <= 5000 ? $"{visibility}M" : $"{visibility / 1000}KM";

                    continue;
                }

                // CLOUD
                var cloud = Regex.Match(item, "^(FEW|SCT|BKN|OVC)\\d{3}(CB|TCU){0,1}$");

                if (cloud.Success)
                {
                    var levelOk = int.TryParse(item.Substring(3, 3), out int level);

                    if (!levelOk) continue;

                    if (level > 50) continue; // TODO: Should be or below MSA + add CB and TCU to ATIS voice.

                    if (Cloud != null) Cloud += ", ";

                    Cloud += item;

                    continue;
                }

                // TEMP/ DEP
                if (Regex.IsMatch(item, "^M{0,1}\\d{2}\\/M{0,1}\\d{2}$"))
                {
                    Temperature = item[0] != '0' ? item.Substring(0, 2) : item.Substring(1, 1);

                    DewPoint = item[3] != '0' ? item.Substring(3) : item.Substring(4);

                    continue;
                }

                // QNH
                if (Regex.IsMatch(item, "^(Q|A)\\d{4}$"))
                {
                    QNH = item.Substring(1).TrimStart('0');

                    continue;
                }
            }

            if (Cloud == null && (Visibility == "GT 10KM" || Visibility == null) && Weather == null)
            {
                Visibility = null;
                Weather = "CAVOK";
            }

            return this;
        }

        public string GetField(METARField field)
        {
            switch (field)
            {
                case METARField.None:
                    return null;
                case METARField.Wind:
                    return Wind;
                case METARField.Visibility:
                    return Visibility;
                case METARField.Cloud:
                    return Cloud;
                case METARField.Weather:
                    return Weather;
                case METARField.Temperature:
                    return Temperature;
                case METARField.DewPoint:
                    return DewPoint;
                case METARField.QNH:
                    return QNH;
                default:
                    return null;
            }
        }
    }

    public enum METARField
    {
        None,
        Wind,
        Visibility,
        Cloud,
        Weather,
        Temperature,
        DewPoint,
        QNH,
    }
}