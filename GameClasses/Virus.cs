using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using SlimDX;
using SlimDX.Direct3D11;

namespace Dr_Mario.Object_Classes
{
    public class Virus : BottleItem
    {
        protected static ShaderResourceView R0, R1, R2, R3, R4, RD0, RD1, RD2;
        protected static ShaderResourceView B0, B1, B2, B3, B4, BD0, BD1, BD2;
        protected static ShaderResourceView Y0, Y1, Y2, Y3, Y4, YD0, YD1,YD2;

        public static void Initialize(SlimDX.Direct3D11.Device device)
        {
          var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            
          
            Image MainImage = Bitmap.FromStream(assembly.GetManifestResourceStream("GameClasses.images.virusred32.png"));
            R0 = InitImage32(0, MainImage, device);// new Bitmap(8, 8, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            R1 = InitImage32(32, MainImage, device);// new Bitmap(8, 8, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            R2 = InitImage32(64, MainImage, device);
            R3 = InitImage32(96, MainImage, device);
            R4 = InitImage32(128, MainImage, device);
            RD0 = InitImage32(160, MainImage, device);
            RD1 = InitImage32(192, MainImage, device);
            RD2 = InitImage32(224, MainImage, device);
            MainImage.Dispose();

            MainImage = Bitmap.FromStream(assembly.GetManifestResourceStream("GameClasses.images.virusblue32.png"));
            B0 = InitImage32(0, MainImage, device);// new Bitmap(8, 8, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            B1 = InitImage32(32, MainImage, device);// new Bitmap(8, 8, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            B2 = InitImage32(64, MainImage, device);
            B3 = InitImage32(96, MainImage, device);
            B4 = InitImage32(128, MainImage, device);
            BD0 = InitImage32(160, MainImage, device);
            BD1 = InitImage32(192, MainImage, device);
            BD2 = InitImage32(224, MainImage, device);
            MainImage.Dispose();

            MainImage = Bitmap.FromStream(assembly.GetManifestResourceStream("GameClasses.images.virusyellow32.png"));
            Y0 = InitImage32(0, MainImage, device);// new Bitmap(8, 8, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Y1 = InitImage32(32, MainImage, device);// new Bitmap(8, 8, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Y2 = InitImage32(64, MainImage, device);
            Y3 = InitImage32(96, MainImage, device);
            Y4 = InitImage32(128, MainImage, device);
            YD0 = InitImage32(160, MainImage, device);
            YD1 = InitImage32(192, MainImage, device);
            YD2 = InitImage32(224, MainImage, device);
            MainImage.Dispose();
        }

        public static void Destroy()
        {
            R0.Resource.Dispose();
            R0.Dispose();
            R1.Resource.Dispose();
            R1.Dispose();
            R2.Resource.Dispose();
            R2.Dispose();
            R3.Resource.Dispose();
            R3.Dispose();
            R4.Resource.Dispose();
            R4.Dispose();
            RD0.Resource.Dispose();
            RD1.Resource.Dispose();
            RD1.Dispose();
            RD2.Resource.Dispose();
            RD2.Dispose();

            B0.Resource.Dispose();
            B0.Dispose();
            B1.Resource.Dispose();
            B1.Dispose();
            B2.Resource.Dispose();
            B2.Dispose();
            B3.Resource.Dispose();
            B3.Dispose();
            B4.Resource.Dispose();
            B4.Dispose();
            BD0.Resource.Dispose();
            BD1.Resource.Dispose();
            BD1.Dispose();
            BD2.Resource.Dispose();
            BD2.Dispose();

            Y0.Resource.Dispose();
            Y0.Dispose();
            Y1.Resource.Dispose();
            Y1.Dispose();
            Y2.Resource.Dispose();
            Y2.Dispose();
            Y3.Resource.Dispose();
            Y3.Dispose();
            Y4.Resource.Dispose();
            Y4.Dispose();
            YD0.Resource.Dispose();
            YD1.Resource.Dispose();
            YD1.Dispose();
            YD2.Resource.Dispose();
            YD2.Dispose();

        }

        private static ShaderResourceView InitImage32(int x, Image MainImage, SlimDX.Direct3D11.Device device)
        {
            int y = 0;
            using (Image img = new Bitmap(32, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(img))
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                g.FillRectangle(new SolidBrush(System.Drawing.Color.Black), new Rectangle(0, 0, 32, 32));
                g.DrawImage(MainImage, new Rectangle(0, 0, 32, 32), new Rectangle(x, y, 32, 32), GraphicsUnit.Pixel);
                g.Dispose();

                img.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                
                var t2d = SlimDX.Direct3D11.Texture2D.FromMemory(device, ms.ToArray());
                return new SlimDX.Direct3D11.ShaderResourceView(device, t2d);
            }

        }

        public Virus(vColors color)
        {
            this._Color = color;
        }

        public override bool Locked
        {
            get { return true; }
        }

        public override JoinDirection JoinDirection
        {
            get
            {
                return Object_Classes.JoinDirection.UP;
            }
            set
            {
                
            }
        }

        public override bool Joined
        {
            get
            {
                return false;
            }
            set
            {

            }
        }

        public override ShaderResourceView Image
        {
            get {
                switch (this.Color)
                {
                    case vColors.R: return R0;
                    case vColors.B: return B0;
                    case vColors.Y: return Y0;
                    default: return null;
                }
            }
        }
        public override ShaderResourceView Image2
        {
            get {
                switch (this.Color)
                {
                    case vColors.R: return R1;
                    case vColors.B: return B1;
                    case vColors.Y: return Y1;
                    default: return null;
                }
            }
        }
        public override ShaderResourceView Image3
        {
            get
            {
                switch (this.Color)
                {
                    case vColors.R: return R2;
                    case vColors.B: return B2;
                    case vColors.Y: return Y2;
                    default: return null;
                }
            }
        }

        public override ShaderResourceView Image4
        {
            get
            {
                switch (this.Color)
                {
                    case vColors.R: return R3;
                    case vColors.B: return B3;
                    case vColors.Y: return Y3;
                    default: return null;
                }
            }
        }

        public override ShaderResourceView Image5
        {
            get
            {
                switch (this.Color)
                {
                    case vColors.R: return R4;
                    case vColors.B: return B4;
                    case vColors.Y: return Y4;
                    default: return null;
                }
            }
        }

        public override ShaderResourceView ImageDeath1
        {
            get
            {
                switch (this.Color)
                {
                    case vColors.R: return RD0;
                    case vColors.B: return BD0;
                    case vColors.Y: return YD0;
                    default: return null;
                }
            }
        }

        public override ShaderResourceView ImageDeath2
        {
            get
            {
                switch (this.Color)
                {
                    case vColors.R: return RD1;
                    case vColors.B: return BD1;
                    case vColors.Y: return YD1;
                    default: return null;
                }
            }
        }
        public override ShaderResourceView ImageDeath3
        {
            get
            {
                switch (this.Color)
                {
                    case vColors.R: return RD2;
                    case vColors.B: return BD2;
                    case vColors.Y: return YD2;
                    default: return null;
                }
            }
        }

        public override vColors Color
        {
            get { return this._Color;}
        }
    }
}
