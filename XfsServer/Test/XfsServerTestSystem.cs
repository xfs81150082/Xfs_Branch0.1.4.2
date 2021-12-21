using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xfs;

namespace XfsServer
{
    [XfsObjectSystem]
     public class XfsServerTestSystem : XfsUpdateSystem<XfsServerTest>
    {
        public override void Update(XfsServerTest self)
        {
            //TestSessionSend(self);


        }

        int time = 0;
        int restime = 4000;
        void TestSessionSend(XfsServerTest self)
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

        async void TestCall2(XfsServerTest self)
        {
            time += 1;
            if (time > restime)
            {
                time = 0;
                XfsOpcodeTypeComponent xfsOpcode = XfsGame.XfsSence.GetComponent<XfsOpcodeTypeComponent>();
                XfsMessageDispatcherComponent xfsMessage = XfsGame.XfsSence.GetComponent<XfsMessageDispatcherComponent>();

                Console.WriteLine(XfsTimeHelper.CurrentTime() + " XfsTestSystem-38: " + xfsMessage.Handlers.Count);
                Console.WriteLine(XfsTimeHelper.CurrentTime() + " XfsTestSystem-38: " + xfsMessage.Handlers.Values);

                Console.WriteLine(XfsTimeHelper.CurrentTime() + " XfsTestSystem-38: " + xfsOpcode.MessagesCount());
                Console.WriteLine(XfsTimeHelper.CurrentTime() + " XfsTestSystem-38: " + xfsOpcode.Keys());
                Console.WriteLine(XfsTimeHelper.CurrentTime() + " XfsTestSystem-38: " + xfsOpcode.Messages());






                Console.WriteLine(XfsTimeHelper.CurrentTime() + " XfsTestSystem-38: ");

                await new XfsTask();
            }
        }




    }


    public class XfsServerTest : XfsEntity
    {
        public string longin = "2020-10-18";
        public string? par { get; set; }

        public bool IsUserLogin = false;

        public float time = 0;

        public float restime = 3000;

        public string call = "发送测试回调请求-20201106";

    }

}
