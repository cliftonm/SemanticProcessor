namespace WinFormDemo
{
	partial class Form1
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
			this.label1 = new System.Windows.Forms.Label();
			this.cbSign = new System.Windows.Forms.ComboBox();
			this.lblPleaseWait = new System.Windows.Forms.Label();
			this.tbHoroscope = new System.Windows.Forms.TextBox();
			this.tbLog = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.tbHoroscopePartner = new System.Windows.Forms.TextBox();
			this.lblPleaseWaitPartner = new System.Windows.Forms.Label();
			this.cbSignPartner = new System.Windows.Forms.ComboBox();
			this.label4 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(13, 13);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(89, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Select Your Sign:";
			// 
			// cbSign
			// 
			this.cbSign.FormattingEnabled = true;
			this.cbSign.Items.AddRange(new object[] {
            "Aquarius",
            "Pisces",
            "Aries",
            "Taurus",
            "Gemini",
            "Cancer",
            "Leo",
            "Virgo",
            "Libra",
            "Scorpio",
            "Sagittarius",
            "Capricorn"});
			this.cbSign.Location = new System.Drawing.Point(108, 10);
			this.cbSign.Name = "cbSign";
			this.cbSign.Size = new System.Drawing.Size(180, 21);
			this.cbSign.TabIndex = 0;
			this.cbSign.SelectedIndexChanged += new System.EventHandler(this.cbSign_SelectedIndexChanged);
			// 
			// lblPleaseWait
			// 
			this.lblPleaseWait.AutoSize = true;
			this.lblPleaseWait.Location = new System.Drawing.Point(16, 53);
			this.lblPleaseWait.Name = "lblPleaseWait";
			this.lblPleaseWait.Size = new System.Drawing.Size(73, 13);
			this.lblPleaseWait.TabIndex = 2;
			this.lblPleaseWait.Text = "Please Wait...";
			this.lblPleaseWait.Visible = false;
			// 
			// tbHoroscope
			// 
			this.tbHoroscope.Location = new System.Drawing.Point(19, 69);
			this.tbHoroscope.Multiline = true;
			this.tbHoroscope.Name = "tbHoroscope";
			this.tbHoroscope.ReadOnly = true;
			this.tbHoroscope.Size = new System.Drawing.Size(357, 130);
			this.tbHoroscope.TabIndex = 0;
			this.tbHoroscope.TabStop = false;
			// 
			// tbLog
			// 
			this.tbLog.Location = new System.Drawing.Point(19, 228);
			this.tbLog.Multiline = true;
			this.tbLog.Name = "tbLog";
			this.tbLog.ReadOnly = true;
			this.tbLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.tbLog.Size = new System.Drawing.Size(357, 122);
			this.tbLog.TabIndex = 2;
			this.tbLog.TabStop = false;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(16, 212);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(28, 13);
			this.label2.TabIndex = 5;
			this.label2.Text = "Log:";
			this.label2.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
			// 
			// tbHoroscopePartner
			// 
			this.tbHoroscopePartner.Location = new System.Drawing.Point(388, 69);
			this.tbHoroscopePartner.Multiline = true;
			this.tbHoroscopePartner.Name = "tbHoroscopePartner";
			this.tbHoroscopePartner.ReadOnly = true;
			this.tbHoroscopePartner.Size = new System.Drawing.Size(357, 130);
			this.tbHoroscopePartner.TabIndex = 1;
			this.tbHoroscopePartner.TabStop = false;
			// 
			// lblPleaseWaitPartner
			// 
			this.lblPleaseWaitPartner.AutoSize = true;
			this.lblPleaseWaitPartner.Location = new System.Drawing.Point(385, 53);
			this.lblPleaseWaitPartner.Name = "lblPleaseWaitPartner";
			this.lblPleaseWaitPartner.Size = new System.Drawing.Size(73, 13);
			this.lblPleaseWaitPartner.TabIndex = 8;
			this.lblPleaseWaitPartner.Text = "Please Wait...";
			this.lblPleaseWaitPartner.Visible = false;
			// 
			// cbSignPartner
			// 
			this.cbSignPartner.FormattingEnabled = true;
			this.cbSignPartner.Items.AddRange(new object[] {
            "Aquarius",
            "Pisces",
            "Aries",
            "Taurus",
            "Gemini",
            "Cancer",
            "Leo",
            "Virgo",
            "Libra",
            "Scorpio",
            "Sagittarius",
            "Capricorn"});
			this.cbSignPartner.Location = new System.Drawing.Point(477, 10);
			this.cbSignPartner.Name = "cbSignPartner";
			this.cbSignPartner.Size = new System.Drawing.Size(180, 21);
			this.cbSignPartner.TabIndex = 1;
			this.cbSignPartner.SelectedIndexChanged += new System.EventHandler(this.cbSignPartner_SelectedIndexChanged);
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(382, 13);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(75, 13);
			this.label4.TabIndex = 6;
			this.label4.Text = "Partner\'s Sign:";
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(844, 362);
			this.Controls.Add(this.tbHoroscopePartner);
			this.Controls.Add(this.lblPleaseWaitPartner);
			this.Controls.Add(this.cbSignPartner);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.tbLog);
			this.Controls.Add(this.tbHoroscope);
			this.Controls.Add(this.lblPleaseWait);
			this.Controls.Add(this.cbSign);
			this.Controls.Add(this.label1);
			this.Name = "Form1";
			this.Text = "Semantic Processor Demo";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ComboBox cbSign;
		private System.Windows.Forms.Label lblPleaseWait;
		private System.Windows.Forms.TextBox tbHoroscope;
		private System.Windows.Forms.TextBox tbLog;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox tbHoroscopePartner;
		private System.Windows.Forms.Label lblPleaseWaitPartner;
		private System.Windows.Forms.ComboBox cbSignPartner;
		private System.Windows.Forms.Label label4;
	}
}

