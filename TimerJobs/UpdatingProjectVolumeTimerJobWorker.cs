using Legenda.ProjSpace.Main.Logging;
using Legenda.ProjSpace.Main.Services;
//using Legenda.ProjSpace.Core.Services.Logging;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using System;
using System.Linq;
using System.Threading;

namespace Legenda.ProjSpace.Main.TimerJobs
{
    public class UpdatingProjectVolumeTimerJobWorker : SPJobDefinition
    {
        private static readonly string JobName = typeof(UpdatingProjectVolumeTimerJobWorker).FullName;

        public readonly string JobTitle = "[ProjSpace] Обновление по объёмам у неизвлеченных проектов";

        public override string Description => "[ProjSpace] Обновление по объёмам у неизвлеченных проектов";

        public UpdatingProjectVolumeTimerJobWorker()
        {
        }

        public UpdatingProjectVolumeTimerJobWorker(string jobName, SPService service)
            : base(jobName, service, null, SPJobLockType.Job)
        {
            base.Title = this.JobTitle;
            Name = jobName;
        }

        public UpdatingProjectVolumeTimerJobWorker(string jobName, SPWebApplication webapp)
            : base(jobName, webapp, null, SPJobLockType.Job)
        {
            base.Title = this.JobTitle;
            Name = jobName;
        }

        public override void Execute(Guid targetInstanceId)
        {
            Logger.Log("!!! UpdatingProjectVolumeTimerJobWorker Start");
            try
            {
                Logger.Log("Start thread");
                new Thread((ThreadStart)delegate
                {
                    string url = ((SPWebApplication)base.Parent).Sites.First().Url;
                    Logger.Log("Start CallService() UpdatingProjectVolumeSrv url:" + url);
                    PsService.CallGetWebService(url, "/_vti_bin/Legenda/ProjSpace/WebService.svc/UpdatingProjectVolumeSrv");
                }).Start();
                Logger.Log("Thread started");
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        internal static void AddOrUpdateJobToFarm(SPWebApplication webApplication)
        {
            RemoveJobFromFarm(webApplication);
            UpdatingProjectVolumeTimerJobWorker updatingProjectVolume = new UpdatingProjectVolumeTimerJobWorker(JobName, webApplication)
            {
                Schedule = new SPDailySchedule
                {
                    BeginHour = 23,
                    EndHour = 23,
                    BeginMinute = 10,
                    EndMinute = 20
                }
            };
            updatingProjectVolume.Update();
        }

        internal static void RemoveJobFromFarm(SPWebApplication webApplication)
        {
            SPJobDefinition sPJobDefinition = webApplication.JobDefinitions.FirstOrDefault(j => j.Name == JobName);
            sPJobDefinition?.Delete();
        }

    }
}
