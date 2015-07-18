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
		}

		public void Process(ISemanticProcessor proc, ST_RssFeedItem feedItem)
		{
			this.BeginInvoke(() =>
				{
					lblPleaseWait.Text = "Your Horoscope:";
					tbHoroscope.Text = feedItem.Text;
				});
		}

		public void Process(ISemanticProcessor proc, ST_Log log)
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

			Program.SemProc.ProcessInstance(new ST_Url() 
			{ 
				Url = String.Format("http://www.findyourfate.com/rss/dailyhoroscope-feed.asp?sign={0}", cbSign.SelectedItem.ToString())
			});
		}
	}
}
