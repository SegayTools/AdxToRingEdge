using AdxToRingEdge.Core.Keyboard.Base;
using AdxToRingEdge.Core.Utils;

using LogEntity = AdxToRingEdge.Core.Log<AdxToRingEdge.Core.Keyboard.KeyboardService>;

namespace AdxToRingEdge.Core.Keyboard
{
    public class KeyboardService : IService
    {
        private readonly ProgramArgumentOption option;
        private readonly ButtonController buttonController;
        private CancellationTokenSource cancelSource;
        private FileStream currentInputStream;

        public KeyboardService(ProgramArgumentOption option)
        {
            this.option = option;
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

        private async void OnKeyboardInputRead(CancellationToken token)
        {
            LogEntity.Debug($"OnKeyboardInputRead() begin.");

            var file = new FileInfo(option.AdxKeyboardByIdPath);
            using var fs = file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            token.Register(() => fs?.Dispose());
            currentInputStream = fs;

            LogEntity.Debug($"fs.CanRead = {fs.CanRead}");

            var buffer = new byte[24];
            var readBuffer = new byte[36];
            var fillIdx = 0;

            while (!token.IsCancellationRequested)
            {
                if (!fs.CanRead)
                    continue;

                var read = await fs.ReadAsync(readBuffer, 0, readBuffer.Length, token);
                if (token.IsCancellationRequested)
                    continue;

                for (int i = 0; i < read; i++)
                {
                    buffer[fillIdx++] = readBuffer[i];
                    fillIdx = fillIdx % buffer.Length;

                    //mean that buffer is full.
                    if (fillIdx == 0)
                        ProcessRawEventData(buffer);
                }
            }

            currentInputStream = default;
            LogEntity.Debug($"OnKeyboardInputRead() exit.");
        }

        private void ProcessRawEventData(byte[] buffer)
        {
            var type = BitConverter.ToUInt16(buffer, 16);
            var code = BitConverter.ToUInt16(buffer, 18);
            var value = BitConverter.ToInt32(buffer, 20);

            //LogEntity.Debug($"OnKeyboardInputRead() read buffer : {BitConverter.ToString(buffer)}");

            ProcessRawEventData(type, code, value);
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
            currentInputStream = default;
        }

        public void Dispose()
        {
            Stop();
        }

        public void PrintStatus()
        {
            LogEntity.User($"IsRunning: {cancelSource != null}");
            if (buttonController is not null)
            {
                LogEntity.User($"Print button state:");
                foreach (var button in buttonController.ButtonMap.Keys)
                    LogEntity.User($"\t* button:{button}\tisPressed:{buttonController.GetButtonState(button)}");
            }
            else
                LogEntity.User($"NO button controller!");

            if (currentInputStream is not null)
            {
                LogEntity.User($"currentInputStream.CanRead: {currentInputStream.CanRead}");
                try
                {
                    LogEntity.User($"currentInputStream Posisiion/Length: {currentInputStream.Position}/{currentInputStream.Length} ({currentInputStream.Length - currentInputStream.Position} remain.)");
                }
                catch
                {

                }
            }
            else
                LogEntity.User($"NO input event stream!");
        }

        public bool TryProcessUserInput(string[] args)
        {
            return false;
        }
    }
}
