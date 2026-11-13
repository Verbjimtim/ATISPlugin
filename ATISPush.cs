using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ATISPlugin
{
    // Sends ATISs to ozserver as they go live (PUT), at the same address they're read from. Each push replaces the
    // airport's last one, so nothing is sent when an ATIS goes. ozserver doesn't check what it's sent, so
    // ATISControl only sends what it has checked itself. Fire and forget, as with reading: whatever happens to a
    // request, nothing is shown or retried.
    internal static class ATISPush
    {
        public static void Put(string icao, JObject body) => Send(HttpMethod.Put, icao, body);

        // Who's sending.
        public static JObject Client => new JObject
        {
            ["name"] = "vatSys ATIS Plugin",
            ["version"] = typeof(ATISPush).Assembly.GetName().Version.ToString(2)
        };

        private static void Send(HttpMethod method, string icao, JObject body)
        {
            var url = SavedATIS.Url + Uri.EscapeDataString(icao);

            var json = body.ToString(Formatting.None);

            // Off the calling thread, which is the GUI's, as the host is resolved before the timeout applies.
            Task.Run(async () =>
            {
                try
                {
                    using (var timeout = new CancellationTokenSource(SavedATIS.Timeout))
                    using (var request = new HttpRequestMessage(method, url) { Content = new StringContent(json, Encoding.UTF8, "application/json") })
                    using (await SavedATIS.Client.SendAsync(request, timeout.Token).ConfigureAwait(false)) { }
                }
                catch
                {
                    // ozserver down or refusing: the ATIS is on the network all the same.
                }
            });
        }
    }
}
