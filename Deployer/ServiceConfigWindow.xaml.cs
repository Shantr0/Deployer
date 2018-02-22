using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using Deployer.Core;
using log4net;
using log4net.Repository.Hierarchy;


namespace Deployer
{
    /// <summary>
    /// Логика взаимодействия для ServiceConfigWindow.xaml
    /// </summary>
    public partial class ServiceConfigWindow : Window
    {
        private ServiceConfig config;
        private List<Service> services;
        private ILog log = LogManager.GetLogger("Deployer");
        public ServiceConfigWindow(ServiceConfig config)
        {
            InitializeComponent();
            this.config = config;
            services = config.GetAllVersionServices();
        }

        private void Init()
        {
            ServiceVersionsTable.ItemsSource = services;
        }
        private void LogMessage(string message)
        {
            LogTextBox.AppendText(message);
            log.Info(message);
        }

        private void Error(Exception e)
        {
            LogTextBox.AppendText(e.Message);
            LogTextBox.AppendText(e.StackTrace);
            LogTextBox.AppendText(e.Source);
            LogTextBox.AppendText(e.Data.ToString());
            log.Error(e.Message);
            log.Error(e.StackTrace);
            log.Error(e.Source);
            log.Error(e.Data.ToString());
            if (e.InnerException != null) Error(e.InnerException);
        }
        public void DeployService(string version)
        {
            try
            {
                int status = 0;
                LogMessage("создается резервная копия сервиса");
                config.BackupService(version);
                status++;
                LogMessage("копия успешно создана, останавливаю сервис");
                config.StopService();
                status++;
                LogMessage(config.ServiceName+" остановлен, устанавливается обновление");
                config.Build();
                status++;
                LogMessage("обновление завершено, запускаю сервис");
                config.StartService();
                status++;
                LogMessage($"сервис {config.ServiceName} запущен успешно");
            }
            catch (Exception e)
            {
                LogMessage($"ошибка развертывания на этапе ");
                Error(e);
            }
        }
        public void Rollback(string version)// откат на версию version
        {
            //config.currentVersion = IsMultiversion ? version : "";
            config.StopService();
            config.Build(config.GetVersionBackupFolder(version));
            config.StartService();
        }
        private void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            string newVersion = NewVersionTextBox.Text;
            config.BackupService(newVersion);
        }

        private void DeployButton_Click(object sender, RoutedEventArgs e)
        {
            string newVersion = NewVersionTextBox.Text;
            DeployService(newVersion);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var item = ServiceVersionsTable.SelectedCells[3];// версия
            string version =(string) item.Item;
            config.DeleteVersion(version);
        }

        private void ServiceLauncherButton_Click(object sender, RoutedEventArgs e)
        {
            System.ServiceProcess.ServiceController ser = new System.ServiceProcess.ServiceController(config.ServiceName);
            LogMessage($"статус службы {ser.Status}");
            if(ser.Status==ServiceControllerStatus.Running) config.StopService();
            else if(ser.Status==ServiceControllerStatus.Stopped || ser.Status==ServiceControllerStatus.Paused) config.StartService();
            LogMessage($"статус службы изменен на {ser.Status}");
        }
    }
}
