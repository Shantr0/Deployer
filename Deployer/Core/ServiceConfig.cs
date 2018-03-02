using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Remoting;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Deployer.Core
{
     public class ServiceConfig
    {
        private Service baseService;

        public Dictionary<string, string> Versions { get; } = new Dictionary<string, string>();// все версии пара- <версия, расположение>
        private string currentVersion;

        public string CurrentVersion
        {
            get { return currentVersion;}
            set
            {
                if (value != null)
                    if (Versions.ContainsKey(value))
                        currentVersion = value;
                    else AddVersion(value, true);
                else currentVersion = null;
            }
        } // рабочая версия сервиса

        public string ServiceName { get; set; }// название сервиса
        public string DeployPath { get; set; } // путь развертывания
        public string SourcePath { get; set; } // путь откуда будут браться обновления
        public string ExeFile { get; set; }
        public bool IsMultiversion { get; set; }// много или 1 бекап

        public bool IsBuilded()
        {
            return currentVersion != null;
        }

        public ServiceConfig()
        {
            baseService = new Service();
        }
        public ServiceConfig(string exePath, string sourcePath)
        {
            Service service=new Service(exePath);
            baseService = service;
            SourcePath = sourcePath;
            Init();
            if (!IsMultiversion) AddVersion("",true);
            else AddVersion(service.Version,true);
            CurrentVersion = service.Version;
        }
        public ServiceConfig(Service service, string sourcePath)// создание конфигурации сервиса service с уазанием источника обновлений sourcePath
        {
            baseService = service;
            SourcePath = sourcePath;
            Init();

            if (!IsMultiversion) CurrentVersion = "";
            else CurrentVersion = service.Version;
        }

        //protected void RegisterService()
        //{
        //    if(IsMultiversion)
        //}
        public void AddVersion(string version,bool setAsCurrent)
        {
            AddVersion(version,CreateVersionFolderName(version),setAsCurrent);
        }

        public void AddVersion(string version, string path, bool setAsCurrent)
        {
            try
            {
                Versions.Add(version, path);
                if (setAsCurrent) CurrentVersion = version;
                // можно добавить обновление
            }
            catch (ArgumentNullException e)
            {
                if(version==null)
                    throw new ArgumentNullException("Версия не указана", e);
            }
            catch (ArgumentException e)
            {
                if (version != null)
                    throw new ArgumentException($"Версия {version} уже существует", e);
                else throw e;
            }
        }

        public string CreateVersionFolderName(string version)
        {
            string path =version!=""? DeployPath + "_" + version:DeployPath;
            return path;
        }

        protected void CopyFiles(string source, string dest, List<string> ignoreList = null)
        {
            source = source.Replace(@"\", @"\\")+"\\\\";
            DirectoryInfo directory=new DirectoryInfo(source);
            List<FileInfo> files = directory.GetFiles("*",SearchOption.AllDirectories).ToList();
            if (ignoreList != null)
            {
                foreach (var ignoreFile in ignoreList)
                {
                    //files.Remove(ignoreFile);
                }
            }
            foreach (var file in files)
            {
                string name= new Regex(source).Replace(file.FullName,"");// имя файла в каталоге (включая подпапку) 
                string newName = Path.Combine(dest, name);
                string newPath= Path.GetDirectoryName(newName);
                /*if (!Directory.Exists(newPath)) */Directory.CreateDirectory(newPath);
                File.Copy(file.FullName, newName,true);
            }
        }
        public string BackupService(string newVersion, bool changeExistVersion, List<string> ignoreFiles=null)
        {
            string backupFolder;
            //if(!IsBuilded())throw new ArgumentException($"не найден собранный экземпляр. Выполните сборку перед созданием резервной копии");
            if (IsMultiversion)
            {
                //AddVersion(newVersion);
                backupFolder = DeployPath + "_" + newVersion;
            }
            else
            {
                backupFolder = DeployPath + "_backup";
            }
            return BackupService(newVersion,backupFolder,changeExistVersion,ignoreFiles);
        }

        //public string BackupService(string oldVersion, string newVersion, string backupDirectory, List<string> ignoreFiles = null)
        //{
        //    string fromPath = Versions[oldVersion];
        //    if (Versions.ContainsKey(newVersion) && !IsMultiversion) return ($"версия ({Versions}) уже существует");
        //    if (!Directory.Exists(backupDirectory)) Directory.CreateDirectory(backupDirectory);
        //    if (IsMultiversion) AddVersion(newVersion, false);
        //    else if (!Versions.ContainsKey(newVersion)) AddVersion("backup");
        //    CopyFiles(DeployPath, backupDirectory, ignoreFiles);
        //    return "done";
        //}

        public void DeleteVersion(string delVersion)
        {
            if (delVersion == currentVersion)
            {
                var ex= new InvalidOperationException($"Удаление действующей версии недопустимо");
                ex.Data.Add("version",delVersion);
                throw ex;
            }
            string path = Versions[delVersion];
            Directory.Delete(path,true);
            Versions.Remove(delVersion);
        }
        public string BackupService(string newVersion, string sourceDirectory, string backupDirectory, List<string> ignoreFiles = null)
        {
            return BackupService(newVersion, sourceDirectory, backupDirectory, false, ignoreFiles);
        }
        public string BackupService(string newVersion,string sourceDirectory, string backupDirectory,bool changeExistVersion, List<string> ignoreFiles = null)
        {
            if (Versions.ContainsKey(newVersion) && IsMultiversion && !changeExistVersion) return ($"версия ({Versions}) уже существует");
            Directory.CreateDirectory(backupDirectory);
            if (IsMultiversion) AddVersion(newVersion,backupDirectory, false);
            else if (!Versions.ContainsKey(newVersion))
                AddVersion("backup",backupDirectory, false);
            else
                Versions[newVersion] = backupDirectory;
            if(backupDirectory!=sourceDirectory)
                CopyFiles(sourceDirectory, backupDirectory, ignoreFiles);
            return "done";
        }
        public string BackupService(string newVersion, string backupDirectory, bool changeExistVersion, List<string> ignoreFiles = null)
        {
            return BackupService(newVersion, DeployPath, backupDirectory, changeExistVersion,ignoreFiles);
            // генерим событие по добавлению версиии
        }
        public string BackupService(string newVersion, string backupDirectory, List<string> ignoreFiles = null)
        {
            return BackupService(newVersion, DeployPath, backupDirectory, ignoreFiles);
            // генерим событие по добавлению версиии

        }
        protected void SetCurVersion(string version)
        {
            if(Versions.ContainsKey(version))CurrentVersion = version;
            else AddVersion(version,true);
        }

        protected void SetVersionPath(string version, string newPath)
        {
            if (Versions.ContainsKey(version)) Versions[version] = newPath;
            else Versions.Add(version,newPath);
        }

        public void BackupService(List<string> ignoreFiles = null)
        {
            if(!IsMultiversion) BackupService("",true);
        }
        public void StopService()
        {
            ServiceController ser = new System.ServiceProcess.ServiceController(ServiceName);
            if(ser.Status!=ServiceControllerStatus.Stopped) ser.Stop();
            ser.WaitForStatus(ServiceControllerStatus.Stopped);
        }
        public void StartService()
        {
            ServiceController ser = new System.ServiceProcess.ServiceController(ServiceName);
            if (ser.Status != ServiceControllerStatus.Running) ser.Start();
            ser.WaitForStatus(ServiceControllerStatus.Running);
        }
        
        public void Build()// первоначальная сборка/ обновление
        {
            Directory.CreateDirectory(DeployPath);
            CopyFiles(SourcePath,DeployPath);
            //CurrentVersion = baseService.Version;
            if (Versions.Count==0) AddVersion(CurrentVersion,true);
        }
        public void Build(string version) // сборка новой версии из источника обновлений
        {
            Build(version,SourcePath);
        }
        public void Build(string version,string versionPath) // сборка существующей версии из директории версии
        {
            Directory.CreateDirectory(DeployPath);
            //string sourcePath = Versions[version];
            if (versionPath != DeployPath)
                CopyFiles(versionPath, DeployPath);
            if (!IsMultiversion)
            {
                CurrentVersion = "";
            }
            else CurrentVersion = version;
        }

        public void Rollback(string version)
        {
            string verPath = Versions[version];
            Build(version,verPath);
        }

        public void Init()
        {
            if(baseService==null) baseService=new Service();
            ServiceName = baseService.ServiceName;
            DeployPath = baseService.ServicePath;
            ExeFile = baseService.ExeName;
            //Versions=new Dictionary<string, string>();
        }
        public List<Service> GetAllVersionServices()
        {
            List<Service> services=new List<Service>(Versions.Count);
            foreach (var version in Versions.Keys)
            {
                var newService = (Service) baseService.Clone();
                newService.ServicePath = Versions[version];
                newService.ServiceName = ServiceName;
                newService.ExeName = ExeFile;
                newService.Version = version;
                services.Add(newService);
            }
            return services;
        }

        //public string GetCurrentVersion()
        //{
        //    return CurrentVersion;
        //}

        //public void SetCurrentVersion(string version)
        //{
        //    CurrentVersion = version;
        //}
    }
}
