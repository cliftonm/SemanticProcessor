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
	/// Test normal logging and exception logging.
	/// </summary>
	[TestFixture]
	public class LoggingTests
	{
		public static bool stLogged;
		public static bool exLogged;

		public class TestMembrane : IMembrane { }
		public class TestSemanticType : ISemanticType { }
		public class TypeThrowsException : ISemanticType { }

		public class TestReceptor : IReceptor
		{
			public void Process(ISemanticProcessor proc, IMembrane membrane, TestSemanticType t)
			{
			}

			public void Process(ISemanticProcessor proc, IMembrane membrane, TypeThrowsException t)
			{
				throw new ApplicationException("Receptor exception");
			}
		}

		public class LoggerReceptor : IReceptor
		{
			public void Process(ISemanticProcessor proc, IMembrane membrane, ISemanticType t)
			{
				stLogged = t is TestSemanticType;
			}
		}

		public class ExceptionReceptor : IReceptor
		{
			public void Process(ISemanticProcessor proc, IMembrane membrane, ST_Exception ex)
			{
				exLogged = true;
			}
		}

		/// <summary>
		/// Test the a process call is logged.
		/// </summary>
		[Test]
		public void ProcessCallIsLogged()
		{
			stLogged = false;
			SemanticProcessor sp = new SemanticProcessor();
			sp.Register<LoggerMembrane, LoggerReceptor>();
			sp.Register<LoggerMembrane, ExceptionReceptor>();
			sp.Register<TestMembrane, TestReceptor>();
			sp.ProcessInstance<TestMembrane, TestSemanticType>(true);
			Assert.That(stLogged, "Expected Process call to be logged.");
		}

		/// <summary>
		/// Verify that an exception log is generated when a receptor process creates an exception.
		/// </summary>
		[Test]
		public void ExceptionIsLogged()
		{
			exLogged = false;
			SemanticProcessor sp = new SemanticProcessor();
			sp.Register<LoggerMembrane, LoggerReceptor>();
			sp.Register<LoggerMembrane, ExceptionReceptor>();
			sp.Register<TestMembrane, TestReceptor>();
			sp.ProcessInstance<TestMembrane, TypeThrowsException>(true);
			Assert.That(exLogged, "Expected Exception call to be logged.");
		}
	}
}
