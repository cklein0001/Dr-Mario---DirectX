using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using SlimDX.Direct3D11;
using Buffer = SlimDX.Direct3D11.Buffer;
using Device = SlimDX.Direct3D11.Device;
using SlimDX.DXGI;
using SlimDX.Windows;
using SlimDX;
using Resource = SlimDX.Direct3D11.Resource;
using Dr_Mario.Form_Classes;

namespace Dr_Mario
{
    static class Program
    {
        private static Form_Classes.MainForm form = null;
        public static bool Debug = true;
        private static bool HasFocus = false;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>

        static void Main()
        {
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            
            form = new Form_Classes.MainForm("Dr. Mario");
            form.FormBorderStyle = FormBorderStyle.None;
            //form.FormBorderStyle = FormBorderStyle.None;
            form.GotFocus += form_GotFocus;
            form.LostFocus += form_LostFocus;
            form.KeyDown += FullScreen;

            form.Height = Debug ? 450 : System.Windows.Forms.SystemInformation.PrimaryMonitorSize.Height;
            form.Width = Debug ? 800 : System.Windows.Forms.SystemInformation.PrimaryMonitorSize.Width;
            form.AppActivated += form_GotFocus;
            //form.Deactivate += form_LostFocus;
            form.Activated += form_GotFocus;
            form.MouseEnter += (s, e) => Cursor.Hide();
            form.MouseLeave += (s, e) => Cursor.Show();

            Dr_Mario.Form_Classes.Engine.Initialize(form);
            Dr_Mario.Object_Classes.Virus.Initialize();
            Dr_Mario.Object_Classes.Capsule.Initialize();
            Dr_Mario.Object_Classes.Bottle.BorderTexture = Form_Classes.Engine.CreateView(@".\images\Bottle.png");
            Dr_Mario.Object_Classes.Bottle.RedBrush = Engine.CreateBrush(new Vector2(), System.Drawing.Color.Red);
            Dr_Mario.Object_Classes.Bottle.BlueBrush = Engine.CreateBrush(new Vector2(), System.Drawing.Color.CornflowerBlue);
            Dr_Mario.Object_Classes.Bottle.YellowBrush = Engine.CreateBrush(new Vector2(), System.Drawing.Color.Yellow);
            SoundManager.Initialize(form);
            form.InitializeResources();
            PlayerMenu.InitializeResources();
            Application.ApplicationExit += Application_ApplicationExit;
            //form.Focus();

            SoundManager.Play(@"C:\Users\Christopher\Documents\Visual Studio 11\Projects\Dr Mario - DirectX\Dr. Mario Online Rx\Music\chill_full.wav");

            MessagePump.Run(form, Run);
          
        }

        static void Run()
        {
            if (Program.HasFocus)
            {
                //  Engine.ClearFrame();
                form.Draw(form, EventArgs.Empty);
                Engine.Draw();
                form.Poll_Controllers(form, EventArgs.Empty);

                
            }
            else form_LostFocus(form, EventArgs.Empty);
            
        }

        static void Application_ApplicationExit(object sender, EventArgs e)
        {
            Dr_Mario.Object_Classes.Capsule.Destroy();
            Dr_Mario.Object_Classes.Virus.Destroy();
            PlayerMenu.Dispose();
            Dr_Mario.Object_Classes.Bottle.BorderTexture.Dispose();
            Engine.Destroy();
            form.Dispose();
            Object_Classes.Bottle.RedBrush.Dispose();
            Object_Classes.Bottle.BlueBrush.Dispose();
            Object_Classes.Bottle.YellowBrush.Dispose();
            SoundManager.Cleanup();
        }



        static void form_LostFocus(object sender, EventArgs e)
        {
            if (!form.Paused)
            {
                Engine.Pause(true);
                form.GamePauseCall(new Object_Classes.GamePausedEventArgs(Object_Classes.Player.System, true));
                Cursor.Show();

                if(!Program.Debug)
                    form.WindowState = FormWindowState.Minimized;
                Program.HasFocus = false;
            }
        }

        static void form_GotFocus(object sender, EventArgs e)
        {
            form.Focus();
            //if (swapChain != null)
            {
                Cursor.Hide();
                //  swapChain.SetFullScreenState(true, outputDevice);
                Engine.Pause(false);
                //if (!swapChain.IsFullScreen)
                 //   swapChain.IsFullScreen = true;
                //form.WindowState = FormWindowState.Maximized;
                form.GamePauseCall(new Object_Classes.GamePausedEventArgs(Object_Classes.Player.System, false));
                //  swapChain.ResizeTarget(new ModeDescription(0, 0, new Rational(60, 1), Format.R8G8B8A8_UNorm));
                Program.HasFocus = true;
            }
        }

        static void FullScreen(object sender, KeyEventArgs e)
        {
            if (e.Alt && e.KeyCode == Keys.Enter)
            {
                //form_LostFocus(sender, e);
               
               
                e.Handled = true;
                //   form_LostFocus(sender, e);
                //else
                //    form_GotFocus(sender, e);
            }
            // if (e.Alt && e.KeyCode == Keys.Tab)
            //     form_LostFocus(sender, e);
            else if (e.Alt && e.KeyCode == Keys.Tab)
            {
                form_LostFocus(sender, e);
                e.Handled = true;
            }
            else if (e.Alt && e.KeyCode == Keys.F4)
            {
                Application.Exit();
                e.Handled = true;
            }
            else if (e.Alt)
            {
                Cursor.Hide();
                form.Focus();
                e.Handled = true;
            }
          
        }
    }
}
