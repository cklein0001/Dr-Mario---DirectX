using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dr_Mario.Object_Classes
{
   public class GamePausedEventArgs : EventArgs
    {
       public GamePausedEventArgs(Player p, bool stop)
       {
           this.Player = p;
           this.Pause = stop;
       }

       public Player Player { get; private set; }
       public bool Pause { get; private set; }
    }
}
