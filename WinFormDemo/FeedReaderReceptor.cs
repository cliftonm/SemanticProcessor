using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using Clifton.Extensions;
using Clifton.Semantics;

namespace WinFormDemo
{
	public class FeedReaderReceptor : IReceptor
	{
		public void Process(ISemanticProcessor proc, IMembrane membrane, ST_Url url)
		{
			SyndicationFeed sf = GetFeed(url.Url);
			sf.Items.ForEach(si => proc.ProcessInstance(membrane, new ST_RssFeedItem() { Text = si.Summary.Text }));
		}

		protected SyndicationFeed GetFeed(string feedUrl)
		{
			XmlReaderSettings settings = new XmlReaderSettings();
			settings.XmlResolver = null;
			settings.DtdProcessing = DtdProcessing.Ignore;

			XmlReader xr = XmlReader.Create(feedUrl);
			SyndicationFeed sfeed = SyndicationFeed.Load(xr);
			xr.Close();

			return sfeed;
		}
	}
}
