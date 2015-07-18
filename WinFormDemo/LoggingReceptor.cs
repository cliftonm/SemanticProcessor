using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Clifton.Semantics;

namespace WinFormDemo
{
	/// <summary>
	/// We create a receptor specifically to capture all semantic types.  This must be done in a 
	/// receptor that doesn't process any other types, otherwise the ISemanticType interface gets
	/// superceded by the concrete install handler.  What we do here is reformat the information
	/// into a specific Log sementic type.
	/// </summary>
	public class LoggingReceptor : IReceptor
	{
		public void Process(ISemanticProcessor proc, IMembrane membrane, ISemanticType type)
		{
			// Don't log our log message, otherewise we get an infinite loop!
			if (!(type is ST_Log))
			{
				proc.ProcessInstance(proc.Logger, new ST_Log() { Message = type.GetType().ToString() });
			}
		}
	}
}
