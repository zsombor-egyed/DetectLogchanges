using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Permissions;
using System.Text;
using System.Threading;

namespace DetectLogchanges
{
    class SingleThreadFileWatcher
    {
        string watchedFolderPath;
        string filter; //e.g "*.txt"
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

        /// <summary>
        /// Read the initial state of files. 
        /// Count how many lines are in a file.
        /// Take the results to a Dictionary<string, long> (filename, line count)
        /// </summary>
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
                Console.WriteLine(fileName + ": " + numberOfLines);
            }
        }

        /// <summary>
        /// Go over the files in a specific folder, and watch for changes.
        /// </summary>                  
        public void watchChanges()
        {
            while (true)
            {
                Thread.Sleep(1500);
                string[] fileEntries = Directory.GetFiles(watchedFolderPath, filter, SearchOption.AllDirectories);
                foreach (string fileName in fileEntries)
                {
                    try
                    {
                        writeOutChanges(fileName);
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine("Unexpected exception occured. "+e.StackTrace);
                    }

                }
            }
 
        }

        /// <summary>
        /// Read a specific file, and insert the new lines to the database. 
        /// 
        /// </summary>
        /// <param name="fullPath"></param>
        static void writeOutChanges(string fullPath)
        {
            if (stateOfFiles.ContainsKey(fullPath))
            {
                using (var fs = new FileStream(fullPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                {
                    string line;
                    long offset = 0;
                    //read the first part of the file (no changes here)
                    for (long i = 0; i < stateOfFiles[fullPath]; i++)
                        sr.ReadLine();

                    PostgreSQL pg = new PostgreSQL(dbconn); //open the DB connection
                    //read the new lines which appended to the file
                    while ((line = sr.ReadLine()) != null)
                    {
                        offset++;
                        try {
                            pg.insertToServerlogsTable(Path.GetFileName(fullPath), line);
                            Console.WriteLine(line);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Cannot add to database this line: "+line +"\nException: "+ e.StackTrace);
                        }
                    }
                    pg.closeDB(); //close database connection
                    stateOfFiles[fullPath] += offset;
                }
            }
            else
            {
                using (var fs = new FileStream(fullPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                {
                    long lineCounter = 0;
                    while (sr.ReadLine() != null)
                        lineCounter++;
                    stateOfFiles.Add(fullPath, lineCounter);
                }
            }
        }
    }
}