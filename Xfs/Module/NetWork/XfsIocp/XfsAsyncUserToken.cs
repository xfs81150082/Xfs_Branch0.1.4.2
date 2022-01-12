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
    ///用户对象///注意事项:一个Socket的Send和Receive最好分别对应一个SocketAsyncEventArgs 
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
        
        /////动态的接收缓冲区
        //private XfsDynamicBufferManager _receiveBuffer;  
        //public XfsDynamicBufferManager ReceiveBuffer
        //{
        //    get { return _receiveBuffer; }
        //    set { _receiveBuffer = value; }
        //}

        /////动态的发送缓冲区
        //private XfsDynamicBufferManager _sendBuffer;
        //public XfsDynamicBufferManager SendBuffer
        //{
        //    get { return _sendBuffer; }
        //    set { _sendBuffer = value; }
        //}
        
        public int? MessageSize { get; set; }
        
        public int DataStartOffset { get; set; }
        
        public int NextReceiveOffset { get; set; }

        private const int MessageHeaderSize = 4;       

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

            //this._receiveBuffer = new XfsDynamicBufferManager(this._buffersize);
            this._asyncReceiveBuffer = new byte[this._buffersize];
            this._receiveEventArgs = new SocketAsyncEventArgs();
            this._receiveEventArgs.UserToken = this;
            this._receiveEventArgs.SetBuffer(_asyncReceiveBuffer, 0, _asyncReceiveBuffer.Length);       ///设置接收缓冲区
            this._receiveEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(this.OnCompleted);

            //this._sendBuffer = new XfsDynamicBufferManager(this._buffersize);
            this._asyncSendBuffer = new byte[this._buffersize];
            this._sendEventArgs = new SocketAsyncEventArgs();
            this._sendEventArgs.UserToken = this;
            this._sendEventArgs.SetBuffer(_asyncSendBuffer, 0, _asyncSendBuffer.Length);                ///设置发送缓冲区
            this._sendEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(this.OnCompleted);
        }
        
        /// 当Socket上的发送或接收请求被完成时，调用此函数///激发事件的对象///与发送或接收完成操作相关联的SocketAsyncEventArg对象
        private void OnCompleted(object sender, SocketAsyncEventArgs e)
        {
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
                //this.ReceiveBuffer.WriteBuffer(e.Buffer, e.Offset, e.BytesTransferred);               

                //string packetBodyStr2 = Encoding.Default.GetString(e.Buffer, 4, 8);
                //Console.WriteLine(XfsTimeHelper.CurrentTime() + " 132. packetBodyStr2: " + packetBodyStr2 + ".");

                ///解析接收到的字节
                ProcessReceivedData(this.DataStartOffset, (this.NextReceiveOffset - this.DataStartOffset + e.BytesTransferred), 0, e);

                ///更新下一个要接收数据的起始位置
                this.NextReceiveOffset += e.BytesTransferred;

                ///如果达到缓冲区的结尾，则将NextReceiveOffset复位到缓冲区起始位置，并迁移可能需要迁移的未处理的数据
                if (this.NextReceiveOffset == e.Buffer.Length)
                {
                    ///将NextReceiveOffset复位到缓冲区起始位置
                    this.NextReceiveOffset = 0;

                    ///如果还有未处理的数据，则把这些数据迁移到数据缓冲区的起始位置
                    if (this.DataStartOffset < e.Buffer.Length)
                    {
                        var notYesProcessDataSize = e.Buffer.Length - this.DataStartOffset;
                        Buffer.BlockCopy(e.Buffer, this.DataStartOffset, e.Buffer, 0, notYesProcessDataSize);

                        ///数据迁移到缓冲区起始位置后，需要再次更新NextReceiveOffset
                        this.NextReceiveOffset = notYesProcessDataSize;
                    }

                    this.DataStartOffset = 0;
                }
                ///更新接收数据的缓冲区下次接收数据的起始位置和最大可接收数据的长度
                e.SetBuffer(this.NextReceiveOffset, e.Buffer.Length - this.NextReceiveOffset);

                ///接收后续的数据
                if (!this.Socket.ReceiveAsync(e))//为接收下一段数据，投递接收请求，这个函数有可能同步完成，这时返回false，并且不会引发SocketAsyncEventArgs.Completed事件
                {

                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " 165. ProcessReceive 循环接收等待中。");

                    ///同步接收时处理接收完成事件
                    ProcessReceive(e);
                }
            }
            else
            {
                //this.Close();
            }
        }
        private void ProcessReceivedData(int dataStartOffset, int totalReceivedDataSize, int alreadyProcessedDataSize, SocketAsyncEventArgs e)
        {
            if (alreadyProcessedDataSize >= totalReceivedDataSize)
            {
                ///返回，继续接收数据...                
                Console.WriteLine(XfsTimeHelper.CurrentTime() + " 204. ProcessReceivedData 返回,继续接收数据。");
                return;
            }

            if (MessageSize == null)
            {
                ///如果之前接收到到数据加上当前接收到的数据大于消息头的大小，则可以解析消息头
                if (totalReceivedDataSize > MessageHeaderSize)
                {
                    ///解析消息长度 
                    var headerData = new byte[MessageHeaderSize];

                    Buffer.BlockCopy(e.Buffer, dataStartOffset, headerData, 0, MessageHeaderSize);

                    var messageSize = BitConverter.ToInt32(headerData, 0);           

                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " 220. 消息体长度: " + messageSize);

                    MessageSize = messageSize;
                    DataStartOffset = dataStartOffset + MessageHeaderSize;                   

                    ///递归处理
                    ProcessReceivedData(DataStartOffset, totalReceivedDataSize, alreadyProcessedDataSize + MessageHeaderSize, e);
                }
                ///如果之前接收到到数据加上当前接收到的数据仍然没有大于消息头的大小，则需要继续接收后续的字节
                else
                {
                    ///这里不需要做什么事情
                }
            }
            else
            {
                var messageSize = MessageSize.Value;
                //判断当前累计接收到的字节数减去已经处理的字节数是否大于消息的长度，如果大于，则说明可以解析消息了
                if (totalReceivedDataSize - alreadyProcessedDataSize >= messageSize)
                {
                    var messageData = new byte[messageSize];
                    Buffer.BlockCopy(e.Buffer, dataStartOffset, messageData, 0, messageSize);

                    ///打印出来看看
                    //string packetBodyStr2 = Encoding.UTF8.GetString(messageData);
                    //string packetBodyStr3 = Encoding.Default.GetString(e.Buffer, 4, 8);                   

                    ///将接收好的完整的消息包拿走
                    //ProcessMessage(messageData, token, e);

                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " 250. 完整的消息体拿走。 " + messageData.Length);

                    ProcessMessage(messageData, e);

                    //消息处理完后，需要清理token，以便接收下一个消息
                    DataStartOffset = dataStartOffset + messageSize;
                    MessageSize = null;                   

                    //递归处理
                    ProcessReceivedData(DataStartOffset, totalReceivedDataSize, alreadyProcessedDataSize + messageSize,e);
                }
                //说明剩下的字节数还不够转化为消息，则需要继续接收后续的字节
                else
                {
                    //这里不需要做什么事情
                }
            }
        }
        private void ProcessMessage( byte[] BodyBytes, SocketAsyncEventArgs e)
        {
            ///拿出包头中的操代码Opcode，位置4-7字节
            //int length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(HeadBytes, 0));
            //ushort opcode = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(HeadBytes, 4));
                        
            ///拿出包头中的操代码Opcode，位置0-3字节
            ushort opcode = (ushort)BitConverter.ToInt32(BodyBytes, 0);

            ///心跳包
            //if (opcode == XfsOuterOpcode.C4S_Heart)
            //{
            //    if ((this.Parent as XfsSession).GetComponent<XfsHeartComponent>() != null)
            //    {
            //        (this.Parent as XfsSession).GetComponent<XfsHeartComponent>().CdCount = 0;
            //    }
            //    return;
            //}

            object? message = null;
            try
            {
                //string jsonStr = Encoding.UTF8.GetString(BodyBytes, 0, BodyBytes.Length);            
                string jsonStr = Encoding.UTF8.GetString(BodyBytes, 4, (BodyBytes.Length - 4));

                XfsOpcodeTypeComponent opcodeTypeComponent = XfsGame.XfsSence.GetComponent<XfsOpcodeTypeComponent>();
                object instance = opcodeTypeComponent.GetInstance(opcode);
                message = JsonConvert.DeserializeObject(jsonStr, instance.GetType());   ////反序列化成功

                Console.WriteLine(XfsTimeHelper.CurrentTime() + " 295 XfsAsyncUserToken-opcode: " + opcode);
                Console.WriteLine(XfsTimeHelper.CurrentTime() + " 296 XfsAsyncUserToken-jsonStr: " + jsonStr.Length);
                Console.WriteLine(XfsTimeHelper.CurrentTime() + " 297 XfsAsyncUserToken-message: " + message);
            }
            catch (Exception ex)
            {
                /// 出现任何消息解析异常都要断开Session，防止客户端伪造消息				
                Console.WriteLine(XfsTimeHelper.CurrentTime() + ex);
                if (this.Parent != null && (this.Parent as XfsSession) != null)
                {
                    (this.Parent as XfsSession).Dispose();
                }
                return;
            }
            ////this.readCallback.Invoke(this, message);
            ////this.readCallback(this, message);

            this.Session.OnRead(this, message);
        }
        #endregion

        #region Send发送数据
        /// 异步的发送数据
        public void Send(IXfsMessage message)
        {           
            byte[] packetBytes = BuildMessagePacket(message);

            this.SendEventArgs.SetBuffer(packetBytes, 0, packetBytes.Length);            
            
            this._socket.SendAsync(this.SendEventArgs);      ///异步发送SocketAsyncEventArgs消息包
        }

        private byte[] BuildMessagePacket(IXfsMessage message)
        {
            int opcode = XfsGame.XfsSence.GetComponent<XfsOpcodeTypeComponent>().GetOpcode(message.GetType());///拿到本消息的操作码
            string msgJsons = XfsJsonHelper.ToJson(message);///用Json将参数（message）,序列化转换成字符串（string）

            var opcodeB = BitConverter.GetBytes(opcode);
            var msgB = Encoding.UTF8.GetBytes(msgJsons);
            var packetB = new byte[opcodeB.Length + msgB.Length];
            opcodeB.CopyTo(packetB, 0);
            msgB.CopyTo(packetB, opcodeB.Length);

            var header = BitConverter.GetBytes(packetB.Length);
            var packetBytes = new byte[header.Length + packetB.Length];
            header.CopyTo(packetBytes, 0);
            packetB.CopyTo(packetBytes, header.Length);
            return packetBytes;
        }
  
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

        #region ///Dispose ///Close /// 关闭socket连接
        public void Init(Socket socket)
        {
            ////this.ReadCallback += this.Session.OnRead;

            if (this._socket != null)
            {
                this._socket.Shutdown(SocketShutdown.Send);
                this._socket.Close();
                this._socket = null;
            }

            this._socket = socket;
            this.SendEventArgs.RemoteEndPoint = this._socket.LocalEndPoint;
        }
        public void Init()
        {
            ////this.ReadCallback += this.Session.OnRead;

            if (this._socket != null)
            {
                this._socket.Shutdown(SocketShutdown.Send);
                this._socket.Close();
                this._socket = null;
            }
        }
        public void Close()
        {
            ////this.ReadCallback -= this.Session.OnRead;

            if (this._socket == null) return;

            try
            {
                this._socket.Shutdown(SocketShutdown.Send);
                this._socket.Close();
            }
            catch (Exception) {  }

            this._socket = null;                                                                //释放引用，并清理缓存，包括释放协议对象等资源
        }
        public override void Dispose()
        {
            base.Dispose();
            this.Close();
        }
        #endregion

    }
}
