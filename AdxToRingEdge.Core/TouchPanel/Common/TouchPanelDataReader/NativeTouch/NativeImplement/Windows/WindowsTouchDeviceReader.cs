using AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.Base;
using Linearstar.Windows.RawInput;

namespace AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.NativeImplement.Windows
{
    public class WindowsTouchDeviceReader : NativeTouchDeviceReader, IDisposable
    {
        private DummyApplication application;

        public WindowsTouchDeviceReader(ProgramArgumentOption opt) : base(opt)
        {

        }

        public override bool IsRunning => application?.IsRunning ?? false;

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

        private HashSet<int> regSet = new();

        private void OnTouchUpdated(IEnumerable<RawInputDigitizerContact> contacts)
        {
            foreach (var contact in contacts)
            {
                var id = contact.Identifier ?? 0;
                var x = contact.X;
                var y = contact.Y;
                var isTouched = contact.Kind != RawInputDigitizerContactKind.None;

                var arg = new TouchEventArg(id, x, y);
                var evt = OnTouchMove;

                if (!isTouched)
                {
                    regSet.Remove(id);
                    evt = OnTouchEnd;
                }
                else if (!regSet.Contains(id))
                {
                    evt = OnTouchBegin;
                    regSet.Add(id);
                }

                evt.Invoke(id, arg);
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
