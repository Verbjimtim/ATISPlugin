using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using System.Windows.Forms;
using vatsys;
using VATSYSControls;
using static vatsys.Airspace2;

namespace ATISPlugin
{
    public partial class EditorWindow : BaseForm
    {
        private ATISControl Control
        {   
            get
            {
                if (Number == 1) return Plugin.ATIS1;
                else if (Number == 2) return Plugin.ATIS2;
                else if (Number == 3) return Plugin.ATIS3;
                else return Plugin.ATIS4;
            }
        }
        private string ICAO { get; set; }
        private char ID { get; set; }
        private string DisplayName => Airport == null ? ICAO : $"{ICAO} - {Airport.FullName}";
        private Airspace2.Airport Airport => Airspace2.GetAirport(ICAO);
        public int Number { get; private set; } = 1;
        private Dictionary<string, string> Saves { get; set; } = new Dictionary<string, string>();
        private bool Edits => Saves.Any() || 
            TimeCheck != Control.TimeCheck || 
            ID != Control.ID || 
            Control.PromptRate != Rate || 
            Control.VoiceName != VoiceName;
        private bool TimeCheck { get; set; }
        public EventHandler<RefreshEventArgs> RefreshEvent { get; set; }
        public string VoiceName { get; set; }
        public PromptRate Rate { get; set; } 
        private string ZuluFrequency { get; set; }
        public bool CanRecord => Edits == false && Control.Broadcasting == false ? true : false;
        private bool Initialized { get; set; }

        public EditorWindow()
        {
            InitializeComponent();

            BackColor = Colours.GetColour(Colours.Identities.WindowBackground);
            ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);

            ComboBoxTimeCheck.BackColor = Colours.GetColour(Colours.Identities.WindowBackground);
            ComboBoxTimeCheck.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            ComboBoxTimeCheck.FocusColor = Colours.GetColour(Colours.Identities.HighlightedText);
            ComboBoxTimeCheck.Font = MMI.eurofont_winsml;

            ComboBoxAirport.BackColor = Colours.GetColour(Colours.Identities.WindowBackground);
            ComboBoxAirport.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            ComboBoxAirport.FocusColor = Colours.GetColour(Colours.Identities.HighlightedText);
            ComboBoxAirport.Font = MMI.eurofont_winsml;

            ComboBoxLetter.BackColor = Colours.GetColour(Colours.Identities.WindowBackground);
            ComboBoxLetter.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            ComboBoxLetter.FocusColor = Colours.GetColour(Colours.Identities.HighlightedText);
            ComboBoxLetter.Font = MMI.eurofont_winsml;

            Initialized = true;

            RefreshEvent += OnRefreshEvent;
        }

        public void Change(int number)
        {
            try
            {
                Number = number;

                ICAO = Control.ICAO;

                ID = Control.ID;

                TimeCheck = Control.TimeCheck;

                Rate = Control.PromptRate;

                VoiceName = Control.VoiceName;

                Saves.Clear();

                LoadOptions();

                RefreshForm_ClearWindCalculator();

                RefreshForm();
            }
            catch { }
        }

        private void OnRefreshEvent(object sender, RefreshEventArgs e)
        {
            if (!IsHandleCreated)
            {
                CreateHandle();
            }

            if (e != null && e.Number != Number)
            {
                Change(Number);

                return;
            }

            RefreshForm();
        }

        private void EditorWindow_Load(object sender, EventArgs e)
        {
            Change(1);
        }

        private void LoadRunways()
        {
            ComboBoxRunway.Items.Clear();

            ComboBoxRunway.Items.Add("");

            if (ICAO == null) return;

            var airport = Airspace2.GetAirport(ICAO);

            if (airport != null)
            {
                foreach (var runway in airport.Runways.OrderBy(x => x.Name))
                {
                    ComboBoxRunway.Items.Add($"RWY {runway.Name.ToString().PadLeft(2, '0')}");
                }
            }
            else
            {
                for (int i = 1; i <= 36; i++)
                {
                    ComboBoxRunway.Items.Add($"RWY {i.ToString().PadLeft(2, '0')}");
                }
            }
        }

        private void LoadCodes()
        {
            ComboBoxLetter.Items.Clear();

            var codeBlock = Plugin.CodeBlocks.FirstOrDefault(x => x.ICAO == ICAO);

            if (codeBlock == null)
            {
                for (char c = 'A'; c <= 'Y'; c++)
                {
                    ComboBoxLetter.Items.Add(c.ToString());
                }
            }
            else
            {
                var codes = codeBlock.Codes.Split(',');
                foreach (var code in codes)
                {
                    ComboBoxLetter.Items.Add(code);
                }
            }

            if (ID == 'Z')
            {
                ComboBoxLetter.SelectedIndex = 0;

                return;
            }

            var codeValid = ComboBoxLetter.Items.IndexOf(ID.ToString()) != -1;

            if (codeValid) return;

            ComboBoxLetter.SelectedIndex = 0;
        }

