using AdxToRingEdge.Core.TouchPanel.Base;
using AdxToRingEdge.Core.Utils;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.MaiMai
{
    internal class DxTouchPanelDataReader : CommonMaiMaiTouchPanelDataReader
    {
        private BinaryToTouchAreaMap binConverter = new BinaryToTouchAreaMap(DefaultTouchMapImpl.DxTouchMap);

        public DxTouchPanelDataReader(ProgramArgumentOption option) : base(option,9)
        {

        }

        protected override TouchStateCollectionBase CreateTouchStates()
            => new DxTouchStateCollection();

        protected override bool TryParseBufferToTouchData(in TouchStateCollectionBase state, byte[] buffer)
        {
            foreach (var item in binConverter.Map(buffer))
                state.TrySetTouchState(item.Key, item.Value);

            return true;
        }
    }
}
