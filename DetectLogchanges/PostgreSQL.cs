using System;
using Npgsql;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace DetectLogchanges
{
    class PostgreSQL
    {
        /// <summary>
        /// connection string which contains server name, port, user, passw, and DBname
        /// </summary>
        string palettepg_conn;
        NpgsqlConnection TabMon_conn; //connection for TabMon DB

        /// <summary>
        /// Set the connection string, create DB connection and open it.
        /// </summary>
        public PostgreSQL()
        {
            this.palettepg_conn = "Server=palettepg.cakavtkziz1k.us-west-1.rds.amazonaws.com;Port=5432;User Id=palette;Password=palette123;Database=TabMon";
            TabMon_conn = new NpgsqlConnection(palettepg_conn); // table connection 
            TabMon_conn.Open(); //open pgsql connection
        }

        /// <summary>
        /// Set the connection string, create DB connection and open it.
        /// </summary>
        /// <param name="conn"></param>
        public PostgreSQL(string conn)
        {
            palettepg_conn = conn;
            TabMon_conn = new NpgsqlConnection(palettepg_conn); // table connection 
            TabMon_conn.Open(); //open pgsql connection
        }

        /// <summary>
        /// This function insert a row into the Serverlogs table in TabMon DB
        /// sql script: INSERT INTO Serverlogs VALUES (@filename, @host_name, @ts, @pid, @tid, @sev, @req,
        /// @sess, @site, @username, @k, @v)
        /// 
        /// Get a jsonString like this:
        /// {"ts":"2015-11-04T09:35:44.862","pid":11108,"tid":"ac0","sev":"debug","req":"VjnRcApWDfQAAAyQpz0AAAFE",
        /// "sess":"53F596BD0FA8479CA0860F36F27B2346-0:0","site":"Default","user":"tfoldi","k":"msg",
        ///    "v":"   [Time] Building the tuples took 0.0000 sec."}
        /// parse it, and insert it to the dataebase. 
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="jsonString"></param>
        public void insertToServerlogsTable(String filename, String jsonString)
        {
            if (palettepg_conn == null || palettepg_conn == "")
                palettepg_conn = "Server=palettepg.cakavtkziz1k.us-west-1.rds.amazonaws.com;Port=5432;User Id=palette;Password=palette123;Database=TabMon";

            if (TabMon_conn == null)
                TabMon_conn = new NpgsqlConnection(palettepg_conn);

            // stackoverflow.com/questions/6620165/how-can-i-parse-json-with-c
            //parse the jsonString (to object)
            dynamic jsonraw = null;
            try
            {
                jsonraw = JsonConvert.DeserializeObject(jsonString);
            }
            catch (Exception e)
            {
                Console.WriteLine("Json parse exception occured. " + e.StackTrace);
                return;
            }


            //if we find "eqc-log-cache-key" key then we inseret into filter_state_audit table
            if (jsonraw.k == "eqc-log-cache-key")
            {
                insertToFilterState(jsonString);
            }
            else if (jsonraw.k == "qp-batch-summary")
            {
                insertToFilterState(jsonString);
            }

            string tid = jsonraw.tid;

            string insertQuery = "INSERT INTO Serverlogs (filename, host_name, ts, pid, tid, sev, req, sess, site, username, k, v)" +
                "VALUES (@filename, @host_name, @ts, @pid, @tid, @sev, @req, @sess, @site, @username, @k, @v)";

            var insert_cmd = new NpgsqlCommand(insertQuery, TabMon_conn);
            insert_cmd.Parameters.AddWithValue("@filename", NpgsqlTypes.NpgsqlDbType.Text, filename);
            insert_cmd.Parameters.AddWithValue("@host_name", NpgsqlTypes.NpgsqlDbType.Text, Program.strHostName);
            insert_cmd.Parameters.AddWithValue("@ts", NpgsqlTypes.NpgsqlDbType.Timestamp, jsonraw.ts);
            insert_cmd.Parameters.AddWithValue("@pid", NpgsqlTypes.NpgsqlDbType.Integer, (int)jsonraw.pid);
            insert_cmd.Parameters.AddWithValue("@tid", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(tid, 16));
            insert_cmd.Parameters.AddWithValue("@sev", NpgsqlTypes.NpgsqlDbType.Text, jsonraw.sev);
            insert_cmd.Parameters.AddWithValue("@req", NpgsqlTypes.NpgsqlDbType.Text, jsonraw.req);
            insert_cmd.Parameters.AddWithValue("@sess", NpgsqlTypes.NpgsqlDbType.Text, jsonraw.sess);
            insert_cmd.Parameters.AddWithValue("@site", NpgsqlTypes.NpgsqlDbType.Text, jsonraw.site);
            insert_cmd.Parameters.AddWithValue("@username", NpgsqlTypes.NpgsqlDbType.Text, jsonraw.user);
            insert_cmd.Parameters.AddWithValue("@k", NpgsqlTypes.NpgsqlDbType.Text, jsonraw.k);
            insert_cmd.Parameters.AddWithValue("@v", NpgsqlTypes.NpgsqlDbType.Text, jsonraw.v);

            if (TabMon_conn.State != System.Data.ConnectionState.Open)
                TabMon_conn.Open();
            insert_cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// This function is insert some row into the filter_state_audit table in TabMon DB
        /// the sql script: INSERT INTO filter_state_audit VALUES  (@ts, @pid, @tid, @req,@sess, @site,
        ///  @username, @filter_name, @filter_vals, @workbook, @view)       
        /// 
        /// </summary>
        /// <param name="jsonString"></param>
        void insertToFilterState(string jsonString)
        {
            dynamic jsonraw = JsonConvert.DeserializeObject(jsonString);
            string tid = jsonraw.tid;
            string level;
            string member;

            string cache_key_Value = jsonraw.v["cache-key"];
            if (cache_key_Value == null)
            {
                cache_key_Value = jsonraw.v.ToString();
                //    Console.WriteLine(cache_key_Value);
            }

            if (cache_key_Value == null)
            {
                Console.WriteLine("Regex input value was null!");
                return;
            }


            string pattern1 = @"<groupfilter function='member' level='(.*?)' member='(.*?)'.*?/>";

            string insertQuery = "INSERT INTO filter_state_audit (ts, pid, tid, req, sess, site, username, filter_name, " +
                "filter_vals, workbook, view, hostname) VALUES (@ts, @pid, @tid, @req," +
                "@sess, @site, @username, @filter_name, @filter_vals, @workbook, @view, @hostname)";

            if (TabMon_conn.State != System.Data.ConnectionState.Open)
                TabMon_conn.Open();

            MatchCollection mc = Regex.Matches(cache_key_Value, pattern1);
            foreach (Match m in mc)
            {
                level = m.Groups[1].ToString();
                member = m.Groups[2].ToString();
                member = member.Replace("&quot;", "");

                var insert_cmd = new NpgsqlCommand(insertQuery, TabMon_conn);

                insert_cmd.Parameters.AddWithValue("@ts", NpgsqlTypes.NpgsqlDbType.Timestamp, jsonraw.ts);
                insert_cmd.Parameters.AddWithValue("@pid", NpgsqlTypes.NpgsqlDbType.Integer, (int)jsonraw.pid);
                insert_cmd.Parameters.AddWithValue("@tid", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(tid, 16));
                insert_cmd.Parameters.AddWithValue("@req", NpgsqlTypes.NpgsqlDbType.Text, jsonraw.req);
                insert_cmd.Parameters.AddWithValue("@sess", NpgsqlTypes.NpgsqlDbType.Text, jsonraw.sess);
                insert_cmd.Parameters.AddWithValue("@site", NpgsqlTypes.NpgsqlDbType.Text, jsonraw.site);
                insert_cmd.Parameters.AddWithValue("@username", NpgsqlTypes.NpgsqlDbType.Text, jsonraw.user);
                insert_cmd.Parameters.AddWithValue("@filter_name", NpgsqlTypes.NpgsqlDbType.Text, level);
                insert_cmd.Parameters.AddWithValue("@filter_vals", NpgsqlTypes.NpgsqlDbType.Text, member);
                insert_cmd.Parameters.AddWithValue("@workbook", NpgsqlTypes.NpgsqlDbType.Text, "");
                insert_cmd.Parameters.AddWithValue("@view", NpgsqlTypes.NpgsqlDbType.Text, "");
                insert_cmd.Parameters.AddWithValue("@hostname", NpgsqlTypes.NpgsqlDbType.Text, Program.strHostName);
                insert_cmd.ExecuteNonQuery();
            }

            //insert all filters
            string pattern2 = @"<groupfilter function='level-members' level='(.*?)' user:ui-enumeration='(.*?)'.*?/>";
            MatchCollection mc2 = Regex.Matches(cache_key_Value, pattern2);
            foreach (Match m in mc2)
            {
                level = m.Groups[1].ToString();
                //if we find calculation we throw it out (dont insert to DB)
                if (level.Contains("Calculation_"))
                    continue;

                member = m.Groups[2].ToString();
                member = member.Replace("&quot;", "");

                var insert_cmd = new NpgsqlCommand(insertQuery, TabMon_conn);

                insert_cmd.Parameters.AddWithValue("@ts", NpgsqlTypes.NpgsqlDbType.Timestamp, jsonraw.ts);
                insert_cmd.Parameters.AddWithValue("@pid", NpgsqlTypes.NpgsqlDbType.Integer, (int)jsonraw.pid);
                insert_cmd.Parameters.AddWithValue("@tid", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(tid, 16));
                insert_cmd.Parameters.AddWithValue("@req", NpgsqlTypes.NpgsqlDbType.Text, jsonraw.req);
                insert_cmd.Parameters.AddWithValue("@sess", NpgsqlTypes.NpgsqlDbType.Text, jsonraw.sess);
                insert_cmd.Parameters.AddWithValue("@site", NpgsqlTypes.NpgsqlDbType.Text, jsonraw.site);
                insert_cmd.Parameters.AddWithValue("@username", NpgsqlTypes.NpgsqlDbType.Text, jsonraw.user);
                insert_cmd.Parameters.AddWithValue("@filter_name", NpgsqlTypes.NpgsqlDbType.Text, level);
                insert_cmd.Parameters.AddWithValue("@filter_vals", NpgsqlTypes.NpgsqlDbType.Text, member);
                insert_cmd.Parameters.AddWithValue("@workbook", NpgsqlTypes.NpgsqlDbType.Text, "");
                insert_cmd.Parameters.AddWithValue("@view", NpgsqlTypes.NpgsqlDbType.Text, "");
                insert_cmd.Parameters.AddWithValue("@hostname", NpgsqlTypes.NpgsqlDbType.Text, Program.strHostName);
                insert_cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Close the TabMon database connection
        /// </summary>
        public void closeDB()
        {
            if (TabMon_conn != null)
                TabMon_conn.Close();
        }

        /// <summary>
        /// Not in actual used!!!
        /// 
        /// </summary>
        void findLevelAndMember(string xmlString, out string level, out string member)
        {
            level = "";
            member = "";
            string pattern1 = @"<groupfilter function='member' level='(.*?)' member='(.*?)'.*?/>";

            MatchCollection mc = Regex.Matches(xmlString, pattern1);
            foreach (Match m in mc)
            {
                level = m.Groups[1].ToString();
                member = m.Groups[2].ToString();
            }
        }
        /// <summary>
        /// Not implemented!!!
        /// </summary>
        public void createTable()
        {

            // 1. Connect to server to create database:
            const string connStr = "Server=localhost;Port=5432;User Id=postgres;Password=enter;";

            // 2. Connect to server to create table:
            const string connStr2 = "Server=localhost;Port=5432;User Id=postgres;Password=enter;Database=testDb";


            var m_conn = new NpgsqlConnection(connStr); // db connction
            var m_conn2 = new NpgsqlConnection(connStr2); // table connection

            // creating a database in Postgresql
            var m_createdb_cmd = new NpgsqlCommand("CREATE DATABASE IF NOT EXISTS  \"testDb\" " +
                                           "WITH OWNER = \"postgres\" " +
                                           "ENCODING = 'UTF8' " +
                                           "CONNECTION LIMIT = -1;", m_conn);

            // creating a table in Postgresql
            var m_createtbl_cmd = new NpgsqlCommand
            {
                CommandText = "CREATE TABLE table1(ID CHAR(256) CONSTRAINT id PRIMARY KEY, Title CHAR)"
            };

            m_createtbl_cmd.Connection = m_conn2;

            // 3.. Make connection and create

            // open connection to create DB
            m_conn.Open();
            m_createdb_cmd.ExecuteNonQuery();
            m_conn.Close();

            // open connection to create table
            m_conn2.Open();
            m_createtbl_cmd.ExecuteNonQuery();
            m_conn2.Close();
        }

        /// <summary>
        /// This is a destructor/finalizer 
        /// it close the DB connection (if it's even exist) 
        /// </summary>
        ~PostgreSQL()
        {
            if (TabMon_conn != null)
                TabMon_conn.Close();
        }
    }
}