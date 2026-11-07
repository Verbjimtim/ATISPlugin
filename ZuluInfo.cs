using System.Collections.Generic;

namespace ATISPlugin
{
    public class ZuluInfo
    {
        public string ICAO { get; set; }
        public string Text { get; set; }
        public string StartUtc { get; set; }
        public string EndUtc { get; set; }

        // Who pilots are told to contact, best first. The first one online fills {STATION} {FREQ}.
        public List<ZuluContact> Contacts { get; set; } = new List<ZuluContact>();
    }

    public class ZuluContact
    {
        // The controller's network callsign, e.g. CB_APP. A * matches anything, e.g. ML-BIK*_CTR.
        public string Callsign { get; set; }

        // How it's said on the ATIS, e.g. CANBERRA APPROACH.
        public string Station { get; set; }
    }
}
