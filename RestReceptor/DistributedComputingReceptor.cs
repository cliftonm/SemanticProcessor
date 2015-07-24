using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Clifton.BasicWebServer;
using Clifton.Extensions;
using Clifton.Semantics;

namespace RestReceptor
{
	// For unit test support.
	public class DistributedProcessMembrane : Membrane { }

	// For unit test support.  Normally, each distributed system would either declare its own types
	// or share types through a common assembly.
	public class TestDistributedSemanticType : ISemanticType
	{
		public string Message { get; set; }
	}

	/// <summary>
	/// A stateful receptor.
	/// </summary>
	public class OutboundDistributedComputingReceptor : IReceptor<ISemanticType>
    {
		protected int outboundPort;

		public OutboundDistributedComputingReceptor(int outboundPort)
		{
			this.outboundPort = outboundPort;
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, ISemanticType obj)
		{
			string url = String.Format("http://localhost:{0}/semanticType", outboundPort);
			string json = JsonConvert.SerializeObject(obj);
			// Insert our type name:
			json = "{\"_type_\":\"" + obj.GetType().FullName + "\"," + json.Substring(1);
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			request.Method = "POST";
			request.ContentType = "application/json";
			request.ContentLength = json.Length;
			Stream st = request.GetRequestStream();
			byte[] bytes = Encoding.UTF8.GetBytes(json);
			st.Write(bytes, 0, bytes.Length);
			st.Close();
		}
    }

	/// <summary>
	/// A stateful receptor.
	/// </summary>
	public class InboundDistributedComputingReceptor : IReceptor
    {
		protected SemanticProcessor sp;		// the processor for the inbound types.
		protected Server server;
		protected int outboundPort;

		public InboundDistributedComputingReceptor(int inboundPort, SemanticProcessor sp)
		{
			this.sp = sp;

			server = new Server();
			server.OnRequest = (session, context) =>
			{
				session.Authenticated = true;
				session.UpdateLastConnectionTime();
			};

			server.AddRoute(new Route() { Verb = Router.POST, Path = "/semanticType", Handler = new AnonymousRouteHandler(server, ProcessInboundSemanticType) });
			server.Start("", inboundPort);
		}

		protected ResponsePacket ProcessInboundSemanticType(Session session, Dictionary<string, object> parms)
		{
			string json = parms["Data"].ToString();
			JObject jobj = JObject.Parse(json);
			string type = jobj["_type_"].ToString();
			
			// strip off the _type_ so we can then instantiate the semantic type.
			json = "{" + json.RightOf(',');

			// Requires that the namespace also matches the remote's namespace.
			Type ttarget = Type.GetType(type);	
			ISemanticType target = (ISemanticType)Activator.CreateInstance(ttarget);
			JsonConvert.PopulateObject(json, target);
			sp.ProcessInstance<DistributedProcessMembrane>(target);

			ResponsePacket ret = new ResponsePacket() { Data = Encoding.UTF8.GetBytes("OK"), ContentType = "text" };

			return ret;
		}
    }
}
