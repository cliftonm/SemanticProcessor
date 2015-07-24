using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using Clifton.Semantics;

namespace WinFormDemo
{
	static class Program
	{
		public static SemanticProcessor SemProc;

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Form1 form = new Form1();
			SemProc = new SemanticProcessor();
			SemProc.Register<LoggerMembrane, LoggingReceptor>();
			// DistReceptor = new DistributedComputingReceptor();
			// InitializeSemantics(form);

			Application.Run(form);
		}

		static void InitializeSemantics(IReceptor form)
		{
			// SemProc = new SemanticProcessor();
			// SemProc.Register<SurfaceMembrane, FeedReaderReceptor>();
			// SemProc.Register<LoggerMembrane, LoggingReceptor>();
		}
	}
}
