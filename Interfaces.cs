using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clifton.Semantics
{
	public interface ISemanticProcessor
	{
		IMembrane Surface { get; }
		IMembrane Logger { get; }

		void Register<M, T>()
			where M : IMembrane
			where T : IReceptor;

		void Register<M>(IReceptor receptor)
			where M : IMembrane;

		void Register(IMembrane membrane, IReceptor receptor);

		void ProcessInstance<M, T>(Action<T> initializer = null)
			where M : IMembrane
			where T : ISemanticType;
		
		void ProcessInstance<M, T>(T obj)
			where M : IMembrane
			where T : ISemanticType;
		
		void ProcessInstance<T>(IMembrane membrane, T obj) 
			where T : ISemanticType;
	}

	public interface IMembrane
	{
	}

	public interface ISemanticType
	{
	}

	public interface IReceptor
	{
	}

	public interface IReceptor<T> : IReceptor
	{
		void Process(ISemanticProcessor pool, IMembrane membrane, T obj);
	}
}
