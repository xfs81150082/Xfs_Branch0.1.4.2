

namespace Xfs
{
	[XfsObjectSystem]
	public class XfsNetOuterComponentAwakeSystem : XfsAwakeSystem<XfsNetOuterComponent>
	{
		public override void Awake(XfsNetOuterComponent self)
		{
			if (XfsStartConfigComponent.Instance == null) return;
            if (XfsStartConfigComponent.Instance.StartConfig == null) return;
			if (XfsStartConfigComponent.Instance.StartConfig.ServerIP == null) return;
			if (XfsStartConfigComponent.Instance.StartConfig.Port == 0) return;
			self.MessageDispatcher = new XfsOuterMessageDispatcher();
			self.ArgsInit(XfsStartConfigComponent.Instance.StartConfig.ServerIP, XfsStartConfigComponent.Instance.StartConfig.Port, XfsStartConfigComponent.Instance.StartConfig.MaxLiningCount);

		}
	}

	[XfsObjectSystem]
	public class XfsNetOuterComponentAwake1System : XfsAwakeSystem<XfsNetOuterComponent, string>
	{
		public override void Awake(XfsNetOuterComponent self, string address)
		{
			if (XfsStartConfigComponent.Instance == null) return;
			if (XfsStartConfigComponent.Instance.StartConfig == null) return;
			if (XfsStartConfigComponent.Instance.StartConfig.ServerIP == null) return;
			if (XfsStartConfigComponent.Instance.StartConfig.Port == 0) return;
			self.MessageDispatcher = new XfsOuterMessageDispatcher();
			self.ArgsInit(XfsStartConfigComponent.Instance.StartConfig.ServerIP, XfsStartConfigComponent.Instance.StartConfig.Port, XfsStartConfigComponent.Instance.StartConfig.MaxLiningCount);

		}
	}

	[XfsObjectSystem]
	public class XfsNetOuterComponentLoadSystem : XfsLoadSystem<XfsNetOuterComponent>
	{
		public override void Load(XfsNetOuterComponent self)
		{
			self.MessageDispatcher = new XfsOuterMessageDispatcher();
		}
	}

	[XfsObjectSystem]
	public class XfsNetOuterComponentUpdateSystem : XfsUpdateSystem<XfsNetOuterComponent>
	{
		public override void Update(XfsNetOuterComponent self)
		{
			NetOuterStart(self);
		}

		private void NetOuterStart(XfsNetOuterComponent self)
		{

			if (XfsGame.XfsSence.IsServer)
			{
				self.ServerListenStart();
			}
			else
			{
				self.ClientConnentStart();
			}
		}


	}
}