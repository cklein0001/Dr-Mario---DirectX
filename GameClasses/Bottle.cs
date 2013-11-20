using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using SlimDX;

namespace Dr_Mario.Object_Classes
{
    public class Bottle 
    {
        public delegate void GameOverEventHandler(object sender, GameOverEventArgs e);
        public event GameOverEventHandler GameOver;
        private void GameOverCall(GameOverReason reason)
        {
            if (GameOver != null)
                GameOver(this, new GameOverEventArgs(reason));
        }

        /// <summary>
        /// Triggered when the bottle is finished filling with virii.
        /// </summary>
        public event EventHandler BottleReady;
        private void BottleReadyCall()
        {
            if (BottleReady != null)
                BottleReady(this, EventArgs.Empty);
        }
        /// <summary>
        /// Width of the bottle.
        /// </summary>
        public int Width = 8;
        /// <summary>
        /// Height of the bottle.
        /// </summary>
        public int Height = 17;
        /// <summary>
        /// Capsule generator for the bottle.  
        /// </summary>
        private CapsuleGenerator capsuleGenerator;
        /// <summary>
        /// 2 Dimensional Array, Width x Height
        /// </summary>
        public Location[,] Locations;
        #region Timers
        private System.Timers.Timer TimerNextCapsule;
        private System.Timers.Timer TimerDeathFrame;
        private System.Timers.Timer TimerDropFreeCapsules;
        private System.Timers.Timer TimerInitialVirusFiller;
        private System.Timers.Timer TimerPushCapsuleDown;
        #endregion
        /// <summary>
        /// Lock to halt bottle from refreshing internal image.
        /// </summary>
        private object IsPainting = new object();
        private int DeathFrameCounter = 0;
        private List<Location> LocationsMarkedForDeath = new List<Location>();
        private bool LocationsDying = false;
        private object InputLock = new object();
        private bool _InputReady = true;
        public bool InputReady { get { lock (InputLock) { return this.CurrentCapsule != null && this._InputReady; } } }
        public bool Filled { get; private set; }
        private Location _CurrentCapsule = null;
        private Capsule[] nextCapsule = new Capsule[2];

        private Queue<vColors[]> _BombsIncoming = new Queue<vColors[]>();

        private int TimerPushCapsuleDownCounter;
        private ushort initialVirusMax;
        private Random initialRandom;
        // First instance is randomized
        private int initialVirusColorContainer;
        private int _CurrentLevel;
        public int CurrentLevel { get { return this._CurrentLevel; } set { this._CurrentLevel = value; } }

        private ShaderResourceView BorderTexture;

        public Location CurrentCapsule
        {
            get
            {
                return _CurrentCapsule;
            }

        }

        public Location JoinedItem { get { if (_CurrentCapsule != null) return this._CurrentCapsule.JoinedItem(this); else return null; } }

        public Vector2 Size
        {
            get
            {
                float spriteWidth = 2f / 36f;
                float spriteHeight = spriteWidth * 1.6f;
                return new Vector2(spriteWidth * 9, spriteHeight * this.Height + spriteHeight);
            }
        }

        public int Viruses
        {
            get
            {
                int value = 0;
                for (int y = 1; y < this.Height; y++)
                    for (int x = 0; x < this.Width; x++)
                        value += !Locations[x, y].IsEmpty && Locations[x, y].Item.Locked ? 1 : 0;
                return value;
            }
        }

        private object BombLock = new object();
        public void GiveBomb(vColors[] fragments)
        {
            lock (this.BombLock)
            {
                this._BombsIncoming.Enqueue(fragments);
            }
        }

        /// <summary>
        /// Setter for the speed that capsules drop at. 0 Disables.
        /// </summary>
        /// <param name="milliseconds">Time in milliseconds to push capsule down.</param>
        public void CapsuleDropSpeed(int milliseconds)
        {
            this.TimerPushCapsuleDown.Interval = milliseconds;
        }

        public Bottle()
        {
            Locations = new Location[8, 17];
            capsuleGenerator = new CapsuleGenerator();
            TimerNextCapsule = new System.Timers.Timer();
            TimerNextCapsule.Elapsed += new System.Timers.ElapsedEventHandler(TimerNextCapsuleTimer_Elapsed);
            TimerNextCapsule.Interval = 400;
            TimerNextCapsule.AutoReset = false;

            TimerDeathFrame = new System.Timers.Timer();
            TimerDeathFrame.Interval = 275;
            TimerDeathFrame.AutoReset = true;
            TimerDeathFrame.Enabled = false;
            TimerDeathFrame.Elapsed += new System.Timers.ElapsedEventHandler(TimerDeathFrame_Elapsed);

            TimerDropFreeCapsules = new System.Timers.Timer();
            TimerDropFreeCapsules.Interval = 275;
            TimerDropFreeCapsules.AutoReset = true;
            TimerDropFreeCapsules.Enabled = false;
            TimerDropFreeCapsules.Elapsed += new System.Timers.ElapsedEventHandler(TimerDropFreeCapsules_Elapsed);

            TimerPushCapsuleDown = new System.Timers.Timer();
            TimerPushCapsuleDown.Interval = 500;
            TimerPushCapsuleDown.AutoReset = true;
            TimerPushCapsuleDown.Enabled = false;
            TimerPushCapsuleDown.Elapsed += new System.Timers.ElapsedEventHandler(TimerPushCapsuleDown_Elapsed);

            TimerInitialVirusFiller = new System.Timers.Timer();
            TimerInitialVirusFiller.AutoReset = false;
            TimerInitialVirusFiller.Interval = 28;
            TimerInitialVirusFiller.Elapsed += new System.Timers.ElapsedEventHandler(TimerInitialVirusFiller_Elapsed);
            
        }
       
