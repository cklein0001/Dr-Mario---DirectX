using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Drawing;
using System.Reflection;
using Dr_Mario.Object_Classes;
using System.Timers;

namespace Dr_Mario.Form_Classes
{

    public class MainForm : SlimDX.Windows.RenderForm
    {
        Timer timerFrame;
        static readonly int timerFrameInterval = 150;
     //   float width = 2.0f / 40f;
     //   float height = 2.0f / 40f * (16f / 10f);

        Bottle[] b = new Bottle[4];
        SlimDX.XInput.Controller[] controller =new SlimDX.XInput.Controller[4];
        //PlayerMenu[] playerMenu = new PlayerMenu[4];
        public MainForm(string title)
            : base(title)
        {
            controller[0] = new SlimDX.XInput.Controller(SlimDX.XInput.UserIndex.One);
            controller[1] = new SlimDX.XInput.Controller(SlimDX.XInput.UserIndex.Two);
            controller[2] = new SlimDX.XInput.Controller(SlimDX.XInput.UserIndex.Three);
            controller[3] = new SlimDX.XInput.Controller(SlimDX.XInput.UserIndex.Four);


            timerFrame = new Timer();
            this.timerFrame.Enabled = true;
            this.timerFrame.Interval = timerFrameInterval;
            this.timerFrame.Elapsed += this.timer1_Tick;
            this.MouseClick += this.button1_Click;
       //     BottleInit();

         
            ControllerHandler.StartButtonPressed += this.On_StartButtonPressed;

            b[0] = new BottleCpu(1);
            b[1] = new Bottle(2);
            b[2] = new Bottle(3);
            b[3] = new Bottle(4);

            for (int i = 0; i < b.Length; i++)
            {
                b[i].GameOver += new Bottle.GameOverEventHandler(bottle_GameOver);
                b[i].BottleReady += BottleReady;
                b[i].PlayerMenu.PlayerReady += this.PlayerMenu_Ready;
                b[i].BombLauncher += this.On_BombLauched;
            }

            this.Mode = GameMode.Menu;

            this.Disposed += MainForm_Disposed;

        }

        void On_BombLauched(object sender, BombLaunchEventArgs e)
        {
            Random r;
            var otherPlayers = this.b.Where(bt => bt.PlayerIndex != e.IndexPlayer && bt.IsActive);
            if(otherPlayers.Count() == 0)
                return;

            switch (e.Recipient)
            {
                case BombMode.Salt_And_Pepper:
                    r = new Random();
                    b[r.Next(0, otherPlayers.Count())].TakeBomb(e.BombData);
                    break;
                case BombMode.Auto:
                case BombMode.By_Color:
                    if (otherPlayers.Count() == 1)
                        otherPlayers.First().TakeBomb(e.BombData);
                    else if (otherPlayers.Count() == 2)
                    {
                        switch (e.BombData[0])
                        {
                            case vColors.B:
                                otherPlayers.First().TakeBomb(e.BombData);
                                break;
                            case vColors.R:
                                otherPlayers.Last().TakeBomb(e.BombData);
                                break;
                            default:
                                r = new Random();
                                otherPlayers.ToArray()[r.Next(0, 1)].TakeBomb(e.BombData);
                                break;
                        }
                    }
                    else // all 4
                    {
                        Bottle[] otherPlayerArray = new Bottle[3];
                        switch (e.IndexPlayer)
                        {
                            case 1:
                                otherPlayerArray[0] = b[1];
                                otherPlayerArray[1] = b[2];
                                otherPlayerArray[2] = b[3];
                                break;
                            case 2:
                                otherPlayerArray[0] = b[2];
                                otherPlayerArray[1] = b[3];
                                otherPlayerArray[2] = b[0];
                                break;
                            case 3:
                                otherPlayerArray[0] = b[3];
                                otherPlayerArray[1] = b[0];
                                otherPlayerArray[2] = b[1];
                                break;
                            case 4:
                                otherPlayerArray[0] = b[0];
                                otherPlayerArray[1] = b[1];
                                otherPlayerArray[2] = b[2];
                                break;
                        }
                        switch (e.BombData[0])
                        {
                            case vColors.B: otherPlayerArray[0].TakeBomb(e.BombData); break;
                            case vColors.R: otherPlayerArray[1].TakeBomb(e.BombData); break;
                            case vColors.Y: otherPlayerArray[2].TakeBomb(e.BombData); break;
                                
                        }

                    }
                    break;
                case BombMode.All_Up:
                    var recipientUp = otherPlayers
                        .Where(op => op.PlayerIndex > e.IndexPlayer)
                        .OrderBy(op => op.PlayerIndex)
                        .FirstOrDefault();
                    if (recipientUp == null)
                        recipientUp = otherPlayers.OrderBy(op => op.PlayerIndex).First();
                    recipientUp.TakeBomb(e.BombData);
                    break;
                case BombMode.All_Down:
                    var recipientDown = otherPlayers
                        .Where(op => op.PlayerIndex < e.IndexPlayer)
                        .OrderByDescending(op => op.PlayerIndex)
                        .FirstOrDefault();
                    if (recipientDown == null)
                        recipientDown = otherPlayers.OrderByDescending(op => op.PlayerIndex).First();
                    recipientDown.TakeBomb(e.BombData);
                    break;
            }
        }

