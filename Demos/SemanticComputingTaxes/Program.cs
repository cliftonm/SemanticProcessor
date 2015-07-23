using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticComputingTaxes
{
	// Non-semantic implementation
	public class Receipt
	{
		public decimal Total(decimal amount, decimal taxes) { return amount * (1 + taxes); }
	}

	public interface ISemanticType { }

	// Semantic implementation
	public class Purchase : ISemanticType
	{
		public decimal Total { get; set; }
		public decimal Taxes { get; set; }
	}

	public interface IReceptor { }
	public interface IReceptor<T> : IReceptor
		where T : ISemanticType
	{
		void Process(T semanticType);
	}

	public class Computation : IReceptor<Purchase>
	{
		public void Process(Purchase p)
		{
			Console.WriteLine("Semantic:" + p.Total * (1 + p.Taxes));
		}
	}

	public class SemanticProcessor
	{
		protected Dictionary<Type, List<Type>> typeReceptors;

		public SemanticProcessor()
		{
			typeReceptors = new Dictionary<Type, List<Type>>();
		}

		public void Register<T, R>()
			where T : ISemanticType
			where R : IReceptor
		{
			List<Type> receptors;
			Type ttype = typeof(T);
			Type rtype = typeof(R);

			if (!typeReceptors.TryGetValue(ttype, out receptors))
			{
				receptors = new List<Type>();
				typeReceptors[ttype] = receptors;
			}

			receptors.Add(rtype);
		}

		public void Process<T>(Action<T> initializer)
			where T : ISemanticType, new()
		{
			Type ttype = typeof(T);
			T semType = new T();
			initializer(semType);

			foreach (Type rtype in typeReceptors[ttype])
			{
				dynamic receptor = Activator.CreateInstance(rtype);
				receptor.Process(semType);
			}
		}
	}

	class Program
	{
		static void Main(string[] args)
		{
			// non-semantic computation:
			Console.WriteLine("Non-semantic: " + new Receipt().Total(1M, .07M));

			// semantic computing:
			SemanticProcessor sp = new SemanticProcessor();
			sp.Register<Purchase, Computation>();
			sp.Process<Purchase>((t) => { t.Total = 1M; t.Taxes = 0.07M; });
		}
	}
}
