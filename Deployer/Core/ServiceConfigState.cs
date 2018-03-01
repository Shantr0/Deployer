using System.ServiceProcess;
using Newtonsoft.Json;

namespace Deployer.Core
{
    public class ServiceConfigState: ServiceConfig
    {
        private ServiceControllerStatus status;

        [JsonIgnore]
        public ServiceControllerStatus Status
        {
            get { return status; }
            //set { status = value; }
        }
        public ServiceConfigState(ServiceController controller)
        {
            status = controller.Status;
            ServiceName = controller.ServiceName;
            CurrentVersion = "";
            DeployPath = controller.ServiceName;
        }
        public ServiceConfigState()
        {
            status = ServiceControllerStatus.Stopped;
        }
        public ServiceConfigState(ServiceControllerStatus status) : base()
        {
             this.status = status;
        }

        public ServiceConfigState(Service service, string sorcePath, ServiceControllerStatus? status = null) : base(service, sorcePath)
        {
            if(status == null) this.status = ServiceControllerStatus.Stopped;
            else this.status = (ServiceControllerStatus)status;
        }
        public ServiceConfigState(string exePath, string sorcePath, ServiceControllerStatus? status = null) : base(exePath, sorcePath)
        {
            if (status == null) this.status = ServiceControllerStatus.Stopped;
            else this.status = (ServiceControllerStatus)status;
        }

        public ServiceController GetServiceController()
        {
            ServiceController service=new ServiceController(ServiceName);
            //service.Refresh();
            return service;
        }
        public void UpdateStatus()
        {
            status = GetServiceController().Status;
        }

        //public ServiceControllerStatus GetStatus()
        //{
        //    return status;
        //}
    }
}