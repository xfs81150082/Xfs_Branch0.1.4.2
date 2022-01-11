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
    public abstract class XfsNetSocketComponent : XfsEntity
    {
        #region Properties/// 
        private Socket _netSocket;                                                    /// 监听Socket，用于接受客户端的连接请求      
        public bool IsRunning { get; set; }                                           /// 服务器是否正在运行      
        public abstract XfsSenceType SenceType { get;  }                              /// 服务器类型？
        public bool IsServer 
        { 
            get 
            { 
                if (SenceType == XfsSenceType.XfsClient) 
                    return false;

                return true; 
            } 
        }                                       /// 服务端？客户端？
        private IPEndPoint _ipEendPoint { get; set; }                                            /// 监听的IP地址
        private int _port { get; set; } = 4002;                                                 /// 监听的端口
        private string _address { get; set; } = "127.0.0.1";
        private int _maxClientSessiong = 4;                                                            /// 服务器程序允许的最大客户端连接数
        private int _maxSeverSessiong = 10;                                                            /// 服务器程序允许的最大客户端连接数
        public IXfsMessageDispatcher? MessageDispatcher { get; set; }

        public XfsSessionPool _sessionPool;                                                      /// 会话对象池

        public Dictionary<long, XfsSession> Sessions = new Dictionary<long, XfsSession>();       /// 活跃的会话群
        public Queue<Socket> WaitingSockets = new Queue<Socket>();

        private bool isArgsInit = false;
        private bool disposed = false;

        public XfsNetSocketComponent() { }
        /// 初始化函数
        public void ArgsInit(string address,int port,int maxClient)
        {
            if (this.isArgsInit) return;
            this._address = address;
            this._port = port;
            this._maxSeverSessiong = maxClient;

            this.ArgsInit();
        }
        public void ArgsInit()
        {
            if (this.isArgsInit) return;

            this._ipEendPoint = new IPEndPoint(IPAddress.Parse(this._address), this._port) as IPEndPoint;

            if (!this.IsServer)
            {
                this._sessionPool = new XfsSessionPool(this._maxClientSessiong);
            }
            else
            {
                this._sessionPool = new XfsSessionPool(this._maxSeverSessiong);
            }

            if (this.MessageDispatcher == null)
            {
                this.MessageDispatcher = new XfsOuterMessageDispatcher();
            }

            this.isArgsInit = true;           
        }
        #endregion

        #region Server ///启动监听
        /// Server启动 监听
        public void ServerListenStart()
        {
            if (!this.IsRunning)
            {
                this._netSocket = new Socket(_ipEendPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);// 创建监听socket
                this._netSocket.Bind(_ipEendPoint);
                this._netSocket.Listen(this._maxSeverSessiong); // 开始监听
                this.IsRunning = true;

                Console.WriteLine(XfsTimeHelper.CurrentTime() + " 93. 开始监听: " + " XfsNetSocketComponent.");
                Console.WriteLine(XfsTimeHelper.CurrentTime() + " 94. L: " + this._netSocket.LocalEndPoint);
                Console.WriteLine(XfsTimeHelper.CurrentTime() + " 95. R: " + this._netSocket.RemoteEndPoint);

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

                //e.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);和下一行作用相同
                e.Completed += (sender, e) => this.ProcessAccept(e); ///和上一行作用相同

                Console.WriteLine(XfsTimeHelper.CurrentTime() + " 79. e.LastOperation: " + e.LastOperation.ToString());
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
                        if (this.Sessions.Count >= this._maxSeverSessiong)
                        {
                            ///触发事件///在线排队等待
                            this.WaitingSockets.Enqueue(peerSocket);

                            Console.WriteLine(XfsTimeHelper.CurrentTime() + " " + this.GetType().Name + " 118. 客户端排队列表 "  + this.WaitingSockets.Count + " 位.");

                            return;
                        }

                        ///用一个会话Session接收socket
                        this.UseSession(peerSocket);
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

        #region Client///开始连接
        /// 启动连结
        public void ClientConnentStart()
        {
            if (!this.IsRunning)
            {
                this._netSocket = new Socket(this._ipEendPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);// 创建监听socket
                this._netSocket.ConnectAsync(this._ipEendPoint);
            
                this.IsRunning = true;

                ///用一个会话Session接收socket
                this.UseSession(this._netSocket);                
            }
        }
        #endregion

        #region Add Remove///    
        private void UseSession(Socket socket)
        {
            XfsSession? session = _sessionPool.Pop(this);
            if (session == null) return;

            ///会话开始接收
            session.ReceiveAsync(socket);            
        }      
        #endregion

        #region Dispose///

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
