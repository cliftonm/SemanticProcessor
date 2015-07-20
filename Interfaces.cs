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

		void ProcessInstance<M, T>(Action<T> initializer)
			where M : IMembrane
			where T : ISemanticType, new();

		void ProcessInstance<M, T>(bool processOnCallerThread = false)
			where M : IMembrane
			where T : ISemanticType, new();

		void ProcessInstance<M, T>(T obj, bool processOnCallerThread = false)
			where M : IMembrane
			where T : ISemanticType;

		void ProcessInstance<T>(IMembrane membrane, T obj, bool processOnCallerThread = false) 
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
