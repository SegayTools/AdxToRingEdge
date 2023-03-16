namespace AdxToRingEdge.Core.TouchPanel.Base.TouchStateCollection
{
    public class FinaleTouchStateCollection : CommonMaimaiTouchStateCollection
    {
        public FinaleTouchStateCollection() : base(DefaultTouchMapImpl.FinaleTouchMap, 14, 0x40)
        {

        }

        public override bool TrySetTouchState(TouchArea touch, bool isTouched)
        {
            if (touch == TouchArea.C2)
            {
                var another = TouchArea.C1;
                var anotherState = GetTouchState(another);

                if (anotherState)
                    return true;

                return base.TrySetTouchState(touch, isTouched);
                /*
                var prevState = GetTouchState(TouchArea.C);
                var curState = isTouched || anotherState;
                if (prevState != curState)
                    Log<FinaleTouchStateCollection>.Debug($"Touch C: {prevState} -> {curState}");

                return base.TrySetTouchState(TouchArea.C, curState);
                */
            }

            return base.TrySetTouchState(touch, isTouched);
        }
    }
}
