using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX;

namespace Dr_Mario.Form_Classes.Settings
{
  internal  class NewPlayer : SettingDisplay
    {
 private static List<char> ValidCharacters;
        
        static NewPlayer()
        {
            List<char> validChars = new List<char>();

            validChars.Add(' ');
            validChars.AddRange(Enumerable.Range((int)'A', 26).Select(u => (char)u));
            validChars.AddRange(Enumerable.Range((int)'a', 26).Select(u => (char)u));
            validChars.AddRange(Enumerable.Range((int)'0', 10).Select(u => (char)u));
            validChars.Add('!');
            validChars.Add('.');
            validChars.Add('?');
            validChars.AddRange(Enumerable.Range((int)'¿', 260).Select(u => (char)u));

            validChars.AddRange(Enumerable.Range(452, 236).Select(u => (char)u));
            validChars.Add('Ω');
            validChars.Add('♫');
            ValidCharacters = validChars;
            
        }

        char[] newPlayerName = new char[14];
        int newPlayerNamePosition = 0;
        int newPlayerNameCharacterIndex;

        public NewPlayer()
        {
            for (int i = 0; i < newPlayerName.Length; i++) newPlayerName[i] = ' ';
        }

        public string Name { get { return new string(this.newPlayerName).Trim(); } }
      
        public override void Activate()
        {
            this.newPlayerNameCharacterIndex = 0;
            for (int i = 0; i < newPlayerName.Length; i++) newPlayerName[i] = ' ';
        }

        public override void Deactivate()
        {
            throw new NotImplementedException();
        }

        public override void Draw(SlimDX.Vector2 drawLocation, System.Drawing.Color arrowMultiplier)
        {
            Engine.DrawText("Enter your name...", new Vector2(drawLocation.X + 0.04f, drawLocation.Y - 0.2f), SlimDX.DirectWrite.TextAlignment.Leading);
            float increment = 0.03f;
            for (float i = 0; i < 0.41f; i += increment)
            {
                Engine.DrawSprite(whitePixel, new Vector2(drawLocation.X + 0.04f + i, drawLocation.Y - 0.475f), new Vector2(increment - (increment / 10), (increment / 10)));
                int ii = Convert.ToInt32(i / increment);
                if (!newPlayerName[ii].Equals(' '))
                {
                    try
                    {
                        Engine.DrawText(newPlayerName[ii].ToString(), new Vector2(drawLocation.X + 0.04f + i, drawLocation.Y - 0.38f), SlimDX.DirectWrite.TextAlignment.Leading);
                    }
                    catch { continue; }
                }
            }
            Engine.DrawSprite(PlayerMenu.arrowUp, new Vector2(drawLocation.X + 0.04f + (float)this.newPlayerNamePosition * increment, drawLocation.Y - 0.375f), new Vector2(0.03f, 0.03f * Engine.YMultiply), arrowMultiplier);
            Engine.DrawSprite(PlayerMenu.arrowDown, new Vector2(drawLocation.X + 0.04f + (float)this.newPlayerNamePosition * increment, drawLocation.Y - 0.52f), new Vector2(0.03f, 0.03f * Engine.YMultiply), arrowMultiplier);
        
        }

        public override void MoveUp()
        {
            this.newPlayerName[this.newPlayerNamePosition] = ValidCharacters[(++this.newPlayerNameCharacterIndex) % ValidCharacters.Count];
        }

        public override void MoveDown()
        {
            if (this.newPlayerNameCharacterIndex == 0)
                this.newPlayerNameCharacterIndex = ValidCharacters.Count;
            this.newPlayerName[this.newPlayerNamePosition] = ValidCharacters[--this.newPlayerNameCharacterIndex];
        }

        public override void Load(Data.PlayerSettingList settings)
        {
            throw new NotImplementedException();
        }

        public override void Accept()
        {
            if (this.newPlayerNamePosition < newPlayerName.Length - 1)
            {
                this.newPlayerNamePosition++;
                this.newPlayerNameCharacterIndex = 0;
            }
        }

        public override void Cancel()
        {
            if (this.newPlayerNamePosition > 0)
            {
                this.newPlayerNamePosition--;
                this.newPlayerNameCharacterIndex = ValidCharacters.IndexOf(this.newPlayerName[this.newPlayerNamePosition]) + 1;
                this.Active = true;
            }
            else
            {
                this.Active = false;
            }
        }
    }
}
