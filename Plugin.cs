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

        private static readonly string ZuluUrl = "https://raw.githubusercontent.com/badvectors/ATISPlugin/master/Zulu.json";
        private static readonly string CodesUrl = "https://raw.githubusercontent.com/badvectors/ATISPlugin/master/Codes.json";
        private static readonly string PresetsUrl = "https://raw.githubusercontent.com/badvectors/ATISPlugin/master/Presets.json";
        private static readonly string OFCWUrl = "https://raw.githubusercontent.com/badvectors/ATISPlugin/master/OFCW.json";

        private static readonly HttpClient Client = new HttpClient();

        private static CustomToolStripMenuItem ATISMenu;

        public static ATISControl ATIS1;
        public static ATISControl ATIS2;
        public static ATISControl ATIS3;
        public static ATISControl ATIS4;

        private static EditorWindow Editor;

        public static readonly string ManualVoiceName = "Manual Recording";
        public static string DatasetPath => VatSysDatasetPath;

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
        public static List<ZuluInfo> ZuluInfo { get; set; } = new List<ZuluInfo>();
        public static List<OFCWInfo> OFCWInfo { get; set; } = new List<OFCWInfo>();
        public static List<CodeBlock> CodeBlocks { get; set; } = new List<CodeBlock>();
        public static List<PresetOption> PresetOptions { get; set; } = new List<PresetOption>();

        public static SoundPlayer SoundPlayer { get; set; } = new SoundPlayer();
        private static Timer BroadcastTimer { get; set; } = new Timer();
        public static ConcurrentQueue<ATISAudio> ToBroadcast { get; } = new ConcurrentQueue<ATISAudio>();

        // vatSys encodes bot audio with a single shared Opus encoder that is not
        // thread safe, so only one upload may run at a time. Overlapping uploads
        // corrupt the encoder until vatSys is restarted.
        private static readonly SemaphoreSlim BroadcastLock = new SemaphoreSlim(1, 1);

        public static List<VSCSFrequency> Frequencies { get; set; } = new List<VSCSFrequency>();

        public Plugin()
        {
            // Stay inactive on profiles without an ATIS.xml in their dataset.
            if (!File.Exists(Path.Combine(DatasetPath, "ATIS.xml"))) return;

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
            }
            catch (Exception ex)
            {
                Errors.Add(new Exception(ex.Message), DisplayName);
                if (ex.InnerException != null) Errors.Add(new Exception(ex.InnerException.Message), DisplayName);
            }

            _ = GetZuluInfo();

            _ = GetOFCWInfo();

            _ = GetCodeBlocks();

            _ = GetPresetOptions();

            MET.Instance.ProductsChanged += METARChanged;
        }

        private void METARChanged(object sender, MET.ProductsChangedEventArgs e)
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

        private ATISControl GetATIS(string icao)
        {
            if (ATIS1.ICAO == icao) return ATIS1;
            if (ATIS2.ICAO == icao) return ATIS2;
            if (ATIS3.ICAO == icao) return ATIS3;
            if (ATIS4.ICAO == icao) return ATIS4;
            return null;
        }

        private static async Task GetCodeBlocks()
        {
            try
            {
                var response = await Client.GetStringAsync(CodesUrl);

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

        private static async Task GetPresetOptions()
        {
            try
            {
                var response = await Client.GetStringAsync(PresetsUrl);

                var presetOptions = JsonConvert.DeserializeObject<PresetOption[]>(response);

                foreach (var presetOption in presetOptions)
                {
                    PresetOptions.Add(presetOption);
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
                var response = await Client.GetStringAsync(ZuluUrl);

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
                var response = await Client.GetStringAsync(OFCWUrl);

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

        private async void BroadcastTimer_Elasped(object sender, ElapsedEventArgs e)
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

            // Audio queued while this handler was finishing must not wait for the slow poll.
            BroadcastTimer.Interval = ToBroadcast.IsEmpty ? TimeSpan.FromSeconds(5).TotalMilliseconds : 250;

            BroadcastTimer.Start();
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
            while (ToBroadcast.TryDequeue(out _)) { }
            
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

        public void Dispose()
        {
            Network_Disconnected(this, null);
        }
    }
}