        private void LoadOptions()
        {
            LoadRunways();

            ComboBoxAirport.Items.Clear();

            ComboBoxAirport.Items.Add("");

            foreach (var frequency in Plugin.ATISData.Frequencies.OrderBy(x => x.Airport))
            {
                var airport = Airspace2.GetAirport(frequency.Airport);

                if (airport == null)
                {
                    ComboBoxAirport.Items.Add(frequency.Airport);
                }
                else
                {
                    ComboBoxAirport.Items.Add($"{frequency.Airport} - {airport.FullName}");
                }
            }

            LoadCodes();

            ComboBoxVoice.Items.Clear();

            foreach (var voice in Control.SpeechSynth.GetInstalledVoices())
            {
                ComboBoxVoice.Items.Add(voice.VoiceInfo.Name);
            }

            ComboBoxVoice.Items.Add(Plugin.ManualVoiceName);

            ComboBoxZuluFrequency.Items.Clear();

            if (Plugin.Frequencies.Any())
            {
                foreach (var frequency in Plugin.Frequencies)
                {
                    ComboBoxZuluFrequency.Items.Add(string.IsNullOrWhiteSpace(frequency.FriendlyName) ? frequency.Name : frequency.FriendlyName);
                }

                ComboBoxZuluFrequency.SelectedIndex = 0;
            }

            foreach (var line in Control.Lines)
            {
                switch (line.Number)
                {
                    case 1:
                        Label1.Text = line.Name;
                        break;
                    case 2:
                        Label2.Text = line.Name;
                        break;
                    case 3:
                        Label3.Text = line.Name;
                        break;
                    case 4:
                        Label4.Text = line.Name;
                        break;
                    case 5:
                        Label5.Text = line.Name;
                        break;
                    case 6:
                        Label6.Text = line.Name;
                        break;
                    case 7:
                        Label7.Text = line.Name;
                        break;
                    case 8:
                        Label8.Text = line.Name;
                        break;
                    case 9:
                        Label9.Text = line.Name;
                        break;
                    case 10:
                        Label10.Text = line.Name;
                        break;
                    case 11:
                        Label11.Text = line.Name;
                        break;
                    case 12:
                        Label12.Text = line.Name;
                        break;
                    default:
                        break;
                }
            }
        }

        public void RefreshForm()
        {
            Invoke(new Action(() => {

                if (!Initialized) return;

                if (Plugin.SingleATISOnly && Number != 1)
                {
                    Change(1);

                    return;
                }

                if (Profile.Name.Contains("VATNZ"))
                {
                    ButtonZulu.Visible = false;
                }
                else
                {
                    ButtonZulu.Visible = true;
                }

                LabelMETAR.Text = string.Empty;

                if (ICAO == null)
                {
                    ComboBoxAirport.SelectedIndex = ComboBoxAirport.Items.IndexOf("");
                }
                else
                {
                    var index = ComboBoxAirport.Items.IndexOf(DisplayName);

                    if (ComboBoxAirport.SelectedIndex != index) ComboBoxAirport.SelectedIndex = ComboBoxAirport.Items.IndexOf(DisplayName);
                }

                ComboBoxLetter.SelectedIndex = ComboBoxLetter.Items.IndexOf(ID.ToString());

                if (VoiceName != null)
                {
                    ComboBoxVoice.SelectedIndex = ComboBoxVoice.Items.IndexOf(VoiceName);
                }

                ComboBoxRate.SelectedIndex = ComboBoxRate.Items.IndexOf(Rate.ToString());

                ComboBoxTimeCheck.SelectedIndex = ComboBoxTimeCheck.Items.IndexOf(TimeCheck.ToString());

                if (ComboBoxZuluFrequency.Items == null) ComboBoxZuluFrequency.Items = new List<string>();

                ComboBoxZuluFrequency.Items.Clear();

                foreach (var frequency in Plugin.Frequencies)
                {
                    ComboBoxZuluFrequency.Items.Add(string.IsNullOrWhiteSpace(frequency.FriendlyName) ? frequency.Name : frequency.FriendlyName);
                }

                if (!string.IsNullOrWhiteSpace(ZuluFrequency))
                {
                    ComboBoxZuluFrequency.SelectedIndex = ComboBoxZuluFrequency.Items.IndexOf(ZuluFrequency);
                }

                RefreshForm_TopButtons();

                if (Network.IsConnected && Control?.ICAO == null)
                {
                    RefreshForm_ClearWindCalculator();
                    RefreshFrom_NoATIS();
                }
                else if (Network.IsConnected && Control?.ICAO != null)
                {
                    RefreshForm_WithATIS();
                }
                else
                {
                    RefreshForm_ClearWindCalculator();
                    RefreshForm_NotConnected();
                }

            }));
        }

        private void RefreshForm_TopButtons()
        {
            var multipleAllowed = !Plugin.SingleATISOnly;

            ButtonATIS2.Enabled = multipleAllowed;
            ButtonATIS3.Enabled = multipleAllowed;
            ButtonATIS4.Enabled = multipleAllowed;

            if (Plugin.ATIS1.ICAO != null)
            {
                ButtonATIS1.Text = Plugin.ATIS1.ICAO;
            }
            else
            {
                ButtonATIS1.Text = "ATIS #1";
            }

            if (Plugin.ATIS2.ICAO != null)
            {
                ButtonATIS2.Text = Plugin.ATIS2.ICAO;
            }
            else
            {
                ButtonATIS2.Text = "ATIS #2";
            }

            if (Plugin.ATIS3.ICAO != null)
            {
                ButtonATIS3.Text = Plugin.ATIS3.ICAO;
            }
            else
            {
                ButtonATIS3.Text = "ATIS #3";
            }

            if (Plugin.ATIS4.ICAO != null)
            {
                ButtonATIS4.Text = Plugin.ATIS4.ICAO;
            }
            else
            {
                ButtonATIS4.Text = "ATIS #4";
            }

            if (Number != 1 && Plugin.ATIS1.HasUpdates)
            {
                ButtonATIS1.ForeColor = Colours.GetColour(Colours.Identities.Warning);
            }
            else
            {
                ButtonATIS1.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            }

            if (Number != 2 && Plugin.ATIS2.HasUpdates)
            {
                ButtonATIS2.ForeColor = Colours.GetColour(Colours.Identities.Warning);
            }
            else
            {
                ButtonATIS2.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            }

            if (Number != 3 && Plugin.ATIS3.HasUpdates)
            {
                ButtonATIS3.ForeColor = Colours.GetColour(Colours.Identities.Warning);
            }
            else
            {
                ButtonATIS3.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            }

            if (Number != 4 && Plugin.ATIS4.HasUpdates)
            {
                ButtonATIS4.ForeColor = Colours.GetColour(Colours.Identities.Warning);
            }
            else
            {
                ButtonATIS4.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            }

            if (Control.Number == 1)
            {
                ButtonATIS1.Checked = true;
            }
            else
            {
                ButtonATIS1.Checked = false;
            }

            if (Control.Number == 2)
            {
                ButtonATIS2.Checked = true;
            }
            else
            {
                ButtonATIS2.Checked = false;
            }

            if (Control.Number == 3)
            {
                ButtonATIS3.Checked = true;
            }
            else
            {
                ButtonATIS3.Checked = false;
            }

            if (Control.Number == 4)
            {
                ButtonATIS4.Checked = true;
            }
            else
            {
                ButtonATIS4.Checked = false;
            }
        }

