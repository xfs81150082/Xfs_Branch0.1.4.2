using System;
using System.Collections.Generic;

namespace Xfs
{
	[XfsObjectSystem]
	public class XfsOpcodeTypeComponentSystem : XfsAwakeSystem<XfsOpcodeTypeComponent>
	{
		public override void Awake(XfsOpcodeTypeComponent self)
		{
			self.Load();
		}
	}
	
	[XfsObjectSystem]
	public class XfsOpcodeTypeComponentLoadSystem : XfsLoadSystem<XfsOpcodeTypeComponent>
	{
		public override void Load(XfsOpcodeTypeComponent self)
		{
			self.Load();
		}
	}

	public class XfsOpcodeTypeComponent : XfsComponent
	{
		private readonly XfsDoubleMap<ushort, Type> opcodeTypes = new XfsDoubleMap<ushort, Type>();
		
		private readonly Dictionary<int, object> typeMessages = new Dictionary<int, object>();

		public void Load()
		{
			this.opcodeTypes.Clear();
			this.typeMessages.Clear();
			
			List<Type> types = XfsGame.EventSystem.GetTypes(typeof(XfsMessageAttribute));
			foreach (Type type in types)
			{
				object[] attrs = type.GetCustomAttributes(typeof(XfsMessageAttribute), false);
				if (attrs.Length == 0)
				{
					continue;
				}
				
				XfsMessageAttribute messageAttribute = attrs[0] as XfsMessageAttribute;
				if (messageAttribute == null)
				{
					continue;
				}

				this.opcodeTypes.Add(messageAttribute.Opcode, type);
				this.typeMessages.Add(messageAttribute.Opcode, Activator.CreateInstance(type));
			}
		}

		public ushort GetOpcode(Type type)
		{
			return this.opcodeTypes.GetKeyByValue(type);
		}

		public Type GetType(ushort opcode)
		{
			return this.opcodeTypes.GetValueByKey(opcode);
		}
		public object GetInstance(ushort opcode)
		{
			return this.typeMessages[opcode];
		}

		public int MessagesCount()
		{
			return typeMessages.Count;
		}
		public string Messages()
		{
			return typeMessages.Values.ToString();
		}
		public string Keys()
		{
			return typeMessages.Keys.ToString();
		}

		public override void Dispose()
		{
			if (this.IsDisposed)
			{
				return;
			}

			base.Dispose();
		}


	}
}