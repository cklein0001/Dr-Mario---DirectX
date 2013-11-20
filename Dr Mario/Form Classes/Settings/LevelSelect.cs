using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX;

namespace Dr_Mario.Form_Classes.Settings
{
   internal class LevelSelect : SettingDisplay
   {
       #region Static 
       static SlimDX.Direct3D11.ShaderResourceView graph;

       new public static void InitializeResources()
       {
           LevelSelect.graph = Engine.CreateView("./images/LevelSelect.png");
       }

       new public static void DisposeResources()
       {
           LevelSelect.graph.Dispose();
           LevelSelect.overlay.Dispose();
       }

       #endregion

       private int _CurrentLevel { get; set; }
       public int CurrentLevel
       {
           get { return this._CurrentLevel; }
           set
           {
               this._CurrentLevel = value; this.StartingLevel = value;
               this.levelSetting.Value = value.ToString();
           }
       }
       private int StartingLevel { get; set; }
       private int MaxLevel { get; set; }
       private Data.PlayerSetting levelSetting;

        public override void Activate()
        {
            this.Active = true;
        }

        public override void Deactivate()
        {
            this.Active = false;
        }

        public override void Draw(SlimDX.Vector2 location, System.Drawing.Color colorMultiply)
        {
            float lvlMultiplier = ((float)(this.CurrentLevel - 1) * (1f / 25f)) - (0.0001f * CurrentLevel);
            var levelVector = new Vector2(location.X + 0.065f, location.Y - lvlMultiplier);
            Engine.DrawSprite(LevelSelect.graph, new Vector2(location.X + 0.10f, location.Y - 0.024f), new Vector2(0.061f, 1f));

            var writeRect = new System.Drawing.Rectangle(Convert.ToInt32((location.X + 1f) * (float)Engine.Width * 0.5),
                Convert.ToInt32(((this.Active?levelVector.Y: location.Y) - 0.964f) * Engine.Height * -0.5f),
                Convert.ToInt32(Engine.Width * 0.03),
                Convert.ToInt32(Engine.Height / 8));

            if (!this.Active)
            {

                Engine.DrawSprite(overlay, location, new Vector2(0.165f, 1.1f));
                Engine.DrawText(this.CurrentLevel.ToString(),
                    writeRect,
                    SlimDX.DirectWrite.TextAlignment.Trailing);
            }
            else
            {
                Engine.DrawSprite(PlayerMenu.arrowRight, levelVector, new Vector2(0.035f, 0.05f), colorMultiply);

                Engine.DrawText(this.CurrentLevel.ToString(),
                    writeRect,
                    SlimDX.DirectWrite.TextAlignment.Trailing);
            }
        }

        public override void MoveUp()
        {
            if (CurrentLevel > 1)
                this.CurrentLevel--;
        }

        public override void MoveDown()
        {
            if (this.CurrentLevel < Math.Max(20, Convert.ToInt32(this.MaxLevel)))
                this.CurrentLevel++;
        }

        public override void Load(Data.PlayerSettingList settings)
        {
            this.MaxLevel = Convert.ToInt32(settings["MaxLevel"].Value);
            this.levelSetting = settings["Level"];
            this.CurrentLevel=this.StartingLevel = Convert.ToInt32(levelSetting.Value);
        }

        public override void Accept()
        {
            this.StartingLevel = this.CurrentLevel;
            this.levelSetting.Value = this.CurrentLevel.ToString();
            this.Active = false;
        }

        public override void Cancel()
        {
            this.CurrentLevel = this.StartingLevel;
            this.Active = false;
        }
    }
}
