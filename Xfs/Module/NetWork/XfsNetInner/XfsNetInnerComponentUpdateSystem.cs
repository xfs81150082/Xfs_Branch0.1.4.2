using System.Net;
using ETModel;

namespace Xfs
{
	[XfsObjectSystem]
	public class XfsNetInnerComponentAwakeSystem : XfsAwakeSystem<XfsNetInnerComponent>
	{
		public override void Awake(XfsNetInnerComponent self)
		{
			self.Awake();
		}
	}	
	
	[XfsObjectSystem]
	public class XfsNetInnerComponentLoadSystem : XfsLoadSystem<XfsNetInnerComponent>
	{
		public override void Load(XfsNetInnerComponent self)
		{
			self.MessageDispatcher = new XfsInnerMessageDispatcher();
		}
	}

	[XfsObjectSystem]
	public class XfsNetInnerComponentUpdateSystem : XfsUpdateSystem<XfsNetInnerComponent>
	{
		public override void Update(XfsNetInnerComponent self)
		{
			self.Update();
		}
	}

	public static class XfsNetInnerComponentHelper
	{
		public static void Awake(this XfsNetInnerComponent self)
		{
			self.MessageDispatcher = new XfsInnerMessageDispatcher();
		}		

		public static void Update(this XfsNetInnerComponent self)
		{

		}


	}
}