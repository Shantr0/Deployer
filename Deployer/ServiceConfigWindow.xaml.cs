using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private BindingList<Service> services;

        private string actualVersion
        {
            get { return config.CurrentVersion; }
            set { config.CurrentVersion = value; }
        }

        private ILog log = LogManager.GetLogger("Deployer");
        private readonly MainWindow window;
        private bool _changed;

        public ServiceConfigWindow(MainWindow window)
        {
            InitializeComponent();
            this.window = window;
            Init();
        }

        private string GetSelectedVersion()
        {
            int index = ServiceVersionsTable.SelectedIndex;
            string[] versions = config.Versions.Keys.ToArray();
            if (index >= 0)
            {
                string v = versions[index];
                return v;
            }
            return null;
        }
        private void Init()
        {
            config = window.GetSelected();
            services =new BindingList<Service>(config.GetAllVersionServices());
            services.AddingNew += AddServiceVersion;
            actualVersion = config.CurrentVersion;
            ActualVersionLabel.Content = actualVersion;
            //if (services.Count > 0 && config.IsBuilded()) BackupBuildButton.Content = "Backup";
            //else BackupBuildButton.Content = "Build";
            ServiceVersionsTable.ItemsSource = services;
        }

        private void AddServiceVersion(Object sender, AddingNewEventArgs a)
        {
            _changed = true;
        }

        private string BackupService(string newVersion)
        {
            string res= config.BackupService(newVersion);
            Save();
            return res;
        }
        private void Save()
        {
            LogMessage("сохраняется конфигурация");
            window.saveConfigs();
            _changed = false;
            LogMessage("конфигурация сохранена");
        }

        private void refresh()
        {
            ServiceVersionsTable.ItemsSource = services;
            ActualVersionLabel.Content = config.CurrentVersion;
            //ServiceVersionsTable.UpdateLayout();
        }
        private void LogMessage(string message)
        {
            LogTextBox.AppendText(message+"\n");
            log.Info(message);
        }

        private void Error(Exception e)
        {
            LogTextBox.AppendText(e.Message+'\n');
            LogTextBox.AppendText(e.StackTrace+'\n');
            LogTextBox.AppendText(e.Source+'\n');
            LogTextBox.AppendText(e.Data.ToString());
            log.Error(e.Message);
            log.Error(e.StackTrace);
            log.Error(e.Source);
            log.Error(e.Data.ToString());
            if (e.InnerException != null) Error(e.InnerException);
        }
        public void DeployService(string newVersion)// развертывание новой версии
        {
            int status = 0;
            try
            {
                LogMessage("создается резервная копия сервиса");
                string result= BackupService(actualVersion);
                status++;//1
                if (result == "done") LogMessage("копия успешно создана, останавливаю сервис");
                else LogMessage(result);
                config.StopService();
                status++;//2
                LogMessage(config.ServiceName+" остановлен, устанавливается обновление");
                config.Build(newVersion);
                status++;//3
                LogMessage("обновление завершено, запускаю сервис");
                config.StartService();
                status++;//4
                LogMessage($"сервис {config.ServiceName} запущен успешно");
            }
            catch (Exception e)
            {
                LogMessage($"ошибка развертывания на этапе {status}");
                Error(e);
            }
        }
        public void Rollback(string version)// откат на существующую версию version
        {
            config.StopService();
            config.Build(version);
            config.StartService();
        }
        private void BackupBuildButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                
                string newVersion = config.IsMultiversion ? NewVersionTextBox.Text : "backup";
                BackupService(newVersion);
                services = new BindingList<Service>(config.GetAllVersionServices());
                ServiceVersionsTable.ItemsSource = services;
                LogMessage("резервная копия " + newVersion + " cоздана");
                refresh();
            }
            catch (Exception exception)
            {
                Error(exception);
            }
        }

        private void DeployButton_Click(object sender, RoutedEventArgs e)
        {
            string newVersion = NewVersionTextBox.Text;
            LogMessage("начинается развертывание приложения");
            DeployService(newVersion);
            LogMessage("Приложение успешно развернуто");
            refresh();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var item = ServiceVersionsTable.SelectedCells[0]; // версия
                string version = ((Service) item.Item).Version;
                LogMessage("Проводится удаление версии " + version);
                config.DeleteVersion(version);
                Save();
                LogMessage(version + " удален");
                services = new BindingList<Service>(config.GetAllVersionServices());
                refresh();
            }
            catch (ArgumentOutOfRangeException ex)
            {
                if(config.Versions.Count==0)
                    throw new Exception("Не найдено собранных версий. Выполните сборку");
            }
            catch (Exception exception)
            {
                Error(exception);
            }
        }

        private void ServiceLauncherButton_Click(object sender, RoutedEventArgs e)
        {
            System.ServiceProcess.ServiceController ser = new System.ServiceProcess.ServiceController(config.ServiceName);
            LogMessage($"статус службы {ser.Status}");
            if(ser.Status==ServiceControllerStatus.Running) config.StopService();
            else if(ser.Status==ServiceControllerStatus.Stopped || ser.Status==ServiceControllerStatus.Paused) config.StartService();
            LogMessage($"статус службы изменен на {ser.Status}");
        }

        private void SaveChangeButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void BuildButton_Click(object sender, RoutedEventArgs e)
        {
            string version = GetSelectedVersion();
            if (version != null) config.Build(version);
            actualVersion = config.CurrentVersion;
            ActualVersionLabel.Content = actualVersion;
            LogMessage("Перестроение завершено, текущая версия"+actualVersion);
            refresh();
            Save();
        }
    }
}
