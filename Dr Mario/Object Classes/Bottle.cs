using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using SlimDX;
using Color = System.Drawing.Color;
using Engine = Dr_Mario.Form_Classes.Engine;

namespace Dr_Mario.Object_Classes
{
    public class Bottle : IControllerInput, IDisposable
    {
        public static ShaderResourceView BorderTexture;
        public static SlimDX.Direct2D.Brush RedBrush;
        public static SlimDX.Direct2D.Brush BlueBrush;
        public static SlimDX.Direct2D.Brush YellowBrush;

        public void Dispose()
        {
            if (this.RainbowBrush != null)
                this.RainbowBrush.Dispose();
        }

        public delegate void BombLaunchEventHandler(object sender, BombLaunchEventArgs e);
        public event BombLaunchEventHandler BombLauncher;
        private void BombLaunch()
        {
            Form_Classes.SoundManager.Play(@".\Sounds\combo.wav");
            switch (this.BombMode)
            {
                case Object_Classes.BombMode.Saint:
                    return;
                case Object_Classes.BombMode.All_Down:
                case Object_Classes.BombMode.All_Up:
                case Object_Classes.BombMode.Auto:
                case Object_Classes.BombMode.By_Color:
                case Object_Classes.BombMode.Salt_And_Pepper:
                    if (this.BombLauncher != null)
                        this.BombLauncher(this, new BombLaunchEventArgs(this.PlayerIndex, this.BombMode, this.BombsOutgoing.Dequeue()));
                    break;
            }
        }

        public delegate void GameOverEventHandler(object sender, GameOverEventArgs e);
        public event GameOverEventHandler GameOver;
        private void GameOverCall(GameOverReason reason)
        {
            if (!this.IsActive)
                return;
            
       if(reason == GameOverReason.VirusClear && this.PlayMode != PlayMode.Multiplayer)
       {
                        if (Convert.ToInt32(this.Player.Settings["MaxLevel"].Value) < this.CurrentLevel)
                            this.Player.Settings["MaxLevel"].Value = this.CurrentLevel.ToString();
           this.PlayerMenu.CurrentLevel++;

       }
            if (GameOver != null)
                GameOver(this, new GameOverEventArgs(reason));
            this.IsPlaying = false;


        }
        public SlimDX.Direct2D.RadialGradientBrush RainbowBrush;

        /// <summary>
        /// Triggered when the bottle is finished filling with virii.
        /// </summary>
        public event EventHandler BottleReady;
        private void BottleReadyCall()
        {
            if (BottleReady != null)
                BottleReady(this, EventArgs.Empty);
        }
        private BombMode BombMode { get; set; }
        public PlayMode PlayMode { get; set; }
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
        private System.Timers.Timer TimerCountdownStart;
        #endregion
        private int DeathFrameCounter = 0;
        private List<Location> LocationsMarkedForDeath = new List<Location>();
        private bool LocationsDying = false;
        private object BottleLock = new object();
        private bool _InputReady = true;
        public bool InputReady { get { lock (this.BottleLock) { return ((this.CurrentCapsule != null && this._InputReady) || (this.PlayMode == Object_Classes.PlayMode.Menu && this.PlayerMenu.InputReady)); } } }
        public bool Filled { get; private set; }
        private Location _CurrentCapsule = null;
        private Capsule[] nextCapsule = new Capsule[2];

        private Queue<vColors[]> BombsIncoming;
        private Queue<vColors[]> BombsOutgoing;
        private List<vColors> CurrentBomb;

        public bool IsPlaying { get; private set; }
        public bool IsReady { get { return this.PlayerMenu.ReadyToStart; } }
        /// <summary>
        /// The 'master' active bit.
        /// If this is not active, no events 
        /// will fire, and no drawing will 
        /// take place.
        /// </summary>
        public bool IsActive { get; set; }

        private ushort initialVirusMax;
        private Random initialRandom;
        // First instance is randomized
        private int initialVirusColorContainer;
        private int _CurrentLevel;
        public int CurrentLevel { get { return this._CurrentLevel; } set { this._CurrentLevel = value; } }

