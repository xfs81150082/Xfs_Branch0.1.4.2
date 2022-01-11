using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xfs
{
    public class XfsHeartComponent : XfsComponent
    {
        public int CdCount { get; set; } = 0;

        public int MaxCdCount { get; set; } = 4;

        public bool Heartting { get; set; } = false;

        public bool IsPool { get; set; } = true;

        public long LastHeart { get; set; }
        public long NowHeart { get; set; }
        public long HeartTimer { get; set; } = 4;
        public void Init()
        {
            this.CdCount = 0;
            this.MaxCdCount = 4;
            this.Heartting = true;
            this.IsPool = false;
        }

        public void Close()
        {
            this.CdCount = 0;
            this.MaxCdCount = 4;
            this.Heartting = false;
            this.IsPool = true;
        }

    }
}
