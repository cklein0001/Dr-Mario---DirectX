using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;


namespace Dr_Mario.Form_Classes.Settings
{
    internal class BorderColor : SettingDisplay
    {
        static SlimDX.Direct3D11.ShaderResourceView ColorBar;

        new public static void InitializeResources()
        {
            BorderColor.ColorBar = Engine.CreateView("./images/ColorBar.png");
        }

        new public static void DisposeResources()
        {
            BorderColor.ColorBar.Dispose();
        }

        int[] color = new int[3];
        Data.PlayerSettingList settings;
        int index = 0;

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
            Engine.DrawSprite(BorderColor.ColorBar, new SlimDX.Vector2(location.X + 0.128f, location.Y), new SlimDX.Vector2(0.02f, 0.58f), Color.Red);
            Engine.DrawSprite(BorderColor.ColorBar, new SlimDX.Vector2(location.X + 0.253f, location.Y), new SlimDX.Vector2(0.02f, 0.58f), Color.Green);
            Engine.DrawSprite(BorderColor.ColorBar, new SlimDX.Vector2(location.X + 0.378f, location.Y), new SlimDX.Vector2(0.02f, 0.58f), Color.Blue);

            Engine.DrawText("R", new SlimDX.Vector2(location.X + 0.125f, location.Y - 0.55f), SlimDX.DirectWrite.TextAlignment.Leading);
            Engine.DrawText("G", new SlimDX.Vector2(location.X + 0.25f, location.Y - 0.55f), SlimDX.DirectWrite.TextAlignment.Leading);
            Engine.DrawText("B", new SlimDX.Vector2(location.X + 0.375f, location.Y - 0.55f), SlimDX.DirectWrite.TextAlignment.Leading);

            if (this.Active)
            {
                Engine.DrawSprite(PlayerMenu.arrowRight, new SlimDX.Vector2(location.X + 0.09f, location.Y -0.58f + ((this.color[0] / 255.0f) * 0.58f)), new SlimDX.Vector2(0.035f, 0.05f), (this.Active && this.index == 0) ? colorMultiplier : Color.White);
                Engine.DrawSprite(PlayerMenu.arrowRight, new SlimDX.Vector2(location.X + 0.215f, location.Y - 0.58f + ((this.color[1] / 255.0f) * 0.58f)), new SlimDX.Vector2(0.035f, 0.05f), (this.Active && this.index == 1) ? colorMultiplier : Color.White);
                Engine.DrawSprite(PlayerMenu.arrowRight, new SlimDX.Vector2(location.X + 0.34f, location.Y - 0.58f + ((this.color[2] / 255.0f) * 0.58f)), new SlimDX.Vector2(0.035f, 0.05f), (this.Active && this.index == 2) ? colorMultiplier : Color.White);
            }
            else
            {
                Engine.DrawSprite(PlayerMenu.arrowRight, new SlimDX.Vector2(location.X + 0.09f, location.Y - 0.58f + ((this.color[0] / 255.0f) * 0.58f)), new SlimDX.Vector2(0.035f, 0.05f));
                Engine.DrawSprite(PlayerMenu.arrowRight, new SlimDX.Vector2(location.X + 0.215f, location.Y - 0.58f + ((this.color[1] / 255.0f) * 0.58f)), new SlimDX.Vector2(0.035f, 0.05f));
                Engine.DrawSprite(PlayerMenu.arrowRight, new SlimDX.Vector2(location.X + 0.34f, location.Y - 0.58f + ((this.color[2] / 255.0f) * 0.58f)), new SlimDX.Vector2(0.035f, 0.05f));
            }
        }

        public override void MoveUp()
        {
            if (this.color[this.index] < 255)
            {
                this.color[this.index]++;
                this.settings.Color = Color.FromArgb(this.color[0], this.color[1], this.color[2]);
            }
        }

        public override void MoveDown()
        {
            if (this.color[this.index] > 0)
            {
                this.color[this.index]--;
                this.settings.Color = Color.FromArgb(this.color[0], this.color[1], this.color[2]);
            }
        }

        public override void Load(Data.PlayerSettingList settings)
        {
            this.color[0] = (int)settings.Color.R;
            this.color[1] = (int)settings.Color.G;
            this.color[2] = (int)settings.Color.B;
            this.settings = settings;
        }

        public override void Accept()
        {
            if (this.index == 2)
                this.Active = false;
            else
                this.index++;
        }

        public override void Cancel()
        {
            if (this.index == 0)
                this.Active = false;
            else
                this.index--;
        }
    }
}
