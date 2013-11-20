using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX;
using Dr_Mario.Object_Classes;
using Speed = Dr_Mario.Object_Classes.Speed;

namespace Dr_Mario.Form_Classes.Settings
{
    internal class GameSpeed :SettingDisplay
    {
        Speed value { get; set; }
        Speed originalValue { get; set; }
        private Data.PlayerSetting speedSetting;

        public int CapsuleSpeed { get { return (int)this.value; } }

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
            Engine.DrawText(string.Format("Speed: {0}", this.value), new Vector2(location.X, location.Y-0.02f), SlimDX.DirectWrite.TextAlignment.Leading);

            if (this.Active)
            {
                Engine.DrawSprite(PlayerMenu.arrowUp,
                    new Vector2(location.X + 0.15f, location.Y),
                    new Vector2(0.05f, 0.03f), colorMultiplier);
                Engine.DrawSprite(PlayerMenu.arrowDown,
                    new Vector2(location.X + 0.15f, location.Y - 0.135f),
                    new Vector2(0.05f, 0.03f), colorMultiplier);
            }
        }

        public override void MoveUp()
        {
            switch (this.value)
            {
                case Speed.Low:
                    this.value = Speed.Med;
                    break;
                case Speed.Med:
                    this.value = Speed.High;
                    break;
            }
        }

        public override void MoveDown()
        {
            switch (this.value)
            {
                case Speed.High:
                    this.value = Speed.Med;
                    break;
                case Speed.Med:
                    this.value = Speed.Low;
                    break;
            }
        }

        public override void Load(Data.PlayerSettingList settings)
        {
            this.speedSetting = settings["Speed"];
            this.value =this.originalValue= (Speed)Enum.Parse(typeof(Speed), speedSetting.Value);
        }

        public override void Accept()
        {
            this.Active = false;
            this.speedSetting.Value = this.value.ToString();
            this.originalValue = this.value;
        }

        public override void Cancel()
        {
            this.value = this.originalValue;
            this.Active = false;
        }
    }
}
