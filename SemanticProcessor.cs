using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using MoreLinq;

using Clifton.Extensions;

namespace Clifton.Semantics
{
	public class ReceptorCall
	{
		public IReceptor Receptor { get; set; }
		public Action Proc { get; set; }
		public bool AutoDispose { get; set; }

		public ReceptorCall()
		{
			AutoDispose = true;
		}
	}

	public class SemanticProcessor : ISemanticProcessor
	{
		public IMembrane Surface { get; protected set; }
		public IMembrane Logger { get; protected set; }

		public IReadOnlyList<IMembrane> Membranes { get { return membranes.Values.ToList(); } }

		protected const int MAX_WORKER_THREADS = 20;
		protected List<ThreadSemaphore<ReceptorCall>> threadPool;
		protected ConcurrentDictionary<Type, IMembrane> membranes;
		protected ConcurrentDictionary<IMembrane, List<Type>> membraneReceptorTypes;
		protected ConcurrentDictionary<IMembrane, List<IReceptor>> membraneReceptorInstances;

		protected ConcurrentList<IReceptor> statefulReceptors;
		protected ConcurrentDictionary<Type, List<Type>> typeNotifiers;
		protected ConcurrentDictionary<Type, List<IReceptor>> instanceNotifiers;

		public SemanticProcessor()
		{
			membranes = new ConcurrentDictionary<Type, IMembrane>();
			membraneReceptorTypes = new ConcurrentDictionary<IMembrane, List<Type>>();
			membraneReceptorInstances = new ConcurrentDictionary<IMembrane, List<IReceptor>>();

			// Our two hard-coded membranes:
			Surface = RegisterMembrane<SurfaceMembrane>();
			Logger = RegisterMembrane<LoggerMembrane>();

			statefulReceptors = new ConcurrentList<IReceptor>();
			typeNotifiers = new ConcurrentDictionary<Type, List<Type>>();
			instanceNotifiers = new ConcurrentDictionary<Type, List<IReceptor>>();
			threadPool = new List<ThreadSemaphore<ReceptorCall>>();

			// Register our two membranes.
			membranes[Surface.GetType()] = Surface;
			membranes[Logger.GetType()] = Logger;

			InitializePoolThreads();
		}

		public IMembrane RegisterMembrane<M>()
			where M : IMembrane
		{
			IMembrane membrane = RegisterMembrane(typeof(M));

			return membrane;
		}

		/// <summary>
		/// Register a receptor, auto-discovering the semantic types that it processes.
		/// Receptors live in membranes, to we always specify the membrane type.  The membrane
		/// instance is auto-created for us if necessary.
		/// </summary>
		public void Register<M, T>()
			where M : IMembrane
			where T : IReceptor
		{
			Register<T>();
			IMembrane membrane = RegisterMembrane(typeof(M));
			membraneReceptorTypes[membrane].Add(typeof(T));
		}

		/// <summary>
		/// Register an instance receptor living in a membrane type.
		/// </summary>
		public void Register<M>(IReceptor receptor)
			where M : IMembrane
		{
			IMembrane membrane = RegisterMembrane(typeof(M));
			Register(membrane, receptor);
		}

		/// <summary>
		/// Register a stateful receptor contained within the specified membrane.
		/// </summary>
		public void Register(IMembrane membrane, IReceptor receptor)
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

			membranes[membrane.GetType()] = membrane;
			membraneReceptorInstances[membrane].Add(receptor);
		}

		/// <summary>
		/// Remove a semantic (source) type from a target (receptor).
		/// The target type will no longer receive notifications of source type instances.
		/// </summary>
		public void RemoveTypeNotify<TMembrane, TTarget, TSource>()
			where TMembrane : IMembrane
			where TTarget : IReceptor
			where TSource : ISemanticType
		{
			Type tsource = typeof(TSource);
			IMembrane membrane = membranes[typeof(TMembrane)];
			List<Type> targets = GetReceptors(membrane, tsource);

			// Remove from type list.
			foreach (Type ttarget in targets)
			{
				typeNotifiers[tsource].Remove(ttarget);
			}

			// Remove all receptors in the specified membrane that are processing this semantic type.
			List<IReceptor> instanceReceptors = GetStatefulReceptors(membrane, tsource);

			foreach (IReceptor receptor in instanceReceptors)
			{
				instanceNotifiers[tsource].Remove(receptor);
			}
		}

		public void RemoveTypeNotify<TMembrane, TSource>(IReceptor receptor)
			where TMembrane : IMembrane
			where TSource : ISemanticType
		{
			Type tsource = typeof(TSource);
			Type treceptor = receptor.GetType();
			IMembrane membrane = membranes[typeof(TMembrane)];
			List<Type> targets = GetReceptors(membrane, tsource);

			// Remove from type list for this membrane this receptor, by its type.
			foreach (Type ttarget in targets)
			{
				typeNotifiers[tsource].Remove(ttarget);
			}

			// Remove from instance list.
			List<IReceptor> instanceReceptors = GetStatefulReceptors(membrane, tsource);

			foreach (IReceptor ireceptor in instanceReceptors)
			{
				// Remove only this instance receptor from its membrane.
				if (ireceptor == receptor)
				{
					instanceNotifiers[tsource].Remove(receptor);
				}
			}
		}
		
		/// <summary>
		/// Process a semantic type, allowing the caller to specify an initializer before processing the instance.
		/// </summary>
		public void ProcessInstance<M, T>(Action<T> initializer, bool processOnCallerThread = false)
			where M : IMembrane
			where T : ISemanticType, new()
		{
			T inst = new T();
			initializer.IfNotNull(i => i(inst));
			ProcessInstance<M, T>(inst, processOnCallerThread);
		}

