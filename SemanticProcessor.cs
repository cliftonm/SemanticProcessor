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
		public IMembrane Surface { get; protected set; }
		public IMembrane Logger { get; protected set; }

		protected const int MAX_WORKER_THREADS = 20;
		protected List<ThreadSemaphore<Action>> threadPool;
		protected ConcurrentDictionary<Type, IMembrane> membranes;

		protected ConcurrentList<IReceptor> statefulReceptors;
		protected ConcurrentDictionary<Type, List<Type>> typeNotifiers;
		protected ConcurrentDictionary<Type, List<IReceptor>> instanceNotifiers;

		public SemanticProcessor()
		{
			// Our two hard-coded membranes:
			Surface = new SurfaceMembrane();
			Logger = new LoggerMembrane();

			membranes = new ConcurrentDictionary<Type, IMembrane>();
			statefulReceptors = new ConcurrentList<IReceptor>();
			typeNotifiers = new ConcurrentDictionary<Type, List<Type>>();
			instanceNotifiers = new ConcurrentDictionary<Type, List<IReceptor>>();
			threadPool = new List<ThreadSemaphore<Action>>();

			// Register our two membranes.
			membranes[Surface.GetType()] = Surface;
			membranes[Logger.GetType()] = Logger;

			InitializePoolThreads();
		}

		/// <summary>
		/// Register a receptor, auto-discovering the semantic types that it processes.
		/// Receptors live in membranes, to we always specify the membrane type.  The membrane
		/// instance is auto-created for us if necessary.
		/// </summary>
		public void Register<T, M>()
			where T : IReceptor
			where M : IMembrane
		{
			Register<T>();
			RegisterMembrane<M>();
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
				// TODO: Use attribute, not specific function name.
				if (method.Name == "Process")
				{
					ParameterInfo[] parameters = method.GetParameters();
					InstanceNotify(receptor, parameters[2].ParameterType);
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
			List<Type> targets = GetReceptors(tsource);

			foreach (Type ttarget in targets)
			{
				typeNotifiers[tsource].Remove(ttarget);
			}
		}

		/// <summary>
		/// Process an instance of a specific type immediately.  The type T is determined implicitly from the parameter type, so 
		/// a call can look like: ProcessInstance(t1)
		/// </summary>
		public void ProcessInstance<T>(IMembrane membrane, T obj)
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

			List<Type> receptors = GetReceptors(tsource);
			
			foreach (Type ttarget in receptors)
			{
				// We can use dynamic here because we have a <T> generic to resolve the call parameter.
				// If we instead only have the interface ISemanticType, dynamic does not downcast to the concrete type --
				// therefore it can't locate the call point because it implements the concrete type.
				dynamic target = Activator.CreateInstance(ttarget);

				// Pick a thread that has the least work to do.
				threadPool.MinBy(tp => tp.Count).Enqueue(() => target.Process(this, membrane, obj));
			}

			// Also check stateful receptors
			List<IReceptor> sreceptors = GetStatefulReceptors(tsource);

			foreach (IReceptor receptor in sreceptors)
			{
				dynamic target = receptor;
				threadPool.MinBy(tp => tp.Count).Enqueue(() => target.Process(this, membrane, obj));
			}
		}

		protected void Register<T>()
			where T : IReceptor
		{
			Type ttarget = typeof(T);
			MethodInfo[] methods = ttarget.GetMethods();

			foreach (MethodInfo method in methods)
			{
				// TODO: Use attribute, not specific function name.
				if (method.Name == "Process")
				{
					ParameterInfo[] parameters = method.GetParameters();

					// Semantic types are always the third parameter
					// Types can either be concrete or interfaces.
					TypeNotify<T>(parameters[2].ParameterType);
				}
			}
		}

		protected void RegisterMembrane<M>()
			where M:IMembrane
		{
			Type m = typeof(M);

			if (!membranes.ContainsKey(m))
			{
				IMembrane membrane = (IMembrane)Activator.CreateInstance(m);
				membranes[m] = membrane;
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

		protected List<Type> GetReceptors(Type tsource)
		{
			List<Type> receptors = new List<Type>();
			List<Type> baseList;

			// Get the type notifiers for the provided type.
			if (typeNotifiers.TryGetValue(tsource, out baseList))
			{
				receptors.AddRange(baseList);
			}
			
			// Check interfaces and base types of the source type as well to see if there are receptors handling the interfaces.

			foreach (Type t in tsource.GetParentTypes())
			{
				List<Type> tReceptors;

				if (typeNotifiers.TryGetValue(t, out tReceptors))
				{
					receptors.AddRange(tReceptors);
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
