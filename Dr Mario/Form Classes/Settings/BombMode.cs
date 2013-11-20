using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dr_Mario.Form_Classes.Settings
{
   internal class BombMode : SettingDisplay
    {
        public Object_Classes.BombMode Mode { get; private set; }
        private Object_Classes.BombMode original { get; set; }
        private Data.PlayerSetting setting;

        public override void Activate()
        {
            this.Active = true;
        }

        public override void Deactivate()
        {
            this.Active = false;
        }

        public override void Draw(SlimDX.Vector2 location, System.Drawing.Color colorMultiplier)
        {
            Engine.DrawText("Bomb Mode:",
                new SlimDX.Vector2(location.X, location.Y), SlimDX.DirectWrite.TextAlignment.Leading);
            Engine.DrawText(
                new Object_Classes.DrawTextStruct(SlimDX.DirectWrite.TextAlignment.Center,
                    new System.Drawing.Rectangle(Convert.ToInt32((location.X + 0.925f) * Form_Classes.Engine.Width * 0.5), Convert.ToInt32((location.Y - 1.145) * Form_Classes.Engine.Height * -0.5),Form_Classes.Engine.Width/6, Form_Classes.Engine.Height / 8),
                    Engine.WhiteBrush, string.Format("{0}", this.Mode.ToString().Replace('_', ' ').Replace("And", "&"))));

            if (this.Active)
            {
                if (this.Active)
                {
                    Engine.DrawSprite(PlayerMenu.arrowUp,
                        new SlimDX.Vector2(location.X+0.065f, location.Y-0.13f),
                        new SlimDX.Vector2(0.05f, 0.03f), colorMultiplier);
                    Engine.DrawSprite(PlayerMenu.arrowDown,
                        new SlimDX.Vector2(location.X + 0.065f, location.Y - 0.257f),
                        new SlimDX.Vector2(0.05f, 0.03f), colorMultiplier);
                }
            }
        }

        public override void MoveUp()
        {
            this.Mode = (Object_Classes.BombMode)((((int)this.Mode) + 1) % 6);
        }

        public override void MoveDown()
        {
            if (this.Mode == Object_Classes.BombMode.Auto)
                this.Mode = Object_Classes.BombMode.Saint;
            else
                this.Mode = (Object_Classes.BombMode)(((int)this.Mode) - 1);
        }

        public override void Load(Data.PlayerSettingList settings)
        {
            this.setting  =settings["BombMode"];
            this.Mode = this.original = (Object_Classes.BombMode)Enum.Parse(typeof(Object_Classes.BombMode), this.setting.Value);
        }

        public override void Accept()
        {
            this.setting.Value = this.Mode.ToString();
            this.original = this.Mode;
            this.Active = false;
        }

        public override void Cancel()
        {
            this.Mode = this.original;
            this.Active = false;
        }
    }
}
