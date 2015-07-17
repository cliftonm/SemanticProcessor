using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using MoreLinq;

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

	public class ThreadSemaphore<T>
	{
		public int Count { get { return requests.Count; } }
		protected Semaphore sem;

		// Requests on this thread.
		protected ConcurrentQueue<T> requests;

		public ThreadSemaphore()
		{
			sem = new Semaphore(0, Int32.MaxValue);
			requests = new ConcurrentQueue<T>();
		}

		public void Enqueue(T context)
		{
			requests.Enqueue(context);
			sem.Release();
		}

		public void WaitOne()
		{
			sem.WaitOne();
		}

		public bool TryDequeue(out T context)
		{
			return requests.TryDequeue(out context);
		}
	}

	public interface ISemanticPool
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
		void Process(ISemanticPool pool, T obj);
	}

	public class SemanticPool : ISemanticPool
	{
		protected const int MAX_WORKER_THREADS = 20;
		protected List<ThreadSemaphore<Action>> threadPool;

		protected ConcurrentList<Type> types;
		protected ConcurrentDictionary<Type, List<Type>> typeNotifiers;

		public SemanticPool()
		{
			types = new ConcurrentList<Type>();
			typeNotifiers = new ConcurrentDictionary<Type, List<Type>>();
			threadPool = new List<ThreadSemaphore<Action>>();

			InitializePoolThreads();
		}

		/// <summary>
		/// Register a receptor, auto-discovering the semantic types that it processes.
		/// </summary>
		public void Register<T>()
			where T : IReceptor
		{
			Type ttarget = typeof(T);

			MethodInfo[] methods = ttarget.GetMethods();

			foreach (MethodInfo method in methods)
			{
				if (method.Name == "Process")
				{
					ParameterInfo[] parameters = method.GetParameters();

					// Semantic types are always the second parameter
					// Types can either be concrete or interfaces.
					TypeNotify<T>(parameters[1].ParameterType);
				}
			}
		}

		/// <summary>
		/// Explicitly register a receptor type for a semantic type.  
		/// The target receptor type will be notified of new instances of the source semantic type.
		/// Source types can be concrete types, interface types, or derived types, but must implement ISemanticType
		/// somewhere in the inheritance tree.
		/// </summary>
		public void TypeNotify<TTarget, TSource>() 
			where TTarget : IReceptor 
			where TSource : ISemanticType
		{
			TypeNotify<TTarget>(typeof(TSource));
		}

		/// <summary>
		/// Remove a semantic (source) type from a target (receptor).
		/// The target type will no longer receive notifications of source type instances.
		/// </summary>
		public void RemoveTypeNotify<TTarget, TSource>()
			where TTarget : IReceptor
			where TSource : ISemanticType
		{
			Type tsource = typeof(TSource);
			List<Tuple<Type, Type>> targets = GetReceptors(tsource);

			foreach (Tuple<Type, Type> tt in targets)
			{
				typeNotifiers[tsource].Remove(tt.Item1);
			}
		}

		/// <summary>
		/// Process an instance of a specific type immediately.  The type T is determined implicitly from the parameter type, so 
		/// a call can look like: ProcessInstance(t1)
		/// </summary>
		public void ProcessInstance<T>(T obj)
			where T : ISemanticType
		{
			// ProcessInstance((ISemanticType)obj);

			// We get the source object type.
			Type tsource = obj.GetType();

			// Then, for each target type that is interested in this source type, 
			// we construct the target type, then invoke the correct target's Process method.
			// Constructing the target type provides us with some really interesting abilities.
			// The target type can be treated as an immutable object.  We can, for instance, exceute
			// the Process call on a separate thread.  Constructing the target type ensures that the
			// target is stateless -- state must be managed external of any type!

			List<Tuple<Type, Type>> receptors = GetReceptors(tsource);
			
			foreach (Tuple<Type, Type> tt in receptors)
			{
				Type ttarget = tt.Item1;
				// We can use dynamic here because we have a <T> generic to resolve the call parameter.
				// If we instead only have the interface ISemanticType, dynamic does not downcast to the concrete type --
				// therefore it can't locate the call point because it implements the concrete type.
				dynamic target = Activator.CreateInstance(ttarget);

				try
				{
					// Pick a thread that has the least work to do.
					threadPool.MinBy(tp => tp.Count).Enqueue(() => target.Process(this, obj));
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);
				}
				finally
				{
					if (target is IDisposable)
					{
						((IDisposable)target).Dispose();
					}
				}
			}
		}

		/// <summary>
		/// Get the Process method that implements, in its parameters, the source type.
		/// Only one process method is allowed for a specific type -- the compiler would tell us if there's duplicates.
		/// However, we can have different process methods for interfaces and base classes of a given type, as these
		/// each are maintained in unique receptor target lists, since they are, technically, different types!
		/// </summary>
		protected MethodInfo GetProcessMethod(IReceptor target, Type tsource)
		{
			// TODO: Cache the (target type, source type) MethodInfo
			MethodInfo[] methods = target.GetType().GetMethods();

			// Also check interfaces implemented by the source.
			Type[] interfaces = tsource.GetInterfaces();

			foreach (MethodInfo method in methods)
			{
				if (method.Name == "Process")
				{
					ParameterInfo[] parameters = method.GetParameters();

					foreach (ParameterInfo parameter in parameters)
					{
						// Do we have a match for the concrete source type?
						if (parameter.ParameterType == tsource)
						{
							return method;
						}

						// Do we have a match for any interfaces the concrete source type implements?
						foreach (Type iface in interfaces)
						{
							if (parameter.ParameterType == iface)
							{
								return method;
							}
						}
					}
				}
			}

			return null;
		}

		protected void InitializePoolThreads()
		{
			for (int i = 0; i < MAX_WORKER_THREADS; i++)
			{
				Thread thread = new Thread(new ParameterizedThreadStart(ProcessPoolItem));
				thread.IsBackground = true;
				ThreadSemaphore<Action> ts = new ThreadSemaphore<Action>();
				threadPool.Add(ts);
				thread.Start(ts);
			}
		}

		protected void ProcessPoolItem(object state)
		{
			ThreadSemaphore<Action> ts = (ThreadSemaphore<Action>)state;

			while (true)
			{
				ts.WaitOne();
				Action proc;

				if (ts.TryDequeue(out proc))
				{
					try
					{
						proc();
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex.Message);
					}
					finally
					{
						if (proc.Target is IDisposable)
						{
							((IDisposable)proc.Target).Dispose();
						}
					}
				}
			}
		}

		/// <summary>
		/// Add a type notifier for a source type.  The source type can be either a concrete class type or an interface type.
		/// As a result, the list of targets will, in the dictionary, be distinct.  This is also the case for derived types.
		/// </summary>
		protected void TypeNotify<TTarget>(Type tsource)
			where TTarget : IReceptor
		{
			// The source type is the key, containing a list of target types that get notified of source type instances.
			List<Type> targets;

			if (!typeNotifiers.TryGetValue(tsource, out targets))
			{
				targets = new List<Type>();
				typeNotifiers[tsource] = targets;
			}

			targets.Add(typeof(TTarget));
		}

		protected List<Tuple<Type, Type>> GetReceptors(Type tsource)
		{
			List<Tuple<Type, Type>> receptors = new List<Tuple<Type, Type>>();
			List<Type> baseList;

			// Get the type notifiers for the provided type.
			if (typeNotifiers.TryGetValue(tsource, out baseList))
			{
				baseList.ForEach(t => receptors.Add(new Tuple<Type, Type>(t, tsource)));
			}
			
			// Check interfaces and base types of the source type as well to see if there are receptors handling the interfaces.

			foreach (Type t in tsource.GetParentTypes())
			{
				List<Type> tReceptors;

				if (typeNotifiers.TryGetValue(t, out tReceptors))
				{
					tReceptors.ForEach(rt => receptors.Add(new Tuple<Type, Type>(rt, t)));
				}
			}

			return receptors;
		}
	}
}
