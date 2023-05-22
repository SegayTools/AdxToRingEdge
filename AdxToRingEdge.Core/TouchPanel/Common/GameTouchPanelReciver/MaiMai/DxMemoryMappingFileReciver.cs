using AdxToRingEdge.Core.TouchPanel.Base.TouchStateCollection;
using AdxToRingEdge.Core.Utils;
using System;
using System.IO.MemoryMappedFiles;

using LogEntity = AdxToRingEdge.Core.Log<AdxToRingEdge.Core.TouchPanel.Common.GameTouchPanelReciver.MaiMai.DxMemoryMappingFileReciver>;

namespace AdxToRingEdge.Core.TouchPanel.Common.GameTouchPanelReciver.MaiMai
{
    public class DxMemoryMappingFileReciver : IGameTouchPanelReciver
    {
        private readonly ProgramArgumentOption option;
        private MemoryMappedFile mmf;

        public DxMemoryMappingFileReciver(ProgramArgumentOption option)
        {
            this.option = option;
        }

        public void PrintStatus()
        {
            LogEntity.User($"Status : {(mmf is not null ? "Running" : "Stopped")}.");
        }

        public void SendTouchData(TouchStateCollectionBase touchStates)
        {
            if (mmf == null)
                return;

            var state = 0UL;

            foreach (var pair in touchStates)
                if (pair.Value)
                    state |= 1UL << ((int)pair.Key);

            using var accessor = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Write);
            accessor.Write(0, state);
        }

        public void Start()
        {
            mmf?.Dispose();
            mmf = MemoryMappedFile.CreateOrOpen(option.OutMemoryMappingFileName, 1024);
        }

        public void Stop()
        {
            mmf?.Dispose();
            mmf = null;
        }
    }
}