        private int TimerPushCapsuleDown_Incrementor = 0;
        void TimerPushCapsuleDown_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (this.InputLock)
            {
                System.Diagnostics.Debug.WriteLine(this._CurrentCapsule ?? (object)"currently null");
                if (this.CurrentCapsule != null)
                {
                    this.InputReady_Set(false);
                    try
                    {
                        MoveDown();
                        TimerPushCapsuleDown_Incrementor++;
                        if (TimerPushCapsuleDown_Incrementor % 10 == 0)
                        {
                            var interval = TimerPushCapsuleDown.Interval - Math.Ceiling(TimerPushCapsuleDown.Interval * 0.02);
                            if (interval > 0)
                                TimerPushCapsuleDown.Interval = interval;
                            if (this.NextMoveBlockDown > 10)
                                this.NextMoveBlockDown--;
                        }
                    }
                    catch (NullReferenceException nre)
                    {
                        System.Diagnostics.Debug.WriteLine("Exception in Push Down.");
                        System.Diagnostics.Debug.WriteLine(nre);

                        System.Diagnostics.Debugger.Break();
                    }

                    this.InputReady_Set(true);
                }
                else
                    ((System.Timers.Timer)sender).Enabled = false;
            }
        }

        void TimerDropFreeCapsules_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!this.InputReady)
            {
                if (!DropFreeCapsules())
                {
                    ((System.Timers.Timer)sender).Enabled = false;
                    LockCapsule();
                }
            }
            else
                ((System.Timers.Timer)sender).Enabled = false;
                
        }
        
        void TimerNextCapsuleTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
                TimerNextCapsule.Enabled = false;
            if (this.CurrentCapsule == null)
            {
                lock (this.BombLock)
                {
                    if (this._BombsIncoming.Count == 0)
                        nextCapsuleDrop();
                    else
                        BombDeQueue();
                }  
            }
            }

        private void BombDeQueue()
        {
            var bombs = this._BombsIncoming.Dequeue();
            Random bombDropper = new Random();
            var bottleSlice = (1.0 / bombs.Length) * this.Width; //2 bombs = 1/2 the field, 4 = 1/4, and so on.
            for(int bi = 0; bi < bombs.Length; bi++)
            {
                var start = Convert.ToInt32(bi * bottleSlice);
                var end = Convert.ToInt32((bi + 1) * bottleSlice);
                Locations[bombDropper.Next(start, end), 1].Item = new Capsule(bombs[bi]);
            }
            this.InputReady_Set(false);
            this.TimerDropFreeCapsules.Enabled = true;
        }
        public void Start() { nextCapsuleDrop(); }

        void nextCapsuleDrop()
        {
            if (Locations[3, 1].IsEmpty && Locations[4, 1].IsEmpty)
            {
                TimerNextCapsule.Enabled = false;
                Capsule a = null, b = null;
                capsuleGenerator.Next(out a, out b);

                Locations[3, 1].Item = nextCapsule[0];
                Locations[4, 1].Item = nextCapsule[1];
                nextCapsule[0] = a;
                nextCapsule[1] = b;
                this._CurrentCapsule = Locations[3, 1];
                this.TimerPushCapsuleDown.Enabled = true;
            }
            else
            {
                GameOverCall(GameOverReason.EntranceCollision);
            }
        }

        public void Paint(SlimDX.Vector2 location, int frame, SpriteTextRenderer.SpriteRenderer spriteRenderer)
        {
            float spriteWidth = 2f / 36f;
            float spriteHeight = spriteWidth * 1.6f;

            lock (InputLock)
            {
                try
                {
                    lock (IsPainting)
                    {
                        //var sr = Program.spriteRenderer;
                       
                        // Bottle Items
                        try
                        {
                            SlimDX.Vector2 iVector = new SlimDX.Vector2(location.X + (spriteWidth * 0.5f), location.Y - (spriteHeight * 0.5f));
                            SlimDX.Vector2 iSize = new SlimDX.Vector2(spriteWidth, spriteHeight);
                            for (int y = 1; y < 17; y++)
                            {
                                for (int x = 0; x < 8; x++)
                                {
                                    if (Locations[x, y] == null) { Locations[x, y] = new Location(vColors.NULL, x, y); }
                                    if (!Locations[x, y].IsEmpty)
                                        spriteRenderer.Draw(Locations[x, y].ShaderResourceView(frame),
                                            new SlimDX.Vector2(iVector.X + (spriteWidth * x), iVector.Y - (y * spriteHeight)),
                                            iSize, SpriteTextRenderer.CoordinateType.SNorm);
                                }
                            }
                        }
                        catch (NullReferenceException nrei)
                        {
                            System.Diagnostics.Debug.WriteLine("Null writing items.");
                            System.Diagnostics.Debug.WriteLine(nrei);
                        }

                        try
                        {
                            // Borders
                            //left
                            spriteRenderer.Draw(this.BorderTexture, location, new Vector2(spriteWidth * 0.5f, (spriteHeight * this.Height) + (spriteHeight * 0.5f)), SpriteTextRenderer.CoordinateType.SNorm);
                            //top
                            {
                                spriteRenderer.Draw(this.BorderTexture, new Vector2(location.X, location.Y - spriteHeight), new Vector2(spriteWidth * 3.4f, spriteHeight * 0.5f), SpriteTextRenderer.CoordinateType.SNorm);
                                spriteRenderer.Draw(this.BorderTexture, new Vector2(location.X + spriteWidth * 3.4f, location.Y - (spriteHeight * 1.45f)), new Vector2(spriteWidth * 2.2f, spriteHeight * 0.05f), SpriteTextRenderer.CoordinateType.SNorm);
                                spriteRenderer.Draw(this.BorderTexture, new Vector2(location.X + spriteWidth * 5.6f, location.Y - spriteHeight), new Vector2(spriteWidth * 3.4f, spriteHeight * 0.5f), SpriteTextRenderer.CoordinateType.SNorm);
                            }
                            //right
                            spriteRenderer.Draw(this.BorderTexture, new Vector2(location.X, (location.Y - (spriteHeight * this.Height + (spriteHeight * 0.5f)))), new Vector2(spriteWidth * (this.Width + 1), spriteHeight * 0.5f), SpriteTextRenderer.CoordinateType.SNorm);
                            //bottom
                            spriteRenderer.Draw(this.BorderTexture, new Vector2(location.X + (spriteWidth * this.Width) + (spriteWidth * 0.5f), location.Y), new Vector2(spriteWidth * 0.5f, (spriteHeight * this.Height) + (spriteHeight * 0.5f)), SpriteTextRenderer.CoordinateType.SNorm);
                        }
                        catch (NullReferenceException nreb)
                        {
                            System.Diagnostics.Debug.WriteLine("Null writing border.");
                            System.Diagnostics.Debug.WriteLine(nreb);
                        }
                        try
                        {
                            spriteRenderer.Draw(nextCapsule[0].Image, new Vector2(location.X + spriteWidth * 3.5f, location.Y - spriteHeight * 0.40f), new Vector2(spriteWidth, spriteHeight), SpriteTextRenderer.CoordinateType.SNorm);
                            spriteRenderer.Draw(nextCapsule[1].Image, new Vector2(location.X + spriteWidth * 4.5f, location.Y - spriteHeight * 0.40f), new Vector2(spriteWidth, spriteHeight), SpriteTextRenderer.CoordinateType.SNorm);

                        }
                        catch (NullReferenceException nrec)
                        {
                        }
                     //   Program.TextRenderer_System.DrawString("Abcdefghijklmnopqrstuvwxyz", new Vector2(0.3f, 0),0.2f, System.Drawing.Color.White, SpriteTextRenderer.CoordinateType.SNorm );
                     /*   sr.Draw(Program.TextRenderer_System.SRV,
                            new Vector2(-1f, 1f),
                            new Vector2(1, 1.5f),
                             SpriteTextRenderer.CoordinateType.SNorm);*/
                    }
                }
                catch (Exception er)
                {
                    DeO(er);
                }
            }
        }


        void TimerDeathFrame_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (this.LocationsDying)
            {
                DeathFrameCounter++;
                if (DeathFrameCounter < 3)
                {
                    foreach (Location l in LocationsMarkedForDeath)
                        l.DeathFrame = DeathFrameCounter;
                    if (LocationsMarkedForDeath.Where(u => u.Item.Locked).Count() == this.Viruses)
                    {
                        TimerDeathFrame.Enabled = false;
                        GameOverCall(GameOverReason.VirusClear);
                    }
                }
                else
                {
                    foreach (Location l in LocationsMarkedForDeath)
                    {
                        Location joinedItem = l.JoinedItem(this);
                        if (joinedItem != null && !joinedItem.IsEmpty)
                        {
                            joinedItem.Item.Joined = false;
                        }
                        l.Item = null;
                    }
                    DeathFrameCounter = 0;
                    ((System.Timers.Timer)sender).Enabled = false;
                    TimerDropFreeCapsules.Enabled = true;
                    this.LocationsDying = false;
                }
            }
            else
                ((System.Timers.Timer)sender).Enabled = false;
        }

        #region Movement logic

        private bool InputIsCapsuleMovement(Movement command)
        { return (command & (Movement.Left | Movement.Right | Movement.Down)) != 0x0; }



        private int NextMoveBlock = 300;
        private int NextMoveBlockDown = 50;
        private DateTime NextMoveAllowedAt = DateTime.Now;
        private long NextRotateAllowedAt = 0;
        private Movement lastMoveCommand;
        private Movement lastRotateCommand;
        public void Input(Movement command)
        {
            this.InputReady_Set(false);
            lock (InputLock)
            {
                try
                {
                    if ((lastRotateCommand | lastMoveCommand | command) != Movement.None)
                    {
                        if (this.CurrentCapsule != null)
                        {
                            if (this.InputIsCapsuleMovement(command))
                            {
                                if (DateTime.Now >= NextMoveAllowedAt)
                                {
                                    switch (command)
                                    {
                                        case Movement.Down:
                                            MoveDown(); break;
                                        case Movement.Left:
                                            MoveLeft(); break;
                                        case Movement.Right:
                                            MoveRight(); break;
                                        case Movement.NoMovement:
                                            command = Movement.None;
                                            break;
                                    }
                                    if (this.lastMoveCommand == command && command == Movement.Down)
                                        this.NextMoveBlock = this.NextMoveBlockDown;
                                    else if (this.lastMoveCommand == command)
                                        this.NextMoveBlock = 150;
                                    else
                                        this.NextMoveBlock = 300;

                                    if (command != Movement.None)
                                        this.NextMoveAllowedAt = DateTime.Now.AddMilliseconds(this.NextMoveBlock);
                                }
                                this.lastMoveCommand = command;

                            }
                            else
                            {
                                switch (command)
                                {
                                    case Movement.Clockwise:
                                        if (this.lastRotateCommand != command)
                                            RotateClockwise();
                                        this.lastRotateCommand = command;
                                        break;
                                    case Movement.CounterClockwise:
                                        if (this.lastRotateCommand != command)
                                            RotateCounterClockwise();
                                        this.lastRotateCommand = command;
                                        break;
                                    case Movement.NoRotate:
                                        this.lastRotateCommand = Movement.None;
                                        break;
                                    case Movement.None:
                                        this.lastMoveCommand = Movement.None;
                                        this.lastRotateCommand = Movement.None;
                                        this.NextMoveBlock = 300;

                                        break;
                                }
                            }
                        }
                    }
                }
                catch (NullReferenceException nre)
                {
                    System.Diagnostics.Debug.WriteLine("Exception in Input.");
                    System.Diagnostics.Debug.WriteLine(nre);
                    System.Diagnostics.Debugger.Break();
                }
            }
            this.InputReady_Set(true);
        }

        protected void RotateCounterClockwise()
        {
            var joinedItem = CurrentCapsule.JoinedItem(this);
            //First check to see if against left
            if (CurrentCapsule.X == 0)// || CurrentCapsule.X == (this.Width - 1))
            {
                // if pill is currently positioned vertically
                if (CurrentCapsule.Item.JoinDirection == JoinDirection.UP)
                {
                    // if there is room to right
                    if (Locations[CurrentCapsule.X + 1, CurrentCapsule.Y].IsEmpty)
                    {
                        //rotate
                        joinedItem.Item.JoinDirection = JoinDirection.RIGHT;
                        CurrentCapsule.Item.JoinDirection = JoinDirection.LEFT;
                        CurrentCapsule.Transfer(Locations[CurrentCapsule.X + 1, CurrentCapsule.Y]);
                        joinedItem.Transfer(CurrentCapsule);

                    }
                    // other side is wall, drop out.

                }
                // else pill is horizontal
                else
                {
                    // check for room upwards on primary
                    if (Locations[CurrentCapsule.X, CurrentCapsule.Y - 1].IsEmpty)
                    {
                        //joined item
                        CurrentCapsule.Item.JoinDirection = JoinDirection.UP;
                        //CurrentCapsule.Transfer(Locations[CurrentCapsule.X, CurrentCapsule.Y - 1]);
                        joinedItem.Item.JoinDirection = JoinDirection.DOWN;
                        joinedItem.Transfer(Locations[CurrentCapsule.X, CurrentCapsule.Y - 1]);

                    }
                    else if (Locations[joinedItem.X, joinedItem.Y - 1].IsEmpty)
                    {
                        CurrentCapsule.Item.JoinDirection = JoinDirection.UP;
                        joinedItem.Item.JoinDirection = JoinDirection.DOWN;
                        joinedItem.Transfer(Locations[joinedItem.X, joinedItem.Y - 1]);
                        CurrentCapsule.Transfer(joinedItem);
                        this._CurrentCapsule = joinedItem;
                    }
                }
            }
            // check for right wall
            else if (CurrentCapsule.X + 1 == this.Width)
            {
                // if pill is currently positioned vertically
                if (CurrentCapsule.Item.JoinDirection == JoinDirection.UP)
                {
                    // if there is room to left
                    if (Locations[CurrentCapsule.X - 1, CurrentCapsule.Y].IsEmpty)
                    {
                        // Move current into left, joined into current position, move current 
                        joinedItem.Item.JoinDirection = JoinDirection.RIGHT;
                        CurrentCapsule.Item.JoinDirection = JoinDirection.LEFT;
                        joinedItem.Transfer(Locations[CurrentCapsule.X - 1, CurrentCapsule.Y]);
                        //CurrentCapsule.Transfer(Locations[CurrentCapsule.X - 1, CurrentCapsule.Y]);
                        //joinedItem.Transfer(CurrentCapsule);
                        this._CurrentCapsule = Locations[CurrentCapsule.X - 1, CurrentCapsule.Y];
                    }
                    // other side is wall, drop out.

                }
                // else pill is horizontal, and it is impossible to be against wall
                // because 'joinedItem' is in that location at best
            }
            else
            {
                // Pill is somewhere in middle of bottle.
                switch (CurrentCapsule.Item.JoinDirection)
                {
                    case JoinDirection.UP:
                        // Natural rotation is to move joined item to left spot;
                        if (Locations[CurrentCapsule.X + 1, CurrentCapsule.Y].IsEmpty)
                        {
                            joinedItem.Item.JoinDirection = JoinDirection.RIGHT;
                            CurrentCapsule.Item.JoinDirection = JoinDirection.LEFT;
                            CurrentCapsule.Transfer(Locations[CurrentCapsule.X + 1, CurrentCapsule.Y]);
                            joinedItem.Transfer(CurrentCapsule);
                            // current capsule location does not change.
                        }
                        // Else slide left one block
                        else if (Locations[CurrentCapsule.X - 1, CurrentCapsule.Y].IsEmpty)
                        {
                            joinedItem.Transfer(Locations[CurrentCapsule.X - 1, CurrentCapsule.Y]);

                            joinedItem.Item.JoinDirection = JoinDirection.RIGHT;
                            CurrentCapsule.Item.JoinDirection = JoinDirection.LEFT;
                            this._CurrentCapsule = Locations[CurrentCapsule.X - 1, CurrentCapsule.Y];
                        }
                        break;
                    case JoinDirection.RIGHT:
                        // check for room upwards on primary
                        if (Locations[CurrentCapsule.X, CurrentCapsule.Y - 1].IsEmpty)
                        {
                            joinedItem.Item.JoinDirection = JoinDirection.DOWN;
                            CurrentCapsule.Item.JoinDirection = JoinDirection.UP;
                            joinedItem.Transfer(Locations[CurrentCapsule.X, CurrentCapsule.Y - 1]);
                            // Current Capsule location does not change.
                        }
                        else if (Locations[joinedItem.X, joinedItem.Y - 1].IsEmpty)
                        {
                            joinedItem.Item.JoinDirection = JoinDirection.DOWN;
                            CurrentCapsule.Item.JoinDirection = JoinDirection.UP;
                            joinedItem.Transfer(Locations[joinedItem.X, joinedItem.Y - 1]);
                            CurrentCapsule.Transfer(joinedItem);
                            this._CurrentCapsule = joinedItem;
                        }
                        break;
                }
            }
        }

        protected void RotateClockwise()
        {
            var joinedItem = CurrentCapsule.JoinedItem(this);
            //First check to see if against left
            if (CurrentCapsule.X == 0)// || CurrentCapsule.X == (this.Width - 1))
            {
                // if pill is currently positioned vertically
                if (CurrentCapsule.Item.JoinDirection == JoinDirection.UP)
                {
                    // if there is room to right
                    if (Locations[CurrentCapsule.X + 1, CurrentCapsule.Y].IsEmpty)
                    {
                        //rotate
                        joinedItem.Item.JoinDirection = JoinDirection.LEFT;
                        joinedItem.Transfer(Locations[CurrentCapsule.X + 1, CurrentCapsule.Y]);
                        CurrentCapsule.Item.JoinDirection = JoinDirection.RIGHT;

                    }
                    // other side is wall, drop out.

                }
                // else pill is horizontal
                else
                {
                    // check for room upwards on primary
                    if (Locations[CurrentCapsule.X, CurrentCapsule.Y - 1].IsEmpty)
                    {
                        // Move 'current' up, joined item into 'current', current 'place' does not move.
                        CurrentCapsule.Item.JoinDirection = JoinDirection.DOWN;
                        CurrentCapsule.Transfer(Locations[CurrentCapsule.X, CurrentCapsule.Y - 1]);
                        joinedItem.Item.JoinDirection = JoinDirection.UP;
                        joinedItem.Transfer(CurrentCapsule);

                    }
                    else if (Locations[joinedItem.X, joinedItem.Y - 1].IsEmpty)
                    {
                        CurrentCapsule.Item.JoinDirection = JoinDirection.DOWN;
                        joinedItem.Item.JoinDirection = JoinDirection.UP;
                        CurrentCapsule.Transfer(Locations[joinedItem.X, joinedItem.Y - 1]);
                        this._CurrentCapsule = joinedItem;
                    }
                }
            }
            // check for right wall
            else if (CurrentCapsule.X + 1 == this.Width)
            {
                // if pill is currently positioned vertically
                if (CurrentCapsule.Item.JoinDirection == JoinDirection.UP)
                {
                    // if there is room to left
                    if (Locations[CurrentCapsule.X - 1, CurrentCapsule.Y].IsEmpty)
                    {
                        // Move current into left, joined into current position, move current 
                        joinedItem.Item.JoinDirection = JoinDirection.LEFT;
                        CurrentCapsule.Item.JoinDirection = JoinDirection.RIGHT;
                        CurrentCapsule.Transfer(Locations[CurrentCapsule.X - 1, CurrentCapsule.Y]);
                        joinedItem.Transfer(CurrentCapsule);
                        this._CurrentCapsule = Locations[CurrentCapsule.X - 1, CurrentCapsule.Y];
                    }
                    // other side is wall, drop out.

                }
                // else pill is horizontal, and it is impossible to be against wall
                // because 'joinedItem' is in that location at best
            }
            else
            {
                // Pill is somewhere in middle of bottle.
                switch (CurrentCapsule.Item.JoinDirection)
                {
                    case JoinDirection.UP:
                        // Natural rotation is to move joined item to left spot;
                        if (Locations[CurrentCapsule.X + 1, CurrentCapsule.Y].IsEmpty)
                        {
                            joinedItem.Item.JoinDirection = JoinDirection.LEFT;
                            joinedItem.Transfer(Locations[CurrentCapsule.X + 1, CurrentCapsule.Y]);
                            CurrentCapsule.Item.JoinDirection = JoinDirection.RIGHT;
                        }
                        // Else slide left one block
                        else if (Locations[CurrentCapsule.X - 1, CurrentCapsule.Y].IsEmpty)
                        {
                            joinedItem.Item.JoinDirection = JoinDirection.RIGHT;
                            CurrentCapsule.Item.JoinDirection = JoinDirection.LEFT;
                            CurrentCapsule.Transfer(Locations[CurrentCapsule.X - 1, CurrentCapsule.Y]);
                            joinedItem.Transfer(CurrentCapsule);
                            this._CurrentCapsule = Locations[CurrentCapsule.X - 1, CurrentCapsule.Y];
                        }
                        break;
                    case JoinDirection.RIGHT:
                        // check for room upwards on primary
                        if (Locations[CurrentCapsule.X, CurrentCapsule.Y - 1].IsEmpty)
                        {
                            // Move 'current' up, joined item into 'current', current 'place' does not move.
                            CurrentCapsule.Item.JoinDirection = JoinDirection.DOWN;
                            CurrentCapsule.Transfer(Locations[CurrentCapsule.X, CurrentCapsule.Y - 1]);
                            joinedItem.Item.JoinDirection = JoinDirection.UP;
                            joinedItem.Transfer(CurrentCapsule);

                        }
                        else if (Locations[joinedItem.X, joinedItem.Y - 1].IsEmpty)
                        {
                            CurrentCapsule.Item.JoinDirection = JoinDirection.DOWN;
                            joinedItem.Item.JoinDirection = JoinDirection.UP;
                            CurrentCapsule.Transfer(Locations[joinedItem.X, joinedItem.Y - 1]);
                            this._CurrentCapsule = joinedItem;
                        }
                        break;
                }
            }
        }
        private void InputReady_Set(bool value) { lock (this.InputLock) { this._InputReady = value; } }

        protected void MoveLeft()
        {
            if (CurrentCapsule.X == 0)
                return;

            var joinedItem = CurrentCapsule.JoinedItem(this);
            switch (CurrentCapsule.Item.JoinDirection)
            {
                case JoinDirection.UP:
                    if (Locations[CurrentCapsule.X - 1, CurrentCapsule.Y].IsEmpty && Locations[joinedItem.X - 1, joinedItem.Y].IsEmpty)
                    {
                        CurrentCapsule.Transfer(Locations[CurrentCapsule.X - 1, CurrentCapsule.Y]);
                        joinedItem.Transfer(Locations[joinedItem.X - 1, joinedItem.Y]);
                        this._CurrentCapsule = Locations[CurrentCapsule.X - 1, CurrentCapsule.Y];
                    }
                    break;
                case JoinDirection.LEFT:
                case JoinDirection.RIGHT:
                    if (Locations[CurrentCapsule.X - 1, CurrentCapsule.Y].IsEmpty)
                    {
                        CurrentCapsule.Transfer(Locations[CurrentCapsule.X - 1, CurrentCapsule.Y]);
                        joinedItem.Transfer(Locations[joinedItem.X - 1, joinedItem.Y]);
                        this._CurrentCapsule = Locations[CurrentCapsule.X - 1, CurrentCapsule.Y];
                    }
                    break;
            }
        }

        protected void MoveRight()
        {
            var joinedItem = this.CurrentCapsule.JoinedItem(this);
            if (joinedItem.X == (this.Width - 1) || CurrentCapsule.X == (this.Width - 1))
                return;

            switch (this.CurrentCapsule.Item.JoinDirection)
            {
                case JoinDirection.RIGHT:
                case JoinDirection.LEFT:
                    if (Locations[joinedItem.X + 1, joinedItem.Y].IsEmpty)
                    {
                        joinedItem.Transfer(Locations[joinedItem.X + 1, joinedItem.Y]);
                        CurrentCapsule.Transfer(Locations[CurrentCapsule.X + 1, CurrentCapsule.Y]);
                        this._CurrentCapsule = Locations[CurrentCapsule.X + 1, CurrentCapsule.Y];
                    }
                    break;
                case JoinDirection.UP:
                    if (Locations[joinedItem.X + 1, joinedItem.Y].IsEmpty && Locations[CurrentCapsule.X + 1, CurrentCapsule.Y].IsEmpty)
                    {
                        joinedItem.Transfer(Locations[joinedItem.X + 1, joinedItem.Y]);
                        CurrentCapsule.Transfer(Locations[CurrentCapsule.X + 1, CurrentCapsule.Y]);
                        this._CurrentCapsule = Locations[CurrentCapsule.X + 1, CurrentCapsule.Y];
                    }
                    break;
            }
        }

        protected void MoveDown()
        {
            TimerPushCapsuleDown.Enabled = false;
            if (CurrentCapsule != null)
            {
                var joinedItem = CurrentCapsule.JoinedItem(this);
                if (CurrentCapsule.Y == (this.Height - 1))
                {
                    LockCapsule();
                    return;
                }
                
                switch (CurrentCapsule.Item.JoinDirection)
                {
                    case JoinDirection.UP:

                        if (Locations[CurrentCapsule.X, CurrentCapsule.Y + 1].IsEmpty)
                        {
                            CurrentCapsule.Transfer(Locations[CurrentCapsule.X, CurrentCapsule.Y + 1]);
                            joinedItem.Transfer(Locations[joinedItem.X, joinedItem.Y + 1]);
                            this._CurrentCapsule = Locations[CurrentCapsule.X, CurrentCapsule.Y + 1];
                            this.TimerPushCapsuleDown.Enabled = true;
                        }
                        else
                        {
                            LockCapsule();
                        }
                        break;
                    case JoinDirection.LEFT:
                    case JoinDirection.RIGHT:
                        if (Locations[CurrentCapsule.X, CurrentCapsule.Y + 1].IsEmpty && Locations[joinedItem.X, joinedItem.Y + 1].IsEmpty)
                        {

                            joinedItem.Transfer(Locations[joinedItem.X, joinedItem.Y + 1]);
                            CurrentCapsule.Transfer(Locations[CurrentCapsule.X, CurrentCapsule.Y + 1]);
                            this._CurrentCapsule = Locations[CurrentCapsule.X, CurrentCapsule.Y + 1];
                            this.TimerPushCapsuleDown.Enabled = true;
                        }
                        else
                        {
                            LockCapsule();
                        }
                        break;
                }
            }
        }
        #endregion


        private void CheckFor4()
        {
            LocationsMarkedForDeath.Clear();

            lock (IsPainting)
            {
                try
                {
                    for (int y = 1; y < this.Height - 1; y++)
                        for (int x = 0; x < this.Width; x++)
                        {
                            if (Locations[x, y].IsEmpty)
                                continue;

                            vColors thisColor = Locations[x, y].Item.Color;

                            if (x < this.Width - 3)
                            {
                                if (!Locations[x + 1, y].IsEmpty & !Locations[x + 2, y].IsEmpty && !Locations[x + 3, y].IsEmpty)
                                {
                                    if (thisColor == Locations[x + 1, y].Item.Color && thisColor == Locations[x + 2, y].Item.Color && thisColor == Locations[x + 3, y].Item.Color)
                                    {
                                        int x1 = x;
                                        while (x1 < this.Width)
                                        {
                                            if (!Locations[x1, y].IsEmpty && Locations[x1, y].Item.Color == thisColor)
                                            {
                                                if (!LocationsMarkedForDeath.Contains(Locations[x1, y]))
                                                {
                                                    LocationsMarkedForDeath.Add(Locations[x1, y]);
                                                    this.LocationsDying = true;
                                                }
                                            }
                                            else break;
                                            x1++;
                                        }
                                    }
                                }
                            }
                            if (y < this.Height - 3)
                            {
                                if (!Locations[x, y + 1].IsEmpty && !Locations[x, y + 2].IsEmpty && !Locations[x, y + 3].IsEmpty)
                                {
                                    if (thisColor == Locations[x, y + 1].Item.Color && thisColor == Locations[x, y + 2].Item.Color && thisColor == Locations[x, y + 3].Item.Color)
                                    {
                                        int y1 = y;
                                        while (y1 < this.Height)
                                        {
                                            if (!Locations[x, y1].IsEmpty && thisColor == Locations[x, y1].Item.Color)
                                            {
                                                if (!LocationsMarkedForDeath.Contains(Locations[x, y1]))
                                                {
                                                    LocationsMarkedForDeath.Add(Locations[x, y1]);
                                                    this.LocationsDying = true;
                                                }
                                                y1++;
                                            }
                                            else break;
                                        }
                                    }
                                }
                            }
                        }
                    this.TimerDeathFrame.Enabled = true;
                }
                catch (Exception er) { DeO(er); }
            }
        }

        private bool DropFreeCapsules()
        {
            bool somethingFell = false;
            lock (IsPainting)
            {
                try
                {
                    for (int y = this.Height - 2; y > 0; y--)
                    {
                        for (int x = 0; x < this.Width; x++)
                        {
                            Location l = Locations[x, y];
                            if (!l.IsEmpty && !l.Item.Locked)
                            {
                                Location ji = null;
                                if (l.Item.Joined)
                                {
                                    ji = l.JoinedItem(this);
                                    // if vertical
                                    if (l.Item.JoinDirection == JoinDirection.UP)
                                    {
                                        Location tempVerticalLocation = l;
                                        Location tempVertialJoinedItemLocation = ji;
                                        if (tempVerticalLocation.Y < this.Height - 1 && Locations[tempVerticalLocation.X, tempVerticalLocation.Y + 1].IsEmpty)
                                        {
                                            tempVerticalLocation.Transfer(Locations[tempVerticalLocation.X, tempVerticalLocation.Y + 1]);
                                            tempVertialJoinedItemLocation.Transfer(tempVerticalLocation);
                                            tempVerticalLocation = Locations[tempVerticalLocation.X, tempVerticalLocation.Y + 1];
                                            tempVertialJoinedItemLocation = tempVerticalLocation;
                                            somethingFell = true;
                                        }
                                    }
                                    // horizontal
                                    else
                                    {
                                        Location tempHorizontalLocation = l;
                                        Location tempHorizontalJoinedLocation = ji;
                                        if (tempHorizontalLocation.Y < this.Height - 1 && Locations[tempHorizontalLocation.X, tempHorizontalLocation.Y + 1].IsEmpty && Locations[tempHorizontalJoinedLocation.X, tempHorizontalJoinedLocation.Y + 1].IsEmpty)
                                        {
                                            tempHorizontalLocation.Transfer(Locations[tempHorizontalLocation.X, tempHorizontalLocation.Y + 1]);
                                            tempHorizontalJoinedLocation.Transfer(Locations[tempHorizontalJoinedLocation.X, tempHorizontalJoinedLocation.Y + 1]);
                                            tempHorizontalLocation = Locations[tempHorizontalLocation.X, tempHorizontalLocation.Y + 1];
                                            tempHorizontalJoinedLocation = Locations[tempHorizontalJoinedLocation.X, tempHorizontalJoinedLocation.Y + 1];
                                            somethingFell = true;
                                        }
                                    }
                                }
                                else
                                {
                                    if (l.Y < this.Height - 1 && Locations[l.X, l.Y + 1].IsEmpty)
                                    {
                                        l.Transfer(Locations[l.X, l.Y + 1]);
                                        l = Locations[l.X, l.Y + 1];
                                        somethingFell = true;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception er) { DeO(er); }
            }
            return somethingFell;
        }

        private void LockCapsule()
        {
            // First clear the 'top' as it doesn't count towards kills
            for (int yKill = 0; yKill < this.Width; yKill++)
            {
                try
                {
                    if (!this.Locations[yKill, 0].IsEmpty)
                    {
                        this.Locations[yKill, 1].Item.Joined = false;
                        this.Locations[yKill, 0].Item = null;
                    }
                }
                catch (NullReferenceException) { }
            }
            this._CurrentCapsule = null;
            List<vColors> bombs = new List<vColors>();
            CheckFor4();
            if (LocationsMarkedForDeath.Count > 0)
            {
                bombs.AddRange(LocationsMarkedForDeath.GroupBy(u => new { u.X, u.Item.Color }).Where(u => u.Count() >= 4).Select(u => u.Key.Color));
                bombs.AddRange(LocationsMarkedForDeath.GroupBy(u => new { u.Y, u.Item.Color }).Where(u => u.Count() >= 4).Select(u => u.Key.Color));
                TimerDeathFrame.Enabled = true;
            }
            else
                TimerNextCapsule.Enabled = true;
        }

        static void DeO(Exception ee)
        {
            System.Diagnostics.Debug.WriteLine(ee);
            System.Diagnostics.Debug.WriteLine(ee.StackTrace);
        }

        void TimerInitialVirusFiller_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (initialVirusMax != 0)
            {
                lock (IsPainting)
                {
                    try
                    {
                        AddVirus();
                        this.TimerInitialVirusFiller.Enabled = true;
                    }
                    catch (Exception er)
                    {
                        DeO(er);
                    }
                }
            }
            else if(!this.Filled)
            {
                this.TimerInitialVirusFiller.Enabled = false;
                this.Filled = true;
                this.BottleReadyCall();
            }
            else this.TimerInitialVirusFiller.Enabled = false;
                
        }

        public void FillBottle(int? randomSeed)
        {
            lock (IsPainting)
            {
                lock (this.InputLock)
                {
                    //if (this.BorderTexture == null)
                    //    CreateBorder();
                    try
                    {
                        this.TimerPushCapsuleDown.Enabled = false;
                        this.TimerNextCapsule.Enabled = false;
                        this.LocationsDying = false;
                        this.LocationsMarkedForDeath.Clear();
                        this.Filled = false;

                        this._CurrentCapsule = null;
                        this.capsuleGenerator = new CapsuleGenerator(randomSeed);
                        this.capsuleGenerator.Next(out nextCapsule[0], out nextCapsule[1]);

                        int bottleHeight = this.Height - 1;// Math.Abs(bottleHeight);
                        int bottleWidth = this.Width;// Math.Abs(bottleWidth);
                        initialRandom = new Random(randomSeed.HasValue ? randomSeed.Value : Convert.ToInt32(DateTime.Now.Ticks & 0xFFFF));

                        // Initialize location array
                        for (int y = 0; y < Height; y++)
                        {
                            for (int x = 0; x < Width; x++)
                            {
                                Locations[x, y] = new Location(vColors.NULL, x, y);
                            }
                        }
                        this._CurrentCapsule = null;

                        this.initialVirusMax = (ushort)(4 + (this.CurrentLevel * 4));

                        double[] seedVirusValues = { 0.9735, 0.875, 0.8125, 0.75, 0.6875, 0.625, 0.5625, 0.5, 0.4375, 0.375, 0.3125, 0.25, 0.1875, 0.125, 0.0625 };
                        int enumerator;
                        // Holds height cap of virii by percentage.
                        Dictionary<int, double> levelCap = new Dictionary<int, double>();
                        // 0 ~ 14 are the same
                        for (enumerator = 0; enumerator < 15; enumerator++)
                            levelCap.Add(enumerator, 0.5625);
                        // Then it goes up by 1/8 for every two levels until bottleheight - 3 is reached;
                        double percentage = 0.6875;
                        int relativeHeight = 0;
                        for (enumerator = 15; enumerator <= this.CurrentLevel && relativeHeight <= (bottleHeight - 4); enumerator++)
                        {
                            levelCap.Add(enumerator, percentage);
                            enumerator++;
                            levelCap.Add(enumerator, percentage);
                            percentage += 0.0625;
                            relativeHeight = Convert.ToInt32(bottleHeight * percentage);
                        }
                        // If we haven't hit the 'level' yet, add up to it manually, because the cap has been hit.
                        while (enumerator <= this.CurrentLevel)
                            levelCap.Add(enumerator++, percentage);
                        // Set Maximum 'random' number, and maximum relative height;
                        relativeHeight = Convert.ToInt32(levelCap[this.CurrentLevel] * bottleHeight) - 1;
                        this.minimumLocation = (bottleHeight - relativeHeight) * this.Width;
                        // Contains the color of the virus.
                        Dictionary<int, int> maxHeights = new Dictionary<int, int>();
                        foreach (var d in levelCap)
                            maxHeights.Add(d.Key, Convert.ToInt32(Math.Floor(d.Value * bottleHeight)));

                        //TimerInitialVirusFiller.Enabled = true;
                        while (!this.Filled)
                            this.TimerInitialVirusFiller_Elapsed(this, null);
                    }
                    catch (Exception er)
                    { DeO(er); }
                }
            }
        }
        private int minimumLocation = 0;
        private void AddVirus()
        {
            // Counter that makes sure we don't get three viruses in a row.
            int colorSanity = 0;
            int x, y;
            vColors[] virusColors = new vColors[] { vColors.R, vColors.B, vColors.Y };


            initialVirusColorContainer = ((initialVirusColorContainer + 1) % 3);
            vColors gItem = virusColors[initialVirusColorContainer];

        ReSeed:
            // Initialize random number

            // Bring 'down' the height of the virus if it is above maximum height;

            // Get grid location, and check validity of it. 
            //    int minimumLocation = this.Width * 4;
            int gridSpot = initialRandom.Next(minimumLocation, (this.Height - 1) * this.Width) - 1;
            x = gridSpot % this.Width;// initialRandom.Next(0, this.Width - 1);
            y = gridSpot / this.Width;// initialRandom.Next(5, this.Height - 1);

            Increment:
            do
            {
                x++;
                if (x == this.Width)
                { x = 0; y++; }
            } while (y < this.Height && !Locations[x, y].IsEmpty);


            // Check for overflow
            if (y == this.Height)
                goto ReSeed;

            colorSanity = 0;

            // Look up
            if (Locations[x, y - 1].Color == gItem)
            {
                colorSanity = colorSanity | 0x01;//++;
                if (Locations[x, y - 2].Color == gItem)
                {
                    goto Increment;
                }
            }
            // Look down if applicable
            if (y < this.Height - 1 && Locations[x, y + 1].Color == gItem)
            {
                colorSanity = colorSanity | 0x02;
                if (y < this.Height - 2 && Locations[x, y + 2].Color == gItem)
                {
                    goto Increment;
                }
            }
            // Look left if applicable
            if (x > 0 && gItem == Locations[x - 1, y].Color)
            {
                colorSanity = colorSanity | 0x04;
                if (x > 1 && gItem == Locations[x - 2, y].Color)
                {
                    goto Increment;
                }
            }
            // Look right if applicable
            if (x < this.Width - 2 && gItem == Locations[x + 1, y].Color)
            {
                colorSanity = colorSanity | 0x08;
                if (x < this.Width - 3 && gItem == Locations[x + 2, y].Color)
                {
                    goto Increment;
                }
            }
            if ((colorSanity & 0x0C) == 0x0C || (colorSanity & 0x03) == 0x03)
            {
                goto Increment;
            }
            Locations[x, y] = new Location(gItem, x, y);
            initialVirusMax--;
        }

        private List<System.Timers.Timer> PausedTimers = new List<System.Timers.Timer>();
        private bool paused = false;
        public void Pause(bool pause)
        {
           // System.Diagnostics.Debug.WriteLine(string.Format("Pause:{0}, form.Pause:{1}  {2}", pause, Program.form.Paused, DateTime.Now.Ticks));
                    PausedTimers.Add(TimerDeathFrame);
                    PausedTimers.Add(TimerDropFreeCapsules);
                    PausedTimers.Add(TimerPushCapsuleDown);
                    PausedTimers.Add(TimerNextCapsule);
                    PausedTimers.Add(TimerInitialVirusFiller);
          
            foreach (var t in PausedTimers) t.Enabled = !pause;
        }
    }
}