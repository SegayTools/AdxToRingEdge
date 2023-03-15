using AdxToRingEdge.Core.TouchPanel.Base;

namespace AdxToRingEdge.Core.TouchPanel.Common.GameTouchPanelReciver
{
    /// <summary>
    /// 一般用于模拟一个可用于与指定游戏交互的触控设备，虽然可以发送触控数据给游戏，但本身不会产生触摸数据给游戏
    /// </summary>
    public interface IGameTouchPanelReciver
    {
        void Start();
        void Stop();

        void SendTouchData(ReadOnlyTouchStateCollectionBase touchStates);
    }
}
