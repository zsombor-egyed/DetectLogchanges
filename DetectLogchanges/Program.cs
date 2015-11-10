using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace DetectLogchanges
{
    /// <summary>
    /// Watch some files and when any of it is changing we catch the change and save it to a postgreSQL database.
    /// 
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
            configFilePath = @"D:\TabMon_FileWatcher\TabMon\Config\TabMon.config";
            try
            {
                //read configuration 
                cfr = new ConfigReader(configFilePath);
                configlist = cfr.readConfig(); //save configuration
                if (configlist == null)
                {
                    Console.WriteLine("Could not read the configuration.");
                    System.Environment.Exit(1);
                }
            }
            catch(IOException e)
            {
                System.Console.WriteLine("Could not read the "+ configFilePath+" file. "+e.StackTrace);
                System.Environment.Exit(1);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.StackTrace);
                System.Environment.Exit(1);
            }
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