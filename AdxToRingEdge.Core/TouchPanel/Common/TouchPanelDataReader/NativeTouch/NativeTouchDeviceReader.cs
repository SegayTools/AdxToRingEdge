using AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.Base;
using SixLabors.ImageSharp.Advanced;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch
{
    public abstract class NativeTouchDeviceReader
    {
        protected ProgramArgumentOption option;

        public delegate void OnTouchCallbackFunc(int id, TouchEventArg arg);

        public abstract event OnTouchCallbackFunc OnTouchBegin;
        public abstract event OnTouchCallbackFunc OnTouchMove;
        public abstract event OnTouchCallbackFunc OnTouchEnd;

        public abstract bool IsRunning { get; }

        public NativeTouchDeviceReader(ProgramArgumentOption opt)
        {
            option = opt;
        }

        public abstract void Start();
        public abstract void Stop();
        public abstract void PrintStatus();
    }
}
