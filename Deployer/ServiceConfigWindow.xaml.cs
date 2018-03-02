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
            var item =(Service) ServiceVersionsTable.SelectedItem;
            return item.Version;
            //string[] versions = config.Versions.Keys.ToArray();
            //if (index >= 0)
            //{
            //    string v = versions[index];
            //    return v;
            //}
            //return null;
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

        private string BackupService(string newVersion,bool saveExistVersion=false)
        {
            string res;
            if (CheckBoxPath.IsChecked == false)
                res = config.BackupService(newVersion, saveExistVersion);
            else res = config.BackupService(newVersion, PathTextBox.Text,saveExistVersion);
            Save();
            return res;
        }
        private void Save()
        {
            window.SaveConfigs();
            _changed = false;
            LogMessage("конфигурация сохранена");
        }

        private void Refresh()
        {
            services =new BindingList<Service>(config.GetAllVersionServices());
            ServiceVersionsTable.ItemsSource =services;
            ActualVersionLabel.Content = config.CurrentVersion;
            ServiceVersionsTable.SelectedIndex = 0;
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
            LogTextBox.AppendText(e.Data.ToString()+'\n');
            log.Error(e.Message);
            log.Error(e.StackTrace);
            log.Error(e.Source);
            log.Error(e.Data.ToString());
            if (e.InnerException != null) Error(e.InnerException);
        }
        public void DeployService(string version)// развертывание новой версии
        {
            bool typeNew = RadioButtonNewVersion.IsChecked == true;// создавать новую версию
            string deployVersion;
            if (typeNew)
                deployVersion = config.IsMultiversion ? version : "backup"; // бэкап и развертывание новой версии 
            else deployVersion = version;// деплой существующей версии (откат)
            DeployState status=0;
            try
            {
                if (typeNew)
                {
                    LogMessage("создается резервная копия сервиса...");
                    string result = BackupService(deployVersion, true);// сохраняется бэкап version
                    status=DeployState.Backuped;//1
                    if (result == "done") LogMessage("копия успешно создана, останавливаю сервис...");
                    else LogMessage(result);
                    config.StopService();
                    status++;//2
                    LogMessage(config.ServiceName + " остановлен, устанавливается обновление...");
                    config.Build(deployVersion);
                    status++;//3
                    LogMessage("обновление завершено, запускаю сервис...");
                    config.StartService();
                    status++;//4
                    LogMessage($"сервис {config.ServiceName} запущен успешно");
                }
                else
                {
                    LogMessage("Останавливается сервис...");
                    config.StopService();
                    status = DeployState.Stopped;
                    LogMessage(config.ServiceName + " остановлен, устанавливается новая версия");
                    config.Rollback(version);
                    status = DeployState.Builded;
                    LogMessage("обновление завершено, запускаю сервис...");
                    config.StartService();
                    status = DeployState.Started;
                    LogMessage($"сервис {config.ServiceName} запущен успешно");
                }
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
            config.Rollback(version);
            config.StartService();
        }
        private void BackupBuildButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                
                string newVersion = config.IsMultiversion ? NewVersionTextBox.Text : "backup";
                BackupService(newVersion,false);
                services = new BindingList<Service>(config.GetAllVersionServices());
                //ServiceVersionsTable.ItemsSource = services;
                LogMessage("резервная копия " + newVersion + " cоздана");
                Refresh();
            }
            catch (Exception exception)
            {
                Error(exception);
            }
        }

        private void DeployButton_Click(object sender, RoutedEventArgs e)
        {
            string newVersion = NewVersionTextBox.Text;
            int index = ServiceVersionsTable.SelectedIndex;
            if(index<0)return;
            LogMessage("начинается развертывание приложения");
            try
            {
                DeployService(newVersion);
                LogMessage("Приложение успешно развернуто");
                Refresh();
            }
            catch (Exception exception)
            {
                Error(exception);
            }
            
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
                //services = new BindingList<Service>(config.GetAllVersionServices());
                Refresh();
                
            }
            catch (InvalidOperationException ex) when (ex.Data.Contains("version"))
            {
                LogMessage(ex.Message);
            }
            catch (ArgumentOutOfRangeException ex) when(config.Versions.Count == 0)
            {
                LogMessage("Не найдено собранных версий. Выполните сборку");
            }
            catch (Exception exception)
            {
                Error(exception);
            }
        }

        private void ServiceLauncherButton_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void SaveChangeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Save();
            }
            catch (Exception exception)
            {
                LogMessage("ошибка сохранения");
                Error(exception);
            }
        }

        private void BuildButton_Click(object sender, RoutedEventArgs e)
        {
            string version = GetSelectedVersion();
            try
            {
                if (version != null) config.Build(version);
                actualVersion = config.CurrentVersion;
                ActualVersionLabel.Content = actualVersion;
                LogMessage("Перестроение завершено, текущая версия"+actualVersion);
                Refresh();
                Save();
            }
            catch (Exception exception)
            {
                Error(exception);
            }
        }
    }
}
