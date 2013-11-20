using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dr_Mario.Data
{
    public class PlayerSettingList : Dictionary<int, PlayerSetting>
    {
        private System.Drawing.Color _color = System.Drawing.Color.Transparent;
        public System.Drawing.Color Color
        {
            get
            {
                if (this._color == System.Drawing.Color.Transparent)
                {
                    var colorString  = this["BorderColor"].Value;
                    int outValue;
                    if(int.TryParse(colorString, out outValue))
                        this._color = System.Drawing.Color.FromArgb(Convert.ToInt32(outValue));
                    else
                        this._color = System.Drawing.Color.FromName(colorString);
                }
                return _color;
            }
            set
            {
                this._color = value;
                if (value.IsNamedColor)
                    this["BorderColor"].Value = value.ToKnownColor().ToString();
                else
                    this["BorderColor"].Value = value.ToArgb().ToString();
            }
        }

        public PlayerSetting this[string settingName]
        {
            get
            {
                if (Settings.ContainsKey(settingName))
                    return this[Settings[settingName]];
                else return null;
            }
            set
            {
                if (Settings.ContainsKey(settingName))
                    this[Settings[settingName]] = value;
            }
        }

        public void Save()
        {
            foreach (var setting in this)
            {
                setting.Value.Save();
            }
        }

        static PlayerSettingList()
        {
            Settings = new Dictionary<string, int>();
            using (var dt = DAL.ExecuteDataTable("SELECT Name, Id from SETTINGS"))
            {
                foreach (System.Data.DataRow dr in dt.Rows)
                    Settings.Add(Convert.ToString(dr["Name"]), Convert.ToInt32(dr["id"]));
            }
        }
        public static Dictionary<string, int> Settings;
    }
}
