using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace building_bms
{
    class database
    {
        private SQLiteConnection sqlcon;
        public database()
        {
            sqlcon = new SQLiteConnection("Data Source = " + AppDomain.CurrentDomain.BaseDirectory + "\\BMSdb.sqlite; Version = 3;");
        }

        //#insert records to db
        public bool insertlog(string pid, int d, string state, DateTime t)
        {
            try {
                SQLiteCommand insertlog = new SQLiteCommand("INSERT INTO zkrecords(pid, door, state, time) VALUES(@pid, @d, @s, @t)", sqlcon);

                insertlog.Parameters.AddWithValue("@pid", pid);
                insertlog.Parameters.AddWithValue("@d", d);
                insertlog.Parameters.AddWithValue("@s", state);
                insertlog.Parameters.AddWithValue("@t", t);
                insertlog.ExecuteNonQuery();
                return true;
            }
            catch (Exception er)
            {
                return false;
            }
        }

        public string[,] getlogs()
        {
            try
            {
                SQLiteCommand selectlog = new SQLiteCommand("SELECT * FROM zkrecords", sqlcon);
                SQLiteDataReader r = selectlog.ExecuteReader();
                if (r.HasRows)
                {
                    int n = 0;

                    string[,] logs = new string[r.Depth, 5];
                    while (r.Read())
                    {
                        logs[n, 0] = r.GetInt32(0).ToString();
                        logs[n, 1] = r.GetString(1);
                        logs[n, 2] = r.GetInt16(2).ToString();
                        logs[n, 3] = r.GetString(3);
                        logs[n, 4] = r.GetDateTime(4).ToString();
                        n++;
                    }
                    return logs;

                }
                else
                {
                    return null;
                }
            }
            catch (Exception er)
            {
                return null;
            }
        }
    }
}
