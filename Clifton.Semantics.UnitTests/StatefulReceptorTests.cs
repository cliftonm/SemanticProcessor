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
	/// Test stateful receptors.  Stateful receptors are instances that are instantiated externally from the SemanticProcessor.
	/// </summary>
	[TestFixture]
	public class StatefulReceptorTests
	{
		public static bool callSuccess;
		public static bool callSuccess2;
		public static bool constructorCalled;
		public static bool disposeCalled;

		public class TestMembrane : IMembrane { }
		public class TestMembrane2 : IMembrane { }
		public class TestSemanticType : ISemanticType { }

		public interface ITestSemanticType { };
		public class InterfaceTestSemanticType : ISemanticType, ITestSemanticType { }

		public class TestReceptor : IReceptor, IDisposable
		{
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
			sp.Register<TestMembrane>(new TestReceptor());
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
			sp.Register<TestMembrane>(new TestReceptor());
			sp.ProcessInstance<TestMembrane2, TestSemanticType>(true);
			Assert.That(!callSuccess, "Expected TestReceptor.Process to NOT be called.");
		}

		/// <summary>
		/// Test that when we remove a semantic type from a membrane's receptor, the receptor no longer gets Process calls.
		/// </summary>
		[Test]
		public void RemoveReceptorInstanceByType()
		{
			callSuccess = false;
			IReceptor receptor = new TestReceptor();
			SemanticProcessor sp = new SemanticProcessor();
			sp.Register<TestMembrane>(receptor);
			sp.RemoveTypeNotify<TestMembrane, TestReceptor, TestSemanticType>();
			sp.ProcessInstance<TestMembrane, TestSemanticType>(true);
			Assert.That(!callSuccess, "Expected TestReceptor.Process to NOT be called.");
		}

		/// <summary>
		/// Test that when we remove a semantic type from a membrane's receptor, the receptor no longer gets Process calls.
		/// </summary>
		[Test]
		public void RemoveReceptorInstance()
		{
			callSuccess = false;
			IReceptor receptor = new TestReceptor();
			SemanticProcessor sp = new SemanticProcessor();
			sp.Register<TestMembrane>(receptor);
			sp.RemoveTypeNotify<TestMembrane, TestSemanticType>(receptor);
			sp.ProcessInstance<TestMembrane, TestSemanticType>(true);
			Assert.That(!callSuccess, "Expected TestReceptor.Process to NOT be called.");
		}

		/// <summary>
		/// Verify that a stateful receptor's constructor and Dispose method is not called when processing a semantic instance.
		/// </summary>
		[Test]
		public void ReceptorInstanceCreateDestroy()
		{
			SemanticProcessor sp = new SemanticProcessor();
			sp.Register<TestMembrane>(new TestReceptor());
			constructorCalled = false;
			disposeCalled = false;
			sp.ProcessInstance<TestMembrane, TestSemanticType>(true);
			Assert.That(!constructorCalled, "Expected constructor NOT to be called.");
			Assert.That(!disposeCalled, "Expected Dispose NOT to be called.");
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
			sp.Register<TestMembrane>(new DerivedTestReceptor());
			sp.ProcessInstance<TestMembrane, TestSemanticType>(true);
			Assert.That(callSuccess, "Expected TestReceptor.Process to be called.");
		}

		/// <summary>
		/// Test that a receptor that implements Process on an interface gets called.
		/// </summary>
		[Test]
		public void ReceptorOfInterfaceTypCalled()
		{
			callSuccess = false;
			SemanticProcessor sp = new SemanticProcessor();
			sp.Register<TestMembrane>(new InterfaceTestReceptor());
			sp.ProcessInstance<TestMembrane, InterfaceTestSemanticType>(true);
			Assert.That(callSuccess, "Expected TestReceptor.Process to be called.");
		}

		/// <summary>
		/// Verify that more than one receptor instance (but of different types in the same membrane) receives the Process call for the same semantic type.
		/// </summary>
		[Test]
		public void MultipleProcessCalls()
		{
			callSuccess = false;
			callSuccess2 = false;
			SemanticProcessor sp = new SemanticProcessor();
			sp.Register<TestMembrane>(new TestReceptor());
			sp.Register<TestMembrane>(new TestReceptor2());
			sp.ProcessInstance<TestMembrane, TestSemanticType>(true);
			Assert.That(callSuccess, "Expected TestReceptor.Process to be called.");
			Assert.That(callSuccess2, "Expected TestReceptor2.Process to be called.");
		}
	}
}
												 