using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Net;
using Newtonsoft.Json;

namespace Xfs
{
    /// 用户对象///注意事项:一个Socket的Send和Receive最好分别对应一个SocketAsyncEventArgs 
    public class XfsAsyncUserToken : XfsComponent
    {
        #region  pri 字段属性
        ///连接的Socket对象
        private Socket? _socket; 
        public Socket? Socket
        {
            get { return _socket; }
            set { _socket = value; }
        }
        private int _buffersize = 1024;
        //private Stopwatch _watch;
        private BlockingCollection<MessageData>? sendingQueue = new BlockingCollection<MessageData>();
        public XfsSession? Session
        {
            get
            {
                return this.GetParent<XfsSession>();
            }
        }
        ///接收数据的SocketAsyncEventArgs
        private SocketAsyncEventArgs _receiveEventArgs;      
        public SocketAsyncEventArgs ReceiveEventArgs
        {
            get { return _receiveEventArgs; }
            set { _receiveEventArgs = value; }
        }
        ///发送数据的SocketAsyncEventArgs
        private SocketAsyncEventArgs _sendEventArgs;
        public SocketAsyncEventArgs SendEventArgs
        {
            get { return _sendEventArgs; }
            set { _sendEventArgs = value; }
        }
        ///接收数据的缓冲区
        private byte[] _asyncReceiveBuffer;

        ///发送数据的缓冲区
        private byte[] _asyncSendBuffer;
        ///动态的接收缓冲区
        private XfsDynamicBufferManager _receiveBuffer;  
        public XfsDynamicBufferManager ReceiveBuffer
        {
            get { return _receiveBuffer; }
            set { _receiveBuffer = value; }
        }
        ///动态的发送缓冲区
        private XfsDynamicBufferManager _sendBuffer;
        public XfsDynamicBufferManager SendBuffer
        {
            get { return _sendBuffer; }
            set { _sendBuffer = value; }
        }
        public int? MessageSize { get; set; }
        public int DataStartOffset { get; set; }
        public int NextReceiveOffset { get; set; }
        //private const int MessageHeaderSize = 4;
        private const int MessageHeaderSize = 8;
        private byte[] headerDataItem = null;

        private Action<object, object>? readCallback;

        public event Action<object, object> ReadCallback
        {
            add
            {
                this.readCallback += value;
            }
            remove
            {
                this.readCallback -= value;
            }
        }

        #endregion

        #region TokenInit 初始化参数
        public XfsAsyncUserToken()
        {
            TokenInit();
        }
        public XfsAsyncUserToken(int bufferSize)
        {
            this._buffersize = bufferSize;
            this.TokenInit();
        }
        private void TokenInit()
        {
            this._socket = null;

            this._receiveBuffer = new XfsDynamicBufferManager(this._buffersize);
            this._asyncReceiveBuffer = new byte[this._buffersize];
            this._receiveEventArgs = new SocketAsyncEventArgs();
            this._receiveEventArgs.UserToken = this;
            this._receiveEventArgs.SetBuffer(_asyncReceiveBuffer, 0, _asyncReceiveBuffer.Length);       ///设置接收缓冲区
            this._receiveEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(this.OnCompleted);

            this._sendBuffer = new XfsDynamicBufferManager(this._buffersize);
            this._asyncSendBuffer = new byte[this._buffersize];
            this._sendEventArgs = new SocketAsyncEventArgs();
            this._sendEventArgs.UserToken = this;
            this._sendEventArgs.SetBuffer(_asyncSendBuffer, 0, _asyncSendBuffer.Length);                ///设置发送缓冲区
            this._sendEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(this.OnCompleted);
        }
        /// 当Socket上的发送或接收请求被完成时，调用此函数///激发事件的对象///与发送或接收完成操作相关联的SocketAsyncEventArg对象
        private void OnCompleted(object sender, SocketAsyncEventArgs e)
        {
            //XfsAsyncUserToken? userToken = e.UserToken as XfsAsyncUserToken;
            lock (this)
            {
                switch (e.LastOperation)
                {
                    case SocketAsyncOperation.Receive:
                        ProcessReceive(e);
                        break;
                    case SocketAsyncOperation.Send:
                        ProcessSend(e);
                        break;
                }
            }
        }
        #endregion

        #region Receive接收数据
        ///接收完成时处理函数    ///与接收完成操作相关联的SocketAsyncEventArg对象
        public void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                //XfsAsyncUserToken? token = e.UserToken as XfsAsyncUserToken;
                this.ReceiveBuffer.WriteBuffer(e.Buffer, e.Offset, e.BytesTransferred);

                Console.WriteLine(XfsTimeHelper.CurrentTime() + " 122. L: " + this.Socket.LocalEndPoint);
                Console.WriteLine(XfsTimeHelper.CurrentTime() + " 123. R: " + this.Socket.RemoteEndPoint);
                Console.WriteLine(XfsTimeHelper.CurrentTime() + " 124. M: " + e.Buffer.Length);
                Console.WriteLine(XfsTimeHelper.CurrentTime() + " 125. M: " + e.Offset);
                Console.WriteLine(XfsTimeHelper.CurrentTime() + " 126. M: " + e.BytesTransferred);
                Console.WriteLine(XfsTimeHelper.CurrentTime() + " 127. B: " + this.ReceiveBuffer.DataCount);

                //string packetBodyStr = Encoding.UTF8.GetString(e.Buffer);
                string packetBodyStr2 = Encoding.Default.GetString(e.Buffer, 4, 8);

                Console.WriteLine(XfsTimeHelper.CurrentTime() + " 132. packetBodyStr2: " + packetBodyStr2 + ".");

                ///解析接收到的字节
                ProcessReceivedData(this.DataStartOffset, this.NextReceiveOffset - this.DataStartOffset + e.BytesTransferred, 0, this, e);

                //更新下一个要接收数据的起始位置
                this.NextReceiveOffset += e.BytesTransferred;

                //如果达到缓冲区的结尾，则将NextReceiveOffset复位到缓冲区起始位置，并迁移可能需要迁移的未处理的数据
                if (this.NextReceiveOffset == e.Buffer.Length)
                {
                    //将NextReceiveOffset复位到缓冲区起始位置
                    this.NextReceiveOffset = 0;

                    //如果还有未处理的数据，则把这些数据迁移到数据缓冲区的起始位置
                    if (this.DataStartOffset < e.Buffer.Length)
                    {
                        var notYesProcessDataSize = e.Buffer.Length - this.DataStartOffset;
                        Buffer.BlockCopy(e.Buffer, this.DataStartOffset, e.Buffer, 0, notYesProcessDataSize);

                        //数据迁移到缓冲区起始位置后，需要再次更新NextReceiveOffset
                        this.NextReceiveOffset = notYesProcessDataSize;
                    }

                    this.DataStartOffset = 0;
                }
                //更新接收数据的缓冲区下次接收数据的起始位置和最大可接收数据的长度
                e.SetBuffer(this.NextReceiveOffset, e.Buffer.Length - this.NextReceiveOffset);

                ///接收后续的数据
                if (!this.Socket.ReceiveAsync(e))//为接收下一段数据，投递接收请求，这个函数有可能同步完成，这时返回false，并且不会引发SocketAsyncEventArgs.Completed事件
                {

                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " 165. ProcessReceive 循环接收等待中。。。");

