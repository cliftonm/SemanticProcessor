using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using Clifton.Semantics;

namespace Clifton.Semantics.UnitTests
{
	/// <summary>
	/// Test stateless receptors.  These are receptors that the SemanticProcessor creates and destroys as needed.
	/// </summary>
	[TestFixture]
	public class StatelessReceptorTests
	{
		public static bool callSuccess;
		public static bool callSuccess2;
		public static bool constructorCalled;
		public static bool disposeCalled;
		public static bool receptorInitializerCalled;

		public class TestMembrane : IMembrane { }
		public class TestMembrane2 : IMembrane { }
		public class TestSemanticType : ISemanticType { }

		public interface ITestSemanticType { };
		public class InterfaceTestSemanticType : ISemanticType, ITestSemanticType { }
		
		public class TestReceptor : IReceptor, IDisposable
		{
			public bool AFlag { get; set; }

			public TestReceptor()
			{
				constructorCalled = true;
			}

			public void Process(ISemanticProcessor proc, IMembrane membrane, TestSemanticType t)
			{
				callSuccess = true;
			}

			public void Dispose()
			{
				disposeCalled = true;
			}
		}

		public class TestReceptor2 : IReceptor
		{
			public void Process(ISemanticProcessor proc, IMembrane membrane, TestSemanticType t)
			{
				callSuccess2 = true;
			}
		}

		public class DerivedTestReceptor : TestReceptor
		{
		}

		// IReceptor type is optional, but good practice to make sure you implement Process on the semantic type.
		public class InterfaceTestReceptor : IReceptor<ITestSemanticType>	
		{
			public void Process(ISemanticProcessor proc, IMembrane membrane, ITestSemanticType t)
			{
				callSuccess = true;
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
		/// Test that a semantic instance initializer is called when the semantic type is constructed.
		/// </summary>
		[Test]
		public void InitializerCalledForSemanticTypeConstruction()
		{
			bool initializerCalled = false;
			SemanticProcessor sp = new SemanticProcessor();
			sp.Register<TestMembrane, TestReceptor>();
			sp.ProcessInstance<TestMembrane, TestSemanticType>((t) => initializerCalled = true, true);
			Assert.That(initializerCalled, "Expected semantic type initializer to be called.");
		}

		/// <summary>
		/// Test that the base class' Process method gets called for a type that it handles,
		/// even though we instantiated a sub-class.
		/// </summary>
		[Test]
		public void BaseClassProcessCalled()
		{
			callSuccess = false;
			SemanticProcessor sp = new SemanticProcessor();
			sp.Register<TestMembrane, DerivedTestReceptor>();
			sp.ProcessInstance<TestMembrane, TestSemanticType>(true);
			Assert.That(callSuccess, "Expected TestReceptor.Process to be called.");
		}

		/// <summary>
		/// Test that a receptor that implements Process on an interface gets called.
		/// </summary>
		[Test]
		public void ReceptorOfInterfaceTypeCalled()
		{
			callSuccess = false;
			SemanticProcessor sp = new SemanticProcessor();
			sp.Register<TestMembrane, InterfaceTestReceptor>();
			sp.ProcessInstance<TestMembrane, InterfaceTestSemanticType>(true);
			Assert.That(callSuccess, "Expected TestReceptor.Process to be called.");
		}

		/// <summary>
		/// Verify that more than one receptor (but of different types in the same membrane) receives the Process call for the same semantic type.
		/// </summary>
		[Test]
		public void MultipleProcessCalls()
		{
			callSuccess = false;
			callSuccess2 = false;
			SemanticProcessor sp = new SemanticProcessor();
			sp.Register<TestMembrane, TestReceptor>();
			sp.Register<TestMembrane, TestReceptor2>();
			sp.ProcessInstance<TestMembrane, TestSemanticType>(true);
			Assert.That(callSuccess, "Expected TestReceptor.Process to be called.");
			Assert.That(callSuccess2, "Expected TestReceptor2.Process to be called.");
		}

		/// <summary>
		/// Verify that the receptor initializer is called when a stateless receptor is instantiated.
		/// </summary>
		[Test]
		public void ReceptorInitialization()
		{
			receptorInitializerCalled = false;
			SemanticProcessor sp = new SemanticProcessor();
			sp.Register<TestMembrane, TestReceptor>((ir) =>
				{
					// Unfortunately, a cast is required.
					TestReceptor r = (TestReceptor)ir;
					r.AFlag = true;
					receptorInitializerCalled = true;
				});
			sp.ProcessInstance<TestMembrane, TestSemanticType>(true);
			Assert.That(receptorInitializerCalled, "Expected TestReceptor initializer to be called to be called.");
		}
	}
}
