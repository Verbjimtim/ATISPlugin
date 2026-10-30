using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Media;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using System.Xml.Serialization;
using vatsys;
using vatsys.Plugin;
using Timer = System.Timers.Timer;

namespace ATISPlugin
{
    [Export(typeof(IPlugin))]
    public class Plugin : IPlugin, IDisposable
    {
        public string Name => "ATIS Editor";
        public static string DisplayName => "ATIS Editor";

        private static readonly string RepositoryUrl = "https://raw.githubusercontent.com/badvectors/ATISPlugin/master";

        // Config files live in the profile's Plugins\Configs folder, prefixed ATIS there (e.g. ATISZulu.json) to
        // tell them apart from other plugins' configs. The repository copies, under these names, are only a
        // fallback for profiles that don't have them yet.
        private const string ZuluFile = "Zulu.json";
        private const string CodesFile = "Codes.json";
        private const string PresetsFile = "ATISPresets.xml";
        private const string OFCWFile = "OFCW.json";

        private static readonly HttpClient Client = new HttpClient();

        private static CustomToolStripMenuItem ATISMenu;

        public static ATISControl ATIS1;
        public static ATISControl ATIS2;
        public static ATISControl ATIS3;
        public static ATISControl ATIS4;

        private static EditorWindow Editor;

        public static readonly string ManualVoiceName = "Manual Recording";
        public static string DatasetPath => VatSysDatasetPath;
        public static string ConfigsPath => DatasetPath == null ? null : Path.Combine(DatasetPath, "Plugins", "Configs");

        private static string vatSysDatasetPath;
        private static bool vatSysDatasetPathChecked;

        private static string VatSysDatasetPath
        {
            get
            {
                if (vatSysDatasetPathChecked) return vatSysDatasetPath;

                vatSysDatasetPathChecked = true;

                try
                {
                    var settingsType = typeof(vatsys.ATIS).Assembly.GetType("vatsys.Properties.Settings");
                    var settings = settingsType?.GetProperty("Default")?.GetValue(null);
                    var path = settings == null ? null : settingsType.GetProperty("DatasetPath")?.GetValue(settings) as string;

                    if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path)) vatSysDatasetPath = path;
                }
                catch { }

                return vatSysDatasetPath;
            }
        }
        // Tower and below may only operate a single ATIS.
        public static bool SingleATISOnly
        {
            get
            {
                if (!Network.IsConnected) return false;

                var callsign = Network.Callsign;

                if (string.IsNullOrWhiteSpace(callsign)) return false;

                return callsign.EndsWith("_DEL") || callsign.EndsWith("_GND") || callsign.EndsWith("_TWR");
            }
        }

        public static ATIS ATISData { get; set; }
        public static ATISPresets Presets { get; set; } = new ATISPresets();
        public static List<ZuluInfo> ZuluInfo { get; set; } = new List<ZuluInfo>();
        public static List<OFCWInfo> OFCWInfo { get; set; } = new List<OFCWInfo>();
        public static List<CodeBlock> CodeBlocks { get; set; } = new List<CodeBlock>();

        public static SoundPlayer SoundPlayer { get; set; } = new SoundPlayer();
        private static Timer BroadcastTimer { get; set; } = new Timer();
        private static Timer TimedUpdatesTimer { get; set; } = new Timer();
        public static ConcurrentQueue<ATISAudio> ToBroadcast { get; } = new ConcurrentQueue<ATISAudio>();

        // vatSys encodes bot audio with a single shared Opus encoder that is not
        // thread safe, so only one upload may run at a time. Overlapping uploads
        // corrupt the encoder until vatSys is restarted.
        private static readonly SemaphoreSlim BroadcastLock = new SemaphoreSlim(1, 1);

        public static List<VSCSFrequency> Frequencies { get; set; } = new List<VSCSFrequency>();

        public Plugin()
        {
            // Stay inactive on profiles without an ATIS.xml in their dataset.
            if (DatasetPath == null || !File.Exists(Path.Combine(DatasetPath, "ATIS.xml"))) return;

            vatsys.ATIS.Disable();

            try
            {
                Network.Connected += OnUpdate;
                Network.Disconnected += Network_Disconnected;

                Audio.VSCSFrequenciesChanged += Audio_VSCSFrequenciesChanged;
                Audio.FrequencyErrorStateChanged += Audio_VSCSFrequenciesChanged;
                Network.PrimaryFrequencyChanged += Audio_VSCSFrequenciesChanged;

                ATISMenu = new CustomToolStripMenuItem(CustomToolStripMenuItemWindowType.Main, CustomToolStripMenuItemCategory.Windows, new ToolStripMenuItem(DisplayName));
                ATISMenu.Item.Click += ATISMenu_Click;
                MMI.AddCustomMenuItem(ATISMenu);

                GetData();

                if (ATISData == null)
                {
                    Errors.Add(new Exception("Could not load ATIS data."), DisplayName);
                    return;
                }

                ATIS1 = new ATISControl(1);
                ATIS2 = new ATISControl(2);
                ATIS3 = new ATISControl(3);
                ATIS4 = new ATISControl(4);

                ATIS1.StatusChanged += OnUpdate;
                ATIS2.StatusChanged += OnUpdate;
                ATIS3.StatusChanged += OnUpdate;
                ATIS4.StatusChanged += OnUpdate;

                BroadcastTimer.Elapsed += new ElapsedEventHandler(BroadcastTimer_Elasped);
                BroadcastTimer.Interval = TimeSpan.FromSeconds(5).TotalMilliseconds;
                BroadcastTimer.AutoReset = false;
                BroadcastTimer.Start();

                TimedUpdatesTimer.Elapsed += new ElapsedEventHandler(TimedUpdatesTimer_Elapsed);
                TimedUpdatesTimer.Interval = UntilNextMinute();
                TimedUpdatesTimer.AutoReset = false;
                TimedUpdatesTimer.Start();

                // Only once the ATIS exist to take the METARs.
                MET.Instance.ProductsChanged += METARChanged;
            }
            catch (Exception ex)
            {
                Errors.Add(new Exception(ex.Message), DisplayName);
                if (ex.InnerException != null) Errors.Add(new Exception(ex.InnerException.Message), DisplayName);
            }

            _ = GetZuluInfo();

            _ = GetOFCWInfo();

            _ = GetCodeBlocks();

            _ = GetPresets();
        }

        private static IEnumerable<ATISControl> AllATIS => new[] { ATIS1, ATIS2, ATIS3, ATIS4 }.Where(x => x != null);

        // Handled on the GUI thread, like the timed updates, as it changes the suggestions and can open the editor.
        private void METARChanged(object sender, MET.ProductsChangedEventArgs e)
        {
            try
            {
                MMI.InvokeOnGUI(delegate () { UpdateMetar(e); });
            }
            catch (Exception ex)
            {
                Errors.Add(new Exception($"Could not update ATIS from METAR: {ex.Message}"), DisplayName);
            }
        }

        private void UpdateMetar(MET.ProductsChangedEventArgs e)
        {
            try
            {
                var products = MET.Instance.GetProducts(e.ProductRequest);

                if (products == null || products.Count == 0) return;

                var metar = products[0];

                if (metar.Type != MET.ProductType.VATSIM_METAR || metar.Text == "No product available.") return;

                var atis = GetATIS(metar.Icao);

                if (atis == null) return;

                var updated = atis.UpdateMetar(metar.Text);

                OnMETARUpdate(atis.Number, updated);
            }
            catch (Exception ex)
            {
                Errors.Add(new Exception($"Could not update ATIS from METAR: {ex.Message}"), DisplayName);
            }
        }

        private ATISControl GetATIS(string icao) => AllATIS.FirstOrDefault(x => x.ICAO == icao);

        // Reads a config file from the profile. If the profile doesn't have it, it's downloaded from the
        // repository and saved there. A file already in the profile is always used as is.
        private static async Task<string> GetConfigFile(string fileName)
        {
            var profileFileName = fileName.StartsWith("ATIS") ? fileName : "ATIS" + fileName;

            var path = ConfigsPath == null ? null : Path.Combine(ConfigsPath, profileFileName);

            if (path != null && File.Exists(path)) return File.ReadAllText(path);

            var content = await Client.GetStringAsync($"{RepositoryUrl}/{fileName}");

            try
            {
                // Only save into a profile that exists, rather than creating one.
                if (path != null && Directory.Exists(DatasetPath))
                {
                    Directory.CreateDirectory(ConfigsPath);

                    File.WriteAllText(path, content);
                }
            }
            catch (Exception ex)
            {
                Errors.Add(new Exception($"Could not save {profileFileName} to the profile: {ex.Message}"), DisplayName);
            }

            return content;
        }

        private static async Task GetPresets()
        {
            try
            {
                var content = await GetConfigFile(PresetsFile);

                var serializer = new XmlSerializer(typeof(ATISPresets));

                using (var reader = new StringReader(content))
                {
                    Presets = (ATISPresets)serializer.Deserialize(reader) ?? new ATISPresets();
                }
            }
            catch (Exception ex)
            {
                Errors.Add(new Exception($"Could not load ATIS presets: {ex.InnerException?.Message ?? ex.Message}"), DisplayName);
            }
        }

        private static async Task GetCodeBlocks()
        {
            try
            {
                var response = await GetConfigFile(CodesFile);

                var codeBlocks = JsonConvert.DeserializeObject<CodeBlock[]>(response);

                foreach (var codeBlock in codeBlocks)
                {
                    CodeBlocks.Add(codeBlock);
                }
            }
            catch (Exception ex)
            {
                Errors.Add(new Exception(ex.Message), DisplayName);
            }
        }


        private static async Task GetZuluInfo()
        {
            try
            {
                var response = await GetConfigFile(ZuluFile);

                var zuluInfo = JsonConvert.DeserializeObject<ZuluInfo[]>(response);

                foreach (var info in zuluInfo)
                {
                    ZuluInfo.Add(info);
                }
            }
            catch (Exception ex) 
            {
                Errors.Add(new Exception(ex.Message), DisplayName);
            }
        }

        private static async Task GetOFCWInfo()
        {
            try
            {
                var response = await GetConfigFile(OFCWFile);

                var ofcwInfo = JsonConvert.DeserializeObject<OFCWInfo[]>(response);

                foreach (var info in ofcwInfo)
                {
                    OFCWInfo.Add(info);
                }
            }
            catch (Exception ex)
            {
                Errors.Add(new Exception(ex.Message), DisplayName);
            }
        }

        // Dispatch queued audio without waiting for the next poll, so the
        // broadcast starts close to the time the timecheck was spoken for.
        public static void BroadcastNow()
        {
            BroadcastTimer.Stop();
            BroadcastTimer.Interval = 250;
            BroadcastTimer.Start();
        }

        // An exception escaping an async void handler would take vatSys down, so it catches everything.
        private async void BroadcastTimer_Elasped(object sender, ElapsedEventArgs e)
        {
            try
            {
                // BroadcastNow can fire this handler while an earlier one is still
                // uploading, so wait for it to finish rather than running alongside it.
                await BroadcastLock.WaitAsync();

                try
                {
                    while (ToBroadcast.TryDequeue(out var atb))
                    {
                        if (!Network.IsOfficialServer) continue;

                        if (!Network.GetATISConnected(atb.ATISIndex)) continue;

                        try
                        {
                            await StartVoiceATIS(atb);
                        }
                        catch (Exception ex)
                        {
                            Errors.Add(new Exception($"Could not start voice ATIS: {ex.Message}"), DisplayName);
                        }
                    }
                }
                finally
                {
                    BroadcastLock.Release();
                }
            }
            catch (Exception ex)
            {
                Errors.Add(new Exception($"Could not broadcast ATIS: {ex.Message}"), DisplayName);
            }
            finally
            {
                if (!Disposed)
                {
                    // Audio queued while this handler was finishing must not wait for the slow poll.
                    BroadcastTimer.Interval = ToBroadcast.IsEmpty ? TimeSpan.FromSeconds(5).TotalMilliseconds : 250;

                    BroadcastTimer.Start();
                }
            }
        }

        // SFC COND clearing, windshear expiry and OPR INFO FM times are time based, so check them at the start of
        // every minute (an FM time alerts as that minute begins). The timer runs on a background thread, so the
        // check is done on the GUI thread, which owns the editor and the suggestions.
        private void TimedUpdatesTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                MMI.InvokeOnGUI(delegate () { CheckTimedUpdates(); });
            }
            catch (Exception ex)
            {
                Errors.Add(new Exception($"Could not check ATIS timed updates: {ex.Message}"), DisplayName);
            }
            finally
            {
                if (!Disposed)
                {
                    TimedUpdatesTimer.Interval = UntilNextMinute();
                    TimedUpdatesTimer.Start();
                }
            }
        }

        // Just past the next minute, as a timer can fire a little early.
        private static double UntilNextMinute()
        {
            var now = DateTime.UtcNow;

            var next = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, DateTimeKind.Utc).AddMinutes(1);

            return (next - now).TotalMilliseconds + 250;
        }

        private void CheckTimedUpdates()
        {
            try
            {
                foreach (var atis in AllATIS)
                {
                    if (atis.CheckTimedUpdates(DateTime.UtcNow)) OnMETARUpdate(atis.Number, true);
                }
            }
            catch (Exception ex)
            {
                Errors.Add(new Exception($"Could not check ATIS timed updates: {ex.Message}"), DisplayName);
            }
        }

        private static async Task StartVoiceATIS(ATISAudio atb)
        {
            try
            {
                await AFV.AddOrUpdateATISBot(atb.Audio, atb.ATISIndex, atb.Callsign, atb.Frequency, atb.VisPoint, atb.Duration);
            }
            catch (Exception ex) when (IsEncoderFault(ex) && ResetEncoder())
            {
                // A corrupted encoder fails every upload until vatSys is restarted,
                // so try again on the replacement.
                await AFV.AddOrUpdateATISBot(atb.Audio, atb.ATISIndex, atb.Callsign, atb.Frequency, atb.VisPoint, atb.Duration);
            }
        }

        // Network and validation errors are thrown elsewhere in vatSys, only a
        // corrupted encoder throws from inside the encoding itself.
        private static bool IsEncoderFault(Exception ex)
        {
            return !(ex is TimeoutException) && ex.StackTrace != null && ex.StackTrace.Contains("BotHelper2");
        }

        // Swap the shared Opus encoder in vatSys for a new one. Must only be
        // called while holding the BroadcastLock.
        private static bool ResetEncoder()
        {
            try
            {
                var helper = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(x => x.GetName().Name == "GeoVR.Connection.BotHelper2")?
                    .GetType("GeoVR.Connection.BotHelper2");

                var field = helper?.GetField("opusEncoder", BindingFlags.NonPublic | BindingFlags.Static);

                if (field == null || field.IsInitOnly) return false;

                var constructor = field.FieldType.GetConstructors().FirstOrDefault(x => x.GetParameters().Length == 3);

                if (constructor == null) return false;

                var application = Enum.Parse(constructor.GetParameters()[2].ParameterType, "OPUS_APPLICATION_VOIP");

                field.SetValue(null, constructor.Invoke(new object[] { 48000, 1, application }));

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void Audio_VSCSFrequenciesChanged(object sender, EventArgs e)
        {
            Frequencies.Clear();

            var frequenciesUpdate = false;

            foreach (var frequency in Audio.VSCSFrequencies)
            {
                if (!frequency.Transmit) continue;

                frequenciesUpdate = true;

                Frequencies.Add(frequency);
            }

            if (!IsEditorOpen()) return;

            if (Editor != null && !Editor.IsDisposed && frequenciesUpdate)
            {
                Editor?.RefreshEvent.Invoke(this, null);
            }
        }

        private async void Network_Disconnected(object sender, EventArgs e)
        {
            await DeleteAll();
        }

        // Each ATIS is deleted on its own, so one failing doesn't leave the others connected.
        private static async Task DeleteAll()
        {
            while (ToBroadcast.TryDequeue(out _)) { }

            foreach (var atis in AllATIS.ToList())
            {
                try
                {
                    await atis.Delete();
                }
                catch (Exception ex)
                {
                    Errors.Add(new Exception($"Could not delete ATIS {atis.Number}: {ex.Message}"), DisplayName);
                }
            }
        }

        private void OnUpdate(object sender, EventArgs e)
        {
            if (!IsEditorOpen()) return;

            Editor?.RefreshEvent.Invoke(this, null);
        }

        private static void PlayUpdateSound()
        {
            var sound = Path.Combine(Helpers.GetProgramFolder(), "wav", "AIS.wav");

            SoundPlayer.SoundLocation = sound;

            SoundPlayer.Play();
        }

        private void OnMETARUpdate(int number, bool updated)
        {
            if (!updated)
            {
                if (IsEditorOpen() && Editor.Number == number)
                {
                    Editor?.RefreshEvent.Invoke(null, new RefreshEventArgs(number));
                }

                return;
            }

            if (!IsEditorOpen())
            {
                ShowEditorWindow();

                Editor?.RefreshEvent.Invoke(null, new RefreshEventArgs(number));

                PlayUpdateSound();

                return;
            }
            else
            {
                Editor?.RefreshEvent.Invoke(null, null);

                if (Editor?.Number == number) return;

                PlayUpdateSound();
            }
        }

        private void GetData()
        {
            ATISData = (ATIS)LoadXML(DatasetPath + "\\ATIS.xml", typeof(ATIS));
        }

        public object LoadXML(string filePath, Type type)
        {
            if (!File.Exists(filePath)) return null;

            var fileContents = File.ReadAllText(filePath);

            var serializer = new XmlSerializer(type);

            using (StringReader reader = new StringReader(fileContents))
            {
                return serializer.Deserialize(reader);
            }
        }

        private void ATISMenu_Click(object sender, EventArgs e)
        {
            ShowEditorWindow();
        }

        private static bool IsEditorOpen() => Editor != null && !Editor.IsDisposed && Editor.Visible;

        private static void ShowEditorWindow()
        {
            if (ATISData == null)
            {
                Errors.Add(new Exception("Plugin was not started due to missing data."), DisplayName);
                return;
            }

            if (Editor == null || Editor.IsDisposed)
            {
                Editor = new EditorWindow();
            }

            var mainForm = Application.OpenForms["MainForm"];

            if (mainForm == null) return;

            try
            {
                MMI.InvokeOnGUI(delegate ()
                {
                    Editor.Show(mainForm);

                    // Connection state may have changed while the window was closed.
                    Editor.RefreshEvent?.Invoke(null, null);
                });
            }
            catch { }
        }

        public void OnFDRUpdate(FDP2.FDR updated)
        {
            return;
        }

        public void OnRadarTrackUpdate(RDP.RadarTrack updated)
        {
            return;
        }

        private static bool Disposed { get; set; }

        public void Dispose()
        {
            Disposed = true;

            try
            {
                MET.Instance.ProductsChanged -= METARChanged;

                BroadcastTimer.Stop();
                BroadcastTimer.Dispose();

                TimedUpdatesTimer.Stop();
                TimedUpdatesTimer.Dispose();
            }
            catch { }

            _ = DeleteAll();
        }
    }
}
