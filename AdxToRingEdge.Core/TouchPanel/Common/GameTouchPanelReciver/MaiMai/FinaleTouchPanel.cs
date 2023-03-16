using AdxToRingEdge.Core.TouchPanel.Base.TouchStateCollection;
using AdxToRingEdge.Core.Utils;

namespace AdxToRingEdge.Core.TouchPanel.Common.GameTouchPanelReciver.MaiMai
{
    public class FinaleTouchPanel : CommonMaiMaiTouchPanelBase
    {
        public FinaleTouchPanel(ProgramArgumentOption option) : base(option)
        {

        }

        protected override SerialStreamWrapper CreateSerial() =>
            SerialHelper.SetupSerial(option.OutTouchPanelCOM, option.OutTouchPanelBaudRate, option.OutTouchPanelParity, option.OutTouchPanelDataBits, option.OutTouchPanelStopBits);

        protected override TouchStateCollectionBase CreateTouchStates()
            => new  FinaleTouchStateCollection();
    }
}
