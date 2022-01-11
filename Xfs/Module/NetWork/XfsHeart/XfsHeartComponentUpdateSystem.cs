using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Xfs
{
    [XfsObjectSystem]
    class XfsHeartComponentAwakeSystem : XfsStartSystem<XfsHeartComponent>
    {
        public override void Start(XfsHeartComponent self)
        {
            //self.CdCount = 0;
            //self.MaxCdCount = 4;
            //self.Heartting = false;
            //self.IsPool = true;
        }
    }

    [XfsObjectSystem]
    class XfsHeartComponentUpdateSystem : XfsUpdateSystem<XfsHeartComponent>
    {
        public override void Update(XfsHeartComponent self)
        {
            Heartting(self);

            Check(self);

        }

        int heartTime = 0;
        int restime = 4000;
        void Heartting(XfsHeartComponent self)
        {
            heartTime += 1;
            if (heartTime > restime)
            {
                heartTime = 0;
                if (self.IsPool) return;

                XfsSession? session = self.Parent as XfsSession;
                if (session == null || !session.IsRunning) return;

                //Thread.Sleep(4000);
                //XfsGame.XfsSence.GetComponent<XfsTimerComponent>().WaitAsync(4000000);

                C4S_Heart resqustC = new C4S_Heart();
                resqustC.Opcode = XfsGame.XfsSence.GetComponent<XfsOpcodeTypeComponent>().GetOpcode(resqustC.GetType());
                resqustC.Message = XfsTimeHelper.Now().ToString();

                session.Send(resqustC);

                Console.WriteLine(XfsTimeHelper.CurrentTime() + " 57. " + this.GetType().Name + " 心跳包已发出: " + session.InstanceId);

            }
        }

        int checkTime = 0;
        void Check(XfsHeartComponent self)
        {
            checkTime += 1;
            if (checkTime > restime)
            {
                checkTime = 0;
                if (self.IsPool) return;
                if (!self.Heartting) return;

                XfsSession? session = self.Parent as XfsSession;
                if (session == null) return;

                //Thread.Sleep(4000);
                //XfsGame.XfsSence.GetComponent<XfsTimerComponent>().WaitAsync(4000000);

                session.GetComponent<XfsHeartComponent>().CdCount += 1;
                if (session.GetComponent<XfsHeartComponent>().CdCount > session.GetComponent<XfsHeartComponent>().MaxCdCount)
                {
                    session.Close();
                }
            }
        }

    }
}