		public void ProcessInstance<M, T>(bool processOnCallerThread = false)
			where M : IMembrane
			where T : ISemanticType, new()
		{
			T inst = new T();
			ProcessInstance<M, T>(inst, processOnCallerThread);
		}

		/// <summary>
		/// Process an instance in a given membrane type.
		/// </summary>
		public void ProcessInstance<M, T>(T obj, bool processOnCallerThread = false)
			where M : IMembrane
			where T : ISemanticType
		{
			Type mtype = typeof(M);
			IMembrane membrane = RegisterMembrane<M>();
			ProcessInstance<T>(membrane, obj, processOnCallerThread);
		}

		/// <summary>
		/// Process an instance of a specific type immediately.  The type T is determined implicitly from the parameter type, so 
		/// a call can look like: ProcessInstance(t1)
		/// </summary>
		public void ProcessInstance<T>(IMembrane membrane, T obj, bool processOnCallerThread = false)
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

			List<Type> receptors = GetReceptors(membrane, tsource);

			if (!(membrane is LoggerMembrane))
			{
				ProcessInstance(Logger, obj);
			}
			
			foreach (Type ttarget in receptors)
			{
				// We can use dynamic here because we have a <T> generic to resolve the call parameter.
				// If we instead only have the interface ISemanticType, dynamic does not downcast to the concrete type --
				// therefore it can't locate the call point because it implements the concrete type.
				dynamic target = Activator.CreateInstance(ttarget);

				// Call immediately?
				if (processOnCallerThread)
				{
					Call(new ReceptorCall() { Receptor = target, Proc = () => target.Process(this, membrane, obj) });
				}
				else
				{
					// Pick a thread that has the least work to do.
					threadPool.MinBy(tp => tp.Count).Enqueue(new ReceptorCall() { Receptor = target, Proc = () => target.Process(this, membrane, obj) });
				}
			}

			// Also check stateful receptors
			List<IReceptor> sreceptors = GetStatefulReceptors(membrane, tsource);

			foreach (IReceptor receptor in sreceptors)
			{
				dynamic target = receptor;
				// Call immediately?
				if (processOnCallerThread)
				{
					Call(new ReceptorCall() { Receptor = target, Proc = () => target.Process(this, membrane, obj), AutoDispose = false });
				}
				else
				{
					threadPool.MinBy(tp => tp.Count).Enqueue(new ReceptorCall() { Receptor = target, Proc = () => target.Process(this, membrane, obj), AutoDispose = false });
				}
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

		protected IMembrane RegisterMembrane(Type m)
		{
			IMembrane membrane;

			if (!membranes.TryGetValue(m, out membrane))
			{
				membrane = (IMembrane)Activator.CreateInstance(m);
				membranes[m] = membrane;
				membraneReceptorTypes[membrane] = new List<Type>();
				membraneReceptorInstances[membrane] = new List<IReceptor>();
			}

			return membrane;
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

		protected List<Type> GetReceptors(IMembrane membrane, Type tsource)
		{
			List<Type> receptors = new List<Type>();
			List<Type> baseList;

			// Get the type notifiers for the provided type.
			if (typeNotifiers.TryGetValue(tsource, out baseList))
			{
				// Add only receptors that are in the membrane for the semantic instance being processed.
				// TODO: This is where we could put in the rules for moving up/down the membrane hierarchy.
				receptors.AddRange(membraneReceptorTypes[membrane].Where(t => baseList.Contains(t)));
			}
			
			// Check interfaces and base types of the source type as well to see if there are receptors handling the interfaces.

			foreach (Type t in tsource.GetParentTypes())
			{
				List<Type> tReceptors;

				if (typeNotifiers.TryGetValue(t, out tReceptors))
				{
					receptors.AddRange(membraneReceptorTypes[membrane].Where(tr => tReceptors.Contains(tr)));
				}
			}

			return receptors;
		}

		protected List<IReceptor> GetStatefulReceptors(IMembrane membrane, Type tsource)
		{
			List<IReceptor> receptors = new List<IReceptor>();
			List<IReceptor> baseList;

			if (instanceNotifiers.TryGetValue(tsource, out baseList))
			{
				// Add only receptors that are in the membrane for the semantic instance being processed.
				// TODO: This is where we could put in the rules for moving up/down the membrane hierarchy.
				receptors.AddRange(membraneReceptorInstances[membrane].Where(t => baseList.Contains(t)));
			}

			// Check interfaces and base types of the source type as well to see if there are receptors handling the interfaces.

			foreach (Type t in tsource.GetParentTypes())
			{
				List<IReceptor> tReceptors;

				if (instanceNotifiers.TryGetValue(t, out tReceptors))
				{
					receptors.AddRange(membraneReceptorInstances[membrane].Where(tr => tReceptors.Contains(tr)));
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
				ThreadSemaphore<ReceptorCall> ts = new ThreadSemaphore<ReceptorCall>();
				threadPool.Add(ts);
				thread.Start(ts);
			}
		}

		/// <summary>
		/// Invoke the action that we want to run on a thread.
		/// </summary>
		protected void ProcessPoolItem(object state)
		{
			ThreadSemaphore<ReceptorCall> ts = (ThreadSemaphore<ReceptorCall>)state;

			while (true)
			{
				ts.WaitOne();
				ReceptorCall rc;

				if (ts.TryDequeue(out rc))
				{
					Call(rc);
				}
			}
		}

		protected void Call(ReceptorCall rc)
		{
			try
			{
				rc.Proc();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			finally
			{
				if ( (rc.Receptor is IDisposable) && (rc.AutoDispose) )
				{
					((IDisposable)rc.Receptor).Dispose();
				}
			}
		}
	}
}
