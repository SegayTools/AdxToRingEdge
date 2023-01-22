using AdxToRingEdge.Core.Keyboard.Base;
using AdxToRingEdge.Core.TouchPanel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LogEntity = AdxToRingEdge.Core.Log<AdxToRingEdge.Core.Keyboard.KeyboardService>;

namespace AdxToRingEdge.Core.Keyboard
{
    public class KeyboardService : IService
    {
        private readonly CommandArgOption option;
        private readonly ButtonController buttonController;
        private CancellationTokenSource cancelSource;

        public KeyboardService()
        {
            option = CommandArgOption.Instance;
            buttonController = new ButtonController(option);
        }

        public void Start()
        {
            if (cancelSource is not null)
            {
                LogEntity.Error("Please call Stop() before Start().");
                return;
            }

            cancelSource = new CancellationTokenSource();
            Task.Run(() => OnKeyboardInputRead(cancelSource.Token), cancelSource.Token);
        }

        private unsafe void OnKeyboardInputRead(CancellationToken cancellationToken)
        {
            LogEntity.Debug($"OnKeyboardInputRead() begin.");

            var file = new FileInfo(option.AdxKeyboardByIdPath);
            using var fs = file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            cancellationToken.Register(() => fs?.Dispose());

            LogEntity.Debug($"fs.CanRead = {fs.CanRead}");

            var buffer = new byte[24];

            while (!cancellationToken.IsCancellationRequested)
            {
                if (!fs.CanRead)
                    break;

                fs.ReadAtLeast(buffer, buffer.Length, false);

                var type = BitConverter.ToUInt16(buffer, 16);
                var code = BitConverter.ToUInt16(buffer, 18);
                var value = BitConverter.ToInt32(buffer, 20);

                //LogEntity.Debug($"OnKeyboardInputRead() read buffer : {BitConverter.ToString(buffer)}");

                ProcessRawEventData(type, code, value);
            }

            LogEntity.Debug($"OnKeyboardInputRead() exit.");
        }

        private void ProcessRawEventData(ushort type, ushort code, int value)
        {
            if (type != 1 /*EV_KEY*/)
                return;

            var keyState = (KeyState)value;
            var keyCode = (KeyCode)code;

            //Log.Debug("ProcessRawEventData", $"{keyCode} {keyState}");
            buttonController.SetButtonState(keyCode, keyState);
        }

        public void Stop()
        {
            if (cancelSource is null)
                return;

            cancelSource.Cancel();
            cancelSource = default;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
