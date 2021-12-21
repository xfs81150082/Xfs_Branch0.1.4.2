using System;
using System.Collections.Generic;
using Xfs;
using System.Text;
using System.Threading.Tasks;

namespace XfsServer
{
    public class TestIocp
    {
        XfsNetSocketComponent server = null;
        public void Init()
        {
            //server = new XfsNetSocketComponent();
            //server.Init();
            //server.Start();
            Start();
            Console.ReadKey();
        }

        void Start()
        {

        }


    }
}
