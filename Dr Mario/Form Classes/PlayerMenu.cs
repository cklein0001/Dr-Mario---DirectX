using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX;
using SlimDX.DXGI;
using SlimDX.Direct3D11;
using Movement = Dr_Mario.Object_Classes.Movement;

namespace Dr_Mario.Form_Classes
{
    public class PlayerMenu : Object_Classes.IControllerInput
    {

        #region Static
        private static ShaderResourceView borderView;
        internal static ShaderResourceView arrowUp;
        internal static ShaderResourceView arrowDown;
        private static ShaderResourceView whitePixel;
        internal static ShaderResourceView arrowRight;
       
        #endregion

        public event EventHandler PlayerReady;
        private void BottleReadyCall()
        {
            if (PlayerReady != null)
                PlayerReady(this, EventArgs.Empty);
        }


        public bool Active { get; private set; }
        
        bool PlayerSelected = false;
        bool DrawSettingInterface = false;

        private bool _ReadyToStart;
        public bool ReadyToStart
        {
            get { return this._ReadyToStart; }
            private set
            {
                this._ReadyToStart = value;
                if (value)
                {
                    this.Parent.PlayerMenu_PlayerReady(this, EventArgs.Empty);
                    Player.Settings.Save();
                    BottleReadyCall();
                }
            }
        }

        public int CurrentLevel { get { return this.LevelSelect.CurrentLevel; } set { this.LevelSelect.CurrentLevel = value; } }
        public int Speed { get { return this.GameSpeed.CapsuleSpeed; } }

        public int PlayerIndex { get; private set; }
        Data.Player _currentPlayer;
        public Data.Player Player
        {
            get { return this._currentPlayer; }
            set
            {
                this._currentPlayer = value;
                foreach (var setting in this.PlayerSettings)
                    setting.Load(value.Settings);
            }
        }
        System.Timers.Timer TimerArrowFlash;
        System.Drawing.Color arrowMultiplier = System.Drawing.Color.White;
        #region New Player stuff
        bool DrawNameInterface = false;

        #region Settings
        List<Settings.SettingDisplay> PlayerSettings;
        Settings.SettingDisplay CurrentSetting;
        Settings.LevelSelect LevelSelect;
        Settings.GameSpeed GameSpeed;
        Settings.BorderColor BorderColor;
        Settings.BombMode BombMode;
        Settings.NewPlayer NewPlayer;
        Object_Classes.Bottle Parent;

        #endregion
        #endregion
        public PlayerMenu(Object_Classes.Bottle bottle, int playerIndex)
        {
            this.Parent = bottle;
            this.PlayerIndex = playerIndex;
            TimerArrowFlash = new System.Timers.Timer(650);
            TimerArrowFlash.AutoReset = true;
            TimerArrowFlash.Elapsed += TimerArrowFlash_Elapsed;
            TimerArrowFlash.Enabled = true;
            this.LevelSelect = new Settings.LevelSelect();
            this.GameSpeed = new Settings.GameSpeed();
            this.BorderColor = new Settings.BorderColor();
            this.BombMode = new Settings.BombMode();
            this.NewPlayer = new Settings.NewPlayer(); 
            this.PlayerSettings = new List<Settings.SettingDisplay>();
            this.PlayerSettings.Add(this.LevelSelect);
            this.PlayerSettings.Add(this.GameSpeed);
            this.PlayerSettings.Add(this.BombMode);
            this.PlayerSettings.Add(this.BorderColor);
            this.CurrentSetting = this.PlayerSettings[0];
            this.CurrentSetting.Activate();

            switch (playerIndex)
            {
                case 1:
                    this.Active = true;
                    break;
            }
            Object_Classes.ControllerHandler.StartButtonPressed += this.On_StartButtonPressed;
            // Should be last steps...
            this.Player = Data.Player.Load(1);
            this.InputReady = true;

        }