        private void RefreshForm_NotConnected()
        {
            ComboBoxAirport.Enabled = false;
            ButtonCreate.Enabled = false;
            ComboBoxRate.Visible = true;
            ButtonRecord.Visible = false;

            RefreshForm_NormalATIS();

            ButtonZulu.Enabled = false;
            ComboBoxAirport.Enabled = true;
            ButtonCreate.Visible = true;
            ButtonDelete.Visible = false;
            ButtonGetMetar.Enabled = false;
            ComboBoxVoice.Enabled = false;
            ComboBoxRate.Enabled = false;
            ComboBoxLetter.Enabled = false;
            ButtonNext.Enabled = false;
            ComboBoxTimeCheck.Enabled = false;

            TextBox1.Text = string.Empty;
            TextBox2.Text = string.Empty;
            TextBox3.Text = string.Empty;
            TextBox4.Text = string.Empty;
            TextBox5.Text = string.Empty;
            TextBox6.Text = string.Empty;
            TextBox7.Text = string.Empty;
            TextBox8.Text = string.Empty;
            TextBox9.Text = string.Empty;
            TextBox10.Text = string.Empty;
            TextBox11.Text = string.Empty;
            TextBox12.Text = string.Empty;
            TextBoxZulu.Text = string.Empty;

            LabelMETAR.Text = string.Empty;

            ButtonSave.Enabled = false;
            ButtonCancel.Enabled = false;

            ButtonListen.Enabled = false;
            ButtonBroadcast.Enabled = false;

            RefreshForm_DisableTextBoxes();

            RefreshForm_ResetColours();
        }

        private void RefreshFrom_NoATIS()
        {
            ComboBoxAirport.Enabled = true;
            ButtonCreate.Enabled = true;

            if (Control.IsZulu)
            {
                RefreshForm_ZuluATIS();
            }
            else
            {
                RefreshForm_NormalATIS();
            }

            ButtonZulu.Enabled = false;
            ComboBoxAirport.Enabled = true;
            ButtonCreate.Visible = true;
            ButtonDelete.Visible = false;
            ButtonGetMetar.Enabled = false;
            ComboBoxVoice.Enabled = false;
            ComboBoxRate.Enabled = false;
            ComboBoxLetter.Enabled = false;
            ButtonNext.Enabled = false;
            ComboBoxTimeCheck.Enabled = false;

            TextBox1.Text = string.Empty;
            TextBox2.Text = string.Empty;
            TextBox3.Text = string.Empty;
            TextBox4.Text = string.Empty;
            TextBox5.Text = string.Empty;
            TextBox6.Text = string.Empty;
            TextBox7.Text = string.Empty;
            TextBox8.Text = string.Empty;
            TextBox9.Text = string.Empty;
            TextBox10.Text = string.Empty;
            TextBox11.Text = string.Empty;
            TextBox12.Text = string.Empty;
            TextBoxZulu.Text = string.Empty;

            LabelMETAR.Text = string.Empty;

            ButtonSave.Enabled = false;
            ButtonCancel.Enabled = false;

            ButtonListen.Enabled = false;
            ButtonBroadcast.Enabled = false;

            RefreshForm_DisableTextBoxes();

            RefreshForm_ResetColours();
        }

        private void RefreshForm_EnableTextBoxes()
        {
            TextBox1.Enabled = true;
            TextBox2.Enabled = true;
            TextBox3.Enabled = true;
            TextBox4.Enabled = true;
            TextBox5.Enabled = true;
            TextBox6.Enabled = true;
            TextBox7.Enabled = true;
            TextBox8.Enabled = true;
            TextBox9.Enabled = true;
            TextBox10.Enabled = true;
            TextBox11.Enabled = true;
            TextBox12.Enabled = true;
            TextBoxZulu.Enabled = true;

            TextBox1.TextChanged += TextBox_TextChanged;
            TextBox2.TextChanged += TextBox_TextChanged;
            TextBox3.TextChanged += TextBox_TextChanged;
            TextBox4.TextChanged += TextBox_TextChanged;
            TextBox5.TextChanged += TextBox_TextChanged;
            TextBox6.TextChanged += TextBox_TextChanged;
            TextBox7.TextChanged += TextBox_TextChanged;
            TextBox8.TextChanged += TextBox_TextChanged;
            TextBox9.TextChanged += TextBox_TextChanged;
            TextBox10.TextChanged += TextBox_TextChanged;
            TextBox11.TextChanged += TextBox_TextChanged;
            TextBox12.TextChanged += TextBox_TextChanged;
            TextBoxZulu.TextChanged += TextBox_TextChanged;
        }

