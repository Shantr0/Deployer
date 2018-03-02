using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.ServiceProcess;
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
        private void Error(Exception e)
        {
            Output.AppendText(e.Message + '\n');
            Output.AppendText(e.StackTrace + '\n');
            Output.AppendText(e.Source + '\n');
            Output.AppendText(e.Data.ToString());
            //log.Error(e.Message);
            //log.Error(e.StackTrace);
            //log.Error(e.Source);
            //log.Error(e.Data.ToString());
            if (e.InnerException != null) Error(e.InnerException);
        }
        public void SaveConfigs()
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
            //int index = ServiceTable.SelectedIndex;
            ServiceConfigState selectedConfigState =(ServiceConfigState) ServiceTable.SelectedItem;
            return selectedConfigState;
            //try
            //{
            //    if (index >= 0)
            //    {
            //        var config= services[index]=selectedConfigState;
            //        return config;
            //    }
            //    else return null;
            //}
            //catch (Exception e)
            //{
            //    LogMessage(e.Message);
            //    return null;
            //}
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog=new OpenFileDialog(){Filter = "exe files (*.exe)|*.exe" };
            if (dialog.ShowDialog() == true)
            {
                string fname = dialog.FileName;
                Service service=new Service(fname);
                service.Version = "";
                ServiceConfigState config=new ServiceConfigState(service,"");// sourcePath пока пустой
                services.Add(config);
                //ServiceTable.ItemsSource = services;
                LogMessage($"{fname} добавлен");
            }
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveConfigs();
        }

        private void Init()
        {
            string file = "Services.json";
            string text= File.ReadAllText(file);
            services = ReflectionAnalyser.ReadFromJsonString<ServiceList>(text);
            if(services==null)services=new ServiceList();
            else foreach (var service in services) { service.UpdateStatus(); }
            ServiceTable.ItemsSource = services;
            services.AddingNew += AddElement;
            services.ListChanged += ListChange;
        }

        private void AddElement(object sender, AddingNewEventArgs args)
        {
            SaveConfigs();
        }
        private void ListChange(object sender, ListChangedEventArgs args)
        {
            SaveConfigs();
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
            
            try
            {
                if (config != null)
                {
                    LogMessage("проводится обновление");
                    config.Build();
                    LogMessage("обновление завершено");
                }
            }
            catch (ArgumentException exception)
            {
                if(string.IsNullOrEmpty(config.SourcePath)) LogMessage("не задан источник обновлений");
                Output.AppendText(exception.Message);
            }
            catch (Exception ex)
            {
                Output.AppendText(ex.Message);
            }
            
        }

        private void ServiceLauncherButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int index = ServiceTable.SelectedIndex;
                ServiceConfig config = GetSelected();
                if(config==null)return;
                ServiceController ser = new ServiceController(config.ServiceName);
                LogMessage($"статус службы {ser.Status}");

                if (ser.Status == ServiceControllerStatus.Running)
                {
                    config.StopService();
                    ser.Refresh();
                    //ser.WaitForStatus(ServiceControllerStatus.Stopped);
                }
                else if (ser.Status == ServiceControllerStatus.Stopped || ser.Status == ServiceControllerStatus.Paused)
                {
                    config.StartService();
                    ser.Refresh();
                    //ser.WaitForStatus(ServiceControllerStatus.Running);
                }
                //DataGridRow row=(DataGridRow) ServiceTable.SelectedItem;
                //row.Item=ser.Status;
                LogMessage($"статус службы изменен на {ser.Status}");
                services[index].UpdateStatus();
                ServiceTable.ItemsSource = null;
                ServiceTable.ItemsSource = services;
                //ServiceTable.SelectedIndex = index;
            }
            catch (Exception exception)
            {
                Error(exception);
            }
        }
    }
}
