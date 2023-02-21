using AdxToRingEdge.Core.TouchPanel.NativeTouchPanel.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace AdxToRingEdge.Core.TouchPanel.NativeTouchPanel.NativeImplement.Windows
{
    public class WindowsTouchDeviceReader : NativeTouchDeviceReader, IDisposable
    {
        private DummyApplication application;

        public WindowsTouchDeviceReader(ProgramArgumentOption opt) : base(opt)
        {

        }

        public override bool IsRunning => application is not null;

        public override event OnTouchCallbackFunc OnTouchBegin;
        public override event OnTouchCallbackFunc OnTouchMove;
        public override event OnTouchCallbackFunc OnTouchEnd;

        public void Dispose()
        {
            Stop();
        }

        public override void PrintStatus()
        {

        }

        private HashSet<int> keepSet = new();
        private Dictionary<int, TouchEventArg> touchRegisterSet = new();
        private HashSet<int> currentSet = new();

        private void OnTouchUpdated(IEnumerable<TouchEventArg> touches)
        {
            keepSet.Clear();
            foreach (var touch in touches)
            {
                var callback = currentSet.Add(touch.Id) ? OnTouchBegin : OnTouchMove;
                touchRegisterSet[touch.Id] = touch;
                keepSet.Add(touch.Id);

                callback?.Invoke(touch.Id, touch);
            }

            foreach (var removeId in currentSet.Except(keepSet).ToArray())
            {
                var touch = touchRegisterSet[removeId];
                touchRegisterSet.Remove(removeId);
                currentSet.Remove(removeId);

                OnTouchEnd?.Invoke(touch.Id, touch);
            }
        }

        public override void Start()
        {
            application = new DummyApplication();
            application.OnTouchUpdated += OnTouchUpdated;
            application.Start();
        }

        public override void Stop()
        {
            application.Stop();
            application = default;
        }
    }
}
