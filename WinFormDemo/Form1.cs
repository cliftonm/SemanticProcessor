using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Clifton.Extensions;
using Clifton.Semantics;

namespace WinFormDemo
{
	public partial class Form1 : Form, IReceptor
	{
		public Form1()
		{
			InitializeComponent();
			Shown += FormShown;
		}

		protected void FormShown(object sender, EventArgs e)
		{
			// Put a feed reader into each membrane:
			Program.SemProc.Register<MyHoroscopeMembrane, FeedReaderReceptor>();
			Program.SemProc.Register<PartnerHoroscopeMembrane, FeedReaderReceptor>();

			// Put the horoscope readers into each membrane;
			Program.SemProc.Register<MyHoroscopeMembrane>(new HoroscopeReceptor(lblPleaseWait, tbHoroscope, "Your Horoscope:"));
			Program.SemProc.Register<PartnerHoroscopeMembrane>(new HoroscopeReceptor(lblPleaseWaitPartner, tbHoroscopePartner, "Partner's Horoscope:"));

			// We've now effectively wired up a specific feed reader receptor to a specific horoscope receptor.
			// When a feed reader processes the sign, it will emit the summary into its membrane, to be picked up by the
			// horoscope reader receptor in that membrane.

			// Register our logger receptor.
			Program.SemProc.Register<LoggerMembrane>(this);
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, ST_Log log)
		{
			this.BeginInvoke(() =>
				{
					tbLog.AppendText(log.Message+"\r\n");
				});
		}

		private void cbSign_SelectedIndexChanged(object sender, EventArgs e)
		{
			lblPleaseWait.Text = "Please Wait...";
			lblPleaseWait.Visible = true;
			tbHoroscope.Text = "";

			Program.SemProc.ProcessInstance<MyHoroscopeMembrane, ST_Url>
				(url => url.Url = String.Format("http://www.findyourfate.com/rss/dailyhoroscope-feed.asp?sign={0}", cbSign.SelectedItem.ToString()));
		}

		private void cbSignPartner_SelectedIndexChanged(object sender, EventArgs e)
		{
			lblPleaseWaitPartner.Text = "Please Wait...";
			lblPleaseWaitPartner.Visible = true;
			tbHoroscopePartner.Text = "";

			Program.SemProc.ProcessInstance<PartnerHoroscopeMembrane, ST_Url>
				(url => url.Url = String.Format("http://www.findyourfate.com/rss/dailyhoroscope-feed.asp?sign={0}", cbSignPartner.SelectedItem.ToString()));
		}
	}
}