        private void RefreshForm_DisableTextBoxes()
        {
            TextBox1.Enabled = false;
            TextBox2.Enabled = false;
            TextBox3.Enabled = false;
            TextBox4.Enabled = false;
            TextBox5.Enabled = false;
            TextBox6.Enabled = false;
            TextBox7.Enabled = false;
            TextBox8.Enabled = false;
            TextBox9.Enabled = false;
            TextBox10.Enabled = false;
            TextBox11.Enabled = false;
            TextBox12.Enabled = false;
            TextBoxZulu.Enabled = false;

            TextBox1.TextChanged -= TextBox_TextChanged;
            TextBox2.TextChanged -= TextBox_TextChanged;
            TextBox3.TextChanged -= TextBox_TextChanged;
            TextBox4.TextChanged -= TextBox_TextChanged;
            TextBox5.TextChanged -= TextBox_TextChanged;
            TextBox6.TextChanged -= TextBox_TextChanged;
            TextBox7.TextChanged -= TextBox_TextChanged;
            TextBox8.TextChanged -= TextBox_TextChanged;
            TextBox9.TextChanged -= TextBox_TextChanged;
            TextBox10.TextChanged -= TextBox_TextChanged;
            TextBox11.TextChanged -= TextBox_TextChanged;
            TextBox12.TextChanged -= TextBox_TextChanged;
            TextBoxZulu.TextChanged -= TextBox_TextChanged;
        }

        private void RefreshForm_WithATIS()
        {
            if (Control.IsZulu)
            {
                RefreshForm_ZuluATIS();
            }
            else
            {
                RefreshForm_NormalATIS();
            }

            ButtonZulu.Enabled = true;
            ComboBoxAirport.Enabled = false;
            ButtonCreate.Visible = false;
            ButtonDelete.Visible = true;
            ButtonGetMetar.Enabled = true;
            ComboBoxVoice.Enabled = true;
            ComboBoxRate.Enabled = true;
            ComboBoxTimeCheck.Enabled = true;

            RefreshForm_DisableTextBoxes();

            foreach (var line in Control.Lines)
            {
                switch (line.Number)
                {
                    case 1:
                        TextBox1.Text = line.Value;
                        break;
                    case 2:
                        TextBox2.Text = line.Value;
                        break;
                    case 3:
                        TextBox3.Text = line.Value;
                        break;
                    case 4:
                        TextBox4.Text = line.Value;
                        break;
                    case 5:
                        TextBox5.Text = line.Value;
                        break;
                    case 6:
                        TextBox6.Text = line.Value;
                        break;
                    case 7:
                        TextBox7.Text = line.Value;
                        break;
                    case 8:
                        TextBox8.Text = line.Value;
                        break;
                    case 9:
                        TextBox9.Text = line.Value;
                        break;
                    case 10:
                        TextBox10.Text = line.Value;
                        break;
                    case 11:
                        TextBox11.Text = line.Value;
                        break;
                    case 12:
                        TextBox12.Text = line.Value;
                        break;
                    case 13:
                        TextBoxZulu.Text = line.Value;
                        break;
                    default:
                        break;
                }
            }

            RefreshForm_EnableTextBoxes();

            LabelMETAR.Text = Control.METARRaw;

            if (Control.HasUpdates && ID != Control.ID)
            {
                LabelATIS.ForeColor = Colours.GetColour(Colours.Identities.Warning);
            }
            else
            {
                LabelATIS.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            }

            foreach (var line in Control.Lines)
            {
                var suggestedLine = Control.SuggestedLines.FirstOrDefault(x => x.Name == line.Name);

                var saveLine = Saves.FirstOrDefault(x => x.Key == line.Name);

                switch (line.Number)
                {
                    case 1:
                        if (!string.IsNullOrWhiteSpace(saveLine.Value)) TextBox1.Text = saveLine.Value;
                        else if (suggestedLine != null)
                        {
                            TextBox1.Text = suggestedLine.Value;
                            Label1.ForeColor = Colours.GetColour(Colours.Identities.Warning);
                        }
                        else
                        {
                            Label1.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText); 
                        }
                        break;
                    case 2:
                        if (!string.IsNullOrWhiteSpace(saveLine.Value)) TextBox2.Text = saveLine.Value;
                        else if (suggestedLine != null)
                        {
                            TextBox2.Text = suggestedLine.Value;
                            Label2.ForeColor = Colours.GetColour(Colours.Identities.Warning);
                        }
                        else
                        {
                            Label2.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
                        }
                        break;
                    case 3:
                        if (!string.IsNullOrWhiteSpace(saveLine.Value)) TextBox3.Text = saveLine.Value;
                        else if (suggestedLine != null)
                        {
                            TextBox3.Text = suggestedLine.Value;
                            Label3.ForeColor = Colours.GetColour(Colours.Identities.Warning);
                        }
                        else
                        {
                            Label3.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
                        }
                        break;
                    case 4:
                        if (!string.IsNullOrWhiteSpace(saveLine.Value)) TextBox4.Text = saveLine.Value;
                        else if (suggestedLine != null)
                        {
                            TextBox4.Text = suggestedLine.Value;
                            Label4.ForeColor = Colours.GetColour(Colours.Identities.Warning);
                        }
                        else
                        {
                            Label4.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
                        }
                        break;
                    case 5:
                        if (!string.IsNullOrWhiteSpace(saveLine.Value)) TextBox5.Text = saveLine.Value;
                        else if (suggestedLine != null)
                        {
                            TextBox5.Text = suggestedLine.Value;
                            Label5.ForeColor = Colours.GetColour(Colours.Identities.Warning);
                        }
                        else
                        {
                            Label5.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
                        }
                        break;
                    case 6:
                        if (!string.IsNullOrWhiteSpace(saveLine.Value)) TextBox6.Text = saveLine.Value;
                        else if (suggestedLine != null)
                        {
                            TextBox6.Text = suggestedLine.Value;
                            Label6.ForeColor = Colours.GetColour(Colours.Identities.Warning);
                        }
                        else
                        {
                            Label6.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
                        }
                        break;
                    case 7:
                        if (!string.IsNullOrWhiteSpace(saveLine.Value)) TextBox7.Text = saveLine.Value;
                        else if (suggestedLine != null)
                        {
                            TextBox7.Text = suggestedLine.Value;
                            Label7.ForeColor = Colours.GetColour(Colours.Identities.Warning);
                        }
                        else
                        {
                            Label7.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
                        }
                        break;
                    case 8:
                        if (!string.IsNullOrWhiteSpace(saveLine.Value)) TextBox8.Text = saveLine.Value;
                        else if (suggestedLine != null)
                        {
                            TextBox8.Text = suggestedLine.Value;
                            Label8.ForeColor = Colours.GetColour(Colours.Identities.Warning);
                        }
                        else
                        {
                            Label8.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
                        }
                        break;
                    case 9:
                        if (!string.IsNullOrWhiteSpace(saveLine.Value)) TextBox9.Text = saveLine.Value;
                        else if (suggestedLine != null)
                        {
                            TextBox9.Text = suggestedLine.Value;
                            Label9.ForeColor = Colours.GetColour(Colours.Identities.Warning);
                        }
                        else
                        {
                            Label9.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
                        }
                        break;
                    case 10:
                        if (!string.IsNullOrWhiteSpace(saveLine.Value)) TextBox10.Text = saveLine.Value;
                        else if (suggestedLine != null)
                        {
                            TextBox10.Text = suggestedLine.Value;
                            Label10.ForeColor = Colours.GetColour(Colours.Identities.Warning);
                        }
                        else
                        {
                            Label10.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
                        }
                        break;
                    case 11:
                        if (!string.IsNullOrWhiteSpace(saveLine.Value)) TextBox11.Text = saveLine.Value;
                        else if (suggestedLine != null)
                        {
                            TextBox11.Text = suggestedLine.Value;
                            Label11.ForeColor = Colours.GetColour(Colours.Identities.Warning);
                        }
                        else
                        {
                            Label11.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
                        }
                        break;
                    case 12:
                        if (!string.IsNullOrWhiteSpace(saveLine.Value)) TextBox12.Text = saveLine.Value;
                        else if (suggestedLine != null)
                        {
                            TextBox12.Text = suggestedLine.Value;
                            Label12.ForeColor = Colours.GetColour(Colours.Identities.Warning);
                        }
                        else
                        {
                            Label12.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
                        }
                        break;
                    case 13:
                        if (!string.IsNullOrWhiteSpace(saveLine.Value)) TextBoxZulu.Text = saveLine.Value;
                        else if (suggestedLine != null)
                        {
                            TextBoxZulu.Text = suggestedLine.Value;
                            TextBoxZulu.ForeColor = Colours.GetColour(Colours.Identities.Warning);
                        }
                        else
                        {
                            TextBoxZulu.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
                        }
                        break;
                    default:
                        break;
                }
            }