        private Dr_Mario.Data.Player _Player;
        private Dr_Mario.Data.Player Player { get { return this._Player; } set { this._Player = value; this.BorderColor = value.Settings.Color; } }
        public Form_Classes.PlayerMenu PlayerMenu { get; private set; }


        private System.Drawing.Color BorderColor { get; set; }

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
                float spriteHeight = spriteWidth * ((float)Engine.Width / (float)Engine.Height);
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

        public void TakeBomb(vColors[] fragments)
        {
            lock (this.BottleLock)
            {
                this.BombsIncoming.Enqueue(fragments);
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

        public int PlayerIndex { get; protected set; }

        public Bottle(int playerIndex)
        {
            this.PlayerIndex = playerIndex;
            this.PlayerMenu = new Form_Classes.PlayerMenu(this, this.PlayerIndex);

            if (this.PlayerIndex == 1)
                this.IsActive = true;

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

            TimerCountdownStart = new System.Timers.Timer();
            TimerCountdownStart.AutoReset = true;
            TimerCountdownStart.Interval = 1000;
            TimerCountdownStart.Enabled = false;
            TimerCountdownStart.Elapsed += TimerCountdownStart_Elapsed;

            this.CurrentBomb = new List<vColors>();
            this.BombsOutgoing = new Queue<vColors[]>();
            this.BombsIncoming = new Queue<vColors[]>();

            //this.IsActive = true;
            this.IsPlaying = false;
            this.PlayMode = Object_Classes.PlayMode.Menu;
            this.BorderColor = Color.Gray;
        }

        internal void PlayerMenu_PlayerReady(object sender, EventArgs e)
        {
            lock (this.BottleLock)
            {
                this.CurrentLevel = this.PlayerMenu.CurrentLevel;
                this.CapsuleDropSpeed(this.PlayerMenu.Speed);
                this.Player = this.PlayerMenu.Player;
                this.IsActive = true;
            }
        }

     

        private int TimerPushCapsuleDown_Incrementor = 0;
        private DateTime TimerPushCapsuleDownSignal;
        void TimerPushCapsuleDown_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (this.BottleLock)
            {
                this.TimerPushCapsuleDownSignal = e.SignalTime;
                // System.Diagnostics.Debug.WriteLine(this._CurrentCapsule ?? (object)"currently null");
                if (this.CurrentCapsule != null)
                {
                    this._InputReady = false;
                    try
                    {
                        MoveDown();
                    }
                    catch (NullReferenceException nre)
                    {
                        System.Diagnostics.Debug.WriteLine("Exception in Push Down.");
                        System.Diagnostics.Debug.WriteLine(nre);

                        System.Diagnostics.Debugger.Break();
                    }

                    this._InputReady = true;
                }
                else
                    ((System.Timers.Timer)sender).Enabled = false;
            }
        }

        void TimerDropFreeCapsules_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (this.BottleLock)
            {
                if (!this.InputReady)
                {
                    if (!DropFreeCapsules())
                    {
                        ((System.Timers.Timer)sender).Enabled = false;
                        LockCapsule();
                    }
                    else
                    {
                        if (DroppableCapsules())
                            Form_Classes.SoundManager.Play(@".\Sounds\dropremains.wav");
                        else
                            Form_Classes.SoundManager.Play(@".\Sounds\land_two.wav");
                    }
                }
                else
                {
                    ((System.Timers.Timer)sender).Enabled = false;
                }
            }
        }

        void TimerNextCapsuleTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (this.BottleLock)
            {
                TimerNextCapsule.Enabled = false;
                
                if (this.CurrentBomb.Count > 2)
                {
                    this.BombsOutgoing.Enqueue(this.CurrentBomb.ToArray());
                }
                this.CurrentBomb.Clear();
                
                if (this.CurrentCapsule == null)
                {
                    if (this.BombsIncoming.Count == 0)
                        nextCapsuleDrop();
                    else
                        BombDeQueue();
                }
            }
        }

