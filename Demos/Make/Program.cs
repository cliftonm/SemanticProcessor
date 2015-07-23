using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Make
{
	public static class Proggy
	{
		public static T Make<T>()
			where T : new()
		{
			Console.WriteLine("Making " + typeof(T).Name);
			return new T();
		}
	}

	class Program
	{
		static void Main(string[] args)
		{
			Proggy.Make<StringBuilder>();
		}
	}
}
