using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Newtonsoft.Json;

namespace Deployer.Core
{
    public class ServiceList:BindingList<ServiceConfig>
    {
        private string configsPath;

        public ServiceList():base()
        {
            configsPath = Path.Combine(Directory.GetCurrentDirectory(), "Services.json");
        }
        public ServiceList(string path)
        {
            configsPath = path;
        }

        public void CommitChanges()
        {
            string jsonServices = JsonConvert.SerializeObject(this, Formatting.Indented);
            string path = configsPath;
            File.WriteAllText(path, jsonServices);
        }
    }
}