using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Deployer.Core
{
    public class Service: ICloneable
    {
        public Service()
        {
            Version = "0.0";
        }

        public string ExeName { get; set; }// имя exe файла 
        public string ServicePath { get; set; }// путь к ехе файлу
        public string ServiceName { get; set; }// имя сервиса
        public string Version { get; set; }// версия
        public string FullPath
        {
            get { return ServicePath + "\\" + ExeName; }
        }


        public Service(string exePath)
        {
            ExeName = System.IO.Path.GetFileName(exePath);
            ServicePath = System.IO.Path.GetDirectoryName(exePath);
            ServiceName = new Regex("\\.exe$").Replace(ExeName, "");
        }

        public object Clone()
        {
            Service clone = new Service
            {
                ExeName = ExeName,
                ServicePath = ServicePath,
                ServiceName = ServiceName,
                Version = Version
            };
            return clone;
        }
    }
}
