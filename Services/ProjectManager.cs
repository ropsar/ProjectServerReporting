using DocumentFormat.OpenXml.Office2016.Presentation.Command;
using Legenda.ProjSpace.Main.EmailService;
using Legenda.ProjSpace.Main.Entities;
using Legenda.ProjSpace.Main.Extensions;
using Legenda.ProjSpace.Main.Logging;
using Legenda.ProjSpace.Main.Model;
using Legenda.ProjSpace.Main.Model.DB;
using Legenda.ProjSpace.Main.Settings;

using Microsoft.ProjectServer.Client;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Net.Mail;
using System.Security.Principal;
using System.Text;

namespace Legenda.ProjSpace.Main.Services
{

    public static class ProjectManager
    {
        public static readonly string SiteUrl = ProjSpaceSettings.Settings.SiteCollectionUrl;

        
        public static bool ApproveMaterial(Guid projUid, List<ApprovalInfo> infos)
        {
            bool result = true;
            Logger.Log("ApproveMaterial " + SiteUrl + " infos:" + JsonConvert.SerializeObject(infos));
            infos.ForEach(delegate (ApprovalInfo info)
            {
                Guid assId = info.AssnUid;
                Guid resUid = info.ResUid;
                bool flag = DbManager.DelegateUser(resUid);
                Logger.Log($"ApproveMaterial delegated to user: {flag}");
                if (!flag)
                {
                    return;
                }
                try
                {
                    int upNum = 1;
                    string updates = "";
                    DateTime dateTime = new DateTime(1970, 1, 1);
                    string text = $"Date({(info.VolumeInDays.Min(v => v.Date).Date - dateTime).TotalMilliseconds})";
                    string text2 = $"Date({(info.VolumeInDays.Max(v => v.Date).Date.AddDays(1.0) - dateTime).TotalMilliseconds})";
                    info.VolumeInDays.ForEach(delegate (VolumeInDay vd)
                    {
                        vd.Volume = Math.Round(vd.Volume, 2);
                        string text4 = string.Format("{0}/{1}/{2}", vd.Date.ToString("MM"), vd.Date.Day, vd.Date.Year);
                        if (updates != "")
                        {
                            updates += ",";
                        }
                        string[] obj2 = new string[10] { updates, "{\"updates\":[{\"type\":2,\"recordKey\":\"", null, null, null, null, null, null, null, null };
                        Guid guid3 = assId;
                        obj2[2] = guid3.ToString();
                        obj2[3] = "\",\"fieldKey\":\"TPD_ACT_WORK_DP_";
                        obj2[4] = text4;
                        obj2[5] = "\",\"newProp\":{\"dataValue\":\"";
                        obj2[6] = (vd.Volume * 60000.0).ToString().Replace(',', '.');
                        obj2[7] = "\",\"hasDataValue\":true}}],\"changeNumber\":";
                        obj2[8] = upNum.ToString();
                        obj2[9] = "}";
                        updates = string.Concat(obj2);
                        upNum++;
                    });
                    Guid guid = Guid.NewGuid();
                    HttpClientHandler handler = new HttpClientHandler
                    {
                        UseDefaultCredentials = true
                    };
                    HttpClient client = new HttpClient(handler);
                    try
                    {
                        client.DefaultRequestHeaders.Add("Referer", SiteUrl + "/Tasks.aspx");
                        client.DefaultRequestHeaders.Add("Soapaction", "http://schemas.microsoft.com/office/project/server/webservices/PWA/StatusingSendGridUpdatesForNodeConsistency");
                        string[] obj = new string[11]
                        {
                            "<?xml version=\"1.0\" encoding=\"UTF-8\"?><soap:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\"><soap:Body><StatusingSendGridUpdatesForNodeConsistency xmlns=\"http://schemas.microsoft.com/office/project/server/webservices/PWA/\"><unvalidatedChangesJson></unvalidatedChangesJson><validatedChangesJson>[", updates, "]</validatedChangesJson><projectAssignmentsMap>[{\"Key\":\"", null, null, null, null, null, null, null,
                            null
                        };
                        Guid guid2 = assId;
                        obj[3] = guid2.ToString();
                        obj[4] = "\",\"Value\":\"";
                        guid2 = projUid;
                        obj[5] = guid2.ToString();
                        obj[6] = "\"}]</projectAssignmentsMap><compressGuids>false</compressGuids><timephasedStart>\"\\/";
                        obj[7] = text;
                        obj[8] = "\\/\"</timephasedStart><timephasedEnd>\"\\/";
                        obj[9] = text2;
                        obj[10] = "\\/\"</timephasedEnd><durationType>5</durationType><workType>2</workType><dateFormat>1</dateFormat><doSave>true</doSave></StatusingSendGridUpdatesForNodeConsistency></soap:Body></soap:Envelope>";
                        string text3 = string.Concat(obj);
                        Logger.Log("ApproveMaterial 0 " + text3);
                        StringContent content = new StringContent(text3, Encoding.UTF8, "text/xml");
                        HttpResponseMessage response = System.Threading.Tasks.Task.Run(async () => await client.PostAsync(SiteUrl + "/_vti_bin/psi/projectserver.svc", content).ConfigureAwait(false)).Result;
                        string result2 = System.Threading.Tasks.Task.Run(async () => await response.Content.ReadAsStringAsync().ConfigureAwait(false)).Result;
                        Logger.Log("ApproveMaterial 1 " + result2);
                        client.DefaultRequestHeaders.Remove("Soapaction");
                        client.DefaultRequestHeaders.Add("Soapaction", "http://schemas.microsoft.com/office/project/server/webservices/PWA/StatusingSubmitStatusJson");
                        guid2 = assId;
                        text3 = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><soap:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\"><soap:Body><StatusingSubmitStatusJson xmlns=\"http://schemas.microsoft.com/office/project/server/webservices/PWA/\"><updateGuids><guid>" + guid2.ToString() + "</guid></updateGuids><comment></comment></StatusingSubmitStatusJson></soap:Body></soap:Envelope>";
                        content = new StringContent(text3, Encoding.UTF8, "text/xml");
                        response = System.Threading.Tasks.Task.Run(async () => await client.PostAsync(SiteUrl + "/_vti_bin/psi/projectserver.svc", content).ConfigureAwait(false)).Result;
                        result2 = System.Threading.Tasks.Task.Run(async () => await response.Content.ReadAsStringAsync().ConfigureAwait(false)).Result;
                        Logger.Log("ApproveMaterial 2_ " + result2);

                        using (VolumeContext ctx = new VolumeContext())
                        {
                            info.VolumeInDays.ForEach(delegate (VolumeInDay f)
                            {
                                VolumeInDay volumeInDay = ctx.VolumeInDays.FirstOrDefault(w => w.AssnUid == assId && w.Date == f.Date);
                                if (volumeInDay != null)
                                {
                                    volumeInDay.Status = 5;
                                }
                            });
                            DbManager.SaveDbExceptionLog(ctx);
                        }
                    
                        Logger.Log("ApproveMaterial 3");
                    }
                    finally
                    {
                        if (client != null)
                        {
                            ((IDisposable)client).Dispose();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                    result = false;
                }
                finally
                {
                    DbManager.UnDelegateUser();
                }
            });
            return result;
        }

        public static Tuple<bool, List<AssnNewFinish>> ApproveNonMaterial(Guid projUid, List<ApprovalWorkInfo> infos, Dictionary<Guid, int> percentTasks)
        {
            Logger.Log("START");
            var startGlobal = DateTime.Now;
            bool item = false;
            Dictionary<Guid, List<CalendarException>> dicExceptions = new Dictionary<Guid, List<CalendarException>>();
            List<AssnNewFinish> assnsGuid = new List<AssnNewFinish>();
            List<AssnByDay> assnsByDay = DbManager.GetAssnsByDay(infos.Select(s => s.AssnUid).ToList());
            try
            {
                WindowsIdentity current = WindowsIdentity.GetCurrent();
                string name = current.Name;
                Logger.Log("LoginName:" + name);
                ProjectContext projectContext = new ProjectContext(SiteUrl);
                try
                {
                    projectContext.RequestTimeout = 360000000;
                    PublishedProject proj = projectContext.Projects.GetByGuid(projUid);
                    projectContext.Load(proj, p => p.IsCheckedOut);
                    projectContext.ExecuteQuery();

                    if (proj.IsCheckedOut)
                    {
                        return new Tuple<bool, List<AssnNewFinish>>(item, assnsGuid);
                    }

                    projectContext.Load(proj, 
                        y => y.IsCheckedOut,
                        y => y.Calendar,
                        y => y.Calendar.Id,
                        y => y.Calendar.BaseCalendarExceptions.IncludeWithDefaultProperties(
                            pr => pr.Finish,
                            pr => pr.Start,
                            pr => pr.Shift1Start,
                            pr => pr.Shift1Finish,
                            pr => pr.Shift2Start,
                            pr => pr.Shift2Finish)
                    );
                    projectContext.ExecuteQuery();
                    Logger.Log($"proj.IsCheckedOut : {proj.IsCheckedOut}");
                   
                    int shift1Start = 540;
                    int shift1Finis = 780;
                    int shift2Start = 840;
                    int shift2Finish = 1080;
                    var now = DateTime.Now;
                    Logger.Log("Cal base 0 start");
                    projectContext.Load(proj.Tasks, 
                        t => t.Include(
                            i => i.Id,
                            i => i.Name,
                            
                            i => i.Calendar,
                            i => i.Calendar.Id,
                            i => i.Calendar.IsStandardCalendar,
                            i => i.Calendar.BaseCalendarExceptions.Include(
                                ii => ii.Id,
                                ii => ii.Start,
                                ii => ii.Finish,
                                ii => ii.Shift1Start,
                                ii => ii.Name)
                            )
                    );

                    projectContext.Load(projectContext.Calendars, 
                        g => g.Include(
                            gg => gg.Id,
                            gg => gg.Name,
                            gg => gg.BaseCalendarExceptions.Include(
                                ii => ii.Id,
                                ii => ii.Start,
                                ii => ii.Finish,
                                ii => ii.Shift1Start,
                                ii => ii.Name)
                            )
                    );
                    projectContext.ExecuteQuery();
                    TimeSpan ts = DateTime.Now - now;
                    Logger.Log($"Cal base 0 end in {ts.TotalSeconds} сек");

                    Logger.Log("Cal base 2");
                    infos.ForEach(delegate (ApprovalWorkInfo info)
                    {
                        PublishedTask publishedTask = proj.Tasks.FirstOrDefault(f => f.Id == info.TaskUid);
                        

                        var publishedTaskCalNotNull = false;
                        if (publishedTask.Calendar.ServerObjectIsNull.HasValue)
                        {
                            publishedTaskCalNotNull = !publishedTask.Calendar.ServerObjectIsNull.Value;
                        }
                       
                        Calendar calendar = null;
                        if (publishedTaskCalNotNull)
                        {
                            calendar = publishedTask.Calendar;
                        }
                        else
                        {
                            if(proj.Calendar.ServerObjectIsNull.HasValue && !proj.Calendar.ServerObjectIsNull.Value)
                            {
                                calendar = proj.Calendar;
                            }
                        }
                        
                        if (calendar != null && calendar.ServerObjectIsNull.HasValue && !calendar.ServerObjectIsNull.Value)
                        {
                           
                            if (!dicExceptions.ContainsKey(calendar.Id))
                            {
                                dicExceptions.Add(calendar.Id, calendar.BaseCalendarExceptions.Any() ? calendar.BaseCalendarExceptions.ToList() : new List<CalendarException>());
                            }
                            
                            CalendarException ex3 = calendar.BaseCalendarExceptions.FirstOrDefault(f => (info.Start.HasValue && f.Shift1Start > 0 && f.Start <= info.Start.Value && info.Start.Value <= f.Finish) || (info.Finish.HasValue && f.Shift1Start > 0 && f.Start <= info.Finish.Value && info.Finish.Value <= f.Finish));
                            
                            if (ex3 == null && (info.Start.HasValue || info.Finish.HasValue))
                            {
                               
                                info.CalendarId = calendar.Id;
                            }
                        }
                        
                    });
                    Logger.Log("Cal base 3");
                    List<Guid> list = (from info in infos
                                       where info.CalendarId != Guid.Empty
                                       select info.CalendarId).Distinct().ToList();
                    list.ForEach(delegate (Guid cUid)
                    {
                        List<DateTime> excludeDate = new List<DateTime>();
                        bool isUpdate = false;
                        Calendar cl = projectContext.Calendars.FirstOrDefault(f => f.Id == cUid);
                        string calName = cl.Name;
                        try
                        {
                            cl.BaseCalendarExceptions.ToList().ForEach(delegate (CalendarException e)
                            {
                                Logger.Log($"Cal {calName} -0 {e.Start} {e.Finish} {e.Name}");
                            });
                        }
                        catch (Exception ex3)
                        {
                            Logger.Log(ex3, "Cal " + calName + " -0 ");
                        }
                        infos.Where(w => w.CalendarId == cUid).ToList().ForEach(delegate (ApprovalWorkInfo info)
                        {
                            DateTime dateTime = DateTime.Now;
                            dateTime = ((!info.Start.HasValue) ? new DateTime(info.Finish.Value.Year, info.Finish.Value.Month, info.Finish.Value.Day, 0, 0, 0) : new DateTime(info.Start.Value.Year, info.Start.Value.Month, info.Start.Value.Day, 0, 0, 0));
                            if ((dateTime.DayOfWeek == DayOfWeek.Sunday || dateTime.DayOfWeek == DayOfWeek.Saturday) && !excludeDate.Contains(dateTime))
                            {
                                Logger.Log($"Cal {calName} {dateTime}");
                                CalendarExceptionCreationInformation calendarExceptionCreationInformation = new CalendarExceptionCreationInformation
                                {
                                    Start = dateTime,
                                };
                                calendarExceptionCreationInformation.Finish = calendarExceptionCreationInformation.Start.AddHours(11.0);
                                calendarExceptionCreationInformation.Shift1Start = shift1Start;
                                calendarExceptionCreationInformation.Shift1Finish = shift1Finis;
                                calendarExceptionCreationInformation.Shift2Start = shift2Start;
                                calendarExceptionCreationInformation.Shift2Finish = shift2Finish;
                                calendarExceptionCreationInformation.Name = "Рабочий день для согласования в модуле. " + calendarExceptionCreationInformation.Start.ToShortDateString();
                                CalendarException item2 = cl.BaseCalendarExceptions.Add(calendarExceptionCreationInformation);
                                dicExceptions[cUid].Add(item2);
                                isUpdate = true;
                                excludeDate.Add(dateTime);
                            }
                        });
                        Logger.Log("Cal " + calName + " excludeDate:" + JsonConvert.SerializeObject(excludeDate) + " 0_");
                        if (isUpdate)
                        {
                            try
                            {
                                Logger.Log("Start update projectContext.Calendars");
                                projectContext.Calendars.Update();
                                projectContext.ExecuteQuery(); 
                                Logger.Log("!!!!! Calendars.Update ExecuteQuery OK");
                            }
                            catch(Exception ex)
                            {
                                Logger.Log(ex);
                            }
                        }
                        Logger.Log("Cal " + calName + " 1_");
                    });
                    Logger.Log("Cal 1_1");
                    
                    DraftProject draftProj = null;
                    try
                    {
                        draftProj = 
                            proj.CheckOut();
                        projectContext.Load(draftProj, g => g.Calendar);
                        projectContext.ExecuteQuery(); 
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex, "5555555555");
                    }
                    
                    Logger.Log("Cal 2");
                    
                    try
                    {
                        Logger.Log("Start get tasks Start Finish");
                        int count = 0;

                        infos.ForEach(delegate (ApprovalWorkInfo info)
                        {
                            try
                            {
                                Logger.Log("Get Task by guid " + info.TaskUid);
                                DraftTask byGuid = draftProj.Tasks.GetByGuid(info.TaskUid);
                                projectContext.Load(byGuid, i => i.Id, i => i.Start, i => i.Finish);
                                projectContext.ExecuteQuery(); 
                                info.OriginalDateStart = byGuid.Start;
                                info.OriginalDateFinish = byGuid.Finish;
                            }
                            catch (Exception ex)
                            {
                                Logger.Log(ex);
                            }
                        });

                        

                        var start = DateTime.Now;
                        Logger.Log("ApproveNonMaterial START infos:" + JsonConvert.SerializeObject(infos));
                        infos.ForEach(delegate (ApprovalWorkInfo info)
                        {
                            DraftTask task = draftProj.Tasks.GetByGuid(info.TaskUid);
                            DraftAssignment byGuid = proj.Draft.Assignments.GetByGuid(info.AssnUid);
                            projectContext.Load(task,
                                i => i.Id,
                                i => i.Work,
                                i => i.ActualWork,
                                i => i.Start,
                                i => i.Finish,
                                i => i.Duration,
                                i => i.PercentComplete,
                                i => i.Calendar.Id,
                                i => i.Assignments.IncludeWithDefaultProperties(f => f.Finish),
                                i => i.Predecessors,
                                i => i.CustomFields.IncludeWithDefaultProperties(
                                    f => f.InternalName,
                                    f => f.Name,
                                    f => f.LookupTable,
                                    f => f.LookupEntries
                                    ),
                                i => i.Calendar,
                                i => i.Assignments
                            );
                            projectContext.Load(byGuid,
                                i => i.Id,
                                i => i.Finish,
                                i => i.PercentWorkComplete
                            );
                            projectContext.ExecuteQuery();

                            List<CalendarException> list2 = new List<CalendarException>();
                            if (dicExceptions.Any())
                            {
                                Guid guid = Guid.Empty;
                                if (task.Calendar.ServerObjectIsNull.HasValue && !task.Calendar.ServerObjectIsNull.Value)
                                {
                                    guid = task.Calendar.Id;
                                }
                                else if (proj.Calendar.ServerObjectIsNull.HasValue && !proj.Calendar.ServerObjectIsNull.Value)
                                {
                                    guid = proj.Calendar.Id;
                                }
                                if (guid != Guid.Empty && dicExceptions.ContainsKey(guid))
                                {
                                    list2 = dicExceptions[guid];
                                }
                            }
                            count++;
                            AssnByDay assnByDay = assnsByDay.FirstOrDefault(f => f.AssnUid == info.AssnUid);
                            if (info.Start.HasValue)
                            {
                                Logger.Log($"ApproveNonMaterial {count} Start {info.Start.Value} pre info.TaskUid:{info.TaskUid}");
                                try
                                {
                                    if (task.Predecessors.Any())
                                    {
                                        projectContext.Load(task, j => j.Predecessors.IncludeWithDefaultProperties(ik => ik.Id, ik => ik.Start, ik => ik.Start.Id, ik => ik.Start.Start)
                                        );
                                        projectContext.ExecuteQuery();
                                        List<DraftTaskLink> list3 = task.Predecessors.ToList();
                                        if (list3.Any())
                                        {
                                            foreach (DraftTaskLink item3 in list3)
                                            {
                                                task.Predecessors.Remove(item3);
                                            }
                                        }
                                        projectContext.WaitForQueue(draftProj.Update(), 10000000);
                                    }
                                }
                                catch (Exception ex3)
                                {
                                    Logger.Log(ex3, "task.Predecessors info.TaskUid:{info.TaskUid}");
                                }
                                Logger.Log($"ApproveNonMaterial {count} Start {info.Start.Value} pre info.TaskUid:{info.TaskUid}");
                                task.Start = info.Start.Value;
                                projectContext.WaitForQueue(draftProj.Update(), 10000000);
                                if (!info.Finish.HasValue && task.PercentComplete == 0 && (!info.PercentComplete.HasValue || info.PercentComplete.Value == 0))
                                {
                                    task.Finish = info.OriginalDateFinish;
                                }
                            }
                            if (info.Finish.HasValue)
                            {
                                Logger.Log($"ApproveNonMaterial {count} New Finish:{info.Finish.Value} Finish:{task.Finish} info.TaskUid:{info.TaskUid} info.IsMaterial:{info.IsMaterial}");
                                DateTime dateTime = new DateTime(task.Finish.Year, task.Finish.Month, task.Finish.Day, task.Finish.Hour, task.Finish.Minute, task.Finish.Second, DateTimeKind.Utc);
                                DateTime dateTime2 = new DateTime(info.Finish.Value.Year, info.Finish.Value.Month, info.Finish.Value.Day, info.Finish.Value.Hour, info.Finish.Value.Minute, info.Finish.Value.Second, DateTimeKind.Utc);
                                Logger.Log($"ApproveNonMaterial {count} next New Finish:{dateTime2} Finish:{dateTime} {System.TimeZone.CurrentTimeZone.ToLocalTime(dateTime2)} info.TaskUid:{info.TaskUid} info.IsMaterial:{info.IsMaterial}");
                                if (dateTime2 > dateTime && info.IsMaterial)
                                {
                                    AssnNewFinish assnNewFinish = new AssnNewFinish
                                    {
                                        AssnUid = info.AssnUid,
                                        TaskUid = info.TaskUid,
                                        Finish = task.Finish,
                                        NewFinish = info.Finish.Value,
                                        Type = 1
                                    };
                                    if (assnByDay != null)
                                    {
                                        AssnByDay.DayFact dayFact = assnByDay.DaysFacts.FirstOrDefault(f => f.Date.ToShortDateString() == task.Finish.ToShortDateString());
                                        if (dayFact != null && dayFact.Fact > 0.0)
                                        {
                                            Logger.Log("ApproveNonMaterial finishFact:" + JsonConvert.SerializeObject(dayFact));
                                            if (percentTasks[info.TaskUid] == 100)
                                            {
                                                assnNewFinish.Type = 5;
                                            }
                                            else
                                            {
                                                int num = 0;
                                                Logger.Log($"ApproveNonMaterial finish:{dateTime} newFinish:{dateTime2} taskClExceptions:{JsonConvert.SerializeObject(list2)}");
                                                DateTime dateTime3 = dateTime.AddDays(1.0);
                                                while (dateTime3 <= dateTime2)
                                                {
                                                    DateTime date = new DateTime(dateTime3.Year, dateTime3.Month, dateTime3.Day, 0, 0, 0);
                                                    if (!list2.Any(a => a.Shift1Start == 0 && a.Start <= date && date <= a.Finish))
                                                    {
                                                        if (list2.Any(a => a.Shift1Start > 0 && a.Start <= date && date <= a.Finish))
                                                        {
                                                            num++;
                                                        }
                                                        else if (date.DayOfWeek != DayOfWeek.Sunday && date.DayOfWeek != DayOfWeek.Saturday)
                                                        {
                                                            num++;
                                                        }
                                                    }
                                                    dateTime3 = dateTime3.AddDays(1.0);
                                                }
                                                if (num == 1)
                                                {
                                                    assnNewFinish.Type = 2;
                                                }
                                                else if (num > 1)
                                                {
                                                    assnNewFinish.Type = 3;
                                                    task.Finish = info.Finish.Value.AddDays(-1.0);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            AssnByDay.DayFact dayFact2 = assnByDay.DaysFacts.FirstOrDefault(f => f.Date.ToShortDateString() == task.Finish.AddDays(-1.0).ToShortDateString());
                                            if ((task.Finish - task.Start).TotalDays >= 2.0 && dayFact2 != null && dayFact2.Fact > 0.0)
                                            {
                                                assnNewFinish.Type = 4;
                                            }
                                            else if (percentTasks[info.TaskUid] > 0)
                                            {
                                                task.Finish = info.Finish.Value.AddDays(-1.0);
                                            }
                                            else
                                            {
                                                task.Finish = info.Finish.Value;
                                            }
                                        }
                                    }
                                    Logger.Log("ApproveNonMaterial Add outAssn:" + JsonConvert.SerializeObject(assnNewFinish));
                                    assnsGuid.Add(assnNewFinish);
                                }
                                else
                                {
                                    task.Finish = info.Finish.Value;
                                }
                                if (!info.Start.HasValue && task.PercentComplete == 0 && (!info.PercentComplete.HasValue || info.PercentComplete.Value == 0))
                                {
                                    projectContext.WaitForQueue(draftProj.Update(), 10000000);
                                    task.Start = info.OriginalDateStart;
                                    task.Finish = info.Finish.Value;
                                }
                            }
                            if (info.PercentComplete.HasValue)
                            {
                                Logger.Log($"ApproveNonMaterial {count} PercentCompleteCustom {info.PercentComplete.Value} info.TaskUid:{info.TaskUid}");
                                task[ConfigManager.GetConfigSetting("PercentCompleteCustom")] = info.PercentComplete.Value; 
                                
                                if (info.IsMaterial)
                                {
                                    Logger.Log("!!! IsMaterial, обновляем назначение");
                                    if (info.PercentComplete.HasValue)
                                    {
                                        try
                                        {
                                            var taskAss = task.Assignments.FirstOrDefault();
                                            taskAss.PercentWorkComplete = info.PercentComplete.Value;
                                            Logger.Log("!!! taskAss.PercentWorkComplete = " + info.PercentComplete.Value);
                                        }
                                        catch(Exception ex)
                                        {
                                            Logger.Log(ex);
                                        }
                                    }
                                }

                                double result = 0.0;

                                if (!info.IsMaterial && !string.IsNullOrEmpty(task.Work) && double.TryParse(task.Work.Replace("ч", "").Replace("\u00a0", ""), out result))
                                {
                                    double num2 = result / 100.0 * (double)info.PercentComplete.Value;
                                    Logger.Log($"actualWork:{num2} work:{result} info.TaskUid:{info.TaskUid}");
                                    if (info.Finish.HasValue || info.Start.HasValue)
                                    {
                                        task.PercentComplete = info.PercentComplete.Value;
                                        projectContext.WaitForQueue(draftProj.Update(), 10000000);
                                        byGuid.PercentWorkComplete = info.PercentComplete.Value;
                                        Logger.Log($"info.PercentComplete.Value:{info.PercentComplete.Value} info.Finish.Value:{info.Finish.Value} info.TaskUid:{info.TaskUid}");
                                    }
                                    else
                                    {
                                        task.ActualWork = num2 + "ч";
                                        Logger.Log(string.Format("task.ActualWork:{0} info.TaskUid:{1}", num2 + "ч", info.TaskUid));
                                    }
                                }
                            }
                            Logger.Log($"Update DraftProj task start, info.TaskUid:{info.TaskUid}");
                            var startTaskUpdate = DateTime.Now;
                            
                            var job = draftProj.Update();
                            var jobStatus = projectContext.WaitForQueue(job, 10000000);  

                            TimeSpan endTaskUpdate = DateTime.Now - startTaskUpdate;
                            Logger.Log($"DraftProj Update end, Status: {jobStatus}, time: {endTaskUpdate.TotalMinutes} мин, {endTaskUpdate.TotalSeconds} сек");
                        });                       

                        Logger.Log("END");
                        item = true; 
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                    }
                    finally
                    {
                        Logger.Log("Start Update project");

                        

                        var job = draftProj.Publish(true);
                        var jobstatus = projectContext.WaitForQueue(job, 10000000);
                        Logger.Log($"DraftProj Publish end, Status: " + jobstatus.ToString());
                        TimeSpan end = DateTime.Now - startGlobal;
                        Logger.Log($"[END]. INFOS count: {infos.Count}, in time: {end.TotalMinutes} м, {end.TotalSeconds} с");
                    }
                }
                finally
                {
                    if (projectContext != null)
                    {
                        ((IDisposable)projectContext).Dispose();
                    }
                }

            }
            catch (Exception ex2)
            {
                Logger.Log(ex2);
            }
            return new Tuple<bool, List<AssnNewFinish>>(item, assnsGuid);
        }

       

        public static void RePublish(Guid projUid, List<AssnNewFinish> assnsNewFinish, string currentUserName, string currentUserLogin, string currentUserEmail, List<Guid> accept)
        {
            
            Logger.Log("REPUBLISH START");
            var start = DateTime.Now;
            try
            {
                Logger.Log($"RePublish projUid:{projUid} start_ assnsNewFinish:{JsonConvert.SerializeObject(assnsNewFinish)} currentUserName:{currentUserName} accept:{JsonConvert.SerializeObject(accept)}");
                List<AssnByDay> assnsByDay = DbManager.GetAssnsByDay(assnsNewFinish.Select(s => s.AssnUid).ToList());
                List<ApprovalInfo> infos = new List<ApprovalInfo>();
                foreach (AssnNewFinish item in assnsNewFinish.Where(w => w.Type == 1).ToList())
                {
                    Logger.Log($"RePublish item.AssnUid:{item.AssnUid} item.TaskUid:{item.TaskUid} Type = 1");
                    AssnByDay assnByDay = assnsByDay.FirstOrDefault(f => f.AssnUid == item.AssnUid);
                    if (assnByDay == null || !assnByDay.DaysFacts.Any() || !assnByDay.DaysFacts.Any(a => a.Fact > 0.0))
                    {
                        continue;
                    }
                    Logger.Log($"RePublish item.AssnUid:{item.AssnUid} item.TaskUid:{item.TaskUid} assnByDay:{JsonConvert.SerializeObject(assnByDay)} item.Finish:{item.Finish} item.NewFinish:{item.NewFinish} Type = 1");
                    DateTime lastFactDate = assnByDay.DaysFacts.Where(w => w.Fact > 0.0).Max(a => a.Date);
                    int count = assnByDay.DaysFacts.Where(a => item.Finish < a.Date && a.Date <= item.NewFinish.AddDays(-1.0) && a.Plan > 0.0).ToList().Count;
                    Logger.Log($"RePublish item.AssnUid:{item.AssnUid} item.TaskUid:{item.TaskUid} lastFact:{lastFactDate} count:{count} Type = 1");
                    List<AssnByDay.DayFact> saveFact = new List<AssnByDay.DayFact>();
                    assnByDay.DaysFacts = assnByDay.DaysFacts.OrderBy(ob => ob.Date).ToList();
                    for (int num = 0; num < count; num++)
                    {
                        AssnByDay.DayFact dayFact = assnByDay.DaysFacts.FirstOrDefault(f => f.Date > lastFactDate.Date && !saveFact.Any(a => a.Date == f.Date) && f.Plan > 0.0);
                        if (dayFact != null)
                        {
                            saveFact.Add(dayFact);
                        }
                    }
                    if (saveFact.Any())
                    {
                        ApprovalInfo info = new ApprovalInfo
                        {
                            AssnUid = item.AssnUid,
                            ResUid = item.ResUid,
                            VolumeInDays = new List<VolumeInDay>()
                        };
                        saveFact.ForEach(delegate (AssnByDay.DayFact f)
                        {
                            VolumeInDay item8 = new VolumeInDay
                            {
                                AssnUid = item.AssnUid,
                                Date = f.Date,
                                Status = 5,
                                TimeStamp = DateTime.Now,
                                Uid = Guid.NewGuid(),
                                Volume = f.Plan
                            };
                            info.VolumeInDays.Add(item8);
                        });
                        infos.Add(info);
                    }
                }
                if (infos.Any() && ApproveMaterial(projUid, infos)) 
                {
                    DbManager.PauseAfterProjectChange(projUid, 3600);
                    infos.ForEach(delegate (ApprovalInfo f)
                    {
                        f.VolumeInDays.ForEach(delegate (VolumeInDay ff)
                        {
                            ff.Volume = 0.0;
                        });
                    });
                    var approveMaterialResult = ApproveMaterial(projUid, infos);
                    Logger.Log($"approveMaterialResult {approveMaterialResult}");
                    if (approveMaterialResult) 
                    {
                        DbManager.PauseAfterProjectChange(projUid, 3600);
                        PublishedProject byGuid = null;
                        using (ProjectContext projectContext = new ProjectContext(SiteUrl))
                        {
                            try
                            {
                                Logger.Log($"Check for project IsCheckedOut projUid: [{projUid}]");
                                projectContext.RequestTimeout = 360000000;
                                byGuid = projectContext.Projects.GetByGuid(projUid);
                                projectContext.Load(byGuid, x => x.IsCheckedOut, x => x.CheckedOutBy, x => x.CheckedOutBy.Title); 
                                projectContext.ExecuteQuery();
                                if (byGuid.IsCheckedOut)
                                {
                                    Logger.Log("Project " + projUid + " извлечен");
                                    try
                                    {
                                        

                                        var emails = SpEmailService.GetEmailTemplate("PUBLISHEDERROR", "MANAGER");
                                        if (emails.Count > 0)
                                        {
                                            var projectInfo = DbManager.GetProjectInfo(projUid.ToString());
                                            var tplm = new TemplateManager();
                                            tplm.AddKeyValue("PROJECT", projectInfo.Proj_Name);
                                            tplm.AddKeyValue("ERRORS", "Проект извлечен пользователем " + byGuid.CheckedOutBy.Title + ", сохранение невозможно. Необходимо вернуть проект в Project Server и опубликовать");
                                            var emF = emails.FirstOrDefault();
                                            if (emF.IsActive)
                                            {
                                                emF.Body = tplm.ReplaceParse(emF.Body);
                                                emF.Subject = tplm.ReplaceParse(emF.Subject);
                                                EmailSendService.SendEmail(new MailAddress(emF.FromEmail, emF.FromName), new MailAddress(currentUserEmail, currentUserName), emF.Subject, emF.Body);
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Log(ex);
                                    }
                                    return;
                                }
                                else
                                {
                                    Logger.Log("Project " + projUid + " не извлечен");
                                }
                            }catch(Exception ex)
                            {
                                Logger.Log(ex);
                            }

                            
                        }
                        List<AssnNewFinish> assnsType1 = assnsNewFinish.Where(w => w.Type == 1).ToList();
                        List<AssnByDay> assnsByDay2 = DbManager.GetAssnsByDay(assnsType1.Select(s => s.AssnUid).ToList());
                        List<ApprovalInfo> newInfos = new List<ApprovalInfo>();
                        assnsByDay2.ForEach(delegate (AssnByDay f)
                        {
                            AssnNewFinish a = assnsType1.FirstOrDefault(ff => ff.AssnUid == f.AssnUid);
                            if (f.DaysFacts.Any(v => v.Date == a.NewFinish.AddDays(-1.0)) && !f.DaysFacts.Any(v => v.Date == a.NewFinish))
                            {
                                ApprovalInfo approvalInfo4 = infos.FirstOrDefault(n => n.AssnUid == f.AssnUid);
                                DateTime maxDate = approvalInfo4.VolumeInDays.Max(s => s.Date);
                                VolumeInDay volumeInDay = approvalInfo4.VolumeInDays.FirstOrDefault(vd => vd.Date == maxDate);
                                DateTime date = f.DaysFacts.OrderBy(o => o.Date).ToList().FirstOrDefault(ff => ff.Date > maxDate && ff.Plan > 0.0)
                                    .Date;
                                volumeInDay.Date = date;
                                volumeInDay.Volume = 0.0;
                                approvalInfo4.VolumeInDays = new List<VolumeInDay> { volumeInDay };
                                newInfos.Add(approvalInfo4);
                            }
                        });
                        if (newInfos.Any())
                        {
                            ApproveMaterial(projUid, infos);
                            DbManager.PauseAfterProjectChange(projUid, 3600);
                        }
                    }
                }
                infos = new List<ApprovalInfo>();
                foreach (AssnNewFinish item2 in assnsNewFinish.Where(w => w.Type == 2).ToList())
                {
                    Logger.Log($"RePublish item.AssnUid:{item2.AssnUid} item.TaskUid:{item2.TaskUid} Type = 2");
                    AssnByDay assnByDay2 = assnsByDay.FirstOrDefault(f => f.AssnUid == item2.AssnUid);
                    DateTime lastFact = assnByDay2.DaysFacts.Where(w => w.Fact > 0.0).Max(a => a.Date);
                    Logger.Log($"RePublish item.AssnUid:{item2.AssnUid} item.TaskUid:{item2.TaskUid} lastFact:{lastFact}  assnByDay:{JsonConvert.SerializeObject(assnByDay2)} item.Finish:{item2.Finish} item.NewFinish:{item2.NewFinish} Type = 2");
                    ApprovalInfo approvalInfo = new ApprovalInfo
                    {
                        AssnUid = item2.AssnUid,
                        ResUid = item2.ResUid,
                        VolumeInDays = new List<VolumeInDay>()
                    };
                    VolumeInDay item3 = new VolumeInDay
                    {
                        AssnUid = item2.AssnUid,
                        Date = lastFact.Date,
                        Status = 5,
                        TimeStamp = DateTime.Now,
                        Uid = Guid.NewGuid(),
                        Volume = assnByDay2.DaysFacts.FirstOrDefault(f => f.Date == lastFact).Fact
                    };
                    approvalInfo.VolumeInDays.Add(item3);
                    infos.Add(approvalInfo);
                }
                if (infos.Any() && ApproveMaterial(projUid, infos))
                {
                    DbManager.PauseAfterProjectChange(projUid, 3600);
                }
                infos = new List<ApprovalInfo>();
                List<ApprovalInfo> list = new List<ApprovalInfo>();
                foreach (AssnNewFinish item4 in assnsNewFinish.Where(w => w.Type == 3).ToList())
                {
                    Logger.Log($"RePublish item.AssnUid:{item4.AssnUid} item.TaskUid:{item4.TaskUid} Type = 3");
                    AssnByDay assnByDay3 = assnsByDay.FirstOrDefault(f => f.AssnUid == item4.AssnUid);
                    assnByDay3.DaysFacts = assnByDay3.DaysFacts.OrderBy(ob => ob.Date).ToList();
                    Logger.Log($"RePublish item.AssnUid:{item4.AssnUid} item.TaskUid:{item4.TaskUid} assnByDay:{JsonConvert.SerializeObject(assnByDay3)} Type = 3");
                    DateTime lastFactDate2 = assnByDay3.DaysFacts.Where(w => w.Fact > 0.0).Max(a => a.Date);
                    AssnByDay.DayFact lastFact2 = assnByDay3.DaysFacts.FirstOrDefault(f => f.Date == lastFactDate2);
                    int count2 = assnByDay3.DaysFacts.Where(a => (item4.Finish <= a.Date || item4.Finish.ToShortDateString() == a.Date.ToShortDateString()) && a.Date < item4.NewFinish.AddDays(-1.0) && a.Plan > 0.0).ToList().Count;
                    Logger.Log($"RePublish item.AssnUid:{item4.AssnUid} item.TaskUid:{item4.TaskUid} lastFactDate:{lastFactDate2} count:{count2} item.Finish:{item4.Finish} item.NewFinish:{item4.NewFinish} Type = 3");
                    ApprovalInfo info2 = new ApprovalInfo
                    {
                        AssnUid = item4.AssnUid,
                        ResUid = item4.ResUid,
                        VolumeInDays = new List<VolumeInDay>()
                    };
                    VolumeInDay d = new VolumeInDay
                    {
                        AssnUid = item4.AssnUid,
                        Date = assnByDay3.DaysFacts.LastOrDefault().Date,
                        Status = 5,
                        TimeStamp = DateTime.Now,
                        Uid = Guid.NewGuid(),
                        Volume = lastFact2.Fact
                    };
                    info2.VolumeInDays.Add(d);
                    list.Add(info2);
                    List<AssnByDay.DayFact> saveFact2 = new List<AssnByDay.DayFact>();
                    for (int num2 = 0; num2 < count2; num2++)
                    {
                        AssnByDay.DayFact dayFact2 = assnByDay3.DaysFacts.FirstOrDefault(f => f.Date >= lastFact2.Date && !saveFact2.Any(a => a.Date == f.Date) && f.Plan > 0.0);
                        if (dayFact2 != null)
                        {
                            saveFact2.Add(dayFact2);
                        }
                    }
                    if (saveFact2.Any())
                    {
                        info2 = new ApprovalInfo
                        {
                            AssnUid = item4.AssnUid,
                            ResUid = item4.ResUid,
                            VolumeInDays = new List<VolumeInDay>()
                        };
                        saveFact2.ForEach(delegate (AssnByDay.DayFact f)
                        {
                            d = new VolumeInDay
                            {
                                AssnUid = item4.AssnUid,
                                Date = f.Date,
                                Status = 5,
                                TimeStamp = DateTime.Now,
                                Uid = Guid.NewGuid(),
                                Volume = f.Plan
                            };
                            info2.VolumeInDays.Add(d);
                        });
                        infos.Add(info2);
                    }
                }
                if (infos.Any() && ApproveMaterial(projUid, infos)) 
                {
                    Logger.Log(JsonConvert.SerializeObject(infos, Formatting.Indented));
                    DbManager.PauseAfterProjectChange(projUid, 3600);
                    infos.ForEach((f)=>
                    {
                        f.VolumeInDays.ForEach((ff)=>
                        {
                            ff.Volume = 0.0;
                        });
                    });
                    if (ApproveMaterial(projUid, infos))
                    {
                        DbManager.PauseAfterProjectChange(projUid, 3600);
                        ApproveMaterial(projUid, list);
                    }
                }
                foreach (AssnNewFinish item5 in assnsNewFinish.Where(w => w.Type == 4).ToList())
                {
                    Logger.Log($"RePublish item.AssnUid:{item5.AssnUid} item.TaskUid:{item5.TaskUid} Type = 4");
                    AssnByDay value = assnsByDay.FirstOrDefault(f => f.AssnUid == item5.AssnUid);
                    Logger.Log($"RePublish item.AssnUid:{item5.AssnUid} item.TaskUid:{item5.TaskUid} assnByDay:{JsonConvert.SerializeObject(value)} item.Finish:{item5.Finish} item.NewFinish:{item5.NewFinish} Type = 4");
                    ApprovalInfo approvalInfo2 = new ApprovalInfo
                    {
                        AssnUid = item5.AssnUid,
                        ResUid = item5.ResUid,
                        VolumeInDays = new List<VolumeInDay>()
                    };
                    VolumeInDay item6 = new VolumeInDay
                    {
                        AssnUid = item5.AssnUid,
                        Date = item5.Finish,
                        Status = 5,
                        TimeStamp = DateTime.Now,
                        Uid = Guid.NewGuid(),
                        Volume = 0.0
                    };
                    approvalInfo2.VolumeInDays.Add(item6);
                    infos.Add(approvalInfo2);
                }
                if (infos.Any() && ApproveMaterial(projUid, infos))
                {
                    DbManager.PauseAfterProjectChange(projUid, 3600);
                }
                infos = new List<ApprovalInfo>();
                foreach (AssnNewFinish item7 in assnsNewFinish.Where(w => w.Type == 5).ToList())
                {
                    Logger.Log($"RePublish item.AssnUid:{item7.AssnUid} item.TaskUid:{item7.TaskUid} Type = 5");
                    AssnByDay assnByDay4 = assnsByDay.FirstOrDefault(f => f.AssnUid == item7.AssnUid);
                    DateTime lastFact3 = assnByDay4.DaysFacts.Where(w => w.Fact > 0.0).Max(a => a.Date);
                    Logger.Log($"RePublish item.AssnUid:{item7.AssnUid} item.TaskUid:{item7.TaskUid} lastFact:{lastFact3}  assnByDay:{JsonConvert.SerializeObject(assnByDay4)} item.Finish:{item7.Finish} item.NewFinish:{item7.NewFinish} Type = 5");
                    ApprovalInfo approvalInfo3 = new ApprovalInfo
                    {
                        AssnUid = item7.AssnUid,
                        ResUid = item7.ResUid,
                        VolumeInDays = new List<VolumeInDay>()
                    };
                    approvalInfo3.VolumeInDays.Add(new VolumeInDay
                    {
                        AssnUid = item7.AssnUid,
                        Date = lastFact3.Date,
                        Status = 5,
                        TimeStamp = DateTime.Now,
                        Uid = Guid.NewGuid(),
                        Volume = 0.0
                    });
                    approvalInfo3.VolumeInDays.Add(new VolumeInDay
                    {
                        AssnUid = item7.AssnUid,
                        Date = item7.NewFinish,
                        Status = 5,
                        TimeStamp = DateTime.Now,
                        Uid = Guid.NewGuid(),
                        Volume = assnByDay4.DaysFacts.FirstOrDefault(f => f.Date == lastFact3).Fact
                    });
                    infos.Add(approvalInfo3);
                }
                if (infos.Any() && ApproveMaterial(projUid, infos))
                {
                    DbManager.PauseAfterProjectChange(projUid, 3600);
                }
                infos = new List<ApprovalInfo>();

                Logger.Log($"RePublish projUid:{projUid} end assnsNewFinish:{JsonConvert.SerializeObject(assnsNewFinish)} currentUserName:{currentUserName} accept:{JsonConvert.SerializeObject(accept)}");

                

                List<string> senderEmail = new List<string>();

                using (VolumeContext ctx = new VolumeContext())
                {
                    accept.ForEach(delegate (Guid r)
                    {
                        
                        IQueryable<VolumeInDay> items = ctx.VolumeInDays.Where(v => v.AssnUid == r && v.Status == 2);
                        items.ForEach((e) =>
                        {
                            e.Status = 5;
                            e.TimeStamp = DateTime.Now;
                        });
                        VolumeData volumeData = ctx.VolumeDatas.FirstOrDefault(a => a.AssnUid == r);
                        volumeData.Status = 5;
                        volumeData.TimeStamp = DateTime.Now;
                        if (!string.IsNullOrWhiteSpace(volumeData.SenderEmail))
                        {
                            if (!senderEmail.Contains(volumeData.SenderEmail))
                            {
                                senderEmail.Add(volumeData.SenderEmail);
                                Logger.Log("Add email for sender to approve: " + volumeData.SenderEmail);
                            }
                        }
                        if (!string.IsNullOrEmpty(currentUserName))
                        {
                            volumeData.Comments = currentUserName + ": " + DateTime.Now.ToShortDateString();
                        }
                        Logger.Log($"RePublish projUid:{projUid} на сохранение vel:{JsonConvert.SerializeObject(volumeData)} currentUserName:{currentUserName} r:{r}");
                        ctx.VolumeDatas.AddOrUpdate(volumeData);
                    });
                    Logger.Log($"RePublish projUid:{projUid} сохранение результатов assnsNewFinish:{JsonConvert.SerializeObject(assnsNewFinish)} currentUserName:{currentUserName} accept:{JsonConvert.SerializeObject(accept)}");
                    DbManager.SaveDbExceptionLog(ctx);
                    Logger.Log($"RePublish projUid:{projUid} после сохранения результатов assnsNewFinish:{JsonConvert.SerializeObject(assnsNewFinish)} currentUserName:{currentUserName} accept:{JsonConvert.SerializeObject(accept)}");
                }
                TimeSpan end = DateTime.Now - start;
                Logger.Log($"REPUBLISH DONE in {end.TotalMinutes} мин, {end.TotalSeconds} сек");

                
                List<TaskShortInfo> approvedTasks = new List<TaskShortInfo>();
                accept.ForEach((r) => {
                    var tmp = DbManager.GetTaskShortInfo(r.ToString());
                    approvedTasks.Add(tmp);
                });
                StringBuilder sbTasks = new StringBuilder();
                if(approvedTasks.Count>0)
                {
                    foreach(var t in approvedTasks)
                    {
                        sbTasks.AppendLine($"<tr><td style=\"border-bottom: 1px solid #eeeeee;\">{t.Name}</td><td align=\"right\" style=\"border-bottom: 1px solid #eeeeee;\">{t.PercentComplete}</td></tr>");
                    }
                }
                else
                {
                    sbTasks.AppendLine("<tr><td></td></tr>");
                }


                    
                    Logger.Log("Отправляем письма утвердившему");
                var projInfo = DbManager.GetProjectInfo(projUid.ToString());
                List<EmailTemplate> emailList = SpEmailService.GetEmailTemplate("PUBLISHED", "MANAGER");
                if (emailList.Count > 0)
                {
                    var emailTo = "";
                    try
                    {
                        emailTo = currentUserEmail;
                        if (!string.IsNullOrWhiteSpace(emailTo))
                        {
                            var email = emailList.FirstOrDefault();
                            if (email.IsActive)
                            {
                               

                                TemplateManager tplm = new TemplateManager();
                                tplm.AddKeyValue("PROJECT", projInfo.Proj_Name);
                                tplm.AddKeyValue("TASKS", sbTasks.ToString());
                                email.Body = tplm.ReplaceParse(email.Body);
                                email.Subject = tplm.ReplaceParse(email.Subject);
                                EmailSendService.SendEmail(new MailAddress(email.FromEmail, email.FromName), new MailAddress(emailTo), email.Subject, email.Body);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex, "Send email to approver");
                    }
                }

                Logger.Log("senderEmail count: " + senderEmail.Count);
                if(senderEmail.Count > 0)
                {
                    emailList = SpEmailService.GetEmailTemplate("ACCEPT", "USER");
                    if (emailList.Count > 0)
                    {
                        foreach (var em in senderEmail)
                        {
                            var emailTo = "";
                            try
                            {
                                emailTo = em;
                                if (!string.IsNullOrWhiteSpace(emailTo))
                                {
                                    var email = emailList.FirstOrDefault();
                                    if (email.IsActive)
                                    {
                                        TemplateManager tplm = new TemplateManager();
                                        tplm.AddKeyValue("PROJECT", projInfo.Proj_Name);
                                        tplm.AddKeyValue("TASKS", sbTasks.ToString());
                                        email.Body = tplm.ReplaceParse(email.Body);
                                        email.Subject = tplm.ReplaceParse(email.Subject);
                                        EmailSendService.SendEmail(new MailAddress(email.FromEmail, email.FromName), new MailAddress(emailTo), email.Subject, email.Body);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Log(ex, "Send email");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "RePublish");
            }
        }

        
        public static void JobUpdatingProjectVolume()
        {
            Logger.Log(">>>>>> START JobUpdatingProjectVolume");
            try
            {
                List<Guid> projUids = new List<Guid>();
                using (VolumeContext volumeContext = new VolumeContext())
                {
                    volumeContext.VolumeDatas.Where(w => w.Status == 4).ToList().ForEach(delegate (VolumeData f)
                    {
                        if (!projUids.Contains(f.ProjectUid))
                        {
                            if (projUids.Count < 2) 
                            {
                                projUids.Add(f.ProjectUid);
                            }
                        }
                    });
                }
                if (!projUids.Any())
                {
                    Logger.Log("JobUpdatingProjectVolume нет проектов на обновление");
                    return;
                }
                List<Guid> allProjectsNoCheckedOut = DbManager.GetAllProjectsNoCheckedOut(projUids);
                if (!allProjectsNoCheckedOut.Any())
                {
                    Logger.Log("JobUpdatingProjectVolume нет неизвлеченных проектов на обновление");
                    return;
                }
                else
                {
                    allProjectsNoCheckedOut.ForEach(delegate (Guid projUid)
                    {
                        List<ApprovalWorkInfo> approvalWorkInfo = GetApprovalWorkInfo(projUid);
                        if (approvalWorkInfo.Any())
                        {
                            Dictionary<Guid, int> percentTasks = DbManager.GetPercentTasks(projUid);
                            Tuple<bool, List<AssnNewFinish>> tuple = ApproveNonMaterial(projUid, DbManager.GetApproveNonMaterial(projUid, approvalWorkInfo), percentTasks);
                            if (tuple.Item1)
                            {
                               
                                UpdateStatuses(approvalWorkInfo,5);
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            Logger.Log(">>>>>> END JobUpdatingProjectVolume");
        }

       
        public static void UpdateStatuses(List<ApprovalWorkInfo> infos, int status)
        {
            try
            {
                using (VolumeContext ctx = new VolumeContext())
                {
                    infos.ForEach(delegate (ApprovalWorkInfo info)
                    {
                        if (ctx.VolumeDatas.Any(a => a.AssnUid == info.AssnUid))
                        {
                            ctx.VolumeDatas.FirstOrDefault(a => a.AssnUid == info.AssnUid).Status = status;
                        }
                    });
                    DbManager.SaveDbExceptionLog(ctx);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }        

       
        public static List<ApprovalWorkInfo> GetApprovalWorkInfo(Guid projUid)
        {
            List<ApprovalWorkInfo> result = new List<ApprovalWorkInfo>();
            try
            {
                using (VolumeContext volumeContext = new VolumeContext())
                {
                    volumeContext.VolumeDatas.Where(w => w.ProjectUid == projUid && w.Status == 4).ToList().ForEach((f) =>
                    {
                        result.Add(new ApprovalWorkInfo
                        {
                            AssnUid = f.AssnUid,
                            Start = f.StartDate,
                            Finish = f.FinishDate,
                            TaskUid = f.TaskUid,
                            PercentComplete = Convert.ToInt32(f.PercentCompleted)
                        });
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            Logger.Log("На публикацию задач: " + result.Count);
            return result;
        }
    }
}