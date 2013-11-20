using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using SlimDX.Direct3D11;

namespace Dr_Mario.Object_Classes
{
   public class Capsule : BottleItem
    {
       static ShaderResourceView B, BU, BD, BL, BR, B_0, B_1, B_2;
       static ShaderResourceView Y, YU, YD, YL, YR, Y_0, Y_1, Y_2;
       static ShaderResourceView R, RU, RD, RL, RR, R_0, R_1, R_2;

       public static void Initialize(SlimDX.Direct3D11.Device device)
       {
           using (var stream = System.Reflection.Assembly.GetAssembly(typeof(Capsule)).GetManifestResourceStream("GameClasses.images.capsule32.png"))
           using (Image MainImage = Bitmap.FromStream(stream))
           {
               InitImage0(0, ref  B, ref  BU, ref  BD, ref  BL, ref  BR, ref  B_0, ref  B_1, ref B_2, MainImage, device);
               InitImage0(32, ref  Y, ref  YU, ref  YD, ref  YL, ref  YR, ref  Y_0, ref  Y_1, ref Y_2, MainImage, device);
               InitImage0(64, ref  R, ref  RU, ref  RD, ref  RL, ref  RR, ref  R_0, ref  R_1, ref R_2, MainImage, device);
               MainImage.Dispose();
           }
       }

       public override string ToString()
       {
           return string.Format("Capsule ({0},{1},{2})", this._Color, this._Direction, this._Joined);
       }

       public static void Destroy()
       {
           B.Resource.Dispose();
           B.Dispose();
           BU.Resource.Dispose();
           BU.Dispose();
           BD.Resource.Dispose();
           BD.Dispose();
           BL.Resource.Dispose();
           BL.Dispose();
           BR.Resource.Dispose();
           BR.Dispose();
           B_0.Resource.Dispose();
           B_0.Dispose();
           B_1.Resource.Dispose();
           B_1.Dispose();
           B_2.Resource.Dispose();
           B_2.Dispose();

           Y.Resource.Dispose();
           Y.Dispose();
           YU.Resource.Dispose();
           YU.Dispose();
           YD.Resource.Dispose();
           YD.Dispose();
           YL.Resource.Dispose();
           YL.Dispose();
           YR.Resource.Dispose();
           YR.Dispose();
           Y_0.Resource.Dispose();
           Y_0.Dispose();
           Y_1.Resource.Dispose();
           Y_1.Dispose();
           Y_2.Resource.Dispose();
           Y_2.Dispose();

           R.Resource.Dispose();
           R.Dispose();
           RU.Resource.Dispose();
           RU.Dispose();
           RD.Resource.Dispose();
           RD.Dispose();
           RL.Resource.Dispose();
           RL.Dispose();
           RR.Resource.Dispose();
           RR.Dispose();
           R_0.Resource.Dispose();
           R_0.Dispose();
           R_1.Resource.Dispose();
           R_1.Dispose();
           R_2.Resource.Dispose();
           R_2.Dispose();
       }


       private static void InitImage0(int y, ref ShaderResourceView s, ref  ShaderResourceView u, ref ShaderResourceView d, ref  ShaderResourceView l, ref ShaderResourceView r, ref ShaderResourceView d0, ref ShaderResourceView d1,ref ShaderResourceView d2, Image baseImage, SlimDX.Direct3D11.Device device)
       {
           u = InitImage(0, y, baseImage, device);
           d = InitImage(32, y, baseImage, device);
           l = InitImage(64, y, baseImage, device);
           r = InitImage(96, y, baseImage, device);
           s = InitImage(128, y, baseImage, device);
           d0 = InitImage(160, y, baseImage, device);
           d1 = InitImage(192, y, baseImage, device);
           d2 = InitImage(224, y, baseImage, device);
       }

       private static ShaderResourceView InitImage(int x, int y, Image MainImage, SlimDX.Direct3D11.Device device)
       {
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

       public static void New(vColors a, vColors b, ref Capsule A, ref Capsule B)
       {
           A = new Capsule(a);
           B = new Capsule(b);
           A.Joined = B.Joined = true;
           A.JoinedItem = B;
           B.JoinedItem = A;
           A.JoinDirection = JoinDirection.RIGHT;
           B.JoinDirection = JoinDirection.LEFT;
       }

       public Capsule(vColors v)
       {
           this._Color = v;
       }

       public override bool Locked
       {
           get { return false; }
       }

       private bool _Joined;
       public override bool Joined
       {
           get
           {
               return this._Joined;
           }
           set
           {
               this._Joined = value;
           }
       }

       private ShaderResourceView CapsuleImage()
       {
           if (this.Joined)
           {
               switch (this.JoinDirection)
               {
                   case Object_Classes.JoinDirection.DOWN:

                       switch (this.Color)
                       {
                           case vColors.B: return BU;
                           case vColors.Y: return YU;
                           case vColors.R: return RU;
                       }

                       break;
                   case Object_Classes.JoinDirection.LEFT:
                       switch (this.Color)
                       {
                           case vColors.B: return BR;
                           case vColors.Y: return YR;
                           case vColors.R: return RR;
                       }
                       break;
                   case Object_Classes.JoinDirection.RIGHT:
                       switch (this.Color)
                       {
                           case vColors.B: return BL;
                           case vColors.Y: return YL;
                           case vColors.R: return RL;
                       }
                       break;
                   case Object_Classes.JoinDirection.UP:
                       switch (this.Color)
                       {
                           case vColors.B: return BD;
                           case vColors.Y: return YD;
                           case vColors.R: return RD;
                       }
                       break;
               }
           }
           else
           {
               switch (this.Color)
               {
                   case vColors.B: return B;
                   case vColors.Y: return Y;
                   case vColors.R: return R;
                   default: return null;
               }
           }
           return null;
       }

       public override ShaderResourceView Image
       {
           get { return CapsuleImage();}
       }
       public override ShaderResourceView Image2
       {
           get { return CapsuleImage(); }
       }
       public override vColors Color
       {
           get { return this._Color;}// throw new NotImplementedException(); }
       }

       public override ShaderResourceView Image3
       {
           get { return CapsuleImage(); }
       }

       public override ShaderResourceView Image4
       {
           get { return CapsuleImage(); }
       }
       public override ShaderResourceView Image5
       {
           get { return CapsuleImage(); }
       }
       public override ShaderResourceView ImageDeath1
       {
           get
           {
               switch (this.Color)
               {
                   case vColors.B: return B_0;
                   case vColors.Y: return Y_0;
                   case vColors.R: return R_0;
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
                   case vColors.B: return B_1;
                   case vColors.Y: return Y_1;
                   case vColors.R: return R_1;
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
                   case vColors.B: return B_2;
                   case vColors.Y: return Y_2;
                   case vColors.R: return R_2;
                   default: return null;
               }
           }
       }


       protected JoinDirection _Direction;
       public override JoinDirection JoinDirection
       {
           get
           {
               return _Direction;
           }
           set
           {
               this._Direction = value;
           }
       }

       public void Rotate(Movement rotation)
       {
           int iDirection = (int)_Direction;
           int rDirection = rotation == Movement.Clockwise ? 1 : -1;

           iDirection = iDirection + rDirection;
           if (iDirection < 0)
               iDirection = (int)JoinDirection.DOWN;
           else if (iDirection > 3)
               iDirection = (int)JoinDirection.LEFT;

           this._Direction = (JoinDirection)iDirection;

       }
    }
}
