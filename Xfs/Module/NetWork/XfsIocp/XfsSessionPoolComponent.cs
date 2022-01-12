using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xfs
{
    public class XfsSessionPoolComponent : XfsComponent
    {
        private ConcurrentQueue<XfsSession> queue;
        public int _maxSession = 10;

        public XfsSessionPoolComponent()
        {
            //this.Init(this._maxSession);
        }

        public XfsSessionPoolComponent(Int32 _maxSession)
        {
            this._maxSession = _maxSession;
            //this.Init(this._maxSession);
        }

        public void Init(Int32 _maxSession)
        {
            this._maxSession = _maxSession;

            this.queue = new ConcurrentQueue<XfsSession>();

            for (int i = 0; i < this._maxSession; i++)
            {
                XfsSession? session = XfsComponentFactory.CreateWithParent<XfsSession>(this);
                ///添加心跳包
                if (session.GetComponent<XfsHeartComponent>() == null)
                {
                    session.AddComponent<XfsHeartComponent>();
                }

                if (session.GetComponent<XfsAsyncUserToken>() == null)
                {
                    session.AddComponent<XfsAsyncUserToken>();
                }

                session.Close();

                this.Push(session);
            }
        }

        public XfsSession? Pop()
        {
            XfsSession? session;
            if (this.queue.TryDequeue(out session))
            {
                return session;
            }
            return null;
        }
        public XfsSession? Pop(XfsComponent parent)
        {
            XfsSession? session;
            if (this.queue.TryDequeue(out session))
            {
                session.Parent = parent;
                return session;
            }
            return null;
        }

        public void Push(XfsSession item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(" 52. 池子出错了...");
            }
            this.queue.Enqueue(item);
        }

        public int Count
        {
            get { return this.queue.Count; }
        }

        public bool IsEmty()
        {
            if (this.queue.Count == 0)
            {
                return false;
            }
            return true;
        }


    }
}
