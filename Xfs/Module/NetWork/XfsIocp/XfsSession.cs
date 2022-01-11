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
		
		public Socket? Socket
		{
			get
			{
				if (this.UserToken != null && this.UserToken.Socket!= null)
				{
					return this.UserToken.Socket;
                }
                else
                {
					return null;
                }
			}
		}                                 ///创建一个套接字，用于储藏代理服务端套接字，与客户端通信///客户端Socket 
		
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

		public bool IsClosed = false;

		public XfsSenceType SenceType { get; set; }
		
		private static int RpcId { get; set; }
		
		public readonly Dictionary<int, Action<IXfsResponse>> requestCallback = new Dictionary<int, Action<IXfsResponse>>();
		
		public void Awake()
		{
			this.requestCallback.Clear();			
		}
        #endregion

        #region OnRead XfsRun///接收包裹信息
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

		#region Call Send /// 外网发送消息				
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
			this.Send(message);
		}

		public void Send(IXfsMessage message)
		{
			if (this.IsClosed)
			{
				Console.WriteLine(XfsTimeHelper.CurrentTime() + " 216. " + this.GetType().Name +  " InstanceId: " + this.InstanceId + " 已经被Closed了");

				this.Close();
				
				return;
			}

			if (this.UserToken != null && this.UserToken.Socket != null)
			{
				this.UserToken.Send(message);
			}
		}
		#endregion

		#region Call Send Inner /// 内网发送消息	
		public XfsTask<IXfsResponse> CallInner(IXfsRequest request, CancellationToken cancellationToken)
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
			this.SendInner(request);
			return tcs.Task;
		}

		public XfsTask<IXfsResponse> CallInner(IXfsRequest request)
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
			this.SendInner(request);
			return tcs.Task;
		}

		public void ReplyInner(IXfsResponse message)
		{
			if (this.IsDisposed)
			{
				throw new Exception("session已经被Dispose了");
			}

			this.SendInner(message);
		}
		
		public void SendInner(IXfsMessage message)
		{			
			this.XfsRun(this, message);		
		}
		#endregion

		#region ReceiveAsync Init Close
		public void ReceiveAsync(Socket socket)
		{
			this.SenceType = XfsGame.XfsSence.Type;
			this.IsServer = XfsGame.XfsSence.IsServer;
			this.IsClosed = false;
			this.IsRunning = true;

			///添加心跳包
			if (this.GetComponent<XfsHeartComponent>() == null)
			{
				this.AddComponent<XfsHeartComponent>();
			}
			this.GetComponent<XfsHeartComponent>().Init();

			if (this.GetComponent<XfsAsyncUserToken>() == null)
			{
				this.AddComponent<XfsAsyncUserToken>();
			}
			this.GetComponent<XfsAsyncUserToken>().Init(socket);

			///Client，如果服户端有消息包投递过来，则开始接收消息包
			if (!socket.ReceiveAsync(this.GetComponent<XfsAsyncUserToken>().ReceiveEventArgs))//投递接收请求
			{
				this.GetComponent<XfsAsyncUserToken>().ProcessReceive(this.GetComponent<XfsAsyncUserToken>().ReceiveEventArgs);
			}
		}
	
		public void Close()
		{
			if (this.IsClosed)
			{
				return;
			}			

			if (this.GetComponent<XfsHeartComponent>() != null)
			{
				this.GetComponent<XfsHeartComponent>().Close();
			}
			if (this.GetComponent<XfsAsyncUserToken>() != null)
			{
				this.GetComponent<XfsAsyncUserToken>().Close();
			}

			foreach (Action<IXfsResponse> action in this.requestCallback.Values.ToArray())
			{
				action.Invoke(new XfsResponseMessage());
			}
			this.requestCallback.Clear();

			if (this.NetSocket != null)
			{
				///从会话列表中移除
				this.NetSocket.Remove(this.InstanceId);

				///回收到Session池里
				this.NetSocket._sessionPool.Push(this);

				///如果是客户端，修改客户端的状态为没运行，没连结，然后客户端会自动自动重连
				if (!this.NetSocket.IsServer)
				{
					this.NetSocket.IsRunning = false;
				}

			}

			this.IsClosed = true;
			this.IsRunning = false;			
		}

		public override void Dispose()
		{
			if (this.IsDisposed)
			{
				return;
			}

			base.Dispose();			

			this.Close();		
		}
		#endregion

	}
}
