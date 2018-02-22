using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Deployer.Core
{
     public class ServiceConfig
    {
        private Service baseService;

        private Dictionary<string,string> Versions { get; set; }// все версии пара- <версия, расположение>
        private string currentVersion;// рабочая версия сервиса
        public string ServiceName { get; set; }// название сервиса
        public string DeployPath { get; set; } // путь развертывания
        public string SourcePath { get; set; } // путь откуда будут браться обновления
        public bool IsMultiversion { get; set; }// много или 1 бекап

        public ServiceConfig(Service service, string sourcePath)
        {
            baseService = service;
            SourcePath = sourcePath;
            Init();
            currentVersion = baseService.Version;
        }
        public void AddVersion(string version,bool setAsCurrent=true)
        {
            AddVersion(version,CreateVersionFolderName(version),setAsCurrent);
        }

        public void AddVersion(string version, string path, bool setAsCurrent)
        {
            try
            {
                Versions.Add(version,path);
                if (setAsCurrent) currentVersion = version;
            }
            catch (ArgumentException e)
            {

                throw new ArgumentException("Такая версия уже существует", e);
            }
        }
        public string CreateVersionFolderName(string version)
        {
            return DeployPath + "_" + version;
        }

        protected void CopyFiles(string source, string dest, List<string> ignoreList = null)
        {
            List<string> files = Directory.GetFiles(source).ToList();
            if (ignoreList != null)
            {
                foreach (var ignoreFile in ignoreList)
                {
                    files.Remove(ignoreFile);
                }
            }

            foreach (string file in files)
            {
                File.Copy(file, dest);
            }
        }
        public void BackupService(string newVersion,List<string> ignoreFiles=null)
        {
            string backupFolder;
            if (IsMultiversion)
            {
                //AddVersion(newVersion);
                backupFolder = DeployPath + "_" + newVersion;
            }
            else
            {
                backupFolder = DeployPath + "_backup";
            }
            BackupService(newVersion,backupFolder,ignoreFiles);
        }

        public void DeleteVersion(string version)
        {
            string path = Versions[version];
            Directory.Delete(path,true);
            Versions.Remove(version);
        }
        public void BackupService(string newVersion, string backupDirectory, List<string> ignoreFiles = null)
        {
            if (Versions.ContainsKey(newVersion) && !IsMultiversion) throw new ArgumentException($"версия ({Versions}) уже существует");
            if (!Directory.Exists(backupDirectory)) Directory.CreateDirectory(backupDirectory);
            if(IsMultiversion) AddVersion(newVersion,false);
            CopyFiles(DeployPath, backupDirectory, ignoreFiles);
        }
        protected void SetCurVersion(string version)
        {
            if(Versions.ContainsKey(version))currentVersion = version;
            else AddVersion(version,true);
        }

        protected void SetVersionPath(string version, string newPath)
        {
            if (Versions.ContainsKey(version)) Versions[version] = newPath;
            else Versions.Add(version,newPath);
        }

        public void BackupService(List<string> ignoreFiles = null)
        {
            if(!IsMultiversion) BackupService("");
        }
        public void StopService()
        {
            System.ServiceProcess.ServiceController ser = new System.ServiceProcess.ServiceController(ServiceName);
            ser.Stop();
        }
        public void StartService()
        {
            System.ServiceProcess.ServiceController ser = new System.ServiceProcess.ServiceController(ServiceName);
            ser.Start();
        }


        
        public void Build()
        {
            CopyFiles(SourcePath,DeployPath);
        }
        public void Build(string sourcePath)
        {
            CopyFiles(sourcePath, DeployPath);
        }

        public string GetVersionBackupFolder(string version)
        {
            return Versions[version];
        }
        public void Init()
        {
            if(baseService==null) baseService=new Service();
            ServiceName = baseService.ServiceName;
            DeployPath = baseService.ServicePath;
            Versions=new Dictionary<string, string>();
        }
        public List<Service> GetAllVersionServices()
        {
            List<Service> services=new List<Service>(Versions.Count);
            foreach (var version in Versions.Keys)
            {
                var newService = (Service) baseService.Clone();
                newService.Version = version;
                services.Add(newService);
            }
            return services;
        }
    }
}
