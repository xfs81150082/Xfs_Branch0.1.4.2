using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
namespace Xfs
{
    public abstract class XfsNetWorkComponent : XfsComponent
    {
        #region ///自定义属性
        public abstract XfsSenceType SenceType { get; }                     //服务器类型？
        public abstract bool IsListen { get; }                              //服务端？客户端？
        public string? IpString { get; set; }                               //监听的IP地址  
        public int Port { get; set; }                                       //监听的端口  
        public IPAddress? Address { get; set; }                             //监听的IP地址  
        public bool IsRunning { get; set; } = false;                        //服务器是否正在运行
        public Socket? NetSocket { get; set; }                              //服务器使用的异步socket
        public int MaxListenCount { get; set; } = 10;                       //服务器程序允许的最大客户端连接数  
        public IXfsMessageDispatcher? MessageDispatcher { get; set; }

        public Queue<Socket> WaitingSockets = new Queue<Socket>();
        public Dictionary<long, XfsSession> Sessions { get; set; } = new Dictionary<long, XfsSession>();
        #endregion

        #region ///服务端专用，启动保持监听
        public void ServerInit(string ipString, int port, int maxListenCount)
        {
            this.IpString = ipString;
            this.Port = port;
            this.MaxListenCount = maxListenCount;
        }
        public void Listening()
        {
            if (!this.IsRunning)
            {
                if (this.NetSocket == null)
                {
                    this.Address = IPAddress.Parse(this.IpString);
                    this.NetSocket = new Socket(this.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    this.NetSocket.Bind(new IPEndPoint(this.Address, this.Port));
                }
                this.NetSocket.Listen(this.MaxListenCount);
                this.IsRunning = true;

                Console.WriteLine("{0} 服务端启动，监听L {1}成功", XfsTimeHelper.CurrentTime(), this.NetSocket.LocalEndPoint);
                Console.WriteLine("{0} 服务端启动，监听R {1}成功", XfsTimeHelper.CurrentTime(), this.NetSocket.RemoteEndPoint);

                ///开始一个异步操作以接受传入的一个连接尝试
                this.NetSocket.BeginAccept(new AsyncCallback(this.AcceptCallback), this.NetSocket);
            }
        }
        private void AcceptCallback(IAsyncResult ar)
        {
            Socket? server = ar.AsyncState as Socket;
            Socket peerSocket = server.EndAccept(ar);

            ///触发事件///创建一个方法接收peerSocket (在方法里创建一个peer来处理读取数据//开始接受来自该客户端的数据)
            this.ReceiveSocket(peerSocket);

            ///接受下一个请求  
            server.BeginAccept(new AsyncCallback(this.AcceptCallback), server);
        }
        #endregion

        #region ///客户端专用，启动保持连接
        public void ClientInit(string ipString, int port)
        {
            this.IpString = ipString;
            this.Port = port;
        }
        public void Connecting()    //连接服务器
        {
            if (!this.IsRunning)
            {
                try
                {
                    if (this.NetSocket == null)
                    {
                        this.Address = IPAddress.Parse(this.IpString);
                        this.NetSocket = new Socket(this.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    }
                    this.NetSocket.BeginConnect(new IPEndPoint(this.Address, this.Port), new AsyncCallback(this.ConnectCallback), this.NetSocket);
                    this.IsRunning = true;

                    Console.WriteLine("{0} 客户端连接，连接L {1}成功", XfsTimeHelper.CurrentTime(), this.NetSocket.LocalEndPoint);
                    Console.WriteLine("{0} 客户端连接，连接R {1}成功", XfsTimeHelper.CurrentTime(), this.NetSocket.RemoteEndPoint);
                }
                catch (Exception ex)
                {
                    if (this.NetSocket != null)
                    {
                        this.NetSocket.Close();
                        this.NetSocket = null;
                    }
                    this.IsRunning = false;

                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " 38 " + ex.ToString());
                }
            }
        }
        private void ConnectCallback(IAsyncResult ar)
        {
            ///触发事件//创建一个Socket接收传递过来的TmSocket
            Socket? client = ar.AsyncState as Socket;
            try
            {
                //得到成功的连接
                client.EndConnect(ar);
                ///创建一个方法接收peerSocket (在方法里创建一个peer来处理读取数据//开始接受来自该客户端的数据)
                this.ReceiveSocket(client);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        #endregion

        #region ///服务器与客户端共用
        private void ReceiveSocket(Socket socket)
        {
            if (this.IsListen)
            {
                ///限制监听数量
                if (this.Sessions.Count >= this.MaxListenCount)
                {
                    ///触发事件///在线排队等待
                    this.WaitingSockets.Enqueue(socket);
                }
                else
                {
                    ///创建一个XfsSession接收socket
                    this.BeginReceiveSocket(socket);                    
                }
            }
            else
            {
                ///创建一个XfsSession接收socket
                this.BeginReceiveSocket(socket);
            }
        }
        private void BeginReceiveSocket(Socket socket)
        {
            ///创建一个Session接收socket
            XfsSession? session = XfsComponentFactory.CreateWithParent<XfsSession>(this);
            if (session == null) return;

            session.SenceType = this.SenceType;
            session.IsListen = this.IsListen;
            session.IsRunning = true;

            ///添加心跳包
            if (session.GetComponent<XfsHeartComponent>() == null)
            {
                session.AddComponent<XfsHeartComponent>();
            }
            
            ///加入会话字典
            this.Add(session);

            ///开始接收通信信息
            session.BeginReceiveMessage(socket);

            Console.WriteLine(XfsTimeHelper.CurrentTime() + " IsListen: " + this.IsListen + " Sessions: " + this.Sessions.Count + " session-L: " + session.Socket.LocalEndPoint);
            Console.WriteLine(XfsTimeHelper.CurrentTime() + " IsListen: " + this.IsListen + " Sessions: " + this.Sessions.Count + " session-R: " + session.Socket.RemoteEndPoint);
        }
        public virtual void Add(XfsSession session)
        {
            XfsSession? ses;
            if (this.Sessions.TryGetValue(session.InstanceId, out ses))
            {
                this.Sessions.Remove(ses.InstanceId);
            }
            this.Sessions.Add(session.InstanceId, session);
        }
        public virtual void Remove(long id)
        {
            XfsSession? session;
            if (!this.Sessions.TryGetValue(id, out session))
            {
                return;
            }
            this.Sessions.Remove(id);
            session.Dispose();
        }
        public XfsSession? Get(long id)
        {
            XfsSession? session;
            this.Sessions.TryGetValue(id, out session);
            return session;
        }     
        #endregion


    }
}