using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace DetectLogchanges
{
    /// <summary>
    /// TODO: beégetett pareméterekkel kezdeni valamit, illetve exception handling 
    /// </summary>
    class Program
    {
        public static string strHostName = string.Empty;
        public static string pgsqlConnString;
        static string configFilePath;
        static ConfigReader cfr;
        static List<string> configlist;

        /// <summary>
        /// set hostname, DB connection string and read configuration from TabMon.config
        /// </summary>
        static void init()
        {
            // Getting the host name of local machine.
            strHostName = Dns.GetHostName();

            pgsqlConnString = "Server=palettepg.cakavtkziz1k.us-west-1.rds.amazonaws.com;Port=5432;User Id=palette;Password=palette123;Database=TabMon";
            //set config file path
            configFilePath = @"C:\tmp\TabMon.config";
            //read configuration 
            cfr = new ConfigReader(configFilePath);
            configlist = cfr.readConfig(); //save configuration
        }
     
        static void Main(string[] args)
        {
            init();
            //configlist.ElementAt(0) is path to watched folder
            //configlist.ElementAt(1) is a pattern e.g. "*.txt"
            SingleThreadFileWatcher stfw = new SingleThreadFileWatcher(configlist.ElementAt(0), configlist.ElementAt(1),
                pgsqlConnString);
            //start watching the files
            stfw.watchChanges();
        } 

    }
}
