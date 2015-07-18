using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clifton.Semantics
{
	public class Membrane : IMembrane
	{
		public Membrane Parent { get; set; }
		public List<Membrane> Children { get; set; }

		public Membrane()
		{
			Parent = null;
			Children = new List<Membrane>();
		}
	}

	/// <summary>
	/// Type for our built-in membrane
	/// </summary>
	public class SurfaceMembrane : Membrane
	{
	}

	/// <summary>
	/// Type for our built-in membrane
	/// </summary>
	public class LoggerMembrane : Membrane
	{
	}
}
