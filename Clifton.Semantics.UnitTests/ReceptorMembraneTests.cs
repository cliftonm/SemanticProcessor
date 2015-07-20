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
	public class ReceptorMembraneTests
	{
		public static bool callSuccess;
		public static bool constructorCalled;
		public static bool disposeCalled;

		public class TestMembrane : IMembrane { }
		public class TestMembrane2 : IMembrane { }
		public class TestSemanticType : ISemanticType { }
		public class TestReceptor : IReceptor, IDisposable
		{
			public TestReceptor()
			{
				constructorCalled = true;
			}

			public void Process(ISemanticProcessor proc, IMembrane membrane, TestSemanticType t)
			{
				ReceptorMembraneTests.callSuccess = true;
			}

			public void Dispose()
			{
				disposeCalled = true;
			}
		}

		/// <summary>
		/// Given a receptor in a membrane, a semantic type put into that membrane is received by that receptor.
		/// </summary>
		[Test]
		public void ReceptorReceivesSemanticTypeOnItsMembrane()
		{
			callSuccess = false;
			SemanticProcessor sp = new SemanticProcessor();
			sp.Register<TestMembrane, TestReceptor>();
			sp.ProcessInstance<TestMembrane, TestSemanticType>(true);
			Assert.That(callSuccess, "Expected TestReceptor.Process to be called.");
		}

		/// <summary>
		/// Given a semantic type put into one membrane, the receptor in another membrane does not receive it.
		/// </summary>
		[Test]
		public void ReceptorDoesNotReceiveSemanticTypeOnAnotherMembrane()
		{
			callSuccess = false;
			SemanticProcessor sp = new SemanticProcessor();
			sp.Register<TestMembrane, TestReceptor>();
			sp.ProcessInstance<TestMembrane2, TestSemanticType>(true);
			Assert.That(!callSuccess, "Expected TestReceptor.Process to NOT be called.");
		}

		/// <summary>
		/// Test that when we remove a semantic type from a membrane's receptor, the receptor no longer gets Process calls.
		/// </summary>
		[Test]
		public void RemoveType()
		{
			callSuccess = false;
			SemanticProcessor sp = new SemanticProcessor();
			sp.Register<TestMembrane, TestReceptor>();
			sp.RemoveTypeNotify<TestMembrane, TestReceptor, TestSemanticType>();
			sp.ProcessInstance<TestMembrane, TestSemanticType>(true);
			Assert.That(!callSuccess, "Expected TestReceptor.Process to NOT be called.");
		}

		/// <summary>
		/// Verify that when processing a semantic type, the receptor, registered by type, is created and destroyed.
		/// </summary>
		[Test]
		public void ReceptorTypeCreateDestroy()
		{
			constructorCalled = false;
			disposeCalled = false;
			SemanticProcessor sp = new SemanticProcessor();
			sp.Register<TestMembrane, TestReceptor>();
			sp.ProcessInstance<TestMembrane, TestSemanticType>(true);
			Assert.That(constructorCalled, "Expected constructor to be called.");
			Assert.That(disposeCalled, "Expected Dispose to be called.");
		}

		/// <summary>
		/// Verify that a stateful receptor's constructor and Dispose method is not called when processing a semantic instance.
		/// </summary>
		[Test]
		public void ReceptorInstanceCreateDestory()
		{
			SemanticProcessor sp = new SemanticProcessor();
			IReceptor r = new TestReceptor();
			constructorCalled = false;
			disposeCalled = false;
			sp.Register<TestMembrane>(r);
			sp.ProcessInstance<TestMembrane, TestSemanticType>(true);
			Assert.That(!constructorCalled, "Expected constructor NOT to be called.");
			Assert.That(!disposeCalled, "Expected Dispose NOT to be called.");
		}
	}
}
