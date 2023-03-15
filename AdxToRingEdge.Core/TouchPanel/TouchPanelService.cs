using AdxToRingEdge.Core.TouchPanel.Base;
using AdxToRingEdge.Core.TouchPanel.Common.GameTouchPanelReciver;
using AdxToRingEdge.Core.TouchPanel.Common.GameTouchPanelReciver.MaiMai;
using AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader;
using AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.MaiMai;

namespace AdxToRingEdge.Core.TouchPanel
{
    internal class TouchPanelService : IService
    {
        private readonly ITouchPanelDataReader reader;
        private readonly IGameTouchPanelReciver sender;

        public TouchPanelService(ITouchPanelDataReader reader, IGameTouchPanelReciver sender)
        {
            this.reader = reader;
            this.sender = sender;

            reader.OnTouchDataReceived += OnTouchDataReceived;
        }

        private void OnTouchDataReceived(ReadOnlyTouchStateCollectionBase touchData)
        {
            sender.SendTouchData(touchData);
        }

        public void Dispose()
        {
            Stop();
        }

        public void PrintStatus()
        {

        }

        public void Start()
        {
            reader.Start();
            sender.Start();
        }

        public void Stop()
        {
            reader.Stop();
            sender.Stop();
        }

        public bool TryProcessUserInput(string[] args)
        {
            return false;
        }
    }
}
