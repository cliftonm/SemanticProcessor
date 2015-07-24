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
	public class MembraneTests
	{
		public static bool callSuccess;

		class TestMembrane : Membrane { }
		class OuterMembrane : Membrane { }
		class InnerMembrane : Membrane { }
		class InnerMembrane2 : Membrane { }
		public class TestSemanticType : ISemanticType { }

		public class TestReceptor : IReceptor
		{
			public void Process(ISemanticProcessor proc, IMembrane membrane, TestSemanticType t)
			{
				callSuccess = true;
			}
		}

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

		/// <summary>
		/// Verify that, when the inner membrane is permeable outbound to a type,
		/// that a receptor in the outer membrane receive the type.
		/// </summary>
		[Test]
		public void TypePermeatesOut()
		{
			callSuccess = false;
			SemanticProcessor sp = new SemanticProcessor();
			sp.OutboundPermeableTo<InnerMembrane, TestSemanticType>();
			sp.InboundPermeableTo<OuterMembrane, TestSemanticType>();
			sp.AddChild<OuterMembrane, InnerMembrane>();
			sp.Register<OuterMembrane, TestReceptor>();
			sp.ProcessInstance<InnerMembrane, TestSemanticType>(true);
			Assert.That(callSuccess, "Expected receptor in outer membrane to process the ST placed in the inner membrane.");
		}

		/// <summary>
		/// Verify that, when the inner membrane is permeable inbound to a type,
		/// that a receptor in the inner membrane receives the type.
		/// </summary>
		[Test]
		public void TypePermeatesIn()
		{
			callSuccess = false;
			SemanticProcessor sp = new SemanticProcessor();
			sp.OutboundPermeableTo<OuterMembrane, TestSemanticType>();
			sp.InboundPermeableTo<InnerMembrane, TestSemanticType>();
			sp.AddChild<OuterMembrane, InnerMembrane>();
			sp.Register<InnerMembrane, TestReceptor>();
			sp.ProcessInstance<OuterMembrane, TestSemanticType>(true);
			Assert.That(callSuccess, "Expected receptor in inner membrane to process the ST placed in the outer membrane.");
		}

		/// <summary>
		/// Verify that a type issued in one inner membrane can cross over to
		/// an adjacent inner membrane via outbound permeability on the source
		/// and inbound permeability on the target membrane.
		/// </summary>
		[Test]
		public void TypePermeatesAcross()
		{
			callSuccess = false;
			SemanticProcessor sp = new SemanticProcessor();
			sp.OutboundPermeableTo<InnerMembrane, TestSemanticType>();
			sp.InboundPermeableTo<OuterMembrane, TestSemanticType>();
			sp.OutboundPermeableTo<OuterMembrane, TestSemanticType>();
			sp.InboundPermeableTo<InnerMembrane2, TestSemanticType>();
			sp.AddChild<OuterMembrane, InnerMembrane>();
			sp.AddChild<OuterMembrane, InnerMembrane2>();
			sp.Register<InnerMembrane2, TestReceptor>();
			sp.ProcessInstance<InnerMembrane, TestSemanticType>(true);
			Assert.That(callSuccess, "Expected receptor in inner membrane to process the ST placed in the adjacent inner membrane.");
		}

		/// <summary>
		/// Outer membrane does not receive semantic type if inner membrane is not outbound permeable to it.
		/// </summary>
		[Test]
		public void NotPermeableOut()
		{
			callSuccess = false;
			SemanticProcessor sp = new SemanticProcessor();
			// sp.OutboundPermeableTo<InnerMembrane, TestSemanticType>();
			sp.InboundPermeableTo<OuterMembrane, TestSemanticType>();
			sp.AddChild<OuterMembrane, InnerMembrane>();
			sp.Register<OuterMembrane, TestReceptor>();
			sp.ProcessInstance<InnerMembrane, TestSemanticType>(true);
			Assert.That(!callSuccess, "Expected receptor in outer membrane to NOT receive the ST placed in the inner membrane.");
		}

		/// <summary>
		/// Outer membrane does not receive semantic type if it is not inbound permeable to it.
		/// </summary>
		[Test]
		public void NotPermeableIn()
		{
			callSuccess = false;
			SemanticProcessor sp = new SemanticProcessor();
			sp.OutboundPermeableTo<InnerMembrane, TestSemanticType>();
			// sp.InboundPermeableTo<OuterMembrane, TestSemanticType>();
			sp.AddChild<OuterMembrane, InnerMembrane>();
			sp.Register<OuterMembrane, TestReceptor>();
			sp.ProcessInstance<InnerMembrane, TestSemanticType>(true);
			Assert.That(!callSuccess, "Expected receptor in outer membrane to NOT receive the ST placed in the inner membrane.");
		}
	}
}
