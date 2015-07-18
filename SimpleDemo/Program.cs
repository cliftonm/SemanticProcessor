using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Clifton.Semantics;

namespace Main
{
	public interface IOneType : ISemanticType
	{
	}

	public class OneType : IOneType
	{
	}

	public class SecondType : ISemanticType
	{
	}

	public class SecondDerivedType : SecondType
	{
	}

	public class Chain1 : IReceptor<IOneType>
	{
		public void Process(ISemanticProcessor pool, IMembrane membrane, IOneType obj)
		{
			Console.WriteLine("Chaining OneType on thread ID " + Thread.CurrentThread.ManagedThreadId);
			pool.ProcessInstance(membrane, new SecondType());
		}
	}

	public class Chain2 : IReceptor<SecondType>
	{
		public void Process(ISemanticProcessor pool, IMembrane membrane, SecondType obj)
		{
			Console.WriteLine("Chaining SecondType on thread ID " + Thread.CurrentThread.ManagedThreadId);
		}
	}

	// Specifying the receptor semantic types is optional, at a minimum, the receptor needs to only derive from IReceptor.
	// public class AnotherType : IReceptor<IOneType>, IReceptor<SecondType>

	public class AReceptor : IReceptor //, IReceptor<IOneType>, IReceptor<SecondType>// , IDisposable	// demonstrate dispose being called.
	{
		public void Process(ISemanticProcessor pool, IMembrane membrane, IOneType obj)
		{
			Console.WriteLine("A: Processing IOneType on thread ID " + Thread.CurrentThread.ManagedThreadId);
		}

		public void Process(ISemanticProcessor pool, IMembrane membrane, SecondType obj)
		{
			Console.WriteLine("A: Processing SecondType on thread ID " + Thread.CurrentThread.ManagedThreadId);
		}

		public void Dispose()
		{
			Console.WriteLine("Dispose called.");
		}
	}

	public class BReceptor : IReceptor
	{
		public void Process(ISemanticProcessor pool, IMembrane membrane, OneType obj)
		{
			Console.WriteLine("B: Processing OneType on thread ID " + Thread.CurrentThread.ManagedThreadId);
		}
	}

	public class CReceptor : IReceptor
	{
		public void Process(ISemanticProcessor pool, IMembrane membrane, SecondDerivedType obj)
		{
			Console.WriteLine("C: Processing SecondDerivedType on thread ID " + Thread.CurrentThread.ManagedThreadId);
		}

	}

	class Program
	{
		//public static T Cast<T>(object o)
		//{
		//	return (T)o;
		//}

		static void Main(string[] args)
		{
			SemanticProcessor sp = new SemanticProcessor();

			// AnotherType gets notified when instances of OneType are added to the pool.
			sp.Register<AReceptor, Clifton.Semantics.SurfaceMembrane>();		// auto register
			sp.Register<BReceptor, Clifton.Semantics.SurfaceMembrane>();
			sp.Register<CReceptor, Clifton.Semantics.SurfaceMembrane>();

			// Explicit register
			//sp.TypeNotify<AReceptor, OneType>();
			//sp.TypeNotify<AReceptor, SecondType>();

			OneType t1 = new OneType();
			SecondType t2 = new SecondType();
			SecondDerivedType t3 = new SecondDerivedType();

			// object foo = Cast<SecondType>(t3);

			sp.ProcessInstance(sp.Surface, t1);
			sp.ProcessInstance(sp.Surface, t2);
			sp.ProcessInstance(sp.Surface, t3);

			Thread.Sleep(1000);		// Wait for threaded processes to complete.

			Console.WriteLine("\r\nChained processing...");
			sp.RemoveTypeNotify<AReceptor, IOneType>();
			sp.RemoveTypeNotify<AReceptor, SecondType>();
			sp.RemoveTypeNotify<BReceptor, OneType>();
			sp.RemoveTypeNotify<CReceptor, SecondDerivedType>();

			// Chaining...
			// auto register:
			sp.Register<Chain1, Clifton.Semantics.SurfaceMembrane>();
			sp.Register<Chain2, Clifton.Semantics.SurfaceMembrane>();

			// Explicit register:
			//sp.TypeNotify<Chain1, OneType>();
			//sp.TypeNotify<Chain2, SecondType>();

			sp.ProcessInstance(sp.Surface, new OneType());

			Thread.Sleep(1000);		// Wait for threaded processes to complete.
		}
	}
}