            if (Edits)
            {
                ButtonSave.Enabled = true;
                ButtonCancel.Enabled = true;
            }
            else
            {
                ButtonSave.Enabled = false;
                ButtonCancel.Enabled = false;
            }

            if (Control.CanListen)
            {
                ButtonListen.Enabled = true;
                ButtonBroadcast.Enabled = true;
            }
            else
            {
                ButtonListen.Enabled = false;
                ButtonBroadcast.Enabled = false;
            }

            if (CanRecord)
            {
                ButtonRecord.Enabled = true;
            }
            else
            {
                ButtonRecord.Enabled = false;
            }

            if (Control.Recording)
            {
                ButtonRecord.Checked = true;
            }
            else
            {
                ButtonRecord.Checked = false;
            }

            if (Control.Listening)
            {
                ButtonListen.Checked = true;
            }
            else
            {
                ButtonListen.Checked = false;
            }

            if (Control.Broadcasting)
            {
                ButtonBroadcast.Checked = true;
            }
            else
            {
                ButtonBroadcast.Checked = false;
            }
        }

        private void RefreshForm_NormalATIS()
        {
            TextBoxZulu.Visible = false;
            ComboBoxZuluFrequency.Visible = false;
            LabelFrequency.Visible = false;
            ComboBoxLetter.Enabled = true;
            ButtonNext.Enabled = true;
            ButtonZulu.Checked = false;

            TextBox1.Visible = true;
            TextBox2.Visible = true;
            TextBox3.Visible = true;
            TextBox4.Visible = true;
            TextBox5.Visible = true;
            TextBox6.Visible = true;
            TextBox7.Visible = true;
            TextBox8.Visible = true;
            TextBox9.Visible = true;
            TextBox10.Visible = true;
            TextBox11.Visible = true;
            TextBox12.Visible = true;
            Label1.Visible = true;
            Label2.Visible = true;
            Label3.Visible = true;
            Label4.Visible = true;
            Label5.Visible = true;
            Label6.Visible = true;
            Label7.Visible = true;
            Label8.Visible = true;
            Label9.Visible = true;
            Label10.Visible = true;
            Label11.Visible = true;
            Label12.Visible = true;
            LabelTimeCheck.Visible = true;
            ComboBoxTimeCheck.Visible = true;
        }

        private void RefreshForm_ZuluATIS()
        {
            TextBoxZulu.Visible = true;
            ComboBoxZuluFrequency.Visible = true;
            LabelFrequency.Visible = true;
            ComboBoxLetter.Enabled = false;
            ButtonNext.Enabled = false;
            ButtonZulu.Checked = true;

            TextBox1.Visible = false;
            TextBox2.Visible = false;
            TextBox3.Visible = false;
            TextBox4.Visible = false;
            TextBox5.Visible = false;
            TextBox6.Visible = false;
            TextBox7.Visible = false;
            TextBox8.Visible = false;
            TextBox9.Visible = false;
            TextBox10.Visible = false;
            TextBox11.Visible = false;
            TextBox12.Visible = false;
            Label1.Visible = false;
            Label2.Visible = false;
            Label3.Visible = false;
            Label4.Visible = false;
            Label5.Visible = false;
            Label6.Visible = false;
            Label7.Visible = false;
            Label8.Visible = false;
            Label9.Visible = false;
            Label10.Visible = false;
            Label11.Visible = false;
            Label12.Visible = false;
            LabelTimeCheck.Visible = false;
            ComboBoxTimeCheck.Visible = false;
        }

        private void RefreshForm_ClearWindCalculator()
        {
            Invoke(new Action(() => {
                LabelWindComponents.Text = "";
                ComboBoxRunway.SelectedIndex = ComboBoxRunway.Items.IndexOf("");
            }));
        }

        private void RefreshForm_ResetColours()
        {
            Label1.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText); 
            Label2.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            Label3.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            Label4.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            Label5.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            Label6.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            Label7.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            Label8.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            Label9.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            Label10.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            Label11.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            Label12.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            LabelATIS.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
        }

        private void GetMetar()
        {
            LabelMETAR.Text = "LOADING";
            
            MET.Instance.RequestProduct(new MET.ProductRequest(MET.ProductType.VATSIM_METAR, ICAO, true));
        }

        private void IncreaseID()
        {
            if (Control.IsZulu) return;

            // Get the current index of the ID in the valid codes list
            int currentIndex = ComboBoxLetter.Items.IndexOf(ID.ToString());
            
            if (currentIndex == -1) 
            {
                // If current ID is not in the valid list, select the first one
                if (ComboBoxLetter.Items.Count > 0)
                {
                    ComboBoxLetter.SelectedIndex = 0;
                    ID = ComboBoxLetter.Items[0].ToString()[0];
                }
                return;
            }
            
            // Calculate the next index, wrapping around if needed
            int nextIndex = (currentIndex + 1) % ComboBoxLetter.Items.Count;
            
            // Update the ID and selection
            ComboBoxLetter.SelectedIndex = nextIndex;
            ID = ComboBoxLetter.Items[nextIndex].ToString()[0];
        }

        private async void ButtonCreate_Click(object sender, EventArgs e)
        {
            if (!Network.IsConnected || !Network.IsValidATC) return;

            if (ICAO == null) return;

            var frequency = Plugin.ATISData.Frequencies.FirstOrDefault(x => x.Airport == ICAO);

            if (frequency == null) return;

            if (Airport == null) return;

            await Control.Create(ICAO, frequency.Frequency.ToString(), Airport.LatLong);

            LoadRunways();

            GetMetar();
        }

        private void ComboBoxAirport_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ComboBoxAirport.SelectedIndex == -1) return;

            if (ComboBoxAirport.SelectedIndex == 0) return;

            var icao = ComboBoxAirport.Items[ComboBoxAirport.SelectedIndex];

            if (icao.Length == 4)
            {
                ICAO = icao;

                LoadCodes();

                return;
            }

            ICAO = ComboBoxAirport.Items[ComboBoxAirport.SelectedIndex].Substring(0, 4);

            LoadCodes();
        }

        private async void ButtonDelete_Click(object sender, EventArgs e)
        {
            await Control.Delete();

            Change(Number);
        }

        private void ButtonGetMetar_Click(object sender, EventArgs e)
        {
            GetMetar();
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            ID = Control.ID;

            TimeCheck = Control.TimeCheck;

            Saves.Clear();

            Control.SuggestedLines.Clear();

            RefreshForm_ResetColours();

            RefreshForm();
        }

        private void ButtonNext_Click(object sender, EventArgs e)
        {
            IncreaseID();
        }

        private async void ButtonSave_Click(object sender, EventArgs e)
        {
            RefreshForm_ClearWindCalculator();

            await SaveATIS();
        }

        private async Task SaveATIS()
        {
            Control.VoiceName = VoiceName;

            Control.PromptRate = Rate;

            await Control.Save(ID, Saves, TimeCheck);

            Saves.Clear();

            RefreshForm_ResetColours();

            RefreshForm();
        }

        private async void ButtonBroadcast_Click(object sender, EventArgs e)
        {
            if (!Control.Broadcasting)
            {
                if (Saves.Any()) await SaveATIS();

                Control.BroadcastStart();
            }
            else
            {
                await Control.BroadcastStop();
            }

            RefreshForm();
        }

        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            if (!(sender is TextBox textBox)) return;

            var lineNumber = 0;

            switch (textBox.Name)
            {
                case "TextBox1":
                    lineNumber = 1;
                    break;
                case "TextBox2":
                    lineNumber = 2;
                    break;
                case "TextBox3":
                    lineNumber = 3;
                    break;
                case "TextBox4":
                    lineNumber = 4;
                    break;
                case "TextBox5":
                    lineNumber = 5;
                    break;
                case "TextBox6":
                    lineNumber = 6;
                    break;
                case "TextBox7":
                    lineNumber = 7;
                    break;
                case "TextBox8":
                    lineNumber = 8;
                    break;
                case "TextBox9":
                    lineNumber = 9;
                    break;
                case "TextBox10":
                    lineNumber = 10;
                    break;
                case "TextBox11":
                    lineNumber = 11;
                    break;
                case "TextBox12":
                    lineNumber = 12;
                    break;
                case "TextBoxZulu":
                    lineNumber = 13;
                    break;
                default:
                    break;
            }

            if (lineNumber == 0) return;

            var line = Control.Lines.FirstOrDefault(x => x.Number == lineNumber);

            if (line == null) return;

            if (line.Value == textBox.Text) return;

            var save = Saves.FirstOrDefault(x => x.Key == line.Name);

            if (save.Key != null)
            {
                Saves.Remove(save.Key);
            }

            Saves.Add(line.Name, textBox.Text);

            if (Edits && ID == Control.ID && !Control.IsZulu)
            {
                IncreaseID();
                ButtonSave.Enabled = true;
                ButtonCancel.Enabled = true;
            }
            else if (Edits && Control.IsZulu)
            {
                ButtonSave.Enabled = true;
                ButtonCancel.Enabled = true;
            }

            if (line.Name.Contains("WIND"))
            {
                CalculateWind();
            }
        }

        private void ComboBoxLetter_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!(sender is DropDownBox comboBox)) return;

            if (comboBox.SelectedIndex == -1) return;

            string selectedLetter = comboBox.Items[comboBox.SelectedIndex];

            var ok = Char.TryParse(selectedLetter, out Char selectedChar);

            if (!ok) return;

            if (ID == selectedChar) return;

            ID = selectedChar;

            ButtonSave.Enabled = true;
            ButtonCancel.Enabled = true;
        }

        private void ComboBoxVoice_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!(sender is DropDownBox comboBox)) return;

            if (comboBox.SelectedIndex == -1) return;

            string selectedVoice = comboBox.Items[comboBox.SelectedIndex];

            if (selectedVoice == null || selectedVoice == VoiceName) return;  

            VoiceName =  selectedVoice;

            ButtonSave.Enabled = true;
            ButtonCancel.Enabled = true;

            if (VoiceName == Plugin.ManualVoiceName)
            {
                ComboBoxRate.Visible = false;
                ButtonRecord.Visible = true;
            }
            else
            {
                ComboBoxRate.Visible = true;
                ButtonRecord.Visible = false;
            }
        }

        private void ComboBoxRate_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!(sender is DropDownBox comboBox)) return;

            if (comboBox.SelectedIndex == -1) return;

            string rateString = comboBox.Items[comboBox.SelectedIndex];

            var rate = PromptRate.Medium;

            switch (rateString)
            {
                case "Extra Fast":
                    rate = PromptRate.ExtraFast;
                    break;
                case "Fast":
                    rate = PromptRate.Fast;
                    break;
                case "Medium":
                    rate = PromptRate.Medium;
                    break;
                case "Slow":
                    rate = PromptRate.Slow;
                    break;
                case "Extra Slow":
                    rate = PromptRate.ExtraSlow;
                    break;
                default:
                    break;
            }

            if (rate == Rate) return;

            Rate = rate;

            ButtonSave.Enabled = true;
            ButtonCancel.Enabled = true;
        }

        private void ButtonListen_Click(object sender, EventArgs e)
        {
            if (Control.Listening)
            {
                Control.ListenStop();
            }
            else
            {
                if (!Control.CanListen) return;
                Control.ListenStart();
            }

            RefreshForm();
        }

        private void ButtonATIS1_Click(object sender, EventArgs e)
        {
            Control.SoundPlayer.Stop();
            Change(1);
        }

        private void ButtonATIS2_Click(object sender, EventArgs e)
        {
            Control.SoundPlayer.Stop();
            Change(2);
        }

        private void ButtonATIS3_Click(object sender, EventArgs e)
        {
            Control.SoundPlayer.Stop();
            Change(3);
        }

        private void ButtonATIS4_Click(object sender, EventArgs e)
        {
            Control.SoundPlayer.Stop();
            Change(4);
        }

        private void ComboBoxTimeCheck_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!(sender is DropDownBox comboBox)) return;

            if (comboBox.SelectedIndex == -1) return;

            var timeCheckOK = bool.TryParse(comboBox.Items[comboBox.SelectedIndex], out bool timeCheck);

            if (!timeCheckOK) return;

            if (TimeCheck == timeCheck) return;

            TimeCheck = timeCheck;

            ButtonSave.Enabled = true;
            ButtonCancel.Enabled = true;
        }

        private void ButtonZulu_Click(object sender, EventArgs e)
        {
            ID = 'Z';

            Saves.Clear();

            if (!Control.IsZulu)
            {
                Control.IsZulu = true;

                GenerateZulu();
            }
            else
            {
                Control.IsZulu = false;

                TextBoxZulu.Text = string.Empty;

                ZuluFrequency = string.Empty;
            }

            RefreshForm();
        }

        private void ComboBoxZuluFrequency_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ComboBoxZuluFrequency.SelectedIndex == -1) return;

            var zuluFrequency = ComboBoxZuluFrequency.Items[ComboBoxZuluFrequency.SelectedIndex];

            if (zuluFrequency == ZuluFrequency) return;

            ZuluFrequency = zuluFrequency;

            GenerateZulu();
        }

        private void GenerateZulu()
        {
            var zuluLine = Control.Lines.FirstOrDefault(x => x.Name == "ZULU");

            if (zuluLine == null) return;

            var zuluInfo = Zulu.GetInfo(ICAO);

            if (zuluInfo == null) return;

            var atis = zuluInfo.Text;

            var frequency = Plugin.Frequencies.FirstOrDefault(x => x.Name == ZuluFrequency || x.FriendlyName == ZuluFrequency);

            if (frequency != null)
            {
                var station = string.IsNullOrWhiteSpace(frequency.FriendlyName) ? frequency.Name : frequency.FriendlyName;
                
                atis = atis.Replace("{STATION}", station.ToUpper());
                
                atis = atis.Replace("{FREQ}", Conversions.FrequencyToString(frequency.Frequency));
            }

            zuluLine.Value = atis;

            TextBoxZulu.Text = atis;
        }

        private string WindOutput(string wind, double rwyHeading)
        {
            if (string.IsNullOrWhiteSpace(wind))
            {
                return string.Empty;
            }

            if (wind == "CALM") return "CALM";

            if (wind == "LIGHT AND VRB") return "VARIABLE";

            var split = wind.Split('/');

            if (split.Length != 2) return string.Empty;

            var windDirection = split.First();

            var windSpeed = split.Last();

            var gust = windSpeed.Split('-');

            if (gust.Length == 2)
            {
                windSpeed = gust.Last();
            }

            var windSpeedOK = double.TryParse(windSpeed, out double wSpeed);

            if (!windSpeedOK) return string.Empty;

            var windDirectionOk = double.TryParse(windDirection, out double wDirection);

            if (!windDirectionOk) return string.Empty;

            if (double.Parse(windSpeed) == 0.0) return "CALM";

            double radians = Conversions.DegreesToRadians(wDirection);
            double num1 = Conversions.DegreesToRadians(rwyHeading) - radians;
            double num3 = Math.Round(wSpeed * Math.Sin(num1));
            double num4 = Math.Round(wSpeed * Math.Cos(num1));
            string str1 = Math.Abs(num3).ToString("F0");
            string str2 = Math.Abs(num4).ToString("F0");

            if (num3 < 0.0)
                str1 += "kt Right Crosswind"; // Right
            else if (num3 > 0.0)
                str1 += "kt Left Crosswind"; // Left

            if (num4 < 0.0)
                str2 += "kt Tailwind";
            else if (num4 > 0.0)
                str2 += "kt Headwind";

            if (str1 == "0") return str2;

            if (str2 == "0") return str1;

            return $"{str2}, {str1}";
        }

        private void CalculateWind()
        {
            var saveWind = Saves.FirstOrDefault(x => x.Key.Contains("WIND"));

            var atisWind = Control.Lines.FirstOrDefault(x => x.Name.Contains("WIND"));

            var wind = saveWind.Key != null ? saveWind.Value : atisWind.Value;

            if (wind == null) return;

            var runwayName = ComboBoxRunway.Text.Replace("RWY ", "");

            var runwayHeading = GetRunwayHeading(runwayName);

            if (runwayHeading == null)
            {
                var runwayNameHeadingOk = double.TryParse(runwayName, out double runwayNameHeading);

                if (!runwayNameHeadingOk) return;

                runwayHeading = runwayNameHeading;
            }

            var windComponents = WindOutput(wind, runwayHeading.Value);

            LabelWindComponents.Text = windComponents;
        }

        private double? GetRunwayHeading(string runwayName)
        {
            if (Control.ICAO == null) return null;

            var airport = Airspace2.GetAirport(Control.ICAO);

            if (airport == null) return null;

            var runway = airport.Runways.FirstOrDefault(x => x.Name == runwayName);

            if (runway == null) return null;

            var oppositeRunway = OppositeRunway(airport, runway.Name);

            if (oppositeRunway == null) return null;

            var position = LogicalPositions.Positions.FirstOrDefault(x => x.Name == Control.ICAO);

            if (position == null) return null;

            var heading = Conversions.CalculateTrack(runway.LatLong, oppositeRunway.LatLong);

            heading = heading + position.MagneticVariation;

            return Math.Round(heading, 0);
        }

        public static Airspace2.Airport.Runway OppositeRunway(Airspace2.Airport airport, string runway)
        {
            if (runway == null) return null;

            var oppRwyNum = int.Parse(runway.Replace("L", "").Replace("C", "").Replace("R", "")) + 18;
            if (oppRwyNum > 36) oppRwyNum -= 36;
            var oppositeRunway = oppRwyNum.ToString("D2");
            if (runway.EndsWith("L"))
                oppositeRunway += "R";
            else if (runway.EndsWith("R"))
                oppositeRunway += "L";
            else if (runway.EndsWith("C"))
                oppositeRunway += "C";

            return airport.Runways.FirstOrDefault(x => x.Name == oppositeRunway);
        }

        private void ComboBoxRunway_SelectedIndexChanged(object sender, EventArgs e)
        {
            CalculateWind();
        }

        private void ButtonRecord_Click(object sender, EventArgs e)
        {
            if (Control.Recording)
            {
                Control.StopRecording();
            }
            else
            {
                Control.StartRecording();
            }

            RefreshForm();
        }
    }
}
