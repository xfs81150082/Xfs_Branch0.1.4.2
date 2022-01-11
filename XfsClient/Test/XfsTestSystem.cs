using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xfs;

namespace XfsClient
{
    [XfsObjectSystem]
    public class XfsTestSystem : XfsUpdateSystem<XfsTest>
    {
        public override void Update(XfsTest self)
        {
            //this.Test1SessionSend(self);

            //this.Test2SessionSend(self);

            //this.Test3SessionSend(self);

            //Test0SessionSend(self);
        }

        int time = 0;
        int restime = 4000;       
        async void Test3SessionSend(XfsTest self)
        {
            time += 1;
            if (time > restime)
            {
                time = 0;


                Dictionary<int, List<IXfsMHandler>> handlers = XfsGame.XfsSence.GetComponent<XfsMessageDispatcherComponent>().Handlers;


                if (handlers.Count > 0)
                {

                    foreach (var tem1 in handlers.Values)
                    {
                        foreach (var tem2 in tem1)
                        {


                            Console.WriteLine(XfsTimeHelper.CurrentTime() + " 45. XfsServerTestSystem handlers: " + handlers.Count);
                            Console.WriteLine(XfsTimeHelper.CurrentTime() + " 46. XfsServerTestSystem handlers: " + tem1.Count);
                            Console.WriteLine(XfsTimeHelper.CurrentTime() + " 47. XfsServerTestSystem handlers: " + tem2.GetType().Name);
                        }
                    }

                    new XfsTask().GetResult();

                }


            }
        }
        async void Test2SessionSend(XfsTest self)
        {
            time += 1;
            if (time > restime)
            {
                time = 0;

                XfsSession session;

                Dictionary<long, XfsSession> sessions = XfsGame.XfsSence.GetComponent<XfsNetOuterComponent>().Sessions;

                if (sessions.Count > 0)
                {
                    session = sessions.Values.ToList()[0];

                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " 46. XfsServerTestSystem: " + session.GetComponent<XfsAsyncUserToken>().Socket.LocalEndPoint);
                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " 46. XfsServerTestSystem: " + self.call);


                    C4S_Ping resqustC = new C4S_Ping();
                    resqustC.Opcode = XfsGame.XfsSence.GetComponent<XfsOpcodeTypeComponent>().GetOpcode(resqustC.GetType());
                    resqustC.Message = self.call;

                    if (session.RemoteAddress != null)
                    {
                        resqustC.Message = self.call + " " + session.RemoteAddress.ToString();
                    }

                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " " + this.GetType().Name + " 52. 我是客户端，开始打电话给服务器." + session.RemoteAddress);

                    S4C_Ping? responseC = await session.Call(resqustC) as S4C_Ping;

                    ///从服务端发回来的信息
                    if (responseC != null)
                    {
                        Console.WriteLine(XfsTimeHelper.CurrentTime() + " " + this.GetType().Name + " 59. 我是客户端，收到服户端回复的信息: " + responseC.Message);
                    }
                    else
                    {
                        Console.WriteLine(XfsTimeHelper.CurrentTime() + " " + this.GetType().Name + " 63. responseC is null");
                    }
                }

            }
        }
        async void Test1SessionSend(XfsTest self)
        {
            time += 1;
            if (time > restime)
            {
                time = 0;

                XfsSession session;

                Dictionary<long, XfsSession> sessions = XfsGame.XfsSence.GetComponent<XfsNetOuterComponent>().Sessions;

                if (sessions.Count > 0)
                {
                    session = sessions.Values.ToList()[0];

                    //Console.WriteLine(XfsTimeHelper.CurrentTime() + " 46. XfsServerTestSystem: " + session.GetComponent<XfsAsyncUserToken>().Socket.LocalEndPoint);
                    //Console.WriteLine(XfsTimeHelper.CurrentTime() + " 46. XfsServerTestSystem: " + self.call);


                    C4S_Heart resqustC = new C4S_Heart();
                    resqustC.Opcode = XfsGame.XfsSence.GetComponent<XfsOpcodeTypeComponent>().GetOpcode(resqustC.GetType());
                    resqustC.Message = self.call;

                    if (session.RemoteAddress != null)
                    {
                        resqustC.Message = self.call + " " + session.RemoteAddress.ToString();
                    }

                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " " + this.GetType().Name + " 132. 开始打电话给服务器." + session.RemoteAddress);
                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " " + this.GetType().Name + " 133. 消息体长度." + resqustC.ToString().Length);

                    S4C_Ping? responseC = await session.Call(resqustC) as S4C_Ping;

                    ///从服务端发回来的信息
                    if (responseC != null)
                    {
                        Console.WriteLine(XfsTimeHelper.CurrentTime() + " " + this.GetType().Name + " 59. 我是客户端，收到服户端回复的信息: " + responseC.Message);
                    }
                    else
                    {
                        Console.WriteLine(XfsTimeHelper.CurrentTime() + " " + this.GetType().Name + " 63. responseC is null");
                    }





                }


            }
        }
        void Test0SessionSend(XfsTest self)
        {
            time += 1;
            if (time > restime)
            {
                time = 0;

                XfsSession session;

                Dictionary<long, XfsSession> sessions = XfsGame.XfsSence.GetComponent<XfsNetOuterComponent>().Sessions;

                if (sessions.Count > 0)
                {
                    session = sessions.Values.ToList()[0];

                    //Console.WriteLine(XfsTimeHelper.CurrentTime() + " 46. XfsServerTestSystem: " + session.GetComponent<XfsAsyncUserToken>().Socket.LocalEndPoint);
                    //Console.WriteLine(XfsTimeHelper.CurrentTime() + " 46. XfsServerTestSystem: " + self.call);


                    C4S_Heart resqustC = new C4S_Heart();
                    resqustC.Opcode = XfsGame.XfsSence.GetComponent<XfsOpcodeTypeComponent>().GetOpcode(resqustC.GetType());
                    resqustC.Message = XfsTimeHelper.Now().ToString();
                                       
                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " " + this.GetType().Name + " 132. 开始打电话给服务器." + session.RemoteAddress);
                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " " + this.GetType().Name + " 133. 消息体长度." + resqustC.ToString().Length);

                    session.Send(resqustC);

                    


                }


            }
        }


    }

    public class XfsTest : XfsEntity
    {
        public float time = 0;

        public float restime = 4000;

        public string call = "起点是客户端... ";
    }

}
