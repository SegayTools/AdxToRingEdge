using AdxToRingEdge.Core.TouchPanel.Base;
using AdxToRingEdge.Core.TouchPanel.Base.TouchStateCollection;
using AdxToRingEdge.Core.TouchPanel.Common.GameTouchPanelReciver;
using AdxToRingEdge.Core.TouchPanel.Common.GameTouchPanelReciver.MaiMai;
using AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader;
using AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.MaiMai;
using Microsoft.Extensions.Logging.Abstractions;
using UnitsNet;

using LogEntity = AdxToRingEdge.Core.Log<AdxToRingEdge.Core.TouchPanel.TouchPanelService>;

namespace AdxToRingEdge.Core.TouchPanel
{
    internal class TouchPanelService : IService
    {
        private readonly ProgramArgumentOption option;
        private readonly ITouchPanelDataReader reader;
        private readonly IGameTouchPanelReciver sender;
        private Dictionary<char, TouchArea[]> map;
        private TouchStateCollectionBase generic = new GeneralTouchStateCollection();

        public TouchPanelService(ProgramArgumentOption option, ITouchPanelDataReader reader, IGameTouchPanelReciver sender)
        {
            this.option = option;
            this.reader = reader;
            this.sender = sender;

            reader.OnTouchDataReceived += OnTouchDataReceived;
            map = Enum.GetValues<TouchArea>().GroupBy(x => x.ToString()[0]).ToDictionary(x => x.Key, x => x.OrderBy(y => y).ToArray());
        }

        private void OnTouchDataReceived(TouchStateCollectionBase touchData)
        {
            OnPostProcessTouchData(touchData);
            sender.SendTouchData(touchData);
        }

        private void OnPostProcessTouchData(TouchStateCollectionBase touchData)
        {
            if (!option.EnableTouchDataPostProcess)
                return;

            generic.ResetAllTouchStates();

            int op(char ch, TouchArea rt, int op)
            {
                var arr = map[ch];
                var i = rt.ToString()[1] - '0' - 1;

                var ri = i + op;
                if (ri < 0)
                    ri = arr.Length + ri;
                else if (ri >= arr.Length)
                    ri = ri - arr.Length;

                return touchData.GetTouchState(arr[ri]) ? 1 : 0;
            }

            int prev(char ch, TouchArea rt) => op(ch, rt, -1);
            int next(char ch, TouchArea rt) => op(ch, rt, +1);
            int same(char ch, TouchArea rt) => op(ch, rt, 0);

            for (var i = TouchArea.E1; i <= TouchArea.E8; i++)
            {
                if (touchData.GetTouchState(i))
                    continue;

                var j1 = prev('A', i) + same('A', i) + same('D', i);
                var j2 = same('B', i) + prev('B', i);

                if (j1 + j2 >= 4)
                    generic.TrySetTouchState(i, true);
            }

            for (var i = TouchArea.D1; i <= TouchArea.D8; i++)
            {
                if (touchData.GetTouchState(i))
                    continue;

                var j2 = prev('A', i) + same('A', i) + same('E', i);

                if (j2 == 3)
                    generic.TrySetTouchState(i, true);
            }

            for (var i = TouchArea.B1; i <= TouchArea.B8; i++)
            {
                if (touchData.GetTouchState(i))
                    continue;

                var j2 = same('A', i) + same('E', i) + next('E', i) + next('B', i) + prev('B', i);
                var j3 = touchData.GetTouchState(i > TouchArea.B4 ? TouchArea.C1 : TouchArea.C2) ? 1 : 0;

                if (j2 + j3 >= 4)
                    generic.TrySetTouchState(i, true);
            }

            //all done.
            touchData.CombineFrom(generic);
        }

        public void Dispose()
        {
            Stop();
        }

        public void PrintStatus()
        {
            LogEntity.User($"--Print Sender {sender?.GetType().Name} Status--");
            sender?.PrintStatus();
            LogEntity.User($"----------------");

            LogEntity.User($"--Print Reader {reader?.GetType().Name} Status--");
            reader?.PrintStatus();
            LogEntity.User($"----------------");
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
