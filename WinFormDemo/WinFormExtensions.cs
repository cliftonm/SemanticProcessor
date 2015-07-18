using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormDemo
{
	public static class WinFormExtensions
	{
		public static void BeginInvoke(this Control control, Action action)
		{
			if (control.InvokeRequired)
			{
				control.BeginInvoke((Delegate)action);
			}
			else
			{
				action();
			}
		}
	}
}
