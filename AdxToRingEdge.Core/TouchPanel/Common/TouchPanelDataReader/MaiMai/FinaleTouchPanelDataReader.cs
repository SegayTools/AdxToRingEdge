using AdxToRingEdge.Core.TouchPanel.Base;
using AdxToRingEdge.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.MaiMai
{
    internal class FinaleTouchPanelDataReader : CommonMaiMaiTouchPanelDataReader
    {
        private BinaryToTouchAreaMap binConverter = new BinaryToTouchAreaMap(DefaultTouchMapImpl.FinaleTouchMap);

        public FinaleTouchPanelDataReader(ProgramArgumentOption option) : base(option,14)
        {

        }

        protected override TouchStateCollectionBase CreateTouchStates()
            => new FinaleTouchStateCollection();

        protected override bool TryParseBufferToTouchData(in TouchStateCollectionBase state, byte[] buffer)
        {
            foreach (var item in binConverter.Map(buffer))
                state.TrySetTouchState(item.Key, item.Value);

            return true;
        }
    }
}
