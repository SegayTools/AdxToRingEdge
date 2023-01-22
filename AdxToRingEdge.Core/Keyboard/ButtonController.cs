using AdxToRingEdge.Core.Keyboard.Base;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LogEntity = AdxToRingEdge.Core.Log<AdxToRingEdge.Core.Keyboard.ButtonController>;

namespace AdxToRingEdge.Core.Keyboard
{
    public class ButtonController : IDisposable
    {
        public Dictionary<Button, int> buttonMap = new();
        public Dictionary<KeyCode, Button> keycodeMap = new();

        public Dictionary<int, bool> cachedPinState = new();

        private GpioController gpioController;

        public ButtonController(CommandArgOption option)
        {
            gpioController = new GpioController();

            BuildButtonMap(option);
            BuildKeycodeMap(option);


            Log.Debug($"---------------");
            Log.Debug($"Dump binding maps:");
            foreach (var pair in keycodeMap)
            {
                var pinStr = buttonMap.TryGetValue(pair.Value, out var p) ? p.ToString() : "<NO PIN OUTPUT>";
                Log.Debug($"[{pair.Key} -> btn:{pair.Value} -> pin:{pinStr}]");
            }
            Log.Debug($"---------------");

            //enable all pins
            foreach (var pin in buttonMap.Values)
                gpioController.OpenPin(pin, PinMode.Output);

            //reset default
            foreach (var button in buttonMap.Keys)
                SetButtonState(button, default);
            Log.Debug($"Buttons were reset.");

            cachedPinState.Clear();
        }

        private void BuildKeycodeMap(CommandArgOption option)
        {
            var map = new Dictionary<KeyCode, Button>()
            {
                [KeyCode.KEY_W] = Button.RingButton1,
                [KeyCode.KEY_E] = Button.RingButton2,
                [KeyCode.KEY_D] = Button.RingButton3,
                [KeyCode.KEY_C] = Button.RingButton4,
                [KeyCode.KEY_X] = Button.RingButton5,
                [KeyCode.KEY_Z] = Button.RingButton6,
                [KeyCode.KEY_A] = Button.RingButton7,
                [KeyCode.KEY_Q] = Button.RingButton8,
                [KeyCode.KEY_7] = Button.ButtonTest,
                [KeyCode.KEY_8] = Button.ButtonService,
            };

            if (option.OverrideKeycodeToButtons?.Any() ?? false)
            {
                foreach ((var overrideKeycode, var overrideButton) in
                    option.OverrideKeycodeToButtons.Select(x => x.Split("=")).Select(x => (Enum.Parse<KeyCode>(x[0]), Enum.Parse<Button>(x[1]))))
                {
                    if (map.FirstOrDefault(x => x.Value == overrideButton) is KeyValuePair<KeyCode, Button> pair)
                        LogEntity.Warn($"detected button conflict : [{overrideKeycode} -> btn:{overrideButton}] [{pair.Key} -> btn:{pair.Value}]");

                    map[overrideKeycode] = overrideButton;
                }
            }

            keycodeMap = map;
        }
        private void BuildButtonMap(CommandArgOption option)
        {
            var map = new Dictionary<Button, int>()
            {
                [Button.RingButton1] = 1,
                [Button.RingButton2] = 2,
                [Button.RingButton3] = 3,
                [Button.RingButton4] = 4,
                [Button.RingButton5] = 5,
                [Button.RingButton6] = 6,
                [Button.RingButton7] = 7,
                [Button.RingButton8] = 8,
                [Button.ButtonTest] = 9,
                [Button.ButtonService] = 10,
            };

            if (option.OverrideButtonToPins?.Any() ?? false)
            {
                foreach ((var overrideBtn, var overridePin) in
                option.OverrideButtonToPins.Select(x => x.Split("=")).Select(x => (Enum.Parse<Button>(x[0]), int.Parse(x[1]))))
                {
                    if (map.FirstOrDefault(x => x.Value == overridePin) is KeyValuePair<Button, int> pair)
                        LogEntity.Warn($"detected pin conflict : [{overrideBtn} -> pin:{overridePin}] [{pair.Key} -> pin:{pair.Value}]");

                    map[overrideBtn] = overridePin;
                }
            }

            buttonMap = map;
        }

        public void SetButtonState(KeyCode keyCode, KeyState state)
        {
            if (!keycodeMap.TryGetValue(keyCode, out var button))
                return;
            var isPressed = state switch
            {
                KeyState.Press or KeyState.Repeat => true,
                KeyState.Release or _ => false,
            };
            SetButtonState(button, isPressed);
        }

        public void SetButtonState(Button button, bool isPressed)
        {
            var pin = buttonMap[button];

            if (cachedPinState.TryGetValue(pin, out var prevState) ? isPressed == prevState : false)
                return;

            gpioController.Write(pin, isPressed ? PinValue.High : PinValue.Low);
            cachedPinState[pin] = isPressed;
            LogEntity.Debug($"[{button} -> pin:{pin} : {(isPressed ? "Pressed" : "Released")}]");
        }

        public void Dispose()
        {
            gpioController.Dispose();
        }
    }
}