        void On_StartButtonPressed(object sender, EventArgs e)
        {
            if (this.Mode == GameMode.Menu)
                return;// Because playermenu will handle...
            this.PressStartToBegin = false;
            if (b.Where(bt => bt.IsActive).Count() == 1)
                b.Where(bt => bt.IsActive).First().FillBottle(null);
            else
            {

            }
        }

        void MainForm_Disposed(object sender, EventArgs e)
        {
            foreach (var b in this.b)
                b.Dispose();
        }

        public void InitializeResources()
        {
        }

        void BottleReady(object sender, EventArgs e)
        {
            if (!b.Any(bb => bb.IsActive && !bb.Filled))
            {
                BeginCountdown();
            }
        }

        void BeginCountdown()
        {
            bool multiplayer = b.Where(bt => bt.IsActive).Count() > 1;
            for (int i = 0; i < b.Length; i++)
                b[i].Start(multiplayer);
        }

        private bool PressStartToBegin = false;

        void bottle_GameOver(object sender, GameOverEventArgs e)
        {
            if (sender != null && sender.GetType().Equals(typeof(Bottle)) && ((Bottle)sender).IsActive)
            {
                if (e.Reason == GameOverReason.EntranceCollision)
                {
                    if(b.Where(bt=>bt.IsActive).Count() == 1)
                        timerFrame.Interval = timerFrameInterval / 6;
                }
                else if (e.Reason == GameOverReason.VirusClear)
                {
                    if (b.Where(bt => bt.IsActive).Count() == 1)
                    {
                        
                        
                        this.PressStartToBegin = true;
                    }
                }
            }
        }

        private int PlayerOneCurrentMenu = 0;
        public void Poll_Controllers(object sender, EventArgs args)
        {
            if (this.Mode == GameMode.Menu)
            {
                ControllerHandler.UpdateControllerState(b[PlayerOneCurrentMenu], controller[0], 0);
                for (int i = 1; i < b.Length; i++)
                {
                    try
                    {

                        // IControllerInput ci = b[i];// (this.Mode == GameMode.Active) ? (IControllerInput)b[i] : (IControllerInput)playerMenu[i];
                        // if(ci != null)
                        ControllerHandler.UpdateControllerState(b[i], controller[i], i);
                        //   else
                        //       playerMenu[i].CheckInput(controller[i]);
                    }
                    catch (Exception)
                    {
                        // MessageBox.Show(e.Message, e.Source);
                    }
                }
            }
            else
            {
                for (int i = 0; i < b.Length; i++)
                {
                    try { ControllerHandler.UpdateControllerState(b[i], controller[i], i); }
                    catch (Exception ee) { Bottle.DeO(ee); }
                }
            }
        }