        void TimerArrowFlash_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (this.arrowMultiplier.Equals(System.Drawing.Color.White))
                this.arrowMultiplier = System.Drawing.Color.Gray;
            else
                this.arrowMultiplier = System.Drawing.Color.White;
        }

        public void Draw(Vector2 drawLocation)
        {

            Engine.DrawSprite(borderView, drawLocation, new Vector2(0.5f, 2f), Player.Settings.Color);
            Engine.DrawText(string.Format("{0}P", this.PlayerIndex), new Vector2(drawLocation.X + 0.03f, drawLocation.Y - 0.02f), SlimDX.DirectWrite.TextAlignment.Leading);
            Engine.DrawText(Player.Name, new Vector2(drawLocation.X + 0.12f, drawLocation.Y - 0.06f), SlimDX.DirectWrite.TextAlignment.Leading);
            if (!this.PlayerSelected)
            {
                Engine.DrawSprite(arrowUp,
                    new Vector2(drawLocation.X + 0.15f, drawLocation.Y - 0.050f),
                    new Vector2(0.05f, 0.03f), this.arrowMultiplier);
                Engine.DrawSprite(arrowDown,
                    new Vector2(drawLocation.X + 0.15f, drawLocation.Y - 0.17f),
                    new Vector2(0.05f, 0.03f), this.arrowMultiplier);
            }
            else if (this.DrawNameInterface)
            {
                this.NewPlayer.Draw(drawLocation, this.arrowMultiplier);
            }
            else if (this.DrawSettingInterface)
            {
                this.LevelSelect.Draw(new Vector2(drawLocation.X + 0.02f, drawLocation.Y - 0.176f), this.arrowMultiplier);
                this.GameSpeed.Draw(new Vector2(drawLocation.X + 0.23f, drawLocation.Y - 0.176f), this.arrowMultiplier);
                this.BorderColor.Draw(new Vector2(drawLocation.X, drawLocation.Y - 1.25f), this.arrowMultiplier);
                this.BombMode.Draw(new Vector2(drawLocation.X + 0.23f, drawLocation.Y - 0.3f), this.arrowMultiplier);
            }
        }

        internal static void InitializeResources()
        {
            borderView = Engine.CreateView("./images/Menu.png");
            arrowUp = Engine.CreateView("./images/ArrowUp.png");
            arrowDown = Engine.CreateView("./images/ArrowDown.png");
            arrowRight = Engine.CreateView("./images/ArrowRight.png");
            Settings.SettingDisplay.InitializeResources();
            Settings.LevelSelect.InitializeResources();
            Settings.BorderColor.InitializeResources();

            using (System.Drawing.Bitmap bm = new System.Drawing.Bitmap(1, 1))
            using(System.IO.MemoryStream ms =new System.IO.MemoryStream())
            {
                bm.SetPixel(0, 0, System.Drawing.Color.White);
                bm.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                whitePixel = Engine.CreateView(ms.ToArray());
            }
        }

