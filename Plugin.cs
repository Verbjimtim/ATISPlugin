using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Media;
using System.Net.Http;
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

        public static readonly Version Version = new Version(3, 19);
        private static readonly string RepositoryUrl = "https://raw.githubusercontent.com/badvectors/ATISPlugin/master";
        private static readonly string VersionUrl = $"{RepositoryUrl}/Version.json";

        // Config files live in the profile's Plugins\Configs folder. The repository copies are only a fallback
        // for profiles that don't have them yet.
        private const string ZuluFile = "Zulu.json";
        private const string CodesFile = "Codes.json";
        private const string PresetsFile = "ATISPresets.xml";

        private static readonly HttpClient Client = new HttpClient();

        private static CustomToolStripMenuItem ATISMenu;

        public static ATISControl ATIS1;
        public static ATISControl ATIS2;
        public static ATISControl ATIS3;
        public static ATISControl ATIS4;

        private static EditorWindow Editor;

        public static string ProfileName()
        {
            if (Profile.Name.Contains("Australia")) return "Australia";
            else if (Profile.Name.Contains("Pacific")) return "Pacific";
            else if (Profile.Name.Contains("VATNZ")) return "New Zealand";
            else return string.Empty;
        }
        public static readonly string ManualVoiceName = "Manual Recording";
        public static string DatasetPath => Path.Combine(Helpers.GetFilesFolder(), "Profiles", ProfileName());
        public static string ConfigsPath => Path.Combine(DatasetPath, "Plugins", "Configs");
        public static ATIS ATISData { get; set; }
        public static ATISPresets Presets { get; set; } = new ATISPresets();
        public static List<ZuluInfo> ZuluInfo { get; set; } = new List<ZuluInfo>();
        public static List<CodeBlock> CodeBlocks { get; set; } = new List<CodeBlock>();

        public static SoundPlayer SoundPlayer { get; set; } = new SoundPlayer();
        private static Timer BroadcastTimer { get; set; } = new Timer();
        private static Timer TimedUpdatesTimer { get; set; } = new Timer();
        public static List<ATISAudio> ToBroadcast { get; set; } = new List<ATISAudio>();

        public static List<VSCSFrequency> Frequencies { get; set; } = new List<VSCSFrequency>();

        public Plugin()
        {
            if (!Profile.Name.Contains("Australia") && !Profile.Name.Contains("Pacific") && !Profile.Name.Contains("VATNZ"))
            {
                return;
            }
            
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

                if (!Directory.Exists(DatasetPath))
                {
                    Errors.Add(new Exception("Could not find profile."), DisplayName);
                    return;
                }

                GetData();

                if (ATISData == null)
                {
                    Errors.Add(new Exception("Could not load ATIS data."), DisplayName);
                    return;
                }

                var directory = Path.Combine(DatasetPath, "Temp");

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var toDelete = Directory
                    .EnumerateFiles(directory, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(s => Path.GetExtension(s).TrimStart('.').ToLowerInvariant() == "wav");

                foreach (var file in toDelete)
                {
                    if (file.Contains("AIS.wav")) continue;
                    File.Delete(file);
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
                TimedUpdatesTimer.Interval = TimeSpan.FromMinutes(1).TotalMilliseconds;
                TimedUpdatesTimer.AutoReset = true;
                TimedUpdatesTimer.Start();
            }
            catch (Exception ex)
            {
                Errors.Add(new Exception(ex.Message), DisplayName);
                if (ex.InnerException != null) Errors.Add(new Exception(ex.InnerException.Message), DisplayName);
            }

            _ = GetZuluInfo();

            _ = GetCodeBlocks();

            _ = GetPresets();

            _ = CheckVersion();

            MET.Instance.ProductsChanged += METARChanged;
        }

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

        private ATISControl GetATIS(string icao)
        {
            if (ATIS1.ICAO == icao) return ATIS1;
            if (ATIS2.ICAO == icao) return ATIS2;
            if (ATIS3.ICAO == icao) return ATIS3;
            if (ATIS4.ICAO == icao) return ATIS4;
            return null;
        }

        private static async Task CheckVersion()
        {
            try
            {
                var response = await Client.GetStringAsync(VersionUrl);

                var version = JsonConvert.DeserializeObject<Version>(response);

                if (version.Major == Version.Major && version.Minor == Version.Minor) return;

                Errors.Add(new Exception("A new version of the plugin is available."), DisplayName);
            }
            catch { }
        }

        // Reads a config file from the profile. If the profile doesn't have it, it's downloaded from the
        // repository and saved there. A file already in the profile is always used as is.
        private static async Task<string> GetConfigFile(string fileName)
        {
            var path = Path.Combine(ConfigsPath, fileName);

            if (File.Exists(path)) return File.ReadAllText(path);

            var content = await Client.GetStringAsync($"{RepositoryUrl}/{fileName}");

            try
            {
                // Only save into a profile that exists, rather than creating one.
                if (Directory.Exists(DatasetPath))
                {
                    Directory.CreateDirectory(ConfigsPath);

                    File.WriteAllText(path, content);
                }
            }
            catch (Exception ex)
            {
                Errors.Add(new Exception($"Could not save {fileName} to the profile: {ex.Message}"), DisplayName);
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

        private async void BroadcastTimer_Elasped(object sender, ElapsedEventArgs e)
        {
            var toBroadcast = ToBroadcast.ToList();

            foreach (var atb in toBroadcast)
            {
                var atis = ToBroadcast.FirstOrDefault(x => x.Id == atb.Id);

                if (atis != null) ToBroadcast.Remove(atis);

                if (!Network.IsOfficialServer) continue;

                if (!Network.GetATISConnected(atb.ATISIndex)) continue;

                try
                {
                    await AFV.AddOrUpdateATISBot(atb.Audio, atb.ATISIndex, atb.Callsign, atb.Frequency, atb.VisPoint, atb.Duration);
                }
                catch (Exception ex)
                {
                    Errors.Add(new Exception($"Could not start voice ATIS: {ex.Message}"), DisplayName);
                }
            }

            BroadcastTimer.Start();
        }

        // SFC COND clearing and windshear expiry are time based, so check them every minute. The timer runs on a
        // background thread, so the check is done on the GUI thread, which owns the editor and the suggestions.
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
        }

        private void CheckTimedUpdates()
        {
            try
            {
                foreach (var atis in new[] { ATIS1, ATIS2, ATIS3, ATIS4 })
                {
                    if (atis == null) continue;

                    if (atis.CheckTimedUpdates(DateTime.UtcNow)) OnMETARUpdate(atis.Number, true);
                }
            }
            catch (Exception ex)
            {
                Errors.Add(new Exception($"Could not check ATIS timed updates: {ex.Message}"), DisplayName);
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
            ToBroadcast.Clear();
            
            await ATIS1?.Delete();
            await ATIS2?.Delete();
            await ATIS3?.Delete();
            await ATIS4?.Delete();
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
                MMI.InvokeOnGUI(delegate () { Editor.Show(mainForm); });
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

        public void Dispose()
        {
            Network_Disconnected(this, null);
        }
    }
}
