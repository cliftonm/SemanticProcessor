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
	/// Test complex types.  These are semantic types that have properties that themselves are semantic types
	/// and will therefore trigger receptors to process those types as well.
	/// </summary>
	[TestFixture]
	public class ComplexTypeTests
	{
		public static bool simpleTypeProcessed;
		public static bool complexTypeProcessed;

		public class TestMembrane : IMembrane { }
		public class SimpleType : ISemanticType { }
		public class ComplexType : ISemanticType
		{
			public SimpleType ASimpleType { get; set; }

			public ComplexType()
			{
				ASimpleType = new SimpleType();
			}
		}

		public class ComplexReceptor : IReceptor<ComplexType>
		{
			public void Process(ISemanticProcessor pool, IMembrane membrane, ComplexType obj)
			{
				complexTypeProcessed = true;
			}
		}

		public class SimpleReceptor : IReceptor<SimpleType>
		{
			public void Process(ISemanticProcessor pool, IMembrane membrane, SimpleType obj)
			{
				simpleTypeProcessed = true;
			}
		}

		[Test]
		public void ComplexTypePropertyProcessing()
		{
			simpleTypeProcessed = false;
			complexTypeProcessed = false;
			SemanticProcessor sp = new SemanticProcessor();
			sp.Register<TestMembrane, ComplexReceptor>();
			sp.Register<TestMembrane, SimpleReceptor>();
			sp.ProcessInstance<TestMembrane, ComplexType>(true);
			Assert.That(complexTypeProcessed, "Expected ComplexReceptor.Process to be called.");
			Assert.That(simpleTypeProcessed, "Expected SimpleReceptor.Process to be called.");
		}
	}
}
