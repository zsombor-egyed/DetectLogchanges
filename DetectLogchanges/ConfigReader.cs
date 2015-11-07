using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DetectLogchanges
{
    class ConfigReader
    {
        String path;
        public ConfigReader()
        {

        }

        public ConfigReader(String configFilePath)
        {
            path = configFilePath;
        }

        public List<String> readConfig()
        {
            XmlTextReader xreader;
            List<String> configs = new List<String>();
            if (path != null)
            {
               xreader = new XmlTextReader(path);
            }
            else
            {
                xreader = new XmlTextReader(@"D:\TabMon_FileWatcher\TabMon\Config\TabMon.config");
            }     
            xreader.ReadToFollowing("watch_directory");
            xreader.MoveToFirstAttribute();
            string watchedDirectory = xreader.Value;
        
            configs.Add(watchedDirectory);

            xreader.ReadToFollowing("logs");
            xreader.MoveToFirstAttribute();
            string match = xreader.Value;
            configs.Add(match);

            return configs;

        }

    }
}
