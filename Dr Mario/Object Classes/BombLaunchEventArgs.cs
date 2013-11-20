using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dr_Mario.Object_Classes
{
    public class BombLaunchEventArgs : EventArgs
    {
        public BombLaunchEventArgs(int PlayerIndex, BombMode recipient, vColors[] theBomb)
        {
            this.IndexPlayer = PlayerIndex;
            this.Recipient = recipient;
            this.BombData = theBomb;
        }
        public int IndexPlayer { get; private set; }
        public Object_Classes.BombMode Recipient { get; private set; }
        public vColors[] BombData { get; private set; }
    }
}
