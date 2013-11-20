using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Dr_Mario.Object_Classes
{
   public class Location
    {
       private static Image _Blank;
       public static Image Blank
       {
           get
           {
               if (_Blank == null)
               {
                   _Blank = new Bitmap(32, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                   Graphics g = Graphics.FromImage(_Blank);
                   g.FillRectangle(new SolidBrush(System.Drawing.Color.Transparent), new Rectangle(0,0,32,32));
                   g.Dispose();

               }
               return _Blank;
           }
       }

       protected BottleItem _Item = null;
       public BottleItem Item
       {
           get { return this._Item; }
           set
           {
               this._DeathFrame = 0;
               this._Item = value;
           }
       }

       public bool IsEmpty { get { return this.Item == null; } }

       public override string ToString()
       {
           return string.Format("Location {{X:{0},Y:{1},Item:{2}}}", this.X, this.Y, this._Item ?? (object)"null");
       }

       public Location(vColors virusColor, int x, int y)
           : this(virusColor)
       {
           this.X_Location = x;
           this.Y_Location = y;
       }

       public Location(vColors virusColor)
       {
           if (virusColor == vColors.NULL)
               Item = null;
           else
               Item = new Virus(virusColor);
       }

       protected int _DeathFrame = 0;
       public int DeathFrame { set { this._DeathFrame = value; } }

       public SlimDX.Direct3D11.ShaderResourceView ShaderResourceView(int frame)
       {
           if (this.IsEmpty)
               return null;
           else
           {
               switch (this._DeathFrame)
               {
                   case 0:
                       switch (frame)
                       {
                           case 0: return Item.Image;
                           case 1: return Item.Image2;
                           case 2: return Item.Image3;
                           case 3: return Item.Image4;
                           case 4: return Item.Image5;
                           default: return null;

                       }
                   case 1:
                       return Item.ImageDeath1;
                   case 2:
                       return Item.ImageDeath2;
                   case 3:
                       return Item.ImageDeath3;
                   default: return null;
               }
           }
       }

       public SlimDX.Direct3D11.ShaderResourceView Image(int frame)
       {
           if(this.IsEmpty)
               return null;
           else
           {
               switch (this._DeathFrame)
               {
                   case 0:
                       switch (frame)
                       {
                           case 0: return Item.Image;
                           case 1: return Item.Image2;
                           case 2: return Item.Image3;
                           case 3: return Item.Image4;
                           case 4: return Item.Image5;
                           default: return null;

                       }
                   case 1:
                       return Item.ImageDeath1;
                   case 2:
                       return Item.ImageDeath2;
                   case 3:
                       return Item.ImageDeath3;
                   default: return null;
               }
           }
       }

       public vColors Color
       {
           get
           {
               if (this.Item == null)
                   return vColors.NULL;
               else
                   return this.Item.Color;
           }
       }

       public void Transfer(Location newLocation)
       {
           newLocation.Item = this.Item;
           this.Item = null;
       }

       public Location JoinedItem(Bottle container)
       {
           Location joinedItem = null;
           if (this.IsEmpty)
               return joinedItem;
           if (!this.Item.Joined)
               return joinedItem;

           switch (this.Item.JoinDirection)
           {
               case JoinDirection.LEFT: joinedItem = container.Locations[this.X - 1, this.Y]; break;
               case JoinDirection.RIGHT: joinedItem = container.Locations[this.X + 1, this.Y]; break;
               case JoinDirection.UP: joinedItem = container.Locations[this.X, this.Y - 1]; break;

           }
           return joinedItem;
       }

       protected int X_Location = 0, Y_Location = 0;
       public int X { get { return X_Location; } set { X_Location = value; } }
       public int Y { get { return Y_Location; } set { Y_Location = value; } }
    }
}
