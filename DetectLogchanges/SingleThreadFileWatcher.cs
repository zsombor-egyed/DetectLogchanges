using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace DetectLogchanges
{
    class SingleThreadFileWatcher
    {
        string watchedFolderPath;
        string filter; //e.g "*.txt
        static string dbconn;
        static Dictionary<string, long> stateOfFiles; //contains filename and actual number of lines in the file
        public SingleThreadFileWatcher(string folderpath, string filter, string pgsqlConnString)
        {
            stateOfFiles = new Dictionary<string, long>();
            dbconn = pgsqlConnString;
            watchedFolderPath = folderpath;
            this.filter = filter;
            initFileState();
        }

        void initFileState()
        {
            string[] fileEntries = Directory.GetFiles(watchedFolderPath, filter, SearchOption.AllDirectories);
            foreach (string fileName in fileEntries)
            {
                long numberOfLines = 0;
                using (var fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                {                    
                    while (sr.ReadLine() != null)
                        numberOfLines++;
                }
                if (!stateOfFiles.ContainsKey(fileName))
                    stateOfFiles.Add(fileName, numberOfLines);
                Console.WriteLine(fileName+": "+ numberOfLines);
            }
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void watchChanges()
        {
            FileSystemWatcher watcher = new FileSystemWatcher(Path.GetDirectoryName(watchedFolderPath), filter);
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Changed += new FileSystemEventHandler(OnChanged);

            // Begin watching.
            watcher.EnableRaisingEvents = true;
            // Wait for the user to quit the program.
            Console.WriteLine("Press \'q\' to quit the sample.");
            while (Console.Read() != 'q') ;
        }

        // Define the event handlers.
        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            if(stateOfFiles.ContainsKey(e.FullPath))
            {
                using (var fs = new FileStream(e.FullPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                {
                    string line;
                    long offset = 0;
                    //read the first part of the file (no changes here)
                    for (long i = 0; i < stateOfFiles[e.FullPath]; i++)
                        sr.ReadLine();
                    PostgreSQL pg = new PostgreSQL(dbconn); //open the DB connection
                    //read the new lines which appended to the file
                    while ((line = sr.ReadLine()) != null)
                    {
                        offset++;
                        //    pg.insertToServerlogsTable(Path.GetFileName(fullpath), line);
                        Console.WriteLine(line);
                    }
                    pg.closeDB(); //close database connection
                    stateOfFiles[e.FullPath] += offset;
                }
            }
            else
            {
                using (var fs = new FileStream(e.FullPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                {
                    long lineCounter = 0;
                    while ( sr.ReadLine() != null)
                        lineCounter++;    
                                   
                    stateOfFiles.Add(e.FullPath, lineCounter);
                }
            }
        }
    }
}