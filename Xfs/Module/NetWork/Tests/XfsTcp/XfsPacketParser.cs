using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace Xfs
{
    public class XfsPacketParser : XfsComponent
    {
        #region Socket        
        public Socket? Socket { get; private set; }                         ///一个套接字
        public bool IsRunning { get; set; }
        EndPoint? endPoint { get; set; }
        #endregion

        #region byte[] Bytes        
        private byte[]? RecvBuffer { get; set; }                             ///接收缓冲区   
        private int BufferSize { get; set; } = 1024;
        private int RecvLength { get; set; }
        private List<byte> RecvBuffList { get; set; } = new List<byte>();    ///接收字节列表  
        private List<byte> SendBuffList { get; set; } = new List<byte>();    ///发送字节列表  
        private int iBytesHead { get; set; } = 8;
        private int surHL { get; set; } 
        private int surBL { get; set; } = 0;
        private bool isHead { get; set; } = true;
        private bool isBody { get; set; } = false;
        #endregion

        #region ReceiveMsg
        public void BeginReceiveMessage(object obj)
        {
            this.Socket = obj as Socket;
            if (this.Socket != null)
            {
                surHL = iBytesHead;
                RecvBuffer = new byte[BufferSize];
                IsRunning = true;
                endPoint = this.Socket.RemoteEndPoint;
                this.Socket.BeginReceive(RecvBuffer, 0, BufferSize, SocketFlags.None, new AsyncCallback(this.ReceiveCallback), this);
            }
            else
            {
                Console.WriteLine("{0} 发送端{1}连接关闭", XfsTimeHelper.CurrentTime(), "XfsPacketParser-44: Socket is null");
            }
        }
        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                if (this.Socket != null)
                {
                    RecvLength = this.Socket.EndReceive(ar);
                }
                else
                {
                    Console.WriteLine("{0} 发送端{1}连接关闭", XfsTimeHelper.CurrentTime(), "XfsPacketParser-44: Socket is null");
                }

                if (RecvLength == 0)
                {
                    ///发送端关闭
                    Console.WriteLine("{0} 发送端{1}连接关闭", XfsTimeHelper.CurrentTime(), endPoint);
                    IsRunning = false;
                    //Dispose();
                    return;
                }
                else
                {
                    ///将从Socket中收到的消息，搬到接收字节列表
                    this.AddRange(RecvBuffList, RecvBuffer, RecvLength);
                }
                ///触发事件 解析缓存池RecvBuffList<byte> 读取数据字节
                this.ParsingBytes();
                ///继续接收来自来客户端的数据  
                this.Socket.BeginReceive(RecvBuffer, 0, BufferSize, SocketFlags.None, new AsyncCallback(this.ReceiveCallback), this);
            }
            catch (Exception ex)
            {
                Console.WriteLine(XfsTimeHelper.CurrentTime() + " " + ex.ToString());
                IsRunning = false;
                //Dispose();
            }
        }
        private void ParsingBytes()
        {
            ///接收消息头（消息体的长度存储在消息头的0至4索引位置的字节里）
            byte[] HeadBytes = null;
            ///将本次要剪切的字节数置0
            int iBytesBody = 0;

            try
            {
                if (isHead)
                {
                    ///如果当前需要接收的字节数小于缓存池RecvBuffList，进行下一步操作
                    if (surHL <= RecvBuffList.Count)
                    {
                        //iBytesHead = surHL;
                        surHL = 0;
                    }
                    if (surHL == 0)
                    {
                        isHead = false;
                        isBody = true;
                        /////接收消息体（消息体的长度存储在消息头的0至4索引位置的字节里）
                        //byte[] HeadBytes = new byte[iBytesHead];
                        HeadBytes = new byte[iBytesHead];

                        ///将接收到的字节数的消息头保存到HeadBytes，//减去已经接收到的字节数
                        this.CutTo(RecvBuffList, HeadBytes, 0, iBytesHead);

                        ///拿出包头中后四个字节，此字节是包体长度
                        int msgLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(HeadBytes, 0));                     
                        surBL = msgLength;

                        ///拿出包头中前四个字节，此字节是操代码
                        int opcode = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(HeadBytes, 4));
                        Console.WriteLine(XfsTimeHelper.CurrentTime() + " Recv , Opcode : {0}", opcode);
                        /////一个消息包包头HeadBytes消息包 接收完毕，下面解析消息包包身 
                        //Console.WriteLine(XfsTimeHelper.CurrentTime() + " Recv , Opcode : {0} . BodyBytes.Length:{1}", opcode, msgLength);
                    }
                }
                if (isBody)
                {
                    ///如果当前需要接收的字节数大于0，则循环接收
                    if (surBL <= RecvBuffList.Count)
                    {
                        iBytesBody = surBL;
                        surBL = 0;                    ///归零进入下一步操作
                    }
                    if (surBL == 0)
                    {
                        isBody = false;
                        isHead = true;
                        surHL = 4;
                        ///一个消息包接收完毕，解析消息包
                        byte[] BodyBytes = new byte[iBytesBody];
                        this.CutTo(RecvBuffList, BodyBytes, 0, iBytesBody);

                        ///接受处理完整的字节数据包，包括包头和包身
                        this.OnReadRecv(this, HeadBytes, BodyBytes);

                        //HeadBytes = null;
                        
                        ///一个消息包包头HeadBytes消息包 接收完毕，下面解析消息包包身 
                        Console.WriteLine(XfsTimeHelper.CurrentTime() + " PacketParser-Recv {0} + {1} Bytes.", HeadBytes.Length, BodyBytes.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(XfsTimeHelper.CurrentTime() + ex.ToString());
                this.Dispose();
            }
        }

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
        void OnReadRecv(object obj, byte[] HeadBytes, byte[] BodyBytes)
        {
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
            catch (Exception e)
            {
                // 出现任何消息解析异常都要断开Session，防止客户端伪造消息				
                Console.WriteLine(XfsTimeHelper.CurrentTime() + e);
                if (this.Parent != null && (this.Parent as XfsSession) != null)
                {
                    (this.Parent as XfsSession).Dispose();
                }
                return;
            }

            this.readCallback.Invoke(this, message);
        }
        #endregion

        #region AddRange        
        void CutTo(List<byte> BuffList, byte[] bytes, int bytesoffset, int size)
        {
            BuffList.CopyTo(0, bytes, bytesoffset, size);
            BuffList.RemoveRange(0, size);
        }/// 提取数据        
        void AddRange(List<byte> BuffList, byte[] buffer, int length)
        {
            byte[] temByte = new byte[length];
            Array.Copy(buffer, 0, temByte, 0, length);
            BuffList.AddRange(temByte);
        }/// 队列数据
        #endregion

        #region Send  IXfsMessage  ///发送信息给客户端
        public void Send(IXfsMessage message, Socket socket)
        {
            this.Socket = socket;
            ///拿到本消息的操作码
            int opcode = XfsGame.XfsSence.GetComponent<XfsOpcodeTypeComponent>().GetOpcode(message.GetType());

            ///用Json将参数（message）,序列化转换成字符串（string）
            string msgJsons = XfsJsonHelper.ToJson(message);

            ///将字符串(string)转换成字节(byte)
            byte[] packetBody = Encoding.UTF8.GetBytes(msgJsons);

            int msgLength = 8 + packetBody.Length;
            byte[] packetBytes = new byte[msgLength];

            ///包体长度（不含包头的8个字节的长度），存在包头，占用4个字节(0,1,2,3).
            BitConverter.GetBytes(IPAddress.HostToNetworkOrder(packetBody.Length)).CopyTo(packetBytes, 0);

            ///信息类型，存在包头，占用4个字节(4,5,6,7).
            BitConverter.GetBytes(IPAddress.HostToNetworkOrder(opcode)).CopyTo(packetBytes, 4);

            ///包体存入消息包，占用位置（8...）,从8开始向后存到底
            packetBody.CopyTo(packetBytes, 8);
            
            this.SendBytes(packetBytes);            
        }    
        public void SendBytes(byte[] packetBytes)
        {
            if (null == Socket.Handle || !Socket.Connected)
            {
                Console.WriteLine(XfsTimeHelper.CurrentTime() + " 连接已中断!");
                (this.Parent as XfsSession).Dispose();
                this.Dispose();
                return;
            }
            ///要发送的信息长度
            int sendLength = packetBytes.Length;

            ///把要发送的信息，搬到发送字节列表
            AddRange(SendBuffList, packetBytes, packetBytes.Length);

            while (sendLength > 0)
            {
                try
                {
                    if (sendLength <= BufferSize)
                    {
                        byte[] temBytes = new byte[sendLength];
                        this.CutTo(SendBuffList, temBytes, 0, sendLength);
                        Socket.BeginSend(temBytes, 0, temBytes.Length, 0, new AsyncCallback(this.SendCallback), Socket);
                        sendLength = 0;
                    }
                    else
                    {
                        byte[] temBytes = new byte[BufferSize];
                        this.CutTo(SendBuffList, temBytes, 0, BufferSize);
                        sendLength -= BufferSize;
                        Socket.BeginSend(temBytes, 0, temBytes.Length, 0, new AsyncCallback(this.SendCallback), Socket);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(XfsTimeHelper.CurrentTime() + ex.ToString());
                    //Dispose();
                }
            }
        } 
        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket? client = ar.AsyncState as Socket;
                if (client != null)
                {
                    int bytesSent = client.EndSend(ar);
                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " PacketParser-Sent {0} Bytes. ThreadId:{1}", bytesSent, Thread.CurrentThread.ManagedThreadId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(XfsTimeHelper.CurrentTime() + ex.ToString());
            }
        }
        #endregion
 
        #region Dispose
        public override void Dispose()
        {
            base.Dispose();
            try
            {
                if (this.Socket == null) return;
                this.Socket.Shutdown(SocketShutdown.Both);
                this.IsRunning = false;
                this.Socket.Close();
                this.Socket = null;
                Console.WriteLine(XfsTimeHelper.CurrentTime() + " InstanceId:" + InstanceId + " XfsPacketParser释放资源...");
            }
            catch (Exception ex)
            {
                Console.WriteLine(XfsTimeHelper.CurrentTime() + " " + ex.Message);
            }
        }
        #endregion

    }
}