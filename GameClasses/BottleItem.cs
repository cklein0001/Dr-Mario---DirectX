using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX.Direct3D11;

namespace Dr_Mario.Object_Classes
{
    public abstract class BottleItem
    {
        protected vColors _Color;
        public abstract vColors Color { get; }
        public abstract bool Locked { get; }
        public abstract bool Joined { get; set; }
        public abstract JoinDirection JoinDirection { get; set; }
        public BottleItem TopItem;
        public BottleItem BottomItem;
        public BottleItem LeftItem;
        public BottleItem RightItem;
        public BottleItem JoinedItem;

        public abstract ShaderResourceView Image { get; }
        public abstract ShaderResourceView Image2 { get; }
        public abstract ShaderResourceView Image3 { get; }
        public abstract ShaderResourceView Image4 { get; }
        public abstract ShaderResourceView Image5 { get; }
        public abstract ShaderResourceView ImageDeath1 { get; }
        public abstract ShaderResourceView ImageDeath2 { get; }
        public abstract ShaderResourceView ImageDeath3 { get; }

    }
}
