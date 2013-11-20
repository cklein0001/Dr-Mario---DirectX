using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX;
using SlimDX.XInput;

namespace Dr_Mario.Object_Classes
{
    static class ControllerHandler
    {
        public delegate void StartButtonEventHandler(object sender, EventArgs e);
        public static event StartButtonEventHandler StartButtonPressed;
        public static void StartButtonPressedCalled(object sender)
        {
            if (StartButtonPressed != null)
                StartButtonPressed(sender, EventArgs.Empty);
        }

        public delegate void SelectButtonEventHandler(object sender, EventArgs e);
        public static event SelectButtonEventHandler SelectButtonPressed;
        private static void SelectButtonPressedCalled(object sender)
        {
            if (SelectButtonPressed != null)
                SelectButtonPressed(sender, EventArgs.Empty);
        }

        private static Dictionary<IControllerInput, State> lastState = new Dictionary<IControllerInput, State>();

        public static void UpdateControllerState(IControllerInput b, SlimDX.XInput.Controller controller, int controllerIndex)
        {
            Movement commandToSend = Movement.None;

            if (controller.IsConnected)
            {
                var state = controller.GetState();

                if (lastState.ContainsKey(b) && lastState[b].Gamepad.Buttons.HasFlag(GamepadButtonFlags.Start) != state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.Start))
                {
                    if (state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.Start))
                        StartButtonPressedCalled(b);
                }

                if (b.InputReady)
                {
                    if (state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadLeft) || state.Gamepad.LeftThumbX < -10000)
                    {
                        commandToSend = commandToSend | Movement.Left;
                    }
                    else if (state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadRight) || state.Gamepad.LeftThumbX > 10000)
                    {
                        commandToSend = commandToSend | Movement.Right;
                    }
                    else if (state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadDown) || state.Gamepad.LeftThumbY < -10000)
                    {
                        commandToSend = commandToSend | Movement.Down;
                    }
                    else if (state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadUp) || state.Gamepad.LeftThumbY > 10000)
                    {
                        commandToSend = commandToSend | Movement.Up;
                    }

                    if (state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.A))
                    {
                        commandToSend = commandToSend | Movement.Clockwise;
                    }
                    else if (state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.X))
                    {
                        commandToSend = commandToSend | Movement.CounterClockwise;
                    }
                    else if (commandToSend != Movement.None)
                        commandToSend = commandToSend | Movement.NoRotate;

                    b.Input(commandToSend);

                }
                if (lastState.ContainsKey(b))
                    lastState[b] = state;
                else
                    lastState.Add(b, state);
            }
            else if ((b as BottleCpu) != null)
                b.Input(Movement.None);
        }
    }
}