        private int _frame = 0;
        private int frame { get{lock(frameLock){ return this._frame;}}set{lock(this.frameLock){this._frame = value;}}}
        private object frameLock = new object();
        private bool ascending = true;
        private void timer1_Tick(object sender, EventArgs e)
        {
            lock (frameLock)
            {
                if (ascending && frame == 4)
                {
                    ascending = false;
                    frame = frame - 1;
                }
                else if (!ascending && frame == 0)
                {
                    ascending = true;
                    frame = frame + 1;
                }
                else
                    frame = frame + (ascending ? 1 : -1);
            }

           // lblVirusCount.SetPropertyThreadSafe(() => lblVirusCount.Text, b.Viruses.ToString());

        }

        public void PlayerMenu_Ready(object sender, EventArgs e)
        {
            if (this.b.Any(bt => bt.PlayerMenu.Active && !bt.PlayerMenu.ReadyToStart))
                return;
            else
            {
                // Transfer menu options to bottle
                int mNow = DateTime.Now.Millisecond;
                var bottles = this.b.Where(bt => bt.PlayerMenu.Active);
                foreach (var bottle in bottles)
                {
                    bottle.InitializeFromPlayerMenu(mNow, bottles.Count());
                }
                this.Mode = GameMode.Active;
            }
        }

        
        private GameMode Mode { get; set; }

        public void Draw(object sender, EventArgs e)
        {

            var players = this.b.Where(bt => bt.IsActive).ToArray();
            switch (players.Length)
            {
                case 1:
                    players[0].Draw(new SlimDX.Vector2(0 - players[0].Size.X * 0.5f, 1), frame, this.Mode);
                    break;
                case 2:
                    players[0].Draw(new SlimDX.Vector2(-0.5f - players[0].Size.X * 0.5f, 1), frame, this.Mode);
                    players[1].Draw(new SlimDX.Vector2(0.5f - players[0].Size.X * 0.5f, 1), frame, this.Mode);
                    break;
                case 3:
                    players[0].Draw(new SlimDX.Vector2(-0.4f - players[0].Size.X * 0.5f, 1), frame, this.Mode);
                    players[1].Draw(new SlimDX.Vector2(0 - players[0].Size.X * 0.5f, 1), frame, this.Mode);
                    players[2].Draw(new SlimDX.Vector2(1.6f - players[0].Size.X * 0.5f, 1), frame, this.Mode);
                    break;
                case 4:
                    players[0].Draw(new SlimDX.Vector2(-1, 1), frame, this.Mode);
                    players[1].Draw(new SlimDX.Vector2(-0.5f, 1), frame, this.Mode);
                    players[2].Draw(new SlimDX.Vector2(0, 1), frame, this.Mode);
                    players[3].Draw(new SlimDX.Vector2(0.5f, 1), frame, this.Mode);
                    break;
            }
        }
    

        
        public delegate void GamePausedEventHandler(object sender, GamePausedEventArgs e);
        public event GamePausedEventHandler GamePaused;

        private bool paused = false;
        private object pausedMutex = new object();
        public bool Paused { get { lock (pausedMutex) { return paused; } } set { lock (pausedMutex) { paused = value; } } }
        public void GamePauseCall(GamePausedEventArgs e)
        {
            if (this.Mode == GameMode.Active)
            {
                if (e.Pause != this.Paused)
                {
                    lock (pausedMutex)
                    {
                        paused = e.Pause;
                        this.timerFrame.Enabled = !e.Pause;
                        foreach (var bottle in b)
                        {
                            if (bottle != null)
                                bottle.Pause(e.Pause);
                        }

                        if (GamePaused != null)
                            GamePaused(this, e);
                    }
                }
            }
        }
        

        public void button1_Click(object sender, EventArgs e)
        {
            //On_StartButtonPressed(sender, e);
            ControllerHandler.StartButtonPressedCalled(this.b[0]);
        }
    }
}
