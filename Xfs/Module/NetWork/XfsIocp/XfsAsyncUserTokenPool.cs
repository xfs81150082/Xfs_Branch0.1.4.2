using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xfs
{
    /// <summary>
    /// AsyncUserToken对象池（固定缓存设计）
    /// </summary>
    public class XfsAsyncUserTokenPool : XfsComponent
    {
        private  ConcurrentQueue<XfsAsyncUserToken> queue;
        private int _bufferSize = 1024;
        private int _maxToken = 10;

        public XfsAsyncUserTokenPool()
        {
            this.Init();
        }

        public XfsAsyncUserTokenPool(Int32 _maxToken)
        {
            this._maxToken = _maxToken;
            this.Init();
        }

        private void Init()
        {
            this.queue = new ConcurrentQueue<XfsAsyncUserToken>();
            for (int i = 0; i < this._maxToken; i++)
            {
                XfsAsyncUserToken userToken = new XfsAsyncUserToken(_bufferSize);
                this.Push(userToken);
            }
        }

        public XfsAsyncUserToken? Pop()
        {
            XfsAsyncUserToken? args;
            if (this.queue.TryDequeue(out args))
            {
                return args;
            }
            return null;
        }

        public void Push(XfsAsyncUserToken item)
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