                    //同步接收时处理接收完成事件
                    ProcessReceive(e);
                }
            }
            else
            {
                //this.Close();
            }
        }
        private void ProcessReceivedData(int dataStartOffset, int totalReceivedDataSize, int alreadyProcessedDataSize, XfsAsyncUserToken token, SocketAsyncEventArgs e)
        {
            if (alreadyProcessedDataSize >= totalReceivedDataSize)
            {
                Console.WriteLine(XfsTimeHelper.CurrentTime() + " 180. dataStartOffset: " + dataStartOffset);
                Console.WriteLine(XfsTimeHelper.CurrentTime() + " 181. totalReceivedDataSize: " + totalReceivedDataSize);
                Console.WriteLine(XfsTimeHelper.CurrentTime() + " 182. alreadyProcessedDataSize: " + alreadyProcessedDataSize);
                
                ///返回，继续接收数据...                
                return;
            }

            if (token.MessageSize == null)
            {

                Console.WriteLine(XfsTimeHelper.CurrentTime() + " 190. MessageSize: " + token.MessageSize);
                //如果之前接收到到数据加上当前接收到的数据大于消息头的大小，则可以解析消息头
                if (totalReceivedDataSize > MessageHeaderSize)
                {

                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " 196. totalReceivedDataSize: " + totalReceivedDataSize);
                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " 197. MessageHeaderSize: " + MessageHeaderSize);
                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " 198. dataStartOffset: " + dataStartOffset);
                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " 199. messageSize: " + token.MessageSize);
                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " 200. DataStartOffset: " + token.DataStartOffset);
                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " 201. e.Buffer: " + e.Buffer.Length);


                    //解析消息长度 
                    //var headerData = new byte[MessageHeaderSize];

                    var headerData = new byte[MessageHeaderSize];
                    Buffer.BlockCopy(e.Buffer, dataStartOffset, headerData, 0, MessageHeaderSize);
                    //var messageSize = BitConverter.ToInt32(headerData, 0);
                    var messageSize = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(headerData, 0));
                    ushort opcode = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(headerData, 4));

                    headerDataItem = headerData;

                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " 237. messageSize: " + messageSize);
                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " 238. opcode: " + opcode);


                    token.MessageSize = messageSize;
                    token.DataStartOffset = dataStartOffset + MessageHeaderSize;

                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " 212. totalReceivedDataSize: " + totalReceivedDataSize);
                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " 213. MessageHeaderSize: " + MessageHeaderSize);
                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " 214. dataStartOffset: " + dataStartOffset);
                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " 215. messageSize: " + token.MessageSize);
                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " 216. DataStartOffset: " + token.DataStartOffset);

                    //递归处理
                    ProcessReceivedData(token.DataStartOffset, totalReceivedDataSize, alreadyProcessedDataSize + MessageHeaderSize, token, e);
                }
                //如果之前接收到到数据加上当前接收到的数据仍然没有大于消息头的大小，则需要继续接收后续的字节
                else
                {
                    //这里不需要做什么事情
                }
            }
            else
            {
                Console.WriteLine(XfsTimeHelper.CurrentTime() + " 229. messageSize: " + token.MessageSize.Value);
                Console.WriteLine(XfsTimeHelper.CurrentTime() + " 230. dataStartOffset: " + dataStartOffset);
                Console.WriteLine(XfsTimeHelper.CurrentTime() + " 231. totalReceivedDataSize: " + totalReceivedDataSize);
                Console.WriteLine(XfsTimeHelper.CurrentTime() + " 232. alreadyProcessedDataSize: " + alreadyProcessedDataSize);


                var messageSize = token.MessageSize.Value;
                //判断当前累计接收到的字节数减去已经处理的字节数是否大于消息的长度，如果大于，则说明可以解析消息了
                if (totalReceivedDataSize - alreadyProcessedDataSize >= messageSize)
                {
                    var messageData = new byte[messageSize];
                    Buffer.BlockCopy(e.Buffer, dataStartOffset, messageData, 0, messageSize);

                    ///打印出来看看
                    string packetBodyStr2 = Encoding.UTF8.GetString(messageData);
                    //string packetBodyStr3 = Encoding.Default.GetString(e.Buffer, 4, 8);

                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " 246. packetBodyStr2 " + packetBodyStr2 + ".");
                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " 247. dataStartOffset " + dataStartOffset);
                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " 248. messageSize " + messageSize);

                    ///将接收好的完整的消息包拿走
                    //ProcessMessage(messageData, token, e);

                    ProcessMessage(headerDataItem, messageData, e, token);

                    //消息处理完后，需要清理token，以便接收下一个消息
                    token.DataStartOffset = dataStartOffset + messageSize;
                    token.MessageSize = null;

                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " 259. dataStartOffset: " + dataStartOffset);
                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " 260. totalReceivedDataSize: " + totalReceivedDataSize);
                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " 261. alreadyProcessedDataSize: " + alreadyProcessedDataSize);

                    //递归处理
                    ProcessReceivedData(token.DataStartOffset, totalReceivedDataSize, alreadyProcessedDataSize + messageSize, token, e);
                }
                //说明剩下的字节数还不够转化为消息，则需要继续接收后续的字节
                else
                {
                    //这里不需要做什么事情
                }
            }
        }
        private void ProcessMessage(byte[] HeadBytes, byte[] BodyBytes, SocketAsyncEventArgs e, object obj)
        {
            //this.sendingQueue.Add(new MessageData { Message = messageData, Token = token });
            //string packetBodyStr2 = Encoding.UTF8.GetString(messageData);
            //Console.WriteLine(XfsTimeHelper.CurrentTime() + " 288. messageData:" + packetBodyStr2 + ".");
            //Console.WriteLine(XfsTimeHelper.CurrentTime() + " 289. sendingQueue " + sendingQueue.Count);

            ///拿出包头中的操代码Opcode，位置4-7字节
            int length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(HeadBytes, 0));
            ushort opcode = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(HeadBytes, 4));

            Console.WriteLine(XfsTimeHelper.CurrentTime() + " Session-98: " + opcode + " + " + length + " ThreadId: " + Thread.CurrentThread.ManagedThreadId);

            ///心跳包
            if (opcode == XfsOuterOpcode.C4S_Heart)
            {
                if ((this.Parent as XfsSession).GetComponent<XfsHeartComponent>() != null)
                {
                    (this.Parent as XfsSession).GetComponent<XfsHeartComponent>().CdCount = 0;
                }
                return;
            }

            object? message = null;
            try
            {
                string jsonStr = Encoding.UTF8.GetString(BodyBytes, 0, BodyBytes.Length);
                XfsOpcodeTypeComponent opcodeTypeComponent = XfsGame.XfsSence.GetComponent<XfsOpcodeTypeComponent>();
                object instance = opcodeTypeComponent.GetInstance(opcode);
                message = JsonConvert.DeserializeObject(jsonStr, instance.GetType());   ////反序列化成功
            }
            catch (Exception ex)
            {
                // 出现任何消息解析异常都要断开Session，防止客户端伪造消息				
                Console.WriteLine(XfsTimeHelper.CurrentTime() + ex);
                if (this.Parent != null && (this.Parent as XfsSession) != null)
                {
                    (this.Parent as XfsSession).Dispose();
                }
                return;
            }
                        
            Console.WriteLine(XfsTimeHelper.CurrentTime() + " 349. message: " + message);

            //this.readCallback.Invoke(this, message);
            this.Session.OnRead(this, message);
        }
        #endregion

        #region Send发送数据
        /// 异步的发送数据
        public void Send(IXfsMessage message)
        {
            byte[] packetBytes = BuildMessage(message);

            this.SendEventArgs.SetBuffer(packetBytes, 0, packetBytes.Length);            
            
            this._socket.SendAsync(this.SendEventArgs);      ///异步发送SocketAsyncEventArgs消息包
        }

        private byte[] BuildMessage(IXfsMessage message)
        {
            int opcode = XfsGame.XfsSence.GetComponent<XfsOpcodeTypeComponent>().GetOpcode(message.GetType());///拿到本消息的操作码
            string msgJsons = XfsJsonHelper.ToJson(message);///用Json将参数（message）,序列化转换成字符串（string）
            byte[] packetBody = Encoding.UTF8.GetBytes(msgJsons); ///将字符串(string)转换成字节(byte)
            int msgLength = 8 + packetBody.Length;
            byte[] packetBytes = new byte[msgLength];
            BitConverter.GetBytes(IPAddress.HostToNetworkOrder(packetBody.Length)).CopyTo(packetBytes, 0); ///包体长度（不含包头的8个字节的长度），存在包头，占用4个字节(0,1,2,3).
            BitConverter.GetBytes(IPAddress.HostToNetworkOrder(opcode)).CopyTo(packetBytes, 4);///信息类型，存在包头，占用4个字节(4,5,6,7).
            packetBody.CopyTo(packetBytes, 8);///包体存入消息包，占用位置（8...）,从8开始向后存到底
            return packetBytes;
        }




        public void Send(string message)
        {
            ///将string字符窜，转成byte字节
            byte[] packetBody = Encoding.UTF8.GetBytes(message);

            Console.WriteLine(XfsTimeHelper.CurrentTime() + " 300. message: " + message);
            Console.WriteLine(XfsTimeHelper.CurrentTime() + " 301. message.Length: " + message.Length);

            Send(packetBody);
        }
        public void Send(byte[] packetBody)
        {
            ///将要发送的消息包体，加上包头，包头的内容为包体长度，包头必须占4个字节。
            byte[] packetBytes = BuildMessage(packetBody);

            ///将包装好的消息包，放到SocketAsyncEventArgs中，准备发送
            this.SendEventArgs.SetBuffer(packetBytes, 0, packetBytes.Length);

            ///打印消息内容
            Console.WriteLine(XfsTimeHelper.CurrentTime() + " 314. packetBody.Length: " + packetBody.Length);
            Console.WriteLine(XfsTimeHelper.CurrentTime() + " 315. packetBytes.Length: " + packetBytes.Length);
            Console.WriteLine(XfsTimeHelper.CurrentTime() + " 316. L: " + this._socket.LocalEndPoint);
            Console.WriteLine(XfsTimeHelper.CurrentTime() + " 317. R: " + this._socket.RemoteEndPoint);

            ///异步发送SocketAsyncEventArgs消息包
            this._socket.SendAsync(SendEventArgs);
        }
        private byte[] BuildMessage(byte[] message)
        {
            var header = BitConverter.GetBytes(message.Length);
            var packetBytes = new byte[header.Length + message.Length];
            header.CopyTo(packetBytes, 0);
            message.CopyTo(packetBytes, header.Length);
            return packetBytes;
        }
         /// 发送完成时处理函数 与发送完成操作相关联的SocketAsyncEventArg对象
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (this.SendEventArgs.SocketError == SocketError.Success)
            {
                //_userTokenPool.Push(userToken);
                //waitSendEvent.Set();
            }
            else
            {
                //this.Close();
            }
        }
      
        #region Send 

        //public void Send(SocketAsyncEventArgs e, byte[] data)
        //{
        //    //XfsAsyncUserToken userToken = e.UserToken as XfsAsyncUserToken;
        //    //if (e != null && e.SocketError == SocketError.Success && userToken.UserSocket.Connected)
        //    //{
        //    //    userToken.SendEventArgs.SetBuffer(data, 0, data.Length);             //写入要发送的数据
        //    //    if (!userToken.UserSocket.SendAsync(userToken.SendEventArgs))        //投递发送请求，这个函数有可能同步发送出去，这时返回false，并且不会引发SocketAsyncEventArgs.Completed事件
        //    //    {
        //    //        // 同步发送时处理发送完成事件
        //    //        ProcessSend(userToken.SendEventArgs);
        //    //    }
        //    //    userToken.SendBuffer.Clear();
        //    //}
        //    //else
        //    //{
        //    //    waitSendEvent.WaitOne();
        //    //    Send(e, data);
        //    //}

        //    XfsAsyncUserToken userToken = e.UserToken as XfsAsyncUserToken;
        //    userToken.SendBuffer.WriteBuffer(data, 0, data.Length);     //写入要发送的数据

        //    if (userToken.SendEventArgs.SocketError == SocketError.Success)
        //    {
        //        if (userToken.Socket.Connected)
        //        {
        //            //设置发送数据
        //            userToken.SendEventArgs.SetBuffer(userToken.SendBuffer.Buffer, 0, userToken.SendBuffer.DataCount);
        //            userToken.Socket.SendAsync(userToken.SendEventArgs);

        //            //userToken.SendEventArgs.SetBuffer(data, 0, data.Length);      //写入要发送的数据

        //            //Array.Copy(data, 0, e.Buffer, 0, data.Length);//设置发送数据

        //            if (!userToken.Socket.SendAsync(userToken.SendEventArgs))//投递发送请求，这个函数有可能同步发送出去，这时返回false，并且不会引发SocketAsyncEventArgs.Completed事件
        //            {
        //                // 同步发送时处理发送完成事件
        //                //ProcessSend(userToken.SendEventArgs);
        //            }
        //        }
        //        else
        //        {
        //            //CloseClientSocket(userToken);
        //        }
        //    }
        //    else
        //    {
        //        //CloseClientSocket(userToken);
        //    }
        //}       

        //private void SendQueueMessage()
        //{
        //    while (true)
        //    {
        //        var messageData = sendingQueue.Take();
        //        if (messageData != null)
        //        {
        //            SendMessage(messageData, BuildMessage(messageData.Message));
        //        }
        //    }
        //}

        //private void SendMessage(MessageData messageData, byte[] message)
        //{
        //    var userToken = messageData.Token;
        //    if (userToken != null)
        //    {
        //        userToken.SendBuffer.WriteBuffer(message, 0, message.Length);
        //        userToken.Socket.SendAsync(userToken.SendEventArgs);
        //    }
        //    else
        //    {
        //        waitSendEvent.WaitOne();
        //        SendMessage(messageData, message);
        //    }
        //}

        #endregion

        #endregion

        #region ///Dispose ///Close /// 关闭socket连接
        private void Close()
        {
            if (this._socket == null) return;

            Console.WriteLine(String.Format("客户 {0} 断开连接!", this._socket.RemoteEndPoint.ToString()));

            try
            {
                this._socket.Shutdown(SocketShutdown.Send);
            }
            catch (Exception)
            {
                // Throw if client has closed, so it is not necessary to catch.
            }
            finally
            {
                this._socket.Close();
            }

            this._socket = null;                                                                //释放引用，并清理缓存，包括释放协议对象等资源


            ////_userTokenPool.Push(userToken);
        }
        public override void Dispose()
        {
            base.Dispose();
            try
            {
                this._socket.Shutdown(SocketShutdown.Send);
            }
            catch (Exception)
            { }

            try
            {
                this._socket.Close();
            }
            catch (Exception)
            { }
        }
        #endregion

    }
}
