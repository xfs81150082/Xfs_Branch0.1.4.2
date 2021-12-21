using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace Xfs
{
	[XfsObjectSystem]
	public class XfsStartConfigComponentAwakeSystem : XfsAwakeSystem<XfsStartConfigComponent>
	{
		public override void Awake(XfsStartConfigComponent self)
		{
			self.Awake();
		}
	}

	public class XfsStartConfigComponent : XfsComponent
	{
		public static XfsStartConfigComponent? Instance { get; private set; }

		private Dictionary<int, XfsStartConfig> configDict = new Dictionary<int, XfsStartConfig>();

		private Dictionary<int, IPEndPoint> innerAddressDict = new Dictionary<int, IPEndPoint>();

		public XfsStartConfig? StartConfig { get; private set; }

		public List<XfsStartConfig> StartConfigs { get; private set; } = new List<XfsStartConfig>();

		public void Awake()
        {            
			Instance = this;

			this.StartConfig = new XfsStartConfig() { SenceType = XfsSenceType.XfsServer, ServerIP = "127.0.0.1", Port = 4001, MaxLiningCount = 10 };
			this.configDict.Add((int)this.StartConfig.SenceType, this.StartConfig);
			this.StartConfigs.Add(this.StartConfig);


		}

		public override void Dispose()
		{
			if (this.IsDisposed)
			{
				return;
			}
			base.Dispose();

			Instance = null;
		}

		public XfsStartConfig Get(int id)
		{
			try
			{
				return this.configDict[id];
			}
			catch (Exception e)
			{
				throw new Exception($"not found startconfig: {id}", e);
			}
		}

		public IPEndPoint GetInnerAddress(int id)
		{
			try
			{
				return this.innerAddressDict[id];
			}
			catch (Exception e)
			{
				throw new Exception($"not found innerAddress: {id}", e);
			}
		}

		public XfsStartConfig[] GetAll()
		{
			return this.configDict.Values.ToArray();
		}

		public int Count
		{
			get
			{
				return this.configDict.Count;
			}
		}


	}
}
