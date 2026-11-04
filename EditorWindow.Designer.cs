using System;
using System.Collections.Generic;
using System.Windows.Forms;
using vatsys;
using VATSYSControls;

namespace ATISPlugin
{
    partial class EditorWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.ComboBoxAirport = new DropDownBox();
            this.ComboBoxAirport.Items = new List<string>();
            this.ComboBoxTimeCheck = new DropDownBox();
            this.ComboBoxTimeCheck.Items = new List<string>();
            this.ComboBoxLetter = new DropDownBox();
            this.ComboBoxLetter.Items = new List<string>();
            this.TextBoxMode = new TextField();
            this.ComboBoxZuluFrequency = new DropDownBox();
            this.ComboBoxZuluFrequency.Items = new List<string>();
            this.ComboBoxRunway = new DropDownBox();
            this.ComboBoxRunway.Items = new List<string>();
            this.ComboBoxVoice = new DropDownBox();
            this.ComboBoxVoice.Items = new List<string>();
            this.ComboBoxRate = new DropDownBox();
            this.ComboBoxRate.Items = new List<string>();

            this.LabelICAO = new TextLabel();
            this.ButtonCreate = new GenericButton();
            this.TextBox1 = new TextField();
            this.TextBox2 = new TextField();
            this.TextBox3 = new TextField();
            this.TextBox4 = new TextField();
            this.TextBox5 = new TextField();
            this.TextBox6 = new TextField();
            this.TextBox7 = new TextField();
            this.TextBox8 = new TextField();
            this.TextBox9 = new TextField();
            this.TextBox10 = new TextField();
            this.TextBox11 = new TextField();
            this.TextBox12 = new TextField();
            this.Label1 = new Label();
            this.Label2 = new Label();
            this.Label3 = new Label();
            this.Label4 = new Label();
            this.Label5 = new Label();
            this.Label6 = new Label();
            this.Label7 = new Label();
            this.Label8 = new Label();
            this.Label10 = new Label();
            this.Label9 = new Label();
            this.Label11 = new Label();
            this.Label12 = new Label();
            this.LabelTimeCheck = new Label();
            this.ButtonNext = new GenericButton();
            this.LabelATIS = new Label();
            this.LabelMode = new Label();
            this.LabelMETAR = new TextLabel();
            this.ButtonSave = new GenericButton();
            this.ButtonCancel = new GenericButton();
            this.ButtonListen = new ToggleButton();
            this.ButtonGetMetar = new GenericButton();
            this.ButtonBroadcast = new ToggleButton();
            this.ButtonDelete = new GenericButton();
            this.LabelVoice = new TextLabel();
            this.ButtonATIS1 = new ToggleButton();
            this.ButtonATIS2 = new ToggleButton();
            this.ButtonATIS3 = new ToggleButton();
            this.ButtonATIS4 = new ToggleButton();
            this.TextBoxZulu = new TextField();
            this.ButtonZulu = new ToggleButton();
            this.LabelFrequency = new Label();
            this.LabelWindCalculator = new TextLabel();
            this.LabelWindComponents = new Label();
            this.ButtonRecord = new ToggleButton();
            this.SuspendLayout();
            // 
            // labelICAO
            // 
            this.LabelICAO.AutoSize = true;
            this.LabelICAO.Location = new System.Drawing.Point(102, 66);
            this.LabelICAO.Name = "labelICAO";
            this.LabelICAO.Size = new System.Drawing.Size(64, 17);
            this.LabelICAO.TabIndex = 0;
            this.LabelICAO.Text = "Airport";
            // 
            // ButtonCreate
            // 
            this.ButtonCreate.Location = new System.Drawing.Point(504, 58);
            this.ButtonCreate.Name = "ButtonCreate";
            this.ButtonCreate.Size = new System.Drawing.Size(160, 28);
            this.ButtonCreate.TabIndex = 4;
            this.ButtonCreate.Text = "Create";
            this.ButtonCreate.UseVisualStyleBackColor = true;
            this.ButtonCreate.Click += new System.EventHandler(this.ButtonCreate_Click);
            // 
            // ComboBoxAirport
            // 
            this.ComboBoxAirport.Location = new System.Drawing.Point(172, 60);
            this.ComboBoxAirport.Name = "ComboBoxAirport";
            this.ComboBoxAirport.Size = new System.Drawing.Size(326, 25);
            this.ComboBoxAirport.TabIndex = 1;
            this.ComboBoxAirport.SelectedIndexChanged += new System.EventHandler(this.ComboBoxAirport_SelectedIndexChanged);
            // 
            // TextBox1
            // 
            this.TextBox1.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.TextBox1.Location = new System.Drawing.Point(172, 260);
            this.TextBox1.Name = "TextBox1";
            this.TextBox1.Size = new System.Drawing.Size(492, 25);
            this.TextBox1.TabIndex = 5;
            // 
            // TextBox2
            // 
            this.TextBox2.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.TextBox2.Location = new System.Drawing.Point(172, 291);
            this.TextBox2.Name = "TextBox2";
            this.TextBox2.Size = new System.Drawing.Size(492, 25);
            this.TextBox2.TabIndex = 6;
            // 
            // TextBox3
            // 
            this.TextBox3.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.TextBox3.Location = new System.Drawing.Point(172, 322);
            this.TextBox3.Name = "TextBox3";
            this.TextBox3.Size = new System.Drawing.Size(492, 25);
            this.TextBox3.TabIndex = 7;
            // 
            // Label1
            // 
            this.Label1.AutoSize = true;
            this.Label1.Location = new System.Drawing.Point(8, 263);
            this.Label1.MinimumSize = new System.Drawing.Size(160, 17);
            this.Label1.Name = "Label1";
            this.Label1.Size = new System.Drawing.Size(160, 17);
            this.Label1.TabIndex = 10;
            this.Label1.Text = "LABEL1";
            this.Label1.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // Label2
            // 
            this.Label2.AutoSize = true;
            this.Label2.Location = new System.Drawing.Point(8, 294);
            this.Label2.MinimumSize = new System.Drawing.Size(160, 17);
            this.Label2.Name = "Label2";
            this.Label2.Size = new System.Drawing.Size(160, 17);
            this.Label2.TabIndex = 11;
            this.Label2.Text = "LABEL2";
            this.Label2.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // Label3
            // 
            this.Label3.AutoSize = true;
            this.Label3.Location = new System.Drawing.Point(8, 325);
            this.Label3.MinimumSize = new System.Drawing.Size(160, 17);
            this.Label3.Name = "Label3";
            this.Label3.Size = new System.Drawing.Size(160, 17);
            this.Label3.TabIndex = 12;
            this.Label3.Text = "LABEL3";
            this.Label3.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // TextBox5
            // 
            this.TextBox5.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.TextBox5.Location = new System.Drawing.Point(172, 384);
            this.TextBox5.Name = "TextBox5";
            this.TextBox5.Size = new System.Drawing.Size(492, 25);
            this.TextBox5.TabIndex = 9;
            // 
            // TextBox6
            // 
            this.TextBox6.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.TextBox6.Location = new System.Drawing.Point(172, 415);
            this.TextBox6.Name = "TextBox6";
            this.TextBox6.Size = new System.Drawing.Size(492, 25);
            this.TextBox6.TabIndex = 10;
            // 
            // TextBox7
            // 
            this.TextBox7.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.TextBox7.Location = new System.Drawing.Point(172, 446);
            this.TextBox7.Name = "TextBox7";
            this.TextBox7.Size = new System.Drawing.Size(492, 25);
            this.TextBox7.TabIndex = 11;
            // 
            // TextBox8
            // 
            this.TextBox8.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.TextBox8.Location = new System.Drawing.Point(172, 477);
            this.TextBox8.Name = "TextBox8";
            this.TextBox8.Size = new System.Drawing.Size(492, 25);
            this.TextBox8.TabIndex = 12;
            // 
            // TextBox9
            // 
            this.TextBox9.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.TextBox9.Location = new System.Drawing.Point(172, 508);
            this.TextBox9.Name = "TextBox9";
            this.TextBox9.Size = new System.Drawing.Size(492, 25);
            this.TextBox9.TabIndex = 13;
            // 
            // TextBox10
            // 
            this.TextBox10.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.TextBox10.Location = new System.Drawing.Point(172, 539);
            this.TextBox10.Name = "TextBox10";
            this.TextBox10.Size = new System.Drawing.Size(492, 25);
            this.TextBox10.TabIndex = 14;
            // 
            // TextBox11
            // 
            this.TextBox11.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.TextBox11.Location = new System.Drawing.Point(172, 570);
            this.TextBox11.Name = "TextBox11";
            this.TextBox11.Size = new System.Drawing.Size(492, 25);
            this.TextBox11.TabIndex = 15;
            // 
            // TextBox12
            // 
            this.TextBox12.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.TextBox12.Location = new System.Drawing.Point(172, 600);
            this.TextBox12.Name = "TextBox12";
            this.TextBox12.Size = new System.Drawing.Size(492, 25);
            this.TextBox12.TabIndex = 16;
            // 
            // ComboBoxTimeCheck
            // 
            this.ComboBoxTimeCheck.Items = new List<string>{
            "True",
            "False"};
            this.ComboBoxTimeCheck.Location = new System.Drawing.Point(172, 631);
            this.ComboBoxTimeCheck.Name = "ComboBoxTimeCheck";
            this.ComboBoxTimeCheck.Size = new System.Drawing.Size(160, 25);
            this.ComboBoxTimeCheck.TabIndex = 17;
            this.ComboBoxTimeCheck.SelectedIndexChanged += new System.EventHandler(this.ComboBoxTimeCheck_SelectedIndexChanged);
            // 
            // Label4
            // 
            this.Label4.AutoSize = true;
            this.Label4.Location = new System.Drawing.Point(8, 356);
            this.Label4.MinimumSize = new System.Drawing.Size(160, 17);
            this.Label4.Name = "Label4";
            this.Label4.Size = new System.Drawing.Size(160, 17);
            this.Label4.TabIndex = 22;
            this.Label4.Text = "LABEL4";
            this.Label4.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // TextBox4
            // 
            this.TextBox4.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.TextBox4.Location = new System.Drawing.Point(172, 353);
            this.TextBox4.Name = "TextBox4";
            this.TextBox4.Size = new System.Drawing.Size(492, 25);
            this.TextBox4.TabIndex = 8;
            // 
            // Label5
            // 
            this.Label5.AutoSize = true;
            this.Label5.Location = new System.Drawing.Point(6, 387);
            this.Label5.MinimumSize = new System.Drawing.Size(160, 17);
            this.Label5.Name = "Label5";
            this.Label5.Size = new System.Drawing.Size(160, 17);
            this.Label5.TabIndex = 24;
            this.Label5.Text = "LABEL5";
            this.Label5.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // Label6
            // 
            this.Label6.AutoSize = true;
            this.Label6.Location = new System.Drawing.Point(6, 418);
            this.Label6.MinimumSize = new System.Drawing.Size(160, 17);
            this.Label6.Name = "Label6";
            this.Label6.Size = new System.Drawing.Size(160, 17);
            this.Label6.TabIndex = 25;
            this.Label6.Text = "LABEL6";
            this.Label6.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // Label7
            // 
            this.Label7.AutoSize = true;
            this.Label7.Location = new System.Drawing.Point(6, 449);
            this.Label7.MinimumSize = new System.Drawing.Size(160, 17);
            this.Label7.Name = "Label7";
            this.Label7.Size = new System.Drawing.Size(160, 17);
            this.Label7.TabIndex = 26;
            this.Label7.Text = "LABEL7";
            this.Label7.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // Label8
            // 
            this.Label8.AutoSize = true;
            this.Label8.Location = new System.Drawing.Point(6, 480);
            this.Label8.MinimumSize = new System.Drawing.Size(160, 17);
            this.Label8.Name = "Label8";
            this.Label8.Size = new System.Drawing.Size(160, 17);
            this.Label8.TabIndex = 27;
            this.Label8.Text = "LABEL8";
            this.Label8.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // Label10
            // 
            this.Label10.AutoSize = true;
            this.Label10.Location = new System.Drawing.Point(6, 542);
            this.Label10.MinimumSize = new System.Drawing.Size(160, 17);
            this.Label10.Name = "Label10";
            this.Label10.Size = new System.Drawing.Size(160, 17);
            this.Label10.TabIndex = 28;
            this.Label10.Text = "LABEL10";
            this.Label10.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // Label9
            // 
            this.Label9.AutoSize = true;
            this.Label9.Location = new System.Drawing.Point(6, 511);
            this.Label9.MinimumSize = new System.Drawing.Size(160, 17);
            this.Label9.Name = "Label9";
            this.Label9.Size = new System.Drawing.Size(160, 17);
            this.Label9.TabIndex = 29;
            this.Label9.Text = "LABEL9";
            this.Label9.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // Label11
            // 
            this.Label11.AutoSize = true;
            this.Label11.Location = new System.Drawing.Point(8, 573);
            this.Label11.MinimumSize = new System.Drawing.Size(160, 17);
            this.Label11.Name = "Label11";
            this.Label11.Size = new System.Drawing.Size(160, 17);
            this.Label11.TabIndex = 30;
            this.Label11.Text = "LABEL11";
            this.Label11.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // Label12
            // 
            this.Label12.AutoSize = true;
            this.Label12.Location = new System.Drawing.Point(8, 603);
            this.Label12.MinimumSize = new System.Drawing.Size(160, 17);
            this.Label12.Name = "Label12";
            this.Label12.Size = new System.Drawing.Size(160, 17);
            this.Label12.TabIndex = 31;
            this.Label12.Text = "LABEL12";
            this.Label12.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // labelTimeCheck
            // 
            this.LabelTimeCheck.AutoSize = true;
            this.LabelTimeCheck.Location = new System.Drawing.Point(78, 635);
            this.LabelTimeCheck.Name = "labelTimeCheck";
            this.LabelTimeCheck.Size = new System.Drawing.Size(88, 17);
            this.LabelTimeCheck.TabIndex = 32;
            this.LabelTimeCheck.Text = "TIME_CHECK";
            this.LabelTimeCheck.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // ComboBoxLetter
            // 
            this.ComboBoxLetter.Location = new System.Drawing.Point(172, 195);
            this.ComboBoxLetter.Name = "ComboBoxLetter";
            this.ComboBoxLetter.Size = new System.Drawing.Size(160, 25);
            this.ComboBoxLetter.TabIndex = 4;
            this.ComboBoxLetter.SelectedIndexChanged += new System.EventHandler(this.ComboBoxLetter_SelectedIndexChanged);
            // 
            // ButtonNext
            // 
            this.ButtonNext.Location = new System.Drawing.Point(338, 194);
            this.ButtonNext.Name = "ButtonNext";
            this.ButtonNext.Size = new System.Drawing.Size(160, 28);
            this.ButtonNext.TabIndex = 34;
            this.ButtonNext.Text = "Next";
            this.ButtonNext.UseVisualStyleBackColor = true;
            this.ButtonNext.Click += new System.EventHandler(this.ButtonNext_Click);
            // 
            // LabelMode
            // 
            this.LabelMode.AutoSize = true;
            this.LabelMode.Location = new System.Drawing.Point(8, 232);
            this.LabelMode.MinimumSize = new System.Drawing.Size(160, 17);
            this.LabelMode.Name = "LabelMode";
            this.LabelMode.Size = new System.Drawing.Size(160, 17);
            this.LabelMode.TabIndex = 61;
            this.LabelMode.Text = "MODE";
            this.LabelMode.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // TextBoxMode
            // 
            this.TextBoxMode.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.TextBoxMode.Location = new System.Drawing.Point(172, 229);
            this.TextBoxMode.Name = "TextBoxMode";
            this.TextBoxMode.ReadOnly = true;
            this.TextBoxMode.Size = new System.Drawing.Size(462, 25);
            this.TextBoxMode.TabIndex = 62;
            this.TextBoxMode.MouseUp += new System.Windows.Forms.MouseEventHandler(this.TextBoxMode_MouseUp);
            // 
            // labelATIS
            // 
            this.LabelATIS.AutoSize = true;
            this.LabelATIS.Location = new System.Drawing.Point(128, 198);
            this.LabelATIS.Name = "labelATIS";
            this.LabelATIS.Size = new System.Drawing.Size(40, 17);
            this.LabelATIS.TabIndex = 35;
            this.LabelATIS.Text = "ATIS";
            this.LabelATIS.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // LabelMETAR
            // 
            this.LabelMETAR.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
            this.LabelMETAR.Location = new System.Drawing.Point(10, 156);
            this.LabelMETAR.Name = "LabelMETAR";
            this.LabelMETAR.Size = new System.Drawing.Size(654, 36);
            this.LabelMETAR.TabIndex = 37;
            this.LabelMETAR.Text = "LOADING";
            this.LabelMETAR.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ButtonSave
            // 
            this.ButtonSave.Location = new System.Drawing.Point(172, 662);
            this.ButtonSave.Name = "ButtonSave";
            this.ButtonSave.Size = new System.Drawing.Size(245, 28);
            this.ButtonSave.TabIndex = 18;
            this.ButtonSave.Text = "Save";
            this.ButtonSave.UseVisualStyleBackColor = true;
            this.ButtonSave.Click += new System.EventHandler(this.ButtonSave_Click);
            // 
            // ButtonCancel
            // 
            this.ButtonCancel.Location = new System.Drawing.Point(423, 662);
            this.ButtonCancel.Name = "ButtonCancel";
            this.ButtonCancel.Size = new System.Drawing.Size(241, 28);
            this.ButtonCancel.TabIndex = 19;
            this.ButtonCancel.Text = "Cancel";
            this.ButtonCancel.UseVisualStyleBackColor = true;
            this.ButtonCancel.Click += new System.EventHandler(this.ButtonCancel_Click);
            // 
            // ButtonListen
            // 
            this.ButtonListen.Location = new System.Drawing.Point(338, 125);
            this.ButtonListen.Name = "ButtonListen";
            this.ButtonListen.Size = new System.Drawing.Size(160, 28);
            this.ButtonListen.TabIndex = 40;
            this.ButtonListen.Text = "Listen";
            this.ButtonListen.UseVisualStyleBackColor = false;
            this.ButtonListen.Click += new System.EventHandler(this.ButtonListen_Click);
            // 
            // ButtonGetMetar
            // 
            this.ButtonGetMetar.Location = new System.Drawing.Point(172, 125);
            this.ButtonGetMetar.Name = "ButtonGetMetar";
            this.ButtonGetMetar.Size = new System.Drawing.Size(160, 28);
            this.ButtonGetMetar.TabIndex = 41;
            this.ButtonGetMetar.Text = "Load METAR";
            this.ButtonGetMetar.UseVisualStyleBackColor = false;
            this.ButtonGetMetar.Click += new System.EventHandler(this.ButtonGetMetar_Click);
            // 
            // ButtonBroadcast
            // 
            this.ButtonBroadcast.Location = new System.Drawing.Point(504, 125);
            this.ButtonBroadcast.Name = "ButtonBroadcast";
            this.ButtonBroadcast.Size = new System.Drawing.Size(160, 28);
            this.ButtonBroadcast.TabIndex = 42;
            this.ButtonBroadcast.Text = "Broadcast";
            this.ButtonBroadcast.UseVisualStyleBackColor = true;
            this.ButtonBroadcast.Click += new System.EventHandler(this.ButtonBroadcast_Click);
            // 
            // ComboBoxVoice
            // 
            this.ComboBoxVoice.Enabled = false;
            this.ComboBoxVoice.Location = new System.Drawing.Point(172, 94);
            this.ComboBoxVoice.Name = "ComboBoxVoice";
            this.ComboBoxVoice.Size = new System.Drawing.Size(326, 25);
            this.ComboBoxVoice.TabIndex = 2;
            this.ComboBoxVoice.SelectedIndexChanged += new System.EventHandler(this.ComboBoxVoice_SelectedIndexChanged);
            // 
            // ComboBoxRate
            // 
            this.ComboBoxRate.Enabled = false;
            this.ComboBoxRate.Items = new List<string>(){
            "Extra Fast",
            "Fast",
            "Medium",
            "Slow",
            "Extra Slow"};
            this.ComboBoxRate.Location = new System.Drawing.Point(504, 94);
            this.ComboBoxRate.Name = "ComboBoxRate";
            this.ComboBoxRate.Size = new System.Drawing.Size(160, 25);
            this.ComboBoxRate.TabIndex = 3;
            this.ComboBoxRate.SelectedIndexChanged += new System.EventHandler(this.ComboBoxRate_SelectedIndexChanged);
            // 
            // ButtonDelete
            // 
            this.ButtonDelete.Location = new System.Drawing.Point(504, 57);
            this.ButtonDelete.Name = "ButtonDelete";
            this.ButtonDelete.Size = new System.Drawing.Size(160, 28);
            this.ButtonDelete.TabIndex = 45;
            this.ButtonDelete.Text = "Delete";
            this.ButtonDelete.UseVisualStyleBackColor = true;
            this.ButtonDelete.Click += new System.EventHandler(this.ButtonDelete_Click);
            // 
            // labelVoice
            // 
            this.LabelVoice.AutoSize = true;
            this.LabelVoice.Location = new System.Drawing.Point(118, 97);
            this.LabelVoice.Name = "labelVoice";
            this.LabelVoice.Size = new System.Drawing.Size(48, 17);
            this.LabelVoice.TabIndex = 46;
            this.LabelVoice.Text = "Voice";
            // 
            // ButtonATIS1
            // 
            this.ButtonATIS1.Location = new System.Drawing.Point(6, 8);
            this.ButtonATIS1.Name = "ButtonATIS1";
            this.ButtonATIS1.Size = new System.Drawing.Size(160, 44);
            this.ButtonATIS1.TabIndex = 47;
            this.ButtonATIS1.Text = "ATIS #1";
            this.ButtonATIS1.UseVisualStyleBackColor = true;
            this.ButtonATIS1.Click += new System.EventHandler(this.ButtonATIS1_Click);
            // 
            // ButtonATIS2
            // 
            this.ButtonATIS2.Location = new System.Drawing.Point(172, 8);
            this.ButtonATIS2.Name = "ButtonATIS2";
            this.ButtonATIS2.Size = new System.Drawing.Size(160, 44);
            this.ButtonATIS2.TabIndex = 48;
            this.ButtonATIS2.Text = "ATIS #2";
            this.ButtonATIS2.UseVisualStyleBackColor = true;
            this.ButtonATIS2.Click += new System.EventHandler(this.ButtonATIS2_Click);
            // 
            // ButtonATIS3
            // 
            this.ButtonATIS3.Location = new System.Drawing.Point(338, 8);
            this.ButtonATIS3.Name = "ButtonATIS3";
            this.ButtonATIS3.Size = new System.Drawing.Size(160, 44);
            this.ButtonATIS3.TabIndex = 49;
            this.ButtonATIS3.Text = "ATIS #3";
            this.ButtonATIS3.UseVisualStyleBackColor = true;
            this.ButtonATIS3.Click += new System.EventHandler(this.ButtonATIS3_Click);
            // 
            // ButtonATIS4
            // 
            this.ButtonATIS4.Location = new System.Drawing.Point(504, 8);
            this.ButtonATIS4.Name = "ButtonATIS4";
            this.ButtonATIS4.Size = new System.Drawing.Size(160, 44);
            this.ButtonATIS4.TabIndex = 50;
            this.ButtonATIS4.Text = "ATIS #4";
            this.ButtonATIS4.UseVisualStyleBackColor = true;
            this.ButtonATIS4.Click += new System.EventHandler(this.ButtonATIS4_Click);
            // 
            // TextBoxZulu
            // 
            this.TextBoxZulu.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.TextBoxZulu.Location = new System.Drawing.Point(172, 261);
            this.TextBoxZulu.Multiline = true;
            this.TextBoxZulu.Name = "TextBoxZulu";
            this.TextBoxZulu.Size = new System.Drawing.Size(492, 365);
            this.TextBoxZulu.TabIndex = 52;
            this.TextBoxZulu.Visible = false;
            this.TextBoxZulu.TextChanged += new System.EventHandler(this.TextBox_TextChanged);
            // 
            // ButtonZulu
            // 
            this.ButtonZulu.CausesValidation = false;
            this.ButtonZulu.Location = new System.Drawing.Point(504, 194);
            this.ButtonZulu.Name = "ButtonZulu";
            this.ButtonZulu.Size = new System.Drawing.Size(160, 28);
            this.ButtonZulu.TabIndex = 53;
            this.ButtonZulu.Text = "Zulu";
            this.ButtonZulu.UseVisualStyleBackColor = true;
            this.ButtonZulu.Click += new System.EventHandler(this.ButtonZulu_Click);
            // 
            // ComboBoxZuluFrequency
            // 
            this.ComboBoxZuluFrequency.Location = new System.Drawing.Point(172, 631);
            this.ComboBoxZuluFrequency.Name = "ComboBoxZuluFrequency";
            this.ComboBoxZuluFrequency.Size = new System.Drawing.Size(492, 25);
            this.ComboBoxZuluFrequency.TabIndex = 54;
            this.ComboBoxZuluFrequency.SelectedIndexChanged += new System.EventHandler(this.ComboBoxZuluFrequency_SelectedIndexChanged);
            // 
            // LabelFrequency
            // 
            this.LabelFrequency.AutoSize = true;
            this.LabelFrequency.Location = new System.Drawing.Point(86, 635);
            this.LabelFrequency.Name = "LabelFrequency";
            this.LabelFrequency.Size = new System.Drawing.Size(80, 17);
            this.LabelFrequency.TabIndex = 55;
            this.LabelFrequency.Text = "FREQUENCY";
            this.LabelFrequency.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // label13
            // 
            this.LabelWindCalculator.AutoSize = true;
            this.LabelWindCalculator.Location = new System.Drawing.Point(6, 715);
            this.LabelWindCalculator.MinimumSize = new System.Drawing.Size(160, 17);
            this.LabelWindCalculator.Name = "label13";
            this.LabelWindCalculator.Size = new System.Drawing.Size(160, 25);
            this.LabelWindCalculator.TabIndex = 56;
            this.LabelWindCalculator.Text = "Wind Calculator";
            this.LabelWindCalculator.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // ComboBoxRunway
            // 
            this.ComboBoxRunway.Location = new System.Drawing.Point(172, 712);
            this.ComboBoxRunway.Name = "ComboBoxRunway";
            this.ComboBoxRunway.Size = new System.Drawing.Size(160, 25);
            this.ComboBoxRunway.TabIndex = 57;
            this.ComboBoxRunway.SelectedIndexChanged += new System.EventHandler(this.ComboBoxRunway_SelectedIndexChanged);
            // 
            // LabelWindComponents
            // 
            this.LabelWindComponents.AutoSize = true;
            this.LabelWindComponents.Location = new System.Drawing.Point(338, 715);
            this.LabelWindComponents.MinimumSize = new System.Drawing.Size(320, 0);
            this.LabelWindComponents.Name = "LabelWindComponents";
            this.LabelWindComponents.Size = new System.Drawing.Size(320, 17);
            this.LabelWindComponents.TabIndex = 58;
            this.LabelWindComponents.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            // 
            // ButtonRecord
            // 
            this.ButtonRecord.Location = new System.Drawing.Point(504, 92);
            this.ButtonRecord.Name = "ButtonRecord";
            this.ButtonRecord.Size = new System.Drawing.Size(160, 28);
            this.ButtonRecord.TabIndex = 59;
            this.ButtonRecord.Text = "Record";
            this.ButtonRecord.UseVisualStyleBackColor = true;
            this.ButtonRecord.Click += new System.EventHandler(this.ButtonRecord_Click);
            // 
            // EditorWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoValidate = System.Windows.Forms.AutoValidate.Disable;
            this.ClientSize = new System.Drawing.Size(676, 753);
            this.Controls.Add(this.TextBoxMode);
            this.Controls.Add(this.LabelMode);
            this.Controls.Add(this.ButtonRecord);
            this.Controls.Add(this.LabelWindComponents);
            this.Controls.Add(this.ComboBoxRunway);
            this.Controls.Add(this.LabelWindCalculator);
            this.Controls.Add(this.LabelFrequency);
            this.Controls.Add(this.ComboBoxZuluFrequency);
            this.Controls.Add(this.ButtonZulu);
            this.Controls.Add(this.ButtonATIS4);
            this.Controls.Add(this.ButtonATIS3);
            this.Controls.Add(this.ButtonATIS2);
            this.Controls.Add(this.ButtonATIS1);
            this.Controls.Add(this.LabelVoice);
            this.Controls.Add(this.ButtonDelete);
            this.Controls.Add(this.ComboBoxRate);
            this.Controls.Add(this.ComboBoxVoice);
            this.Controls.Add(this.ButtonBroadcast);
            this.Controls.Add(this.ButtonGetMetar);
            this.Controls.Add(this.ButtonListen);
            this.Controls.Add(this.ButtonCancel);
            this.Controls.Add(this.ButtonSave);
            this.Controls.Add(this.LabelMETAR);
            this.Controls.Add(this.LabelATIS);
            this.Controls.Add(this.ButtonNext);
            this.Controls.Add(this.ComboBoxLetter);
            this.Controls.Add(this.LabelTimeCheck);
            this.Controls.Add(this.Label12);
            this.Controls.Add(this.Label11);
            this.Controls.Add(this.Label9);
            this.Controls.Add(this.Label10);
            this.Controls.Add(this.Label8);
            this.Controls.Add(this.Label7);
            this.Controls.Add(this.Label6);
            this.Controls.Add(this.Label5);
            this.Controls.Add(this.TextBox4);
            this.Controls.Add(this.Label4);
            this.Controls.Add(this.ComboBoxTimeCheck);
            this.Controls.Add(this.TextBox12);
            this.Controls.Add(this.TextBox11);
            this.Controls.Add(this.TextBox10);
            this.Controls.Add(this.TextBox9);
            this.Controls.Add(this.TextBox8);
            this.Controls.Add(this.TextBox7);
            this.Controls.Add(this.TextBox6);
            this.Controls.Add(this.TextBox5);
            this.Controls.Add(this.Label3);
            this.Controls.Add(this.Label2);
            this.Controls.Add(this.Label1);
            this.Controls.Add(this.TextBox3);
            this.Controls.Add(this.TextBox2);
            this.Controls.Add(this.TextBox1);
            this.Controls.Add(this.ComboBoxAirport);
            this.Controls.Add(this.ButtonCreate);
            this.Controls.Add(this.LabelICAO);
            this.Controls.Add(this.TextBoxZulu);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.HideOnClose = true;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximumSize = new System.Drawing.Size(680, 750);
            this.MinimumSize = new System.Drawing.Size(680, 750);
            this.Name = "EditorWindow";
            this.Resizeable = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = $"ATIS Editor";
            this.TitleTextColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.Load += new System.EventHandler(this.EditorWindow_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private TextLabel LabelICAO;
        private GenericButton ButtonCreate;
        private DropDownBox ComboBoxAirport;
        private TextField TextBox1;
        private TextField TextBox2;
        private TextField TextBox3;
        private TextField TextBox4;
        private TextField TextBox5;
        private TextField TextBox6;
        private TextField TextBox7;
        private TextField TextBox8;
        private TextField TextBox9;
        private TextField TextBox10;
        private TextField TextBox11;
        private TextField TextBox12;
        private Label Label1;
        private Label Label2;
        private Label Label3;
        private Label Label4;
        private Label Label5;
        private Label Label6;
        private Label Label7;
        private Label Label8;
        private Label Label10;
        private Label Label9;
        private Label Label11;
        private Label Label12;
        private Label LabelTimeCheck;
        private DropDownBox ComboBoxTimeCheck;
        private DropDownBox ComboBoxLetter;
        private GenericButton ButtonNext;
        private Label LabelATIS;
        private TextLabel LabelMETAR;
        private GenericButton ButtonSave;
        private GenericButton ButtonCancel;
        private ToggleButton ButtonListen;
        private GenericButton ButtonGetMetar;
        private ToggleButton ButtonBroadcast;
        private DropDownBox ComboBoxVoice;
        private DropDownBox ComboBoxRate;
        private GenericButton ButtonDelete;
        private TextLabel LabelVoice;
        private ToggleButton ButtonATIS1;
        private ToggleButton ButtonATIS2;
        private ToggleButton ButtonATIS3;
        private ToggleButton ButtonATIS4;
        private TextField TextBoxZulu;
        private ToggleButton ButtonZulu;
        private DropDownBox ComboBoxZuluFrequency;
        private Label LabelFrequency;
        private TextLabel LabelWindCalculator;
        private DropDownBox ComboBoxRunway;
        private Label LabelWindComponents;
        private ToggleButton ButtonRecord;
        private Label LabelMode;
        private TextField TextBoxMode;
    }
}