using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using MoreLinq;

namespace Clifton.Semantics
{
	public class SemanticProcessor : ISemanticProcessor
	{
		protected const int MAX_WORKER_THREADS = 20;
		protected List<ThreadSemaphore<Action>> threadPool;

		protected ConcurrentList<IReceptor> statefulReceptors;
		protected ConcurrentDictionary<Type, List<Type>> typeNotifiers;
		protected ConcurrentDictionary<Type, List<IReceptor>> instanceNotifiers;

		public SemanticProcessor()
		{
			statefulReceptors = new ConcurrentList<IReceptor>();
			typeNotifiers = new ConcurrentDictionary<Type, List<Type>>();
			instanceNotifiers = new ConcurrentDictionary<Type, List<IReceptor>>();
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
		/// Register a stateful receptor.
		/// </summary>
		public void Register(IReceptor receptor)
		{
			statefulReceptors.Add(receptor);
			Type ttarget = receptor.GetType();

			MethodInfo[] methods = ttarget.GetMethods();

			foreach (MethodInfo method in methods)
			{
				if (method.Name == "Process")
				{
					ParameterInfo[] parameters = method.GetParameters();
					InstanceNotify(receptor, parameters[1].ParameterType);
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

				// Pick a thread that has the least work to do.
				threadPool.MinBy(tp => tp.Count).Enqueue(() => target.Process(this, obj));
			}

			// Also check stateful receptors
			List<IReceptor> sreceptors = GetStatefulReceptors(tsource);

			foreach (IReceptor receptor in sreceptors)
			{
				dynamic target = receptor;
				threadPool.MinBy(tp => tp.Count).Enqueue(() => target.Process(this, obj));
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

		protected void InstanceNotify(IReceptor receptor, Type tsource)
		{
			List<IReceptor> targets;

			if (!instanceNotifiers.TryGetValue(tsource, out targets))
			{
				targets = new List<IReceptor>();
				instanceNotifiers[tsource] = targets;
			}

			targets.Add(receptor);
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

		protected List<IReceptor> GetStatefulReceptors(Type tsource)
		{
			List<IReceptor> receptors = new List<IReceptor>();
			List<IReceptor> baseList;

			if (instanceNotifiers.TryGetValue(tsource, out baseList))
			{
				receptors.AddRange(baseList);
			}

			// Check interfaces and base types of the source type as well to see if there are receptors handling the interfaces.

			foreach (Type t in tsource.GetParentTypes())
			{
				List<IReceptor> tReceptors;

				if (instanceNotifiers.TryGetValue(t, out tReceptors))
				{
					receptors.AddRange(tReceptors);
				}
			}

			return receptors;
		}

		/// <summary>
		/// Setup thread pool to for calling receptors to process semantic types.
		/// Why do we use our own thread pool?  Because .NET's implementation (and
		/// particularly Task) is crippled and non-functional for long running threads.
		/// </summary>
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

		/// <summary>
		/// Invoke the action that we want to run on a thread.
		/// </summary>
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
	}
}
