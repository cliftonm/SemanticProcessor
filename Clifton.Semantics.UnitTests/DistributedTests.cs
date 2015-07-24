using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using NUnit.Framework;

using Clifton.Semantics;
using RestReceptor;

namespace Clifton.Semantics.UnitTests
{
	[TestFixture]
	public class DistributedTests
	{
		public static string received;

		public class TestMembrane : Membrane { }

		public class TestReceptor : IReceptor
		{
			public void Process(ISemanticProcessor proc, IMembrane membrane, TestDistributedSemanticType t)
			{
				received = t.Message;
			}
		}

		/// <summary>
		/// Verify that a semantic type is received on a "remote" semantic processor.
		/// </summary>
		[Test]
		public void DistributedComputation()
		{
			SemanticProcessor spOut = new SemanticProcessor();
			SemanticProcessor spIn = new SemanticProcessor();

			received = "";
			OutboundDistributedComputingReceptor dcrOut = new OutboundDistributedComputingReceptor(4002);
			InboundDistributedComputingReceptor dcrIn = new InboundDistributedComputingReceptor(4002, spIn);

			// Create an "emitter" in which a semantic type emitted on the TestMembrane permeates
			// into the inner DistributedProcessMembrane for our test type.
			spOut.AddChild<TestMembrane, DistributedProcessMembrane>();
			spOut.OutboundPermeableTo<TestMembrane, TestDistributedSemanticType>();
			spOut.InboundPermeableTo<DistributedProcessMembrane, TestDistributedSemanticType>();

			// The stateful DCR out lives in the distributed process membrane.
			spOut.Register<DistributedProcessMembrane>(dcrOut);

			// Create a "receiver" in which a semantic type is received on the inner DistributedProcessMembrane
			// and the test type permeates out to a "handler" receptor.
			spIn.AddChild<TestMembrane, DistributedProcessMembrane>();
			spIn.OutboundPermeableTo<DistributedProcessMembrane, TestDistributedSemanticType>();
			spIn.InboundPermeableTo<TestMembrane, TestDistributedSemanticType>();

			// The stateful DCR in lives in the distributed process membrane.
			spIn.Register<DistributedProcessMembrane>(dcrIn);
			// The responding receptor lives in the TestMembrane
			spIn.Register<TestMembrane, TestReceptor>();

			// Put a semantic type instance on the outbound side.
			spOut.ProcessInstance<TestMembrane, TestDistributedSemanticType>((t) =>
				{
					t.Message = "Hello World";
				});

			// Wait a bit for threads to do their thing and Http posts to do their things.
			// System.Diagnostics.Debug.WriteLine("Waiting...");
			// !*!*!*!* Sometimes this wait must be longer -- the unit test engine can really slow things down.
			// !*!*!*!* This is particularly true when running the test in the debugger!
			// !*!*!*!* If this delay isn't long enough for the server's message to be processed, you will get
			// !*!*!*!* errors related to accessing objects on an unloaded AppDomain.
			// !*!*!*!* In real life this woudn't happen -- this is an artifact of unit testing a complex
			// !*!*!*!* multi-threaded process.
			//Thread.Sleep(500);

			// Because we know it works, we could actually do this, which is particularly useful when we're
			// debugging and single stepping through code -- we do not want the test in this AppDomain
			// to exit prematurely!
			while (String.IsNullOrEmpty(received))
			{
				Thread.Sleep(0);
			}

			Assert.That(received == "Hello World", "Expected to receive 'Hello World'");
		}
	}
}
