using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dr_Mario.Object_Classes
{
    interface IControllerInput
    {
        void Input(Movement command);
        bool InputReady { get; }
        int PlayerIndex { get; }
    }
}
