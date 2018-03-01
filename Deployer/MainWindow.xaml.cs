using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Deployer.Core;
using Microsoft.Win32;
using Newtonsoft.Json;
using TCB.UBank.Core.Helpers;

namespace Deployer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ServiceList services=new ServiceList();

        private void LogMessage(string message)
        {
            Output.AppendText(DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss")+ " " + message +'\n');
        }
        public void saveConfigs()
        {
            string jsonServices = JsonConvert.SerializeObject(services, Formatting.Indented);
            string path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Services.json");
            File.WriteAllText(path, jsonServices);
            LogMessage($"конфигурация  сохранена: {path} ");
        }
        public void SaveConfigs(string fileName)
        {
            string jsonServices = JsonConvert.SerializeObject(services, Formatting.Indented);
            string path = fileName;
            File.WriteAllText(path, jsonServices);
            LogMessage($"конфигурация  сохранена: {path}");
        }
        public MainWindow()
        {
            InitializeComponent();
            Init();
        }

        public ServiceConfig GetSelected()
        {
            int index = ServiceTable.SelectedIndex;
            if (index >= 0)
            {
                var config = services[index];
                return config;
            }
            else return null;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog=new OpenFileDialog(){Filter = "exe files (*.exe)|*.exe" };
            if (dialog.ShowDialog() == true)
            {
                string fname = dialog.FileName;
                Service service=new Service(fname);
                service.Version = "";
                ServiceConfig config=new ServiceConfig(service,"");// sourcePath пока пустой
                services.Add(config);
                //ServiceTable.ItemsSource = services;
                LogMessage($"{fname} добавлен");
            }
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            saveConfigs();
        }

        private void Init()
        {
            string file = "Services.json";
            string text= File.ReadAllText(file);
            services = ReflectionAnalyser.ReadFromJsonString<ServiceList>(text);
            if(services==null)services=new ServiceList();
            ServiceTable.ItemsSource = services;
            services.AddingNew += AddElement;
            services.ListChanged += ListChange;
        }

        private void AddElement(object sender, AddingNewEventArgs args)
        {
            saveConfigs();
        }
        private void ListChange(object sender, ListChangedEventArgs args)
        {
            //saveConfigs();
        }
        private void changeButton_Click(object sender, RoutedEventArgs e)
        {
            var config = GetSelected();
            if (config != null)
            {
                ServiceConfigWindow window = new ServiceConfigWindow(this);
                window.ShowDialog();
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            ServiceConfig config = GetSelected();
            Output.AppendText("сборка началась \n");
            try
            {
                if (config != null)
                {
                    config.Build();
                    LogMessage("сборка успешно завершена");
                }
            }
            catch (Exception ex)
            {
                Output.AppendText(ex.Message);
            }
            
        }
    }
}
