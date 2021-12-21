using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace Xfs
{
    ///套节字Socket异步通信，用SocketAsyncEventArgs类异步通信，Iocp架构。
    public abstract class XfsNetSocketComponent : XfsComponent
    {
        #region ///Properties 
        private Socket _netSocket;                                                    /// 监听Socket，用于接受客户端的连接请求      
        public bool IsRunning { get; private set; }                                   /// 服务器是否正在运行      
        public abstract XfsSenceType SenceType { get;  }                           /// 服务器类型？
        public abstract bool IsServer { get;  }                                    /// 服务端？客户端？
        private IPEndPoint _ipEendPoint { get; set; }                                 /// 监听的IP地址
        private int _port { get; set; } = 4002;                                       /// 监听的端口
        private string _address { get; set; } = "127.0.0.1";        
        private int _maxClient = 10;                                                  /// 服务器程序允许的最大客户端连接数
        public IXfsMessageDispatcher? MessageDispatcher { get; set; }

        public XfsAsyncUserTokenPool _userTokenPool;                                  /// 对象池

        public Dictionary<long, XfsSession> Sessions = new Dictionary<long, XfsSession>();
        public Queue<Socket> WaitingSockets = new Queue<Socket>();
      
        private bool disposed = false;

        public XfsNetSocketComponent() { }
        /// 初始化函数
        public void ArgsInit(string address,int port,int maxClient)
        {
            this._address = address;
            this._port = port;
            this._maxClient = maxClient;

            this.ArgsInit();
        }
        private void ArgsInit()
        {
            this._ipEendPoint = new IPEndPoint(IPAddress.Parse(this._address), this._port) as IPEndPoint;
            this._userTokenPool = new XfsAsyncUserTokenPool(this._maxClient);
            this._netSocket = new Socket(_ipEendPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            this.MessageDispatcher = new XfsOuterMessageDispatcher();

            Console.WriteLine(XfsTimeHelper.CurrentTime() + " 46. userTokenPool.Count " + this._userTokenPool.Count);
            Console.WriteLine(XfsTimeHelper.CurrentTime() + " 47. IpEendPoint:" + this._ipEendPoint.Address + ":" + this._ipEendPoint.Port + ".");
        }
        #endregion

        #region Server ///启动监听
        /// Server启动 监听
        public void ServerListenStart()
        {
            //this.ArgsInit();

            if (!this.IsRunning)
            {
                this._netSocket = new Socket(_ipEendPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);// 创建监听socket
                this._netSocket.Bind(_ipEendPoint);
                this._netSocket.Listen(this._maxClient); // 开始监听
                this.IsRunning = true;

                Console.WriteLine(XfsTimeHelper.CurrentTime() + " 63. 开始监听: " + " XfsNetSocketComponent.");
                Console.WriteLine(XfsTimeHelper.CurrentTime() + " 64. L: " + this._netSocket.LocalEndPoint);
                Console.WriteLine(XfsTimeHelper.CurrentTime() + " 65. R: " + this._netSocket.RemoteEndPoint);

                // 在监听Socket上投递一个接受请求。
                this.StartAccept(null);
            }
        }
        /// Server从客户端开始接受一个连接操作
        private void StartAccept(SocketAsyncEventArgs e)
        {
            if (e == null)
            {
                e = new SocketAsyncEventArgs();

                //asyniar.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);和下一行作用相同
                Console.WriteLine(XfsTimeHelper.CurrentTime() + " 79. e.LastOperation: " + e.LastOperation.ToString());

                e.Completed += (sender, e) => this.ProcessAccept(e); ///和上一行作用相同
            }
            else
            {
                e.AcceptSocket = null;
            }

            //_maxAcceptedClients.WaitOne();
            if (!this._netSocket.AcceptAsync(e))
            {
                Console.WriteLine(XfsTimeHelper.CurrentTime() + " 91. e.LastOperation: " + e.LastOperation.ToString());

                this.ProcessAccept(e);
                //如果I/O挂起等待异步则触发AcceptAsyn_Asyn_Completed事件
                //此时I/O操作同步完成，不会触发Asyn_Completed事件，所以指定BeginAccept()方法
            }
        }
        /// Server监听Socket接受处理
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                Socket? peerSocket = e.AcceptSocket;//和客户端关联的socket
                if (peerSocket.Connected)
                {
                    try
                    {
                        ///限制监听数量
                        if (this.Sessions.Count >= this._maxClient)
                        {
                            ///触发事件///在线排队等待
                            this.WaitingSockets.Enqueue(peerSocket);

                            Console.WriteLine(XfsTimeHelper.CurrentTime() + " " + this.GetType().Name + " 118. 客户端排队列表 "  + this.WaitingSockets.Count + " 位.");

                            return;
                        }                       

                        ///创建一个Session接收socket
                        XfsSession? session = XfsComponentFactory.CreateWithParent<XfsSession>(this);
                        if (session == null) return;

                        session.SenceType = this.SenceType;
                        session.IsServer = this.IsServer;
                        session.IsRunning = true;

                        ///添加心跳包
                        if (session.GetComponent<XfsHeartComponent>() == null)
                        {
                            session.AddComponent<XfsHeartComponent>();
                        }
                        if (session.GetComponent<XfsAsyncUserToken>() == null)
                        {
                            session.AddComponent<XfsAsyncUserToken>();
                        }

                        session.GetComponent<XfsAsyncUserToken>().Socket = peerSocket;
                        session.GetComponent<XfsAsyncUserToken>().SendEventArgs.RemoteEndPoint = peerSocket.LocalEndPoint;


                        ///加入会话字典
                        this.Add(session);

                        Console.WriteLine(XfsTimeHelper.CurrentTime() + " " + this.GetType().Name + " 159. 客户端 " + peerSocket.RemoteEndPoint.ToString() + " 连入, 共有 " + this.Sessions.Count + " 个会话.");

                        ///Server，如果客户端有消息包投递过来，则开始接收消息包
                        if (!peerSocket.ReceiveAsync(session.GetComponent<XfsAsyncUserToken>().ReceiveEventArgs))//投递接收请求
                        {
                            session.GetComponent<XfsAsyncUserToken>().ProcessReceive(e);
                        }

                    }
                    catch (SocketException ex)
                    {
                        Console.WriteLine(XfsTimeHelper.CurrentTime() + String.Format(" 127. 接收客户 {0} 数据出错, 异常信息： {1} 。", this._ipEendPoint, ex.ToString()));
                        //TODO 异常处理
                    }
                    //投递下一个接受请求
                    this.StartAccept(e);
                }
            }
        }
        #endregion

        #region ///Client开始连接
        /// 启动连结
        public void ClientConnentStart()
        {
            //this.ArgsInit();

            if (!this.IsRunning)
            {
                this._netSocket = new Socket(this._ipEendPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);// 创建监听socket
                this._netSocket.ConnectAsync(this._ipEendPoint);
            
                ///创建一个Session接收socket
                XfsSession? session = XfsComponentFactory.CreateWithParent<XfsSession>(this);
                if (session == null) return;

                session.SenceType = this.SenceType;
                session.IsServer = this.IsServer;
                session.IsRunning = true;

                ///添加心跳包
                if (session.GetComponent<XfsHeartComponent>() == null)
                {
                    session.AddComponent<XfsHeartComponent>();
                }
                if (session.GetComponent<XfsAsyncUserToken>() == null)
                {
                    session.AddComponent<XfsAsyncUserToken>();
                }

                session.GetComponent<XfsAsyncUserToken>().Socket = this._netSocket;
                session.GetComponent<XfsAsyncUserToken>().SendEventArgs.RemoteEndPoint = this._netSocket.LocalEndPoint;

                ///加入会话字典
                this.Add(session);

                this.IsRunning = true;

                Console.WriteLine(XfsTimeHelper.CurrentTime() + " 166. 开始连结: " + " XfsNetSocketComponent.");
                Console.WriteLine(XfsTimeHelper.CurrentTime() + " 167. L: " + this._netSocket.LocalEndPoint);
                Console.WriteLine(XfsTimeHelper.CurrentTime() + " 168. R: " + this._netSocket.RemoteEndPoint);

                ///Client，如果服户端有消息包投递过来，则开始接收消息包
                if (!this._netSocket.ReceiveAsync(session.GetComponent<XfsAsyncUserToken>().ReceiveEventArgs))//投递接收请求
                {
                    session.GetComponent<XfsAsyncUserToken>().ProcessReceive(session.GetComponent<XfsAsyncUserToken>().ReceiveEventArgs);
                }
            }
        }
        #endregion

        #region ///Add      
        public virtual void Add(XfsSession session)
        {
            XfsSession? ses;
            if (this.Sessions.TryGetValue(session.InstanceId, out ses))
            {
                this.Sessions.Remove(ses.InstanceId);
            }
            this.Sessions.Add(session.InstanceId, session);
        }


        #endregion

        #region ///Dispose
        public override void Dispose()
        {
            if (!this.disposed)
            {
                try
                {
                    this.IsRunning = false;
                    if (this._netSocket != null)
                    {
                        this._netSocket = null;
                    }
                }
                catch (SocketException ex)
                {
                    //TODO 事件
                }
                this.disposed = true;
            }
            GC.SuppressFinalize(this);
        }
        #endregion


    }

    public class MessageData : XfsComponent
    {
        public XfsAsyncUserToken? Token;
        public byte[]? Message;
    }

}
