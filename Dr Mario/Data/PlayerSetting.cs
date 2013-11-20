using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;

namespace Dr_Mario.Data
{
    public class PlayerSetting
    {
        private int PlayerId { get; set; }
        private int ID { get; set; }
        public int SettingId { get; private set; }
        private string _Value;
        public string Value
        {
            get { return this._Value; }
            set
            {
                this._Value = value;
                this.Save();
            }
        }

        public void Save()
        {
            if (this.PlayerId != 1)
            {
                DAL.ExecuteNonQuery("UPDATE PLAYERSETTING SET SETTINGVALUE = @VALUE WHERE SETTINGID = @SID AND PLAYERID = @PID",
                    new SQLiteParameter("@VALUE", (object)this.Value ?? DBNull.Value),
                     new SQLiteParameter("@SID", this.SettingId),
                     new SQLiteParameter("@PID", this.PlayerId));
            }
        }

        public PlayerSetting(int? id, int pid, int sid, string value)
        {
            if (id.HasValue)
                this.ID = id.Value;
            else
                this.ID = Convert.ToInt32(DAL.ExecuteScalar("INSERT INTO PLAYERSETTING (SETTINGID, PLAYERID, SETTINGVALUE) VALUES (@SID, @PID, @VALUE); SELECT last_insert_rowid();",
                      new SQLiteParameter("@SID", sid),
                      new SQLiteParameter("@PID", pid),
                      new SQLiteParameter("@VALUE", value)));
            this.PlayerId = pid;
            this.SettingId = sid;
            this._Value = value;
        }

    }
}
