using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdxToRingEdge.Core.TouchPanel.Base
{
    public enum TouchSensorStat
    {
        RSET = 0x45,
        HALT = 0x4C,//初始化开始
        Ratio = 0x74,//设置数值
        Sens = 0x6B,//设置数值
        STAT = 0x41,//初始化完成，可以发送触摸数据
    }
}
