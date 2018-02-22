using System;
using System.Collections.Generic;
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
        private List<ServiceConfig> services=new List<ServiceConfig>();
        public MainWindow()
        {
            InitializeComponent();
            Init();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog=new OpenFileDialog(){Filter = "exe files (*.exe)|*.exe" };
            if (dialog.ShowDialog() == true)
            {
                string fname = dialog.FileName;
                Service service=new Service(fname);
                ServiceConfig config=new ServiceConfig(service,"");
                services.Add(config);
                ServiceTable.ItemsSource = services;
                Output.AppendText($"{fname} добавлен");
            }
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            string jsonServices = JsonConvert.SerializeObject(services, Formatting.Indented);
            //string jsonServices = ReflectionAnalyser.ToJsonString(services);
            string path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Services.json") ;
            File.WriteAllText(path, jsonServices);
            Output.AppendText($"конфигурация  сохранена: " + path);
        }

        private void Init()
        {
            string file = "Services.json";
            string text= File.ReadAllText(file);
            services = ReflectionAnalyser.ReadFromJsonString<List<ServiceConfig>>(text);
            ServiceTable.ItemsSource = services;
        }


        private void changeButton_Click(object sender, RoutedEventArgs e)
        {
            int index = ServiceTable.SelectedIndex;
            var config= services[index];
            ServiceConfigWindow window=new ServiceConfigWindow(config);
            window.ShowDialog();
        }
    }
}
