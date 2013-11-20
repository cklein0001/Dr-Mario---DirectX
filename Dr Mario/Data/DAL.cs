using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Data;
using System.Data.Common;

namespace Dr_Mario.Data
{
    public static class DAL
    {
        private static SQLiteFactory factory;
        static DAL()
        {
            factory = (SQLiteFactory)DbProviderFactories.GetFactory("System.Data.SQLite");
        }

        private static SQLiteConnection GetConnection()
        {
            var conn = (SQLiteConnection)factory.CreateConnection();
            conn.ConnectionString = "Data Source=Game.db3";
            conn.Open();

            return conn;
        }

        private static SQLiteCommand GetCommand(string sql, params SQLiteParameter[] args)
        {
            SQLiteCommand cmd = new SQLiteCommand(sql, GetConnection());
            if(args != null && args.Length > 0)
                cmd.Parameters.AddRange(args);

            return cmd;
        }

        public static int ExecuteNonQuery(string sql, params SQLiteParameter[] args)
        {
            var cmd = GetCommand(sql, args);
            var lines  = cmd.ExecuteNonQuery();
            cmd.Connection.Dispose();
            cmd.Dispose();
            return lines;
        }

        public static object ExecuteScalar(string sql, params SQLiteParameter[] args)
        {
            var cmd = GetCommand(sql, args);
            var obj = cmd.ExecuteScalar();
            cmd.Connection.Dispose();
            cmd.Dispose();
            return obj;
        }

        public static DataTable ExecuteDataTable(string sql, params SQLiteParameter[] args)
        {
            using (var cmd = GetCommand(sql, args))
            using (var a = new SQLiteDataAdapter(cmd))
            {
                var dt = new DataTable();
                a.Fill(dt);
                cmd.Connection.Dispose();
                return dt;
            }
        }
    }
}
