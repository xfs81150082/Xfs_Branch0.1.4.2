using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xfs
{
    [XfsObjectSystem]
    class XfsHeartComponentAwakeSystem : XfsStartSystem<XfsHeartComponent>
    {
        public override void Start(XfsHeartComponent self)
        { 
            self.CdCount = 0; ;
            self.MaxCdCount = 4; ;
            self.Counting = true; ;
        }
    }
    [XfsObjectSystem]
    class XfsHeartComponentUpdateSystem : XfsUpdateSystem<XfsHeartComponent>
    {
        public override void Update(XfsHeartComponent self)
        {
            //CheckSession(self);
        }
        
        int ti = 0;
        int timer = 4000;

        void CheckSession(XfsHeartComponent self)
        {
            //XfsGame.XfsSence.GetComponent<XfsTimerComponent>().WaitAsync(4000);
            
            ti += 1;
            if (ti < timer) return;
            ti = 0;
            self.CdCount += 1;
            if (self.CdCount > self.MaxCdCount)
            {              
                if (!(self.Parent as XfsSession).IsListen)
                {
                    if ((self.Parent as XfsSession).Network != null)
                    {
                        (self.Parent as XfsSession).Network.IsRunning = false;
                    }
                }
                self.Parent.Dispose();
            }
            else
            {
                //发送心跳检测（并等待签到，签到入口在TmTcpSession里，双向发向即：客户端向服务端发送，服务端向客户端发送）
                if ((self.Parent as XfsSession) != null)
                {
                    (self.Parent as XfsSession).Send(new C4S_Heart());
                    //Console.WriteLine(XfsTimeHelper.CurrentTime() + " SenceType: " + (self.Parent as XfsSession).SenceType + " CdCount:{0}-{1} ", self.CdCount, self.MaxCdCount);
                    Console.WriteLine(XfsTimeHelper.CurrentTime() + " " + this.GetType().Name + " 56: 141.");
                }

            }
        }

    }
}