using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Xfs
{
	[XfsObjectSystem]
	public class XfsSessionAwakeSystem : XfsAwakeSystem<XfsSession>
	{
		public override void Awake(XfsSession self)
		{
			self.Awake();
		}
	}

	public sealed class XfsSession : XfsEntity
	{
		#region 自定义属性
		public XfsAsyncUserToken? UserToken 
		{
            get
            {
				return this.GetComponent<XfsAsyncUserToken>();
            }			
		}
		public XfsNetSocketComponent? NetSocket
		{
			get
			{
				return this.GetParent<XfsNetSocketComponent>();
			}
		}
	
		public XfsPacketParser? recvPacketParser { get; private set; }
		public XfsPacketParser? sendPacketParser { get; private set; }
		public XfsNetWorkComponent? Network
		{
			get
			{
				return this.GetParent<XfsNetWorkComponent>();
			}
		}

		public Socket? Socket { get; set; }                ///创建一个套接字，用于储藏代理服务端套接字，与客户端通信///客户端Socket 
		public IPEndPoint? RemoteAddress
		{
			get
			{
				if (this.Socket != null && this.IsRunning == true)
				{
					return this.Socket.RemoteEndPoint as IPEndPoint;
				}
				else
				{
					return null;
				}
			}
		}
		public bool IsRunning { get; set; }
		public bool IsServer { get; set; }
		public bool IsListen { get; set; }
		public XfsSenceType SenceType { get; set; }
		private static int RpcId { get; set; }
		public readonly Dictionary<int, Action<IXfsResponse>> requestCallback = new Dictionary<int, Action<IXfsResponse>>();
		public void Awake()
		{
			this.requestCallback.Clear();
			this.AddComponent<XfsHeartComponent>();
			this.sendPacketParser = XfsComponentFactory.CreateWithParent<XfsPacketParser>(this);
			this.recvPacketParser = XfsComponentFactory.CreateWithParent<XfsPacketParser>(this);
			if (this.recvPacketParser != null)
			{
				this.recvPacketParser.ReadCallback += this.OnRead;
			}
		}
		#endregion

		#region 接收Socket信息        
		public void BeginReceiveMessage(XfsAsyncUserToken userToken)
		{
            if (this.GetComponent<XfsAsyncUserToken>() != null)
            {
				this.GetComponent<XfsAsyncUserToken>().Socket = userToken.Socket;
				this.GetComponent<XfsAsyncUserToken>().SendEventArgs.RemoteEndPoint = userToken.Socket.LocalEndPoint;
				this.GetComponent<XfsAsyncUserToken>().ReadCallback += this.OnRead;
			}
			this.AddComponent(userToken);
			this.Socket = userToken.Socket;
			
			this.OnConnect();
		}

		public void BeginReceiveMessage(Socket socket)
		{
			this.Socket = socket;
			if (this.recvPacketParser == null)
			{
				this.recvPacketParser = XfsComponentFactory.CreateWithParent<XfsPacketParser>(this);
			}
			if (this.recvPacketParser != null && this.Socket != null)
			{
				this.recvPacketParser.BeginReceiveMessage(socket);
			}
			this.OnConnect();
		}

		public void OnConnect()
		{			
			///显示与客户端连接			
			if (this.Socket != null)
			{
				Console.WriteLine(XfsTimeHelper.CurrentTime() + " 一个会话发生， IsListener: " + this.IsListen + " L: " + this.Socket.LocalEndPoint);
				Console.WriteLine(XfsTimeHelper.CurrentTime() + " 一个会话发生， IsListener: " + this.IsListen + " R: " + this.Socket.RemoteEndPoint);
			}
		}
		#endregion

		#region 接收包裹信息
		public void OnRead(object obj, object message)
		{
			try
			{
				this.XfsRun(obj, message);
			}
			catch (Exception e)
			{
				Console.WriteLine(XfsTimeHelper.CurrentTime() + e);
			}
		}
		public void XfsRun(object obj, object message)
		{
			IXfsResponse? response = message as IXfsResponse;

			Console.WriteLine(XfsTimeHelper.CurrentTime() + " 142. XfsSession-message: " + message);

			if (response == null)
			{
				if (message != null)
				{
					///拿到本消息的操作码
					ushort opcode = XfsGame.XfsSence.GetComponent<XfsOpcodeTypeComponent>().GetOpcode(message.GetType());
					
					Console.WriteLine(XfsTimeHelper.CurrentTime() + " 151. XfsSession-opcode: " + opcode);
					Console.WriteLine(XfsTimeHelper.CurrentTime() + " 152. XfsSession-message: " + message);

					///将消息发送进消息分流中心
					if (this.NetSocket != null)
					{
						if (this.NetSocket.MessageDispatcher == null)
						{
							this.NetSocket.MessageDispatcher = new XfsOuterMessageDispatcher();
						}
						this.NetSocket.MessageDispatcher.Dispatch(this, opcode, message);
					}
				}
				return;
			}
			else
			{
				Action<IXfsResponse>? action;
				if (!this.requestCallback.TryGetValue(response.RpcId, out action))
				{
					throw new Exception($"not found rpc, response message: {response}");
				}
				this.requestCallback.Remove(response.RpcId);
				action(response);
			}
		}
		#endregion

		#region Call Send 内外网发送消息
		/// 外网发送消息		
		public XfsTask<IXfsResponse> Call(IXfsRequest request, CancellationToken cancellationToken)
		{
			int rpcId = ++RpcId;
			var tcs = new XfsTaskCompletionSource<IXfsResponse>();

			this.requestCallback[rpcId] = (response) =>
			{
				try
				{
					if (XfsErrorCode.IsRpcNeedThrowException(response.Error))
					{
						Console.WriteLine(XfsTimeHelper.CurrentTime() + " : " + response.Message);
					}

					tcs.SetResult(response);
				}
				catch (Exception e)
				{
					tcs.SetException(new Exception($"Rpc Error: {request.GetType().FullName}", e));
				}
			};

			cancellationToken.Register(() => this.requestCallback.Remove(rpcId));

			request.RpcId = rpcId;
			this.Send(request);
			return tcs.Task;
		}
		public XfsTask<IXfsResponse> Call(IXfsRequest request)
		{
			int rpcId = ++RpcId;
			var tcs = new XfsTaskCompletionSource<IXfsResponse>();

			this.requestCallback[rpcId] = (response) =>
			{
				try
				{
					if (XfsErrorCode.IsRpcNeedThrowException(response.Error))
					{
						Console.WriteLine(XfsTimeHelper.CurrentTime() + " : " + response.Message);
					}

					tcs.SetResult(response);
				}
				catch (Exception e)
				{
					tcs.SetException(new Exception($"Rpc Error: {request.GetType().FullName}", e));
				}
			};

			request.RpcId = rpcId;
			this.Send(request);
			return tcs.Task;
		}
		public void Reply(IXfsResponse message)
		{
			if (this.IsDisposed)
			{
				throw new Exception("session已经被Dispose了");
			}

			this.Send(message);
		}
		public void Send(IXfsMessage message)
		{
			if (this.IsDisposed)
			{
				Console.WriteLine(XfsTimeHelper.CurrentTime() + " : " + this.GetType().Name + "-222" + ": session InstanceId: " + this.InstanceId + " 已经被Dispose了");
				//if (this.Network != null)
				//{
				//	if (this.Network.Sessions.Count > 0)
				//	{
				//		if (this.Network.Sessions.TryGetValue(this.InstanceId, out XfsSession? peer))
				//		{
				//			this.Network.Sessions.Remove(this.InstanceId);
				//		}
				//	}
				//}
				if (this.NetSocket != null)
				{
					if (this.NetSocket.Sessions.Count > 0)
					{
						if (this.NetSocket.Sessions.TryGetValue(this.InstanceId, out XfsSession? peer))
						{
							this.NetSocket.Sessions.Remove(this.InstanceId);
						}
					}
				}
				return;
			}

			//if (this.sendPacketParser == null)
			//{
			//	this.sendPacketParser = XfsComponentFactory.CreateWithParent<XfsPacketParser>(this);
			//}

			///发送信息message
			//if (this.Socket != null && this.sendPacketParser != null)
			//{
			//	this.sendPacketParser.Send(message, this.Socket);
			//}

			if (this.UserToken != null && this.UserToken.Socket != null)
			{
				this.UserToken.Send(message);
			}
		}

		/// 内网发送消息		
		public XfsTask<IXfsResponse> InCall(IXfsRequest request, CancellationToken cancellationToken)
		{
			int rpcId = ++RpcId;
			var tcs = new XfsTaskCompletionSource<IXfsResponse>();

			this.requestCallback[rpcId] = (response) =>
			{
				try
				{
					if (XfsErrorCode.IsRpcNeedThrowException(response.Error))
					{
						Console.WriteLine(XfsTimeHelper.CurrentTime() + " : " + response.Message);
					}

					tcs.SetResult(response);
				}
				catch (Exception e)
				{
					tcs.SetException(new Exception($"Rpc Error: {request.GetType().FullName}", e));
				}
			};

			cancellationToken.Register(() => this.requestCallback.Remove(rpcId));

			request.RpcId = rpcId;
			this.InSend(request);
			return tcs.Task;
		}
		public XfsTask<IXfsResponse> InCall(IXfsRequest request)
		{
			int rpcId = ++RpcId;
			var tcs = new XfsTaskCompletionSource<IXfsResponse>();

			this.requestCallback[rpcId] = (response) =>
			{
				try
				{
					if (XfsErrorCode.IsRpcNeedThrowException(response.Error))
					{
						Console.WriteLine(XfsTimeHelper.CurrentTime() + " : " + response.Message);
					}

					tcs.SetResult(response);
				}
				catch (Exception e)
				{
					tcs.SetException(new Exception($"Rpc Error: {request.GetType().FullName}", e));
				}
			};

			request.RpcId = rpcId;
			this.InSend(request);
			return tcs.Task;
		}
		public void InReply(IXfsResponse message)
		{
			if (this.IsDisposed)
			{
				throw new Exception("session已经被Dispose了");
			}

			this.InSend(message);
		}
		public void InSend(IXfsMessage message)
		{			
			this.XfsRun(this, message);		
		}
		#endregion

		#region Dispose
		public override void Dispose()
		{
			if (this.IsDisposed)
			{
				return;
			}

			base.Dispose();
			this.Socket?.Close();

			foreach (Action<IXfsResponse> action in this.requestCallback.Values.ToArray())
			{
				action.Invoke(new XfsResponseMessage());
			}
			this.requestCallback.Clear();

			if (this.recvPacketParser != null)
			{
				this.recvPacketParser.ReadCallback -= this.OnRead;
				this.recvPacketParser.Dispose();
			}
			if (this.sendPacketParser != null)
			{
				this.sendPacketParser.Dispose();
			}

			if (this.Network != null)
			{
				this.Network.Remove(this.InstanceId);
			}

			this.IsRunning = false;

			if (this.Network != null)
			{
				Console.WriteLine(XfsTimeHelper.CurrentTime() + " 一个Session : 已经中断连接, Sessions: " + this.Network?.Sessions.Count);
			}
		}
		#endregion

	}
}
