using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clifton.Semantics
{
	public static class ExtensionMethods
	{
		// http://stackoverflow.com/questions/8868119/find-all-parent-types-both-base-classes-and-interfaces
		public static IEnumerable<Type> GetParentTypes(this Type type)
		{
			// is there any base type?
			if ((type == null) || (type.BaseType == null))
			{
				yield break;
			}

			// return all implemented or inherited interfaces
			foreach (var i in type.GetInterfaces())
			{
				yield return i;
			}

			// return all inherited types
			var currentBaseType = type.BaseType;
			while (currentBaseType != null)
			{
				yield return currentBaseType;
				currentBaseType = currentBaseType.BaseType;
			}
		}
	}
}
