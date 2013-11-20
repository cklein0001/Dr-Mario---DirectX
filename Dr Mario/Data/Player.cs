using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;

namespace Dr_Mario.Data
{
    public class Player
    {
        public int ID { get; private set; }
        public UInt64 Score { get; private set; }
        public string Name { get; private set; }
        public PlayerSettingList Settings { get; private set; }

        private Player() { }
        private Player(int id, UInt64 score, string name)
        {
            this.ID = id;
            this.Score = score;
            this.Name = name;
            this.Settings = new PlayerSettingList();
        }

        public static Player Create(string name)
        {
            if(string.IsNullOrEmpty(name))
                throw new ArgumentNullException();
            DAL.ExecuteNonQuery("insert into players (name, score) values (@name, 0)",
                new SQLiteParameter("@name", name));
            var id = Convert.ToInt32(DAL.ExecuteScalar("select id from players order by id desc limit 1"));
            lock (playersLock) { players.Add(name, id); }
            return Load(id);
        }

        public static Player Load(int id)
        {
            int originalID=0;
            if (id <= 0)
            {
                originalID = id;
                id = 1;
            }
            var dt = DAL.ExecuteDataTable("select id, name, score from Players where id = @id", new SQLiteParameter("@id", id));
            if (dt.Rows.Count == 0)
                throw new NullReferenceException("No Player Exists.");

            var p = new Player(Convert.ToInt32(dt.Rows[0]["id"]), Convert.ToUInt64(dt.Rows[0]["id"]), Convert.ToString(dt.Rows[0]["name"]));
            dt.Dispose();

            dt = DAL.ExecuteDataTable(@"select ps.ID, p.ID pid, s.ID sid, coalesce(ps.settingvalue, s.DefaultValue) value
from Players p
cross join settings s 
left join PlayerSetting ps on p.ID = ps.PlayerId and s.ID = ps.SettingID
where p.ID = @pid",
                new SQLiteParameter("@pid", id));

            foreach (System.Data.DataRow dr in dt.Rows)
            {
                var newSetting = new PlayerSetting((DBNull.Value.Equals(dr["ID"]) ? null : (int?)Convert.ToInt32(dr["id"])),
                    Convert.ToInt32(dr["pid"]),
                    Convert.ToInt32(dr["sid"]),
                    Convert.ToString(dr["value"]));
                p.Settings.Add(newSetting.SettingId, newSetting);
            }
            if (originalID != 0)
            {
                p.ID = originalID;
                p.Name = "--New--";
            }
            return p;
        }

        private static object playersLock = new object();
        private static Dictionary<string, int> players;
        public static Dictionary<string, int> GetPlayers
        {
            get
            {
                if (players == null)
                {
                    lock (playersLock)
                    {
                        if (players == null)
                        {
                            PlayersLoad();
                        }
                    }
                }
                return players;
            }
        }

        private static void PlayersLoad()
        {
            players = new Dictionary<string, int>();
            using (var dt = DAL.ExecuteDataTable("SELECT [ID], [NAME] FROM PLAYERS ORDER BY [NAME]"))
            {
                foreach (System.Data.DataRow dr in dt.Rows)
                {
                    players.Add(Convert.ToString(dr["Name"]), Convert.ToInt32(dr["ID"]));
                }
            }
            players.Add("--New--", -1);
        }

    }
}
