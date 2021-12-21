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
            //this.TestSessionSend(self);

            this.Test2SessionSend(self);



        }

        int time = 0;
        int restime = 4000;
        void TestSessionSend(XfsTest self)
        {
            time += 1;
            if (time > restime)
            {
                time = 0;

                XfsSession session;

                Dictionary<long, XfsSession> sessions = XfsGame.XfsSence.GetComponent<XfsNetSocketComponent>().Sessions;

                if (sessions.Count > 0)
                {
                    session = sessions.Values.ToList()[0];

                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " 46. XfsServerTestSystem: " + session.GetComponent<XfsAsyncUserToken>().Socket.LocalEndPoint);

                    //session.GetComponent<XfsAsyncUserToken>().Send(self.call);

                    session.UserToken.Send(self.call);

                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " 46. XfsServerTestSystem: " + self.call);
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

                    //session.GetComponent<XfsAsyncUserToken>().Send(self.call);

                    //session.UserToken.Send(self.call);

                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " 46. XfsServerTestSystem: " + self.call);


                    C4S_Ping resqustC = new C4S_Ping();
                    resqustC.Opcode = XfsGame.XfsSence.GetComponent<XfsOpcodeTypeComponent>().GetOpcode(resqustC.GetType());
                    resqustC.Message = self.call;

                    if (session.RemoteAddress != null)
                    {
                        resqustC.Message = self.call + " " + session.RemoteAddress.ToString();
                    }

                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " : " + this.GetType().Name + "-42: 开始打电话给服务器..." + session.RemoteAddress);

                    S4C_Ping? responseC = await session.Call(resqustC) as S4C_Ping;

                    ///从服务端发回来的信息
                    if (responseC != null)
                    {
                        Console.WriteLine(XfsTimeHelper.CurrentTime() + " : " + this.GetType().Name + "-43" + ": 收到服户端回复的信息。RpcId:" + "(" + responseC.RpcId + ":" + responseC.Message + ")");
                    }
                    else
                    {
                        Console.WriteLine(XfsTimeHelper.CurrentTime() + " : " + this.GetType().Name + "-48" + ": responseC is null");
                    }





                }


            }
        }

        async void TestCall3(XfsTest self)
        {
            self.time += 1;
            if (self.time > self.restime)
            {
                self.time = 0;

                XfsNetOuterComponent client = XfsGame.XfsSence.GetComponent<XfsNetOuterComponent>();

                if (client != null && client.Sessions.Count > 0)
                {
                    XfsSession session = client.Sessions.Values.ToList()[0];

                    C4S_Ping resqustC = new C4S_Ping();
                    resqustC.Opcode = XfsGame.XfsSence.GetComponent<XfsOpcodeTypeComponent>().GetOpcode(resqustC.GetType());
                    resqustC.Message = self.call;
                    if (session.RemoteAddress != null)
                    {
                        resqustC.Message = self.call + " " + session.RemoteAddress.ToString();
                    }
                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " : " + this.GetType().Name + "-42: 开始打电话给服务器..." + session.RemoteAddress);

                    S4C_Ping? responseC = await session.Call(resqustC) as S4C_Ping;

                    ///从服务端发回来的信息
                    if (responseC != null)
                    {
                        Console.WriteLine(XfsTimeHelper.CurrentTime() + " : " + this.GetType().Name + "-43" + ": 收到服户端回复的信息。RpcId:" + "(" + responseC.RpcId + ":" + responseC.Message + ")");
                    }
                    else
                    {
                        Console.WriteLine(XfsTimeHelper.CurrentTime() + " : " + this.GetType().Name + "-48" + ": responseC is null");
                    }
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
