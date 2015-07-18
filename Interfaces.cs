using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clifton.Semantics
{
	public interface ISemanticProcessor
	{
		void ProcessInstance<T>(T obj) where T : ISemanticType;
	}

	public interface ISemanticType
	{
	}

	public interface IReceptor
	{
	}

	public interface IReceptor<T> : IReceptor
	{
		void Process(ISemanticProcessor pool, T obj);
	}
}
