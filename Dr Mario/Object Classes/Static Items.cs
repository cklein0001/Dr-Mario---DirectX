using System;

namespace Dr_Mario.Object_Classes
{
    public enum vColors
    {
        R = 1, B = 2, Y = 4, NULL = 0
    }

    public enum JoinDirection
    {
        LEFT=0, RIGHT=1,UP=2, DOWN=3 
    }

    [Flags]
    public enum Movement
    {
        None = 0,
        Left = 1,
        Right = 2, 
        Down = 4, 
        Up = 8,
        Clockwise = 16,
        CounterClockwise = 32,
        NoRotate = 64,
    }

    public enum GameOverReason { VirusClear, EntranceCollision }

    public enum Player { One, Two, Three, Four, System, Computer }
    public enum Speed { Low = 750, Med = 500, High = 250 }

    public enum BombMode { Auto=0, Salt_And_Pepper=1, By_Color=2, All_Up=3, All_Down=4, Saint=5 }
    [Flags]
    public enum PlayMode { SinglePlayer, Multiplayer, Practice, Menu, Dead }
    public enum GameMode { Menu = 0, Active = 1 }

    public struct DrawTextStruct
    {
        public DrawTextStruct(Form_Classes.Engine.TextRenderers textRenderer, SlimDX.DirectWrite.TextAlignment textAlignment, System.Drawing.Rectangle textDrawingArea, SlimDX.Direct2D.Brush brush, string textToWrite)
        {
            this.TextRenderer = textRenderer;
            this.textAlign = textAlignment;
            this.textArea = textDrawingArea;
            this.Brush = brush;
            this.Text = textToWrite;
        }


        public DrawTextStruct( SlimDX.DirectWrite.TextAlignment textAlignment, System.Drawing.Rectangle textDrawingArea, SlimDX.Direct2D.Brush brush, string textToWrite)
        {
            this.TextRenderer = Form_Classes.Engine.TextRenderers.Normal;
            this.textAlign = textAlignment;
            this.textArea = textDrawingArea;
            this.Brush = brush;
            this.Text = textToWrite;
        }

        public DrawTextStruct(SlimDX.DirectWrite.TextAlignment textAlignment, SlimDX.Vector2 location, SlimDX.Direct2D.Brush brush, string textToWrite)
        {
            this.TextRenderer = Form_Classes.Engine.TextRenderers.Normal;
            this.textAlign = textAlignment;
            this.Brush = brush;
            this.Text = textToWrite;
            this.textArea = new System.Drawing.Rectangle(Convert.ToInt32((location.X + 1) * Form_Classes.Engine.Width * 0.5), Convert.ToInt32((location.Y - 1) * Form_Classes.Engine.Height * -0.5), Form_Classes.Engine.Width / 4, Form_Classes.Engine.Height / 8);
        }


        public Form_Classes.Engine.TextRenderers TextRenderer;
        public SlimDX.DirectWrite.TextAlignment textAlign;
        public System.Drawing.Rectangle textArea;
        public SlimDX.Direct2D.Brush Brush;
        public string Text;
    }
}