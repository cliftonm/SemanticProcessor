using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Clifton.Extensions;
using Clifton.Semantics;

namespace WinFormDemo
{
	public class HoroscopeReceptor : IReceptor
	{
		protected Label lbl;
		protected TextBox tb;
		protected string labelText;

		public HoroscopeReceptor(Label lbl, TextBox tb, string labelText)
		{
			this.lbl = lbl;
			this.tb = tb;
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, ST_RssFeedItem feedItem)
		{
			lbl.BeginInvoke(() =>
			{
				lbl.Text = labelText;
				tb.Text = feedItem.Text;
			});
		}
	}
}
