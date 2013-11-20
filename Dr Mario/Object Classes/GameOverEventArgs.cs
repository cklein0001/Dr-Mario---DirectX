using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dr_Mario.Object_Classes
{
    public class GameOverEventArgs : EventArgs
    {
        private GameOverReason _Reason;
        public GameOverReason Reason{get{return _Reason;}}

        public GameOverEventArgs(GameOverReason reason)
        {
            this._Reason = reason;
        }
    }
}