        private void BombDeQueue()
        {
            var bombs = this.BombsIncoming.Dequeue();
            Random bombDropper = new Random();
            var bottleSlice = (1.0 / bombs.Length) * this.Width; //2 bombs = 1/2 the field, 4 = 1/4, and so on.
            for (int bi = 0; bi < bombs.Length; bi++)
            {
                var start = Convert.ToInt32(bi * bottleSlice);
                var end = Convert.ToInt32((bi + 1) * bottleSlice);
                Locations[bombDropper.Next(start, end), 1].Item = new Capsule(bombs[bi]);
            }
            this._InputReady=false;
            this.TimerDropFreeCapsules.Enabled = true;
        }

        public void Start(bool multiplayer)
        {
            if (!this.IsActive)
                return;
            if (multiplayer)
            {
                this.PlayMode = Object_Classes.PlayMode.Multiplayer;
                this.BeginCountdown = true;
                this.CountdownValue = 3;
                this.TimerCountdownStart.Enabled = true;
            }
            else
            {
                this.PlayMode = Object_Classes.PlayMode.SinglePlayer;
                this.BeginCountdown = false;
                this.CountdownValue = 0;
                this.TimerCountdownStart.AutoReset = false;
                this.TimerCountdownStart.Enabled = true;
                
                //CapsuleStart();
            }
        }
        void TimerCountdownStart_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (this.BottleLock)
            {
                this.CountdownValue--;
                if (this.CountdownValue <= 0)
                {
                    this.TimerCountdownStart.Enabled = false;
                    this.CapsuleStart();
                }
            }
        }

        internal void InitializeFromPlayerMenu(int seed, int playerCount)
        {
            this.FillBottle(seed);
            this.PlayMode = (playerCount == 1) ? Object_Classes.PlayMode.SinglePlayer : Object_Classes.PlayMode.Multiplayer;
        }

        private void CapsuleStart()
        {
            nextCapsuleDrop();
            this.IsPlaying = true;
        }

        private int ScoreMultiplier { get; set; }

        void nextCapsuleDrop()
        {
            this.ScoreMultiplier = 1;
            if (Locations[3,1]!= null && Locations[4,1] != null && Locations[3, 1].IsEmpty && Locations[4, 1].IsEmpty)
            {
                TimerNextCapsule.Enabled = false;
                Capsule a = null, b = null;
                capsuleGenerator.Next(out a, out b);

                Locations[3, 1].Item = nextCapsule[0];
                Locations[4, 1].Item = nextCapsule[1];
                nextCapsule[0] = a;
                nextCapsule[1] = b;
                this._CurrentCapsule = Locations[3, 1];

                this.TimerPushCapsuleDown_Incrementor++;
                if (TimerPushCapsuleDown_Incrementor % 10 == 0)
                {
                    var interval = TimerPushCapsuleDown.Interval - Math.Ceiling(TimerPushCapsuleDown.Interval * 0.02);
                    if (interval > 0)
                        TimerPushCapsuleDown.Interval = interval;
                    if (this.NextMoveBlockSlide > 10)
                        this.NextMoveBlockSlide--;
                    Form_Classes.SoundManager.Play(@".\Sounds\ten_pills.wav");
                }

                this.TimerPushCapsuleDown.Enabled = true;
            }
            else
            {
                GameOverCall(GameOverReason.EntranceCollision);
            }
        }

        private SlimDX.Vector2 LastDrawLocation { get; set; }
        public void Draw(SlimDX.Vector2 location, int frame, Object_Classes.GameMode gameMode)
        {

            lock (this.BottleLock)
            {
                if (this.PlayMode == Object_Classes.PlayMode.Menu)
                {
                    this.PlayerMenu.Draw(location);
                }
                else
                {
                    this.LastDrawLocation = location;
                    float spriteWidth = 2f / 36f;
                    float spriteHeight = spriteWidth * ((float)Engine.Width / (float)Engine.Height);

                    try
                    {
                 //       BuildRainbowBrush();
                        SlimDX.Vector2 iVector = new SlimDX.Vector2(location.X + (spriteWidth * 0.5f), location.Y - (spriteHeight * 0.5f));
                        SlimDX.Vector2 iSize = new SlimDX.Vector2(spriteWidth, spriteHeight);
                        #region Bottle 
                        //var sr = Program.spriteRenderer;
                        try
                        {
                            // Borders
                            //left
                            Form_Classes.Engine.DrawSprite(Bottle.BorderTexture, location, new Vector2(0.5f, (spriteHeight * this.Height) + (spriteHeight * 0.75f)), this.BorderColor);
                        }
                        catch (NullReferenceException nreb)
                        {
                            System.Diagnostics.Debug.WriteLine("Null writing border.");
                            System.Diagnostics.Debug.WriteLine(nreb);
                        }
                        #endregion

                        #region Virii and Capsules
                        // Bottle Items
                        try
                        {
                            for (int y = 1; y < 17; y++)
                            {
                                for (int x = 0; x < 8; x++)
                                {
                                    if (Locations[x, y] == null) { Locations[x, y] = new Location(vColors.NULL, x, y); }
                                    if (!Locations[x, y].IsEmpty)
                                        Form_Classes.Engine.DrawSprite(Locations[x, y].ShaderResourceView(frame),
                                            new SlimDX.Vector2(iVector.X + (spriteWidth * x), iVector.Y - (y * spriteHeight)),
                                            iSize);
                                }
                            }
                        }
                        catch (NullReferenceException nrei)
                        {
                            System.Diagnostics.Debug.WriteLine("Null writing items.");
                            System.Diagnostics.Debug.WriteLine(nrei);
                        }
                        #endregion

                        #region Initial Countdown
                        if (this.BeginCountdown && this.CountdownValue != 0)
                        {
                            SlimDX.Direct2D.Brush brush = Engine.WhiteBrush;
                            switch (this.CountdownValue)
                            {
                                case 3: brush = Bottle.RedBrush;
                                    break;
                                case 2: brush = Bottle.BlueBrush;
                                    break;
                                case 1: brush = Bottle.YellowBrush;
                                    break;
                            }

                            Engine.DrawText(new DrawTextStruct(Engine.TextRenderers.LargeCountdown,
                                SlimDX.DirectWrite.TextAlignment.Center,
                                new Rectangle((int)((this.LastDrawLocation.X + 1) / 2 * Engine.Width),
                                    (int)((this.LastDrawLocation.Y - 1) * Height * -0.5),
                                    Engine.Width / 4,
                                    (int)(Engine.Height * 0.9)),
                                    brush,
                                    CountdownValue.ToString()));
                        }
                        #endregion

                        #region Next Capsule
                        try
                        {
                            if (nextCapsule[0] != null && nextCapsule[1] != null)
                            {
                                Form_Classes.Engine.DrawSprite(nextCapsule[0].Image, new Vector2(location.X + spriteWidth * 3.5f, location.Y - spriteHeight * 0.40f), new Vector2(spriteWidth, spriteHeight));
                                Form_Classes.Engine.DrawSprite(nextCapsule[1].Image, new Vector2(location.X + spriteWidth * 4.5f, location.Y - spriteHeight * 0.40f), new Vector2(spriteWidth, spriteHeight));
                            }
                        }
                        catch (NullReferenceException)
                        {
                        }
                        #endregion

                        #region Footer Text
                        Form_Classes.Engine.DrawText(string.Format("LVL:{0:00}", this.CurrentLevel), new Vector2(iVector.X, iVector.Y - (this.Height * spriteHeight + spriteHeight * 0.3f)), SlimDX.DirectWrite.TextAlignment.Leading);
                        Engine.DrawText(
                            string.Format("LEFT:{0:00}", this.Viruses),
                            new System.Drawing.Rectangle(
                                Convert.ToInt32((location.X + 1) * Engine.Width * 0.5), 
                                Convert.ToInt32(((iVector.Y - (this.Height * spriteHeight + spriteHeight * 0.3f)) - 1) * Engine.Height * -0.5), 
                                Convert.ToInt32(Engine.Width / 4.2), 
                                Engine.Height / 16),
                            SlimDX.DirectWrite.TextAlignment.Trailing);

                        Form_Classes.Engine.DrawText(string.Format("SCORE:{0:0000000}", this.Score), new Vector2(iVector.X, iVector.Y - (this.Height * spriteHeight + spriteHeight * 1.1f)), SlimDX.DirectWrite.TextAlignment.Leading);
                        // Once I get the rainbow brush working correctly.
                        /*
                        Form_Classes.Engine.DrawText("SCORE:", new Vector2(iVector.X, iVector.Y - (this.Height * spriteHeight + spriteHeight * 1.1f)), SlimDX.DirectWrite.TextAlignment.Leading);

                        Engine.DrawSprite(Form_Classes.Settings.SettingDisplay.whitePixel, new SlimDX.Vector2(this.LastDrawLocation.X + 0.25f, -0.75f), new Vector2(0.2f, 0.2f)); 
                        
                        if (this.RainbowBrush != null)
                            Form_Classes.Engine.DrawText(new DrawTextStruct(SlimDX.DirectWrite.TextAlignment.Leading, new Vector2(iVector.X+0.17f, iVector.Y - (this.Height * spriteHeight + spriteHeight * 1.1f)), this.RainbowBrush, string.Format("{0:0000000}", this.Score)));
                        */

                        #endregion
                    }
                    catch (Exception er)
                    {
                        DeO(er);
                    }
                }
            }
        }
        

        public int Score { get; set; }

        void TimerDeathFrame_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (this.BottleLock)
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
                            if (l.Item != null)
                            {
                                Location joinedItem = l.JoinedItem(this);
                                if (joinedItem != null && !joinedItem.IsEmpty)
                                {
                                    joinedItem.Item.Joined = false;
                                }
                                l.Item = null;
                            }
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
        }

        #region Movement logic

        private bool InputIsCapsuleMovement(Movement command)
        { return (command & (Movement.Left | Movement.Right | Movement.Down)) != 0x0; }



        private int InitialMoveBlockSlide = 150;
        private int NextMoveBlockSlide = 100;
        private int NextMoveBlockSlideDown = 30;
        private DateTime ControllerPadMovementLastPass = DateTime.Now;
        private DateTime? ControllerPadMovementStartTime = null;
        private DateTime ControllerPadMovementNextSlide;
        //  private long NextRotateAllowedAt = 0;
        protected Movement lastMoveCommand;
        protected Movement lastRotateCommand;
        public virtual void Input(Movement command)
        {
            lock (this.BottleLock)
            {
                if (this.PlayMode == Object_Classes.PlayMode.Menu)
                    this.PlayerMenu.Input(command);
                else
                {
                    if (!this._InputReady)
                        return;
                    this._InputReady = false;
                    try
                    {
                        if (!this.ControllerPadMovementLastPass.Equals(DateTime.Now))
                        {
                            this.ControllerPadMovementLastPass = DateTime.Now;
                            if ((lastRotateCommand | lastMoveCommand | command) != Movement.None)
                            {
                                if (this.CurrentCapsule != null)
                                {
                                    #region Capsule Movement
                                    if (this.InputIsCapsuleMovement(command))
                                    {
                                        if ((command & (Movement.Down | Movement.Left | Movement.Right)) != this.lastMoveCommand)
                                        {
                                            this.ControllerPadMovementStartTime = this.ControllerPadMovementNextSlide = DateTime.Now;
                                        }
                                        if (this.ControllerPadMovementStartTime.HasValue &&
                                            this.ControllerPadMovementNextSlide <= DateTime.Now &&
                                            (this.ControllerPadMovementStartTime.Equals(DateTime.Now) ||
                                            DateTime.Now.Subtract(this.ControllerPadMovementStartTime.Value).TotalMilliseconds > this.InitialMoveBlockSlide))
                                        {
                                            this.ControllerPadMovementNextSlide = DateTime.Now.AddMilliseconds(((command & Movement.Down) != Movement.None) ? this.NextMoveBlockSlideDown : this.NextMoveBlockSlide);
                                            if ((command & Movement.Down) != Movement.None)
                                            {
                                                TimerPushCapsuleDown.Enabled = false;
                                                if (DateTime.Now.Subtract(this.TimerPushCapsuleDownSignal).TotalMilliseconds > 150)
                                                {
                                                    MoveDown();
                                                }
                                            }
                                            else if ((command & Movement.Left) != Movement.None)
                                            {
                                                MoveLeft();
                                            }
                                            else if ((command & Movement.Right) != Movement.None)
                                            {
                                                MoveRight();
                                            }
                                        }
                                        this.lastMoveCommand = (command & (Movement.Down | Movement.Left | Movement.Right));
                                    }
                                    #endregion Capsule Movement

                                    if ((command & Movement.Clockwise) == Movement.Clockwise)
                                    {
                                        if (this.lastRotateCommand != (command & Movement.Clockwise))
                                            RotateClockwise();
                                        this.lastRotateCommand = Movement.Clockwise;
                                    }
                                    else if ((command & Movement.CounterClockwise) == Movement.CounterClockwise)
                                    {
                                        if (this.lastRotateCommand != (command & Movement.CounterClockwise))
                                            RotateCounterClockwise();
                                        this.lastRotateCommand = Movement.CounterClockwise;
                                    }
                                    else if ((command & Movement.NoRotate) == Movement.NoRotate)
                                    {
                                        this.lastRotateCommand = Movement.None;
                                    }

                                    if (command == Movement.None)
                                    {
                                        this.lastMoveCommand = Movement.None;
                                        this.lastRotateCommand = Movement.None;

                                    }
                                }
                            }
                            else
                            {
                                this.lastMoveCommand = Movement.None;
                                this.lastRotateCommand = Movement.None;

                            }

                        }
                        // Disable dropping capsules while down is pushed.  Once its released, it may resume.
                        if (!this.TimerPushCapsuleDown.Enabled && (this.lastMoveCommand & Movement.Down) != Movement.Down)
                            this.TimerPushCapsuleDown.Enabled = true;

                    }
                    catch (NullReferenceException nre)
                    {
                        System.Diagnostics.Debug.WriteLine("Exception in Input.");
                        System.Diagnostics.Debug.WriteLine(nre);
                        System.Diagnostics.Debugger.Break();
                    }
                    this._InputReady = true;
                }
            }
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
                        Form_Classes.SoundManager.Play(@".\Sounds\flip.wav");

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
                        Form_Classes.SoundManager.Play(@".\Sounds\flip.wav");

                    }
                    else if (Locations[joinedItem.X, joinedItem.Y - 1].IsEmpty)
                    {
                        CurrentCapsule.Item.JoinDirection = JoinDirection.UP;
                        joinedItem.Item.JoinDirection = JoinDirection.DOWN;
                        joinedItem.Transfer(Locations[joinedItem.X, joinedItem.Y - 1]);
                        CurrentCapsule.Transfer(joinedItem);
                        this._CurrentCapsule = joinedItem;
                        Form_Classes.SoundManager.Play(@".\Sounds\flip.wav");
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
                        Form_Classes.SoundManager.Play(@".\Sounds\flip.wav");
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
                            Form_Classes.SoundManager.Play(@".\Sounds\flip.wav");
                            // current capsule location does not change.
                        }
                        // Else slide left one block
                        else if (Locations[CurrentCapsule.X - 1, CurrentCapsule.Y].IsEmpty)
                        {
                            joinedItem.Item.JoinDirection = JoinDirection.RIGHT;
                            CurrentCapsule.Item.JoinDirection = JoinDirection.LEFT;
                            joinedItem.Transfer(Locations[CurrentCapsule.X - 1, CurrentCapsule.Y]);
                                
                            this._CurrentCapsule = Locations[CurrentCapsule.X - 1, CurrentCapsule.Y];
                            Form_Classes.SoundManager.Play(@".\Sounds\flip.wav");
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
                            Form_Classes.SoundManager.Play(@".\Sounds\flip.wav");
                        }
                        else if (Locations[joinedItem.X, joinedItem.Y - 1].IsEmpty)
                        {
                            joinedItem.Item.JoinDirection = JoinDirection.DOWN;
                            CurrentCapsule.Item.JoinDirection = JoinDirection.UP;
                            joinedItem.Transfer(Locations[joinedItem.X, joinedItem.Y - 1]);
                            CurrentCapsule.Transfer(joinedItem);
                            this._CurrentCapsule = joinedItem;
                            Form_Classes.SoundManager.Play(@".\Sounds\flip.wav");
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
                        Form_Classes.SoundManager.Play(@".\Sounds\flip.wav");

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
                        Form_Classes.SoundManager.Play(@".\Sounds\flip.wav");

                    }
                    else if (Locations[joinedItem.X, joinedItem.Y - 1].IsEmpty)
                    {
                        CurrentCapsule.Item.JoinDirection = JoinDirection.DOWN;
                        joinedItem.Item.JoinDirection = JoinDirection.UP;
                        CurrentCapsule.Transfer(Locations[joinedItem.X, joinedItem.Y - 1]);
                        this._CurrentCapsule = joinedItem;
                        Form_Classes.SoundManager.Play(@".\Sounds\flip.wav");
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
                        Form_Classes.SoundManager.Play(@".\Sounds\flip.wav");
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
                            Form_Classes.SoundManager.Play(@".\Sounds\flip.wav");
                        }
                        // Else slide left one block
                        else if (Locations[CurrentCapsule.X - 1, CurrentCapsule.Y].IsEmpty)
                        {
                            joinedItem.Item.JoinDirection = JoinDirection.LEFT;
                            CurrentCapsule.Item.JoinDirection = JoinDirection.RIGHT;
                            CurrentCapsule.Transfer(Locations[CurrentCapsule.X - 1, CurrentCapsule.Y]);
                            joinedItem.Transfer(CurrentCapsule);
                            this._CurrentCapsule = Locations[CurrentCapsule.X - 1, CurrentCapsule.Y];
                            Form_Classes.SoundManager.Play(@".\Sounds\flip.wav");
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
                            Form_Classes.SoundManager.Play(@".\Sounds\flip.wav");

                        }
                        else if (Locations[joinedItem.X, joinedItem.Y - 1].IsEmpty)
                        {
                            CurrentCapsule.Item.JoinDirection = JoinDirection.DOWN;
                            joinedItem.Item.JoinDirection = JoinDirection.UP;
                            CurrentCapsule.Transfer(Locations[joinedItem.X, joinedItem.Y - 1]);
                            this._CurrentCapsule = joinedItem;
                            Form_Classes.SoundManager.Play(@".\Sounds\flip.wav");
                        }
                        break;
                }
            }
        }

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
            Dr_Mario.Form_Classes.SoundManager.Play(@".\Sounds\move.wav");
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
            Dr_Mario.Form_Classes.SoundManager.Play(@".\Sounds\move.wav");

        }

        protected void MoveDown()
        {
            TimerPushCapsuleDown.Enabled = false;
            if (CurrentCapsule != null && CurrentCapsule.Item != null)
            {
                var joinedItem = CurrentCapsule.JoinedItem(this);
                if (CurrentCapsule.Y == (this.Height - 1))
                {
                    LockCapsule();
                    return;
                }
                if (CurrentCapsule.Item != null)
                {
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
        }
        #endregion


        private void CheckFor4()
        {
                LocationsMarkedForDeath.Clear();
                try
                {
                    for (int y = this.Height-1; y > 0; y--)
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
                    AddScore();
                    this.TimerDeathFrame.Enabled = true;
                }
                catch (Exception er) { DeO(er); }
            }
        

        void AddScore()
        {
            if (LocationsMarkedForDeath.Count() != 0)
            {
                var xGrp = LocationsMarkedForDeath
                    .GroupBy(l => l.X)
                    .Where(grp => grp.Count() > 3)
                    .Select(g => new { g.Key, vCount = g.Where(gg => gg.Item.Locked).Count(), Begin = g.Min(gg => gg.Y), Last = g.Max(gg => gg.Y) });
                
                var yGrp = LocationsMarkedForDeath
                    .GroupBy(l => l.Y)
                    .Where(grp => grp.Count() > 3)
                    .Select(g => new { g.Key, vCount = g.Where(gg => gg.Item.Locked).Count(), Begin = g.Min(gg=>gg.X), Last=g.Max(gg=>gg.X) });

                this.CurrentBomb.AddRange(xGrp.Select(grp => this.Locations[grp.Key, grp.Begin].Color));
                this.CurrentBomb.AddRange(yGrp.Select(grp => this.Locations[grp.Begin, grp.Key].Color));

                //TODO: Sound!

                int tempScore = (
                    xGrp
                    .Where(grp => grp.vCount == 0)
                    .Count() +
                    yGrp
                    .Where(grp => grp.vCount == 0)
                    .Count()) * (100 * ScoreMultiplier);
                tempScore += LocationsMarkedForDeath.Where(l => l.Item.Locked).Count() * (400 * ScoreMultiplier);

                this.Score += tempScore;
                this.ScoreMultiplier += (xGrp.Count() + yGrp.Count());
            }
        }

        private bool DroppableCapsules()
        {
            for (int y = this.Height - 2; y > 0; y--)
            {
                for (int x = 0; x < this.Width; x++)
                    if (Locations[x, y] != null && !Locations[x, y].IsEmpty && !Locations[x, y].IsLocked && Locations[x, y + 1] != null && Locations[x, y + 1].IsEmpty)
                        return true;
            }
            return false;
        }

        private bool DropFreeCapsules()
        {
                bool somethingFell = false;
                try
                {
                    for (int y = this.Height - 2; y > 0; y--)
                    {
                        for (int x = 0; x < this.Width; x++)
                        {
                            Location l = Locations[x, y];
                            if (l != null && !l.IsEmpty && !l.Item.Locked)
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
                return somethingFell;
            }


        protected virtual void LockCapsule(string lockSound = @".\Sounds\pill_land.wav")
        {
            TimerPushCapsuleDown.Enabled = false;
            var playSound = _CurrentCapsule != null || !lockSound.Equals(@".\Sounds\pill_land.wav");
            if (Locations[0, 0] != null)
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
                CheckFor4();
            }
            if (LocationsMarkedForDeath.Count > 0)
            {
                TimerDeathFrame.Enabled = true;
                if (LocationsMarkedForDeath.Any(lm => lm.Item.Locked))
                    Form_Classes.SoundManager.Play(@".\Sounds\defeat.wav");
                Form_Classes.SoundManager.Play(@".\Sounds\clear.wav");
            }
            else
            {
                TimerNextCapsule.Enabled = true;
                if(playSound)
                Form_Classes.SoundManager.Play(lockSound);
            }
        }

  public      static void DeO(Exception ee)
        {
            System.Diagnostics.Debug.WriteLine(ee);
            System.Diagnostics.Debug.WriteLine(ee.StackTrace);
        }

        void TimerInitialVirusFiller_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (this.BottleLock)
            {
                if (initialVirusMax != 0)
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

                else if (!this.Filled)
                {
                    this.TimerInitialVirusFiller.Enabled = false;
                    this.Filled = true;
                    this.BottleReadyCall();
                }
                else this.TimerInitialVirusFiller.Enabled = false;
            }
        }
        /// <summary>
        /// Not working like I want right now, future todo.
        /// </summary>
        private void BuildRainbowBrush()
        {
            if (this.RainbowBrush != null)
                return;

            Color[] colors = new Color[10];
            Random r = new Random();
            for (int i = 0; i < colors.Length; i++)
            {
                var validColors = Form_Classes.Engine.ValidTextColors.Where(vtc => !colors.Contains(vtc));
                colors[i] = validColors.ToArray()[r.Next(0, validColors.Count())];
            }
         


            this.RainbowBrush = (SlimDX.Direct2D.RadialGradientBrush)Form_Classes.Engine.CreateBrush(
                new SlimDX.Vector2(this.LastDrawLocation.X + 0.25f, -0.75f),
                colors);
        }


        private int CountdownValue;
        private bool BeginCountdown = false;

        public virtual void FillBottle(int? randomSeed)
        {
                lock (this.BottleLock)
                {
                    this.CountdownValue = 3;
                    this.BeginCountdown = false;
                    
                    if(this.RainbowBrush!= null)
                        this.RainbowBrush.Dispose();
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

                        TimerInitialVirusFiller.Enabled = true;
                    }
                    catch (Exception er)
                    { DeO(er); }
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
            this.paused = pause;
            foreach (var t in PausedTimers) t.Enabled = !pause;
        }
    }
}