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
        HALT = 'L',//初始化开始
        Ratio = 't',//设置数值(对于旧框)
        RatioDX = 'r',//设置数值(对于新框)
        Sens = 'k',//设置数值
        STAT = 'A',//初始化完成，可以发送触摸数据
    }
}
