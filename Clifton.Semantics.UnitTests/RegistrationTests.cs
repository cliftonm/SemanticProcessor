using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using Clifton.Semantics;

namespace Clifton.Semantics.UnitTests
{
	[TestFixture]
	public class RegistrationTests
    {
		class TestMembrane : IMembrane { }

		/// <summary>
		/// Registering a membrane creates an instance of that membrane.
		/// </summary>
		[Test]
		public void RegisterMembraneType()
		{
			SemanticProcessor sp = new SemanticProcessor();
			IMembrane membrane = sp.RegisterMembrane<TestMembrane>();
			Assert.That(sp.Membranes.Contains(membrane), "Expected membrane instance.");
		}

		/// <summary>
		/// Registering the same membrane type returns the same instance.
		/// </summary>
		[Test]
		public void RegisterSameMembraneType()
		{
			SemanticProcessor sp = new SemanticProcessor();
			IMembrane membrane1 = sp.RegisterMembrane<TestMembrane>();
			IMembrane membrane2 = sp.RegisterMembrane<TestMembrane>();
			Assert.That(membrane1 == membrane2, "Expected the same membrane instance.");
		}
    }
}
