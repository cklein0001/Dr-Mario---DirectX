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
        public static void UpdateControllerState(Bottle b, SlimDX.XInput.Controller controller)
        {
            if (controller.IsConnected && b.InputReady)
            {
                var state = controller.GetState();
                bool tripped = false;
                if (state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadLeft) || state.Gamepad.LeftThumbX < -10000)
                {
                    b.Input(Movement.Left);
                    tripped = true;
                }
                else if (state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadRight) || state.Gamepad.LeftThumbX > 10000)
                {
                    b.Input(Movement.Right);
                    tripped = true;
                }
                else if (state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadDown) || state.Gamepad.LeftThumbY < -10000)
                {
                    b.Input(Movement.Down);
                    tripped = true;
                }
                else
                    b.Input(Movement.NoMovement);

                if (state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.A))
                {
                    b.Input(Movement.Clockwise);
                    tripped = true;
                }
                else if (state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.X))
                {
                    b.Input(Movement.CounterClockwise);
                    tripped = true;
                }
                else
                    b.Input(Movement.NoRotate);

                if (!tripped)
                    b.Input(Movement.None);


                /*

                        var LastStates = controller.GetState();
                        GamepadButtonFlags buttons = controller.GetState().Gamepad.Buttons;

                        // Get button state and lock/unlock vibration.
                        if ((Controllers[i].GetState().Gamepad.Buttons == 0) &&
                            (LastStates[i].Gamepad.Buttons == 0))
                        {
                            if (!(!LockVibration[i] && vibration.RightMotorSpeed == 0 && vibration.LeftMotorSpeed == 0))
                                LockVibration[i] = !LockVibration[i];
                        }

                        // Set vibration.
                        Controllers[i].SetVibration(vibration);

                        stateText = String.Format(" {4}\n" +
                                                   "  Left Motor Speed: {0}\n" +
                                                   "  Right Motor Speed: {1}\n" +
                                                   "  Rumble Lock: {2}\n" +
                                                   "  Buttons : {3}",
                                                   vibration.LeftMotorSpeed,
                                                   vibration.RightMotorSpeed,
                                                   LockVibration[i],
                                                   LastStates[i].Gamepad.Buttons,
                                                   Controllers[i].IsConnected ? "Connected" : "Not Connected");
                    }

                    switch (i)
                    {
                        case 0:
                            label_Controller1.Text = "Controler 1:" + stateText;
                            break;
                        case 1:
                            label_Controller2.Text = "Controler 2:" + stateText;
                            break;
                        case 2:
                            label_Controller3.Text = "Controler 3:" + stateText;
                            break;
                        case 3:
                            label_Controller4.Text = "Controler 4:" + stateText;
                            break;
                        default:
                            break;
                    }
                }
                 */
            }
        }
    }
}