        #region Input 
        private int InitialMoveBlockSlide = 300;
        private int NextMoveBlockSlide = 200;
        private DateTime ControllerPadMovementLastPass = DateTime.Now;
        private DateTime? ControllerPadMovementStartTime = null;
        private DateTime ControllerPadMovementNextSlide;
        //  private long NextRotateAllowedAt = 0;
        private Movement lastMoveCommand;
        private Movement lastRotateCommand;
        public void Input(Dr_Mario.Object_Classes.Movement command)
        {
            this.InputReady = false;
            if (!this.ControllerPadMovementLastPass.Equals(DateTime.Now))
            {
                this.ControllerPadMovementLastPass = DateTime.Now;
                if ((lastRotateCommand | lastMoveCommand | command) != Movement.None)
                {

                    #region Directional Movement
                    if ((command & (Movement.Down | Movement.Left | Movement.Right | Movement.Up)) != Movement.None)
                    {
                        if ((command & (Movement.Down | Movement.Left | Movement.Right | Movement.Up)) != this.lastMoveCommand)
                        {
                            this.ControllerPadMovementStartTime = this.ControllerPadMovementNextSlide = DateTime.Now;
                        }

                        if (this.ControllerPadMovementStartTime.HasValue &&
                            this.ControllerPadMovementNextSlide <= DateTime.Now &&
                            (this.ControllerPadMovementStartTime.Equals(DateTime.Now) ||
                            DateTime.Now.Subtract(this.ControllerPadMovementStartTime.Value).TotalMilliseconds > this.InitialMoveBlockSlide))
                        {
                            this.ControllerPadMovementNextSlide = DateTime.Now.AddMilliseconds(this.NextMoveBlockSlide);

                            if ((command & Movement.Down) != Movement.None)
                            {
                                MoveDown();

                            }
                            else if ((command & Movement.Left) != Movement.None)
                            {
                                MoveLeft();
                            }
                            else if ((command & Movement.Right) != Movement.None)
                            {
                                MoveRight();
                            }
                            else if ((command & Movement.Up) != Movement.None)
                            {
                                MoveUp();
                            }
                        }
                        this.lastMoveCommand = (command & (Movement.Down | Movement.Left | Movement.Right| Movement.Up));
                    }
                    #endregion Directional Movement

                    #region Accept / Cancel

                    if ((command & Movement.Clockwise) == Movement.Clockwise)
                    {
                        if (this.lastRotateCommand != (command& Movement.Clockwise))
                            Accept();
                        this.lastRotateCommand = command;
                    }
                    else if ((command & Movement.CounterClockwise) == Movement.CounterClockwise)
                    {
                        if (this.lastRotateCommand != (command& Movement.CounterClockwise))
                            Cancel();
                        this.lastRotateCommand = command;
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
                    #endregion
                }
            }

            this.InputReady = true;
        }
        void Accept()
        {
            if (!this.Active)
            {
                this.Active = true;
                return;
            }
            if (!PlayerSelected)
            {
                #region Initial player selection
                this.PlayerSelected = true;
                if (Player.ID.Equals(-1))
                {
                    this.DrawNameInterface = true;
  
                }
                else if (!this.DrawSettingInterface)
                {
                    this.DrawSettingInterface = true;
                    this.CurrentSetting.Activate();
                }
                #endregion
            }
                
            else if (this.DrawNameInterface)
            {
                this.NewPlayer.Accept();
            }
                 
            else if (this.DrawSettingInterface && !this.ReadyToStart)
            {
                this.CurrentSetting.Accept();
                if (!this.CurrentSetting.Active)
                {

                    if (this.PlayerSettings.IndexOf(CurrentSetting) != this.PlayerSettings.Count - 1)
                    {
                        this.CurrentSetting.Deactivate();
                        this.CurrentSetting = this.PlayerSettings[this.PlayerSettings.IndexOf(CurrentSetting) + 1];
                        this.CurrentSetting.Activate();

                    }
                    else
                    {

                        this.ReadyToStart = true;
                        this.CurrentSetting.Deactivate();
                    }
                }
            }
            else if (this.DrawSettingInterface && this.ReadyToStart)
            {
                this.ReadyToStart = false;
                this.CurrentSetting.Activate();
            }
        }

        void Cancel()
        {
            if (!this.PlayerSelected)
                return;
            else if (this.DrawNameInterface)
            {
                this.NewPlayer.Cancel();
                if (!this.NewPlayer.Active)
                {
                    this.PlayerSelected = false;
                    this.DrawNameInterface = false;
                }
            }
            else if (this.DrawSettingInterface && !this.ReadyToStart && this.PlayerSettings.IndexOf(CurrentSetting) == 0)
                this.PlayerSelected = false;
            else if (this.DrawSettingInterface && !this.ReadyToStart && this.PlayerSettings.IndexOf(CurrentSetting) > 0)
            {
                this.CurrentSetting.Cancel();
                if (!this.CurrentSetting.Active)
                {
                    this.CurrentSetting.Deactivate();
                    this.CurrentSetting = this.PlayerSettings[this.PlayerSettings.IndexOf(CurrentSetting) - 1];
                    this.CurrentSetting.Activate();
                }
            }
            else if (this.ReadyToStart)
            {
                this.ReadyToStart = false;
                this.CurrentSetting.Activate();
            }
        }

        public void On_StartButtonPressed(object sender, EventArgs e)
        {
            var _sender = sender as Object_Classes.IControllerInput;

            if (_sender != null && this.PlayerIndex.Equals(_sender.PlayerIndex))
            {
                if (!this.Active)
                    this.Active = true;
                if (!this.Parent.IsActive)
                    this.Parent.IsActive = true;
                else if (!this.PlayerSelected)
                    Accept();
                else if (this.DrawNameInterface)
                {
                    string name = this.NewPlayer.Name;
                    if (!string.IsNullOrEmpty(name))
                    {
                        this.Player = Data.Player.Create(name);
                        this.DrawNameInterface = false;
                        this.DrawSettingInterface = true;
                    }
                    else
                        Cancel();
                }
                else if (this.DrawSettingInterface)
                {
                    this.ReadyToStart = true;
                    this.CurrentSetting.Deactivate();
                    this.CurrentSetting = this.PlayerSettings.First();
                }
            }
        }

        void MoveUp() {
            if (!this.PlayerSelected)
            {
                var ps = Data.Player.GetPlayers.Reverse().ToArray();

                try
                {
                    for (int e = 0; e < ps.Length; e++)
                    {
                        if (ps[e].Value.Equals(this.Player.ID))
                        {

                            this.Player = Data.Player.Load(ps[(e == ps.Length-1) ? 0 : (e + 1)].Value);
                            break;
                        }
                    }
                }
                catch
                {
                    this.Player = Data.Player.Load(ps[0].Value);
                }
            }
            if (this.DrawNameInterface)
            {
                this.NewPlayer.MoveUp();
            }
            if (this.DrawSettingInterface)
            {
                this.CurrentSetting.MoveUp();
            }
        }
        void MoveDown()
        {
            if (!this.PlayerSelected)
            {
                var e = Data.Player.GetPlayers.GetEnumerator();
                try
                {
                    e.MoveNext();
                    while (!e.Current.Key.Equals(this.Player.Name))
                        e.MoveNext();
                    if (e.MoveNext())
                        this.Player = Data.Player.Load(e.Current.Value);
                    else
                        this.Player = Data.Player.Load(Data.Player.GetPlayers.First().Value);
                }
                catch
                {
                    this.Player = Data.Player.Load(Data.Player.GetPlayers.First().Value);
                }
            }
            else if (this.DrawNameInterface)
            {
                this.NewPlayer.MoveDown();
            }
            else if (this.DrawSettingInterface)
            {
                this.CurrentSetting.MoveDown();
            }
        }
        void MoveLeft() {
            if (this.DrawNameInterface)
            {
                this.NewPlayer.Cancel();
                if (this.NewPlayer.Active)
                {
                    this.PlayerSelected = false;
                    this.DrawNameInterface = false;
                }
            }
        }
        void MoveRight() {
            if (this.DrawNameInterface)
                Accept();
        }

        public bool InputReady { get; private set; }
        #endregion Input

        public static void Dispose()
        {
            borderView.Resource.Dispose();
            borderView.Dispose();

            arrowUp.Resource.Dispose();
            arrowUp.Dispose();

            arrowDown.Resource.Dispose();
            arrowDown.Dispose();

            whitePixel.Dispose();
            arrowRight.Dispose();
            Settings.SettingDisplay.DisposeResources();
            Settings.LevelSelect.DisposeResources();
            Settings.BorderColor.DisposeResources();
        }
    }
}
