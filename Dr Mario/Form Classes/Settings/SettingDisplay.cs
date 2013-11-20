using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dr_Mario.Form_Classes.Settings
{
  internal abstract class SettingDisplay
    {
      protected static SlimDX.Direct3D11.ShaderResourceView overlay;
      public static SlimDX.Direct3D11.ShaderResourceView whitePixel;
      public static void InitializeResources()
      {
          using (System.Drawing.Bitmap bm = new System.Drawing.Bitmap(1, 1, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
          using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
          {
              bm.SetPixel(0, 0, System.Drawing.Color.FromArgb(128, System.Drawing.Color.Black));
              bm.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
              LevelSelect.overlay = Engine.CreateView(ms.ToArray());

              ms.Position = 0;
              bm.SetPixel(0, 0, System.Drawing.Color.White);
              bm.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
              LevelSelect.whitePixel = Engine.CreateView(ms.ToArray());
          }

      }

      public static void DisposeResources()
      {
          SettingDisplay.overlay.Dispose();
          SettingDisplay.whitePixel.Dispose();
      }

      public bool Active { get; protected set; }
      public abstract void Activate();
      public abstract void Deactivate();
      public abstract void Draw(SlimDX.Vector2 location, System.Drawing.Color colorMultiplier);
      public abstract void MoveUp();
      public abstract void MoveDown();
      public abstract void Load(Data.PlayerSettingList settings);
      public abstract void Accept();
      public abstract void Cancel();


     // public static abstract void InitializeResources();
     // public static abstract void DisposeResources();
    }
}
