using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using Legenda.ProjSpace.Main.Logging;
using Legenda.ProjSpace.Main.Model;
//using Legenda.ProjSpace.Core.Services.Logging;
using Microsoft.SharePoint;
using Newtonsoft.Json;

namespace Legenda.ProjSpace.Main.Services
{

    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class WebService : IWebService
    {
        public string Ping()
        {
            Logger.Log("Ping called");
            return "Pong";
        }
        

        public List<ProjectDb> GetAllProjectsForUser(string loginName)
        {
            WindowsImpersonationContext windowsImpersonationContext = WindowsIdentity.Impersonate(IntPtr.Zero);
            try
            {
                return DbManager.GetAllProjectsForUser(loginName);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                throw new WebFaultException<string>(ex.Message, HttpStatusCode.BadRequest);
            }
            finally
            {
                windowsImpersonationContext.Undo();
            }
        }

        public string GetAllAssnForUser(string loginName, Guid projUid)
        {
            WindowsImpersonationContext windowsImpersonationContext = WindowsIdentity.Impersonate(IntPtr.Zero);
            try
            {
                List<TaskDb> allAssnForUser = DbManager.GetAllAssnForUser(loginName, projUid, Properties.TypeForm.AgreeValue);
                return JsonConvert.SerializeObject(allAssnForUser);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                throw new WebFaultException<string>(ex.Message, HttpStatusCode.BadRequest);
            }
            finally
            {
                windowsImpersonationContext.Undo();
            }
        }

        public Stream ExportProjectToExcel(CallbackArgs filter, Properties.TypeForm type)
        {
            string loginName = SPContext.Current.Web.CurrentUser.LoginName;
            WindowsImpersonationContext windowsImpersonationContext = WindowsIdentity.Impersonate(IntPtr.Zero);
            try
            {
                ExcelService excelService = new ExcelService
                {
                    _type = type
                };
                return excelService.ExportProjectToExcel(loginName, filter);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                throw new WebFaultException<string>(ex.Message, HttpStatusCode.BadRequest);
            }
            finally
            {
                windowsImpersonationContext.Undo();
            }
        }

        public void UpdatingProjectVolumeSrv()
        {
            Logger.Log("!!!!! Called UpdatingProjectVolumeSrv");
            WindowsImpersonationContext windowsImpersonationContext = WindowsIdentity.Impersonate(IntPtr.Zero);
            try
            {
                ProjectManager.JobUpdatingProjectVolume();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                throw new WebFaultException<string>(ex.Message, HttpStatusCode.BadRequest);
            }
            finally
            {
                windowsImpersonationContext.Undo();
            }
        }

        [Obsolete("This method is gone. Use NewMethod.")]
        public void AcceptDoWorkSvc(Guid ProjUid, string CurrentUserName, string CurrentUserLogin, string CurrentUserEmail, List<Guid> Accept, List<ApprovalWorkInfo> ListAssnWork, List<PercentTask> PercentTasks, List<ResourceUid> ResourceUids)
        {
            
            try
            {
                ListAssnWork.ForEach(delegate (ApprovalWorkInfo a)
                {
                    if (a.Start.HasValue)
                    {
                        a.Start = a.Start.Value.ToLocalTime();
                    }
                    if (a.Finish.HasValue)
                    {
                        a.Finish = a.Finish.Value.ToLocalTime();
                    }
                    a.OriginalDateStart = a.OriginalDateStart.ToLocalTime();
                    a.OriginalDateFinish = a.OriginalDateFinish.ToLocalTime();
                });
                DataThreadRePublish data = new DataThreadRePublish
                {
                    ProjUid = ProjUid,
                    CurrentUserName = CurrentUserName,
                    CurrentUserLogin = CurrentUserLogin,
                    CurrentUserEmail = CurrentUserEmail,
                    Accept = Accept,
                    ListAssnWork = ListAssnWork,
                    PercentTasks = PercentTasks,
                    ResourceUids = ResourceUids
                };
                DbManager.AcceptDoWork(data);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                throw new WebFaultException<string>(ex.Message, HttpStatusCode.BadRequest);
            }
        }

        public void AcceptDoWorkSvcNew(Guid ProjUid, string CurrentUserName, string CurrentUserLogin, string CurrentUserEmail, List<Guid> Accept, List<ApprovalWorkInfo> ListAssnWork, List<PercentTask> PercentTasks, List<ResourceUid> ResourceUids)
        {
            Logger.Log("Started Service AcceptDoWorkSvcNew");
            
            try
            {
                ListAssnWork.ForEach(delegate (ApprovalWorkInfo a)
                {
                    if (a.Start.HasValue)
                    {
                        a.Start = a.Start.Value.ToLocalTime();
                    }
                    if (a.Finish.HasValue)
                    {
                        a.Finish = a.Finish.Value.ToLocalTime();
                    }
                    a.OriginalDateStart = a.OriginalDateStart.ToLocalTime();
                    a.OriginalDateFinish = a.OriginalDateFinish.ToLocalTime();
                });
                DataThreadRePublish data = new DataThreadRePublish
                {
                    ProjUid = ProjUid,
                    CurrentUserName = CurrentUserName,
                    CurrentUserLogin = CurrentUserLogin,
                    CurrentUserEmail = CurrentUserEmail,
                    Accept = Accept,
                    ListAssnWork = ListAssnWork,
                    PercentTasks = PercentTasks,
                    ResourceUids = ResourceUids
                };
                DbManager.AcceptDoWorkNew(data);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                throw new WebFaultException<string>(ex.Message, HttpStatusCode.BadRequest);
            }
        }
    }

}