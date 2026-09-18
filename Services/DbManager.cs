using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Office2010.Excel;
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
using SPMeta2.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Migrations;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Mail;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Web.UI.WebControls;
using static Legenda.ProjSpace.Main.Model.SubmittedApprovalGridInfo;

namespace Legenda.ProjSpace.Main.Services
{
    
    public static class DbManager
    {
        private readonly static string csProject = ProjSpaceSettings.Settings.ProjectConnectionString;

        public static TaskShortInfo GetTaskShortInfo(string assUid)
        {
            TaskShortInfo result = new TaskShortInfo();
            using (SqlConnection sqlConnection = new SqlConnection(csProject))
            {
                try
                {
                    sqlConnection.Open();
                    string query = "SELECT P.[TaskName], V.[PercentCompleted], P.[TaskStartDate], P.[TaskFinishDate] FROM [WSS_Content_PWA].[pjrep].[MSP_EpmTask_UserView] P, [WSS_Custom_PWA].[dbo].[VolumeDatas] V WHERE P.TaskUID = V.[TaskUid] AND V.[AssnUid] like @Uid";
                    SqlCommand sqlCommand = new SqlCommand(query, sqlConnection)
                    {
                        CommandTimeout = 500
                    };
                    sqlCommand.Parameters.AddWithValue("@Uid", "%" + assUid + "%");
                    SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
                    while (sqlDataReader.Read())
                    {
                        try
                        {
                            result.Name = ((sqlDataReader["TaskName"] != DBNull.Value) ? sqlDataReader["TaskName"].ToString() : string.Empty);
                            result.PercentComplete = (sqlDataReader["PercentCompleted"] != DBNull.Value) ? sqlDataReader["PercentCompleted"].ToString() : "";
                            if(sqlDataReader["TaskStartDate"] != DBNull.Value)
                            {
                                result.Start = Convert.ToDateTime(sqlDataReader["TaskStartDate"].ToString());
                            }
                            if (sqlDataReader["TaskFinishDate"] != DBNull.Value)
                            {
                                result.Finish = Convert.ToDateTime(sqlDataReader["TaskFinishDate"].ToString());
                            }

                        }
                        catch (Exception ex)
                        {
                            Logger.Log(ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
            }
            return result;
        }

        public static ProjectInfoEntity GetProjectInfo(string guid)
        {
            ProjectInfoEntity result = new ProjectInfoEntity();

            using (SqlConnection sqlConnection = new SqlConnection(csProject))
            {
                try
                {
                    sqlConnection.Open();
                    string query = $"SELECT PT.[PROJ_UID],PT.[WRES_UID],PT.[PROJ_NAME],RES.[RES_NAME],RES.[WRES_EMAIL]\r\n  FROM [WSS_Content_PWA].[pjpub].[MSP_PROJECTS] AS PT, [WSS_Content_PWA].[pjpub].[MSP_RESOURCES] as RES\r\n  WHERE PT.[PROJ_UID] like @Uid\r\n  AND RES.[RES_UID] = PT.[WRES_UID]";
                    SqlCommand sqlCommand = new SqlCommand(query, sqlConnection)
                    {
                        CommandTimeout = 500
                    };
                    sqlCommand.Parameters.AddWithValue("@Uid", "%" + guid + "%");
                    SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
                    while (sqlDataReader.Read())
                    {
                        try
                        {
                            result.Proj_Name = ((sqlDataReader["PROJ_NAME"] != DBNull.Value) ? sqlDataReader["PROJ_NAME"].ToString() : string.Empty);
                            result.Proj_Uid = ((sqlDataReader["PROJ_UID"] != DBNull.Value) ? sqlDataReader["PROJ_UID"].ToString() : string.Empty);
                            result.Wres_Uid = ((sqlDataReader["WRES_UID"] != DBNull.Value) ? sqlDataReader["PROJ_NAME"].ToString() : string.Empty);
                            result.Res_Name = ((sqlDataReader["RES_NAME"] != DBNull.Value) ? sqlDataReader["RES_NAME"].ToString() : string.Empty);
                            result.Wres_Email = ((sqlDataReader["WRES_EMAIL"] != DBNull.Value) ? sqlDataReader["WRES_EMAIL"].ToString() : string.Empty);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(ex);
                        }
                    }
                }
                catch (Exception ex2)
                {
                    Logger.Log(ex2);
                }
            }

            return result;
        }


        
        public static List<ProjectDb> GetAllProjectsForUser(string loginName)
        {
            Logger.Log("loginName:" + loginName);
            List<ProjectDb> list = new List<ProjectDb>();
            using (SqlConnection sqlConnection = new SqlConnection(csProject))
            {
                try
                {
                    sqlConnection.Open();
                    string text = $"\r\nDECLARE @ResUID uniqueidentifier;\r\nSET @ResUID = (SELECT [RES_UID] FROM [pjpub].[MSP_RESOURCES] WHERE [WRES_ACCOUNT] like @User);\r\n\r\nSELECT DISTINCT A.[PROJ_UID],PT.PROJ_NAME\r\nFROM [pjpub].[MSP_ASSIGNMENTS] AS A\r\n  LEFT JOIN [pjpub].[MSP_PROJECTS] AS PT ON PT.PROJ_UID = A.[PROJ_UID] \r\n  WHERE A.[RES_UID_OWNER] = @ResUID AND A.[TASK_IS_SUMMARY] = 0";
                    SqlCommand sqlCommand = new SqlCommand(text, sqlConnection)
                    {
                        CommandTimeout = 500
                    };
                    sqlCommand.Parameters.AddWithValue("@User", "%" + loginName);
                    SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
                    while (sqlDataReader.Read())
                    {
                        try
                        {
                            list.Add(new ProjectDb
                            {
                                Id = System.Guid.Parse(sqlDataReader["PROJ_UID"].ToString()),
                                Name = ((sqlDataReader["PROJ_NAME"] != DBNull.Value) ? sqlDataReader["PROJ_NAME"].ToString() : string.Empty)
                            });
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(ex);
                        }
                    }
                    Logger.Log($"loginName:{loginName} 3 result:{list.Count}");
                }
                catch (Exception ex2)
                {
                    Logger.Log(ex2);
                }
            }
            return list;
        }

        public static CheckOutInfo ProjectCheckedOut(System.Guid projUid)
        {
            CheckOutInfo result = new CheckOutInfo() { IsCheckedOut = false };

            using (SqlConnection sqlConnection = new SqlConnection(csProject))
            {
                try
                {
                    sqlConnection.Open();
                    string query = "SELECT count(*) FROM [pjpub].[MSP_PROJECTS] WITH (NOLOCK) WHERE [PROJ_CHECKOUTBY] IS NULL AND PROJ_UID in (@ProjUid)";
                    SqlCommand sqlCommand = new SqlCommand(query, sqlConnection)
                    {
                        CommandTimeout = 500
                    };
                    sqlCommand.Parameters.AddWithValue("@ProjUid", projUid);
                    var count = (int)sqlCommand.ExecuteScalar();
                    if (count == 0)
                    {
                        result.IsCheckedOut = true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
            }
            if (result.IsCheckedOut)
            {
                using (SqlConnection sqlConnection = new SqlConnection(csProject))
                {
                    try
                    {
                        sqlConnection.Open();
                        string query = "SELECT PT.[PROJ_CHECKOUTDATE],RES.[RES_NAME]\r\n  FROM [WSS_Content_PWA].[pjpub].[MSP_PROJECTS] AS PT, [WSS_Content_PWA].[pjpub].[MSP_RESOURCES] as RES\r\n  WHERE PT.[PROJ_UID] in (@ProjUid)\r\n  AND PT.[PROJ_CHECKOUTBY] = RES.[RES_UID]\r\n";
                        SqlCommand sqlCommand = new SqlCommand(query, sqlConnection)
                        {
                            CommandTimeout = 500
                        };
                        sqlCommand.Parameters.AddWithValue("@ProjUid", projUid);
                        SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
                        while (sqlDataReader.Read())
                        {
                            try
                            {
                                if (sqlDataReader["PROJ_CHECKOUTDATE"] != DBNull.Value)
                                {
                                    result.CheckedOutDate = ((DateTime)sqlDataReader["PROJ_CHECKOUTDATE"]).ToString("dd.MM.yyyy");
                                }
                                if (sqlDataReader["RES_NAME"] != DBNull.Value)
                                {
                                    result.CheckedOutBy = (string)sqlDataReader["RES_NAME"];

                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Log(ex, "GetAllProjectsCheckedOut.ItemDB");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                    }
                }
            }
            return result;
        }
        
        public static List<Guid> GetAllProjectsNoCheckedOut(List<Guid> projUids)
        {
            Logger.Log("Get checkedout project in " + JsonConvert.SerializeObject(projUids));
            List<Guid> list = new List<Guid>();
            using (SqlConnection sqlConnection = new SqlConnection(csProject))
            {
                try
                {
                    string text = "";
                    for (int i = 0; i < projUids.Count; i++)
                    {
                        text = text + "@ProjUid" + i;
                        if (i != projUids.Count - 1)
                        {
                            text += ", ";
                        }
                    }
                    sqlConnection.Open();
                    string text2 = "SELECT PROJ_UID, [PROJ_CHECKOUTBY]\r\n  FROM [pjpub].[MSP_PROJECTS] WITH (NOLOCK)\r\n  WHERE [PROJ_CHECKOUTBY] IS NULL AND PROJ_UID IN (" + text + ")";
                    SqlCommand sqlCommand = new SqlCommand(text2, sqlConnection)
                    {
                        CommandTimeout = 500
                    };
                    for (int j = 0; j < projUids.Count; j++)
                    {
                        sqlCommand.Parameters.AddWithValue("@ProjUid" + j, projUids[j]);
                    }
                    SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
                    while (sqlDataReader.Read())
                    {
                        try
                        {
                            if (sqlDataReader["PROJ_UID"] != DBNull.Value)
                            {
                                Guid item = Guid.Parse(sqlDataReader["PROJ_UID"].ToString());
                                if (!list.Contains(item))
                                {
                                    list.Add(item);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(ex, "GetAllProjectsCheckedOut.ItemDB");
                        }
                    }
                    Logger.Log($"NoCheckedOut projects: {list.Count}");
                }
                catch (Exception ex2)
                {
                    Logger.Log(ex2);
                }
            }
            return list;
        }

        public static List<ApprovalWorkInfo> GetApproveNonMaterial(Guid projUid, List<ApprovalWorkInfo> infos)
        {
            Logger.Log("Получаем задачи для утверждения по проекту");
            using (SqlConnection sqlConnection = new SqlConnection(csProject))
            {
                try
                {
                    string configSetting = ConfigManager.GetConfigSetting("PercentCompleted");
                    sqlConnection.Open();
                    string text = string.Format("SELECT [ProjectUID]\r\n      ,[TaskUID]\r\n      ,[TaskStartDate]\r\n\t  ,[TaskFinishDate]\r\n      ,[" + configSetting + "]\r\n      ,A.ASSN_RES_TYPE\r\n  FROM [pjrep].[MSP_EpmTask_UserView] AS T\r\n  LEFT JOIN [pjpub].[MSP_ASSIGNMENTS] AS A ON A.TASK_UID = TaskUID\r\n  WHERE [ProjectUID] = @ProjUid\r\n");
                    SqlCommand sqlCommand = new SqlCommand(text, sqlConnection)
                    {
                        CommandTimeout = 500
                    };
                    sqlCommand.Parameters.AddWithValue("@ProjUid", projUid.ToString());
                    SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
                    while (sqlDataReader.Read())
                    {
                        try
                        {
                            Guid taskUid = Guid.Parse(sqlDataReader["TaskUID"].ToString());
                            ApprovalWorkInfo approvalWorkInfo = infos.FirstOrDefault(a => a.TaskUid == taskUid);
                            if (approvalWorkInfo == null)
                            {
                                continue;
                            }
                            if (sqlDataReader["TaskStartDate"] != DBNull.Value)
                            {
                                DateTime dateTime = Convert.ToDateTime(sqlDataReader["TaskStartDate"].ToString());
                                if (approvalWorkInfo.Start.HasValue && approvalWorkInfo.Start.Value == dateTime)
                                {
                                    infos.FirstOrDefault(a => a.TaskUid == taskUid).Start = null;
                                }
                            }
                            if (sqlDataReader["TaskFinishDate"] != DBNull.Value)
                            {
                                DateTime dateTime2 = Convert.ToDateTime(sqlDataReader["TaskFinishDate"].ToString());
                                if (approvalWorkInfo.Finish.HasValue && approvalWorkInfo.Finish.Value == dateTime2)
                                {
                                    infos.FirstOrDefault(a => a.TaskUid == taskUid).Finish = null;
                                }
                            }
                            if (sqlDataReader[configSetting] != DBNull.Value)
                            {
                                int num = Convert.ToInt32(sqlDataReader[configSetting]);
                                if (approvalWorkInfo.PercentComplete.HasValue && approvalWorkInfo.PercentComplete.Value == num)
                                {
                                    infos.FirstOrDefault(a => a.TaskUid == taskUid).PercentComplete = null;
                                }
                            }
                            if (sqlDataReader["ASSN_RES_TYPE"] != DBNull.Value && Convert.ToBoolean(sqlDataReader["ASSN_RES_TYPE"].ToString()))
                            {
                                infos.FirstOrDefault(a => a.TaskUid == taskUid).IsMaterial = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(ex, "GetApproveNonMaterial.ItemDB");
                        }
                    }
                }
                catch (Exception ex2)
                {
                    Logger.Log(ex2);
                }
            }
            return infos;
        }

        
        public static List<ProjectDb> GetAllProjectsForUserStatusManager(string loginName)
        {
            Logger.Log("loginName:" + loginName);
            List<ProjectDb> list = new List<ProjectDb>();
            using (SqlConnection sqlConnection = new SqlConnection(csProject))
            {
                try
                {
                    sqlConnection.Open();
                    string text = $"\r\nSELECT PT.PROJ_UID, R.[RES_NAME] as OwnerProj,PT.PROJ_NAME \r\nFROM [pjpub].[MSP_PROJECTS] AS PT\r\nINNER JOIN [pjpub].[MSP_RESOURCES] AS R ON R.RES_UID = PT.WRES_UID";
                    
                    SqlCommand sqlCommand = new SqlCommand(text, sqlConnection)
                    {
                        CommandTimeout = 500
                    };
                    SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
                    while (sqlDataReader.Read())
                    {
                        try
                        {
                            var proj = new ProjectDb();
                            proj.Id = Guid.Parse(sqlDataReader["PROJ_UID"].ToString());
                            proj.CheckOutInfo = DbManager.ProjectCheckedOut(proj.Id);
                            proj.Name = ((sqlDataReader["PROJ_NAME"] != DBNull.Value) ? sqlDataReader["PROJ_NAME"].ToString() : string.Empty);
                            proj.OwnerName = ((sqlDataReader["OwnerProj"] != DBNull.Value) ? sqlDataReader["OwnerProj"].ToString() : string.Empty);
                            proj.Tasks = new List<TaskDb>();
                            list.Add(proj);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(ex, "GetAllProjectsForUserStatusManager.ProjDB");
                        }
                    }
                    sqlDataReader.Close();
                    text = $"\r\nDECLARE @ResUID uniqueidentifier;\r\nSET @ResUID = (SELECT [RES_UID] FROM [pjpub].[MSP_RESOURCES] WHERE [WRES_ACCOUNT] like @User);\r\n\r\nSELECT A.PROJ_UID as ProjectUID, A.TASK_UID as TaskUID\r\nFROM [pjpub].[MSP_ASSIGNMENTS] AS A\r\nWHERE A.[WRES_UID_MANAGER] = @ResUID AND A.TASK_IS_SUMMARY = 0";
                    sqlCommand = new SqlCommand(text, sqlConnection);
                    sqlCommand.Parameters.AddWithValue("@User", "%" + loginName);
                    sqlDataReader = sqlCommand.ExecuteReader();
                    while (sqlDataReader.Read())
                    {
                        try
                        {
                            Guid projUid = Guid.Parse(sqlDataReader["ProjectUID"].ToString());
                            Guid taskUid = Guid.Parse(sqlDataReader["TaskUID"].ToString());
                            if (list.Any(a => a.Id == projUid))
                            {
                                list.FirstOrDefault(f => f.Id == projUid).Tasks.Add(new TaskDb
                                {
                                    TaskUid = taskUid
                                });
                            }
                        }
                        catch (Exception ex2)
                        {
                            Logger.Log(ex2, "GetAllProjectsForUserStatusManager.TaskDB");
                        }
                    }
                    Logger.Log($"loginName:{loginName} 4 result:{list.Count}");
                }
                catch (Exception ex3)
                {
                    Logger.Log(ex3);
                }
            }
            if (!list.Any())
            {
                return new List<ProjectDb>();
            }
            List<ProjectDb> result = new List<ProjectDb>();
            try
            {
                using (VolumeContext ctx = new VolumeContext())
                {
                    try
                    {
                        list.ForEach(delegate (ProjectDb projPWA)
                        {
                            List<VolumeData> list2 = ctx.VolumeDatas.Where(w => w.ProjectUid == projPWA.Id && w.Status == 2).ToList();
                            if (list2 != null && list2.Any())
                            {
                                list2.ForEach(delegate (VolumeData v)
                                {
                                    if (!result.Any(a => a.Id == projPWA.Id) && projPWA.Tasks.Any(aaa => aaa.TaskUid == v.TaskUid))
                                    {
                                        result.Add(projPWA);
                                    }
                                });
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                    }
                }
            }
            catch (Exception ex4)
            {
                Logger.Log(ex4);
            }
            return result;
        }

        
        public static List<TaskDb> GetAllAssnForUserStatusManager(string loginName, Guid projUid)
        {
            List<TaskDb> tasksProject = new List<TaskDb>();
            List<ProjectDb> list = new List<ProjectDb>();
            List<TaskDb> list2 = new List<TaskDb>();
            try
            {
                Logger.Log("GetAllAssnForUserStatusManager 1");
                List<TaskDb> dbTasks = GetAllAssnForUserDbTasksStatusManager(loginName, projUid);
                if (!dbTasks.Any())
                {
                    return tasksProject;
                }
                Logger.Log("GetAllAssnForUserStatusManager 2");
                try 
                {
                    using (VolumeContext ctx = new VolumeContext())
                    {
                        try
                        {
                            List<VolumeData> list3 = ctx.VolumeDatas.Where(w => w.ProjectUid == projUid && w.Status == 2).ToList();
                            if (list3 != null && list3.Any())
                            {
                                list3.ForEach(delegate (VolumeData v)
                                {
                                    if (dbTasks.Any(a => a.TaskUid == v.TaskUid && a.AssnUid == v.AssnUid))
                                    {
                                        TaskDb t = dbTasks.FirstOrDefault(a => a.TaskUid == v.TaskUid && a.AssnUid == v.AssnUid);
                                        t.Comments = v.Comments;
                                        t.Start = v.StartDate;
                                        t.Finish = v.FinishDate;
                                        t.PercentCompleteWork = (int)v.PercentCompleted;
                                        List<VolumeInDay> source = ctx.VolumeInDays.Where(r => r.AssnUid == v.AssnUid && ((t.Start >= r.Date && r.Date <= t.Finish) || r.Status == 2 || r.Status == 4)).ToList();
                                        if (source.Any())
                                        {
                                            DateTime dateTime = source.Select(r => r.Date).Min();
                                            DateTime dateTime2 = source.Select(r => r.Date).Max();
                                            double num = source.Select(s => s.Volume).Sum();
                                            if (t.VolumePlan < num)
                                            {
                                                t.VolumePlan = num;
                                            }
                                            t.ActualWork = num;
                                        }
                                        tasksProject.Add(t);
                                    }
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(ex);
                        }
                    }
                }
                catch (Exception ex) 
                {
                    Logger.Log(ex);
                }
                

                List<Guid> list4 = new List<Guid>();
                foreach (TaskDb task in dbTasks.OrderByDescending(o => o.OutlineLevel))
                {
                    if (!tasksProject.Any(a => a.TaskUid == task.TaskUid && a.AssnUid == task.AssnUid) && !list4.Contains(task.TaskUid))
                    {
                        continue;
                    }
                    if (tasksProject.Any(a => a.TaskUid == task.TaskUid && a.AssnUid == task.AssnUid))
                    {
                        list2.Add(tasksProject.FirstOrDefault(a => a.TaskUid == task.TaskUid && a.AssnUid == task.AssnUid));
                    }
                    if (list4.Contains(task.TaskUid) && !list2.Any(a => a.TaskUid == task.TaskUid))
                    {
                        list2.Add(task);
                    }
                    if (task.Parent.HasValue && !list4.Contains(task.Parent.Value))
                    {
                        list4.Add(task.Parent.Value);
                    }
                }
            }
            catch (Exception ex2)
            {
                Logger.Log(ex2);
            }
            return list2;
        }

        public static List<TaskDb> GetAllAssnForUser(string loginName, Guid projUid, Properties.TypeForm typeFormEnum) 
        {
            List<TaskDb> list = new List<TaskDb>();
            try
            {
                Logger.Log($"loginName:{loginName} projUid:{projUid} 1");
                Dictionary<Guid, List<AssnDb>> allAssnForUserDbAssn = GetAllAssnForUserDbAssn(loginName, projUid);
                Logger.Log($"loginName:{loginName} projUid:{projUid} 2 dbAssn:{allAssnForUserDbAssn.Count}");
                if (!allAssnForUserDbAssn.Any())
                {
                    return list;
                }
                Logger.Log($"loginName:{loginName} projUid:{projUid} 3 dbAssn:{allAssnForUserDbAssn.Count}");
                string strProj = $" = '{projUid}'";
                Dictionary<Guid, List<TaskDb>> allAssnForUserDbTasks = GetAllAssnForUserDbTasks(allAssnForUserDbAssn, strProj);
                Logger.Log($"loginName:{loginName} projUid:{projUid} 4 dbTasks:{allAssnForUserDbTasks.Count}");
                if (!allAssnForUserDbTasks.Any())
                {
                    return list;
                }
                list = GetTaskTree(allAssnForUserDbTasks.FirstOrDefault().Value, allAssnForUserDbAssn.FirstOrDefault().Value);
                Logger.Log($"loginName:{loginName} projUid:{projUid} 5 result:{list.Count}");
                list = UpdatingWithDataFromDatabase(list, projUid, typeFormEnum);
                Logger.Log($"loginName:{loginName} projUid:{projUid} 6 result:{list.Count}");
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return list;
        }

        public static List<TaskDb> GetTaskTree(List<TaskDb> tasks, List<AssnDb> dbAssn)
        {
            List<TaskDb> list = new List<TaskDb>();
            List<Guid> list2 = new List<Guid>();
            try
            {
                foreach (TaskDb task in tasks.OrderByDescending(o => o.OutlineLevel))
                {
                    if (!dbAssn.Any(a => a.TaskUid == task.TaskUid) && !list2.Contains(task.TaskUid))
                    {
                        continue;
                    }
                    if (dbAssn.Any(a => a.TaskUid == task.TaskUid))
                    {
                        AssnDb assnDb = dbAssn.FirstOrDefault(a => a.TaskUid == task.TaskUid);
                        task.AssnUid = assnDb.AssnUid;
                        task.VolumePlan = assnDb.VolumePlan;
                        task.Measure = assnDb.Measure;
                        task.ActualWork = assnDb.ActualWork;
                        task.PercentCompleteWork = assnDb.PercentCompleteWork;
                    }
                    list.Add(task);
                    if (task.Parent.HasValue && !list2.Contains(task.Parent.Value))
                    {
                        list2.Add(task.Parent.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return list;
        }

        public static List<TaskDb> UpdatingWithDataFromDatabase(List<TaskDb> assns, Guid projUid, Properties.TypeForm typeFormEnum)
        {
            try
            {
                using (VolumeContext ctx = new VolumeContext())
                {
                    try
                    {
                        assns.Where(w => w.AssnUid.HasValue && w.AssnUid != Guid.Empty).ToList().ForEach(delegate (TaskDb assn)
                        {
                            VolumeData volumeData = ctx.VolumeDatas.FirstOrDefault(r => r.AssnUid == assn.AssnUid && r.TaskUid == assn.TaskUid && r.ProjectUid == projUid);
                            if (volumeData != null)
                            {
                                assn.Comments = volumeData.Comments;
                                if (typeFormEnum == Properties.TypeForm.AgreeValue || (typeFormEnum == Properties.TypeForm.InsertValue && (volumeData.Status == 4 || volumeData.Status == 2)))
                                {
                                    assn.Start = volumeData.StartDate;
                                    assn.Finish = volumeData.FinishDate;
                                    assn.PercentCompleteWork = (int)volumeData.PercentCompleted;
                                    IQueryable<VolumeInDay> source = ctx.VolumeInDays.Where(r => r.AssnUid == assn.AssnUid && ((assn.Start >= r.Date && r.Date <= assn.Finish) || r.Status == 2 || r.Status == 4));
                                    double num = 0.0;
                                    if (source.Any())
                                    {
                                        num = source.Select(s => s.Volume).Sum();
                                    }
                                    if (assn.VolumePlan < num)
                                    {
                                        assn.VolumePlan = num;
                                    }
                                    assn.ActualWork = num;
                                }
                                else
                                {
                                    assn.Work = assn.VolumePlan;
                                    assn.IsProjectSource = true;
                                }
                            }
                            else if (assn.ActualWork > 0.0 && typeFormEnum == Properties.TypeForm.InsertValue)
                            {
                                assn.Work = assn.VolumePlan;
                                assn.IsProjectSource = true;
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return assns;
        }

        
        public static List<VolumeShare> GetVolumesShare(List<Guid> assnsUid, Guid projUid)
        {
            List<VolumeShare> result = new List<VolumeShare>();
            try
            {
                using (VolumeContext ctx = new VolumeContext())
                {
                    try
                    {
                        ctx.VolumeDatas.Where(w => w.ProjectUid == projUid).ToList().ForEach(delegate (VolumeData f)
                        {
                            if (assnsUid.Contains(f.AssnUid))
                            {
                                result.Add(new VolumeShare
                                {
                                    AssnUid = f.AssnUid,
                                    VolumeInDays = ctx.VolumeInDays.Where(w => w.AssnUid == f.AssnUid).ToList(),
                                    VolumeData = f
                                });
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return result;
        }

        public static List<VolumeInDay> GetVolumeByDay(Guid assnUid)
        {
            try
            {
                using (VolumeContext volumeContext = new VolumeContext())
                {
                    return volumeContext.VolumeInDays.Where(v => v.AssnUid == assnUid).ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public static VolumeData GetVolumeData(Guid assnUid)
        {
            try
            {
                using (VolumeContext volumeContext = new VolumeContext())
                {
                    return volumeContext.VolumeDatas.FirstOrDefault(v => v.AssnUid == assnUid);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        
        public static void ClearingDeletedTasks(Guid projUid)
        {
            List<Guid> assnsUids = new List<Guid>();
            using (SqlConnection sqlConnection = new SqlConnection(csProject))
            {
                try
                {
                    sqlConnection.Open();
                    string cmdText = $"SELECT [ASSN_UID],[PROJ_UID] FROM [pjpub].[MSP_ASSIGNMENTS] WHERE [PROJ_UID] = @ProjUid";
                    SqlCommand sqlCommand = new SqlCommand(cmdText, sqlConnection)
                    {
                        CommandTimeout = 500
                    };
                    sqlCommand.Parameters.AddWithValue("@ProjUid", projUid.ToString());
                    SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
                    while (sqlDataReader.Read())
                    {
                        try
                        {
                            Guid item = Guid.Parse(sqlDataReader["ASSN_UID"].ToString());
                            assnsUids.Add(item);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(ex, "ClearingDeletedTasks.Item");
                        }
                    }
                }
                catch (Exception ex2)
                {
                    Logger.Log(ex2);
                }
            }
            if (!assnsUids.Any())
            {
                return;
            }
            try
            {
                using (VolumeContext ctx = new VolumeContext())
                {
                    try
                    {
                        List<VolumeData> dellVolumeDatas = new List<VolumeData>();
                        List<VolumeInDay> dellVolumeInDays = new List<VolumeInDay>();
                        ctx.VolumeDatas.Where(v => v.ProjectUid == projUid).ToList().ForEach(delegate (VolumeData vd)
                        {
                            if (!assnsUids.Contains(vd.AssnUid))
                            {
                                dellVolumeDatas.Add(vd);
                                dellVolumeInDays.AddRange(ctx.VolumeInDays.Where(w => w.AssnUid == vd.AssnUid).ToList());
                            }
                        });
                        if (dellVolumeDatas.Any())
                        {
                            ctx.VolumeDatas.RemoveRange(dellVolumeDatas); 
                        }
                        if (dellVolumeInDays.Any())
                        {
                            ctx.VolumeInDays.RemoveRange(dellVolumeInDays); 
                        }
                        SaveDbExceptionLog(ctx);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                    }
                }
            }
            catch (Exception ex3)
            {
                Logger.Log(ex3);
            }
        }

        public static List<AssnByDay> GetAssnsByDay(List<Guid> assnUids)
        {
            Logger.Log("GetAssnsByDay start " + JsonConvert.SerializeObject(assnUids));
            List<AssnByDay> list = new List<AssnByDay>();

            DateTime dateTime = DateTime.MinValue;
            if (!assnUids.Any())
            {
                return list;
            }
            using (SqlConnection sqlConnection = new SqlConnection(csProject))
            {
                try
                {
                    sqlConnection.Open();
                    string cmdText = string.Format("SELECT [AssignmentUID], [TimeByDay], [AssignmentMaterialActualWork], [AssignmentMaterialWork], [AssignmentModifiedDate]\r\n                FROM [pjrep].[MSP_EpmAssignmentByDay]\r\n                WHERE AssignmentUID in (" + assnUids.Aggregate("", delegate (string w, Guid n)
                    {
                        Guid guid = n;
                        return w + ",'" + guid.ToString() + "'";
                    }).Remove(0, 1) + ")");
                    SqlCommand sqlCommand = new SqlCommand(cmdText, sqlConnection)
                    {
                        CommandTimeout = 500
                    };
                    SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
                    while (sqlDataReader.Read())
                    {
                        try
                        {
                            Guid assignmentUID = Guid.Parse(sqlDataReader["AssignmentUID"].ToString());
                            if (dateTime == DateTime.MinValue || Convert.ToDateTime(sqlDataReader["AssignmentModifiedDate"].ToString()) > dateTime)
                            {
                                dateTime = Convert.ToDateTime(sqlDataReader["AssignmentModifiedDate"].ToString());
                            }
                            AssnByDay.DayFact item = new AssnByDay.DayFact
                            {
                                Date = Convert.ToDateTime(sqlDataReader["TimeByDay"].ToString()),
                                Fact = double.Parse(sqlDataReader["AssignmentMaterialActualWork"].ToString()),
                                Plan = double.Parse(sqlDataReader["AssignmentMaterialWork"].ToString())
                            };
                            if (!list.Any(a => a.AssnUid == assignmentUID))
                            {
                                list.Add(new AssnByDay
                                {
                                    AssnUid = assignmentUID,
                                    DaysFacts = new List<AssnByDay.DayFact>()
                                });
                            }
                            list.FirstOrDefault(a => a.AssnUid == assignmentUID).DaysFacts.Add(item);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(ex, "GetAssnsByDay.Item");
                        }
                    }
                }
                catch (Exception ex2)
                {
                    Logger.Log(ex2);
                }
            }

            Logger.Log($"GetAssnsByDay finish {JsonConvert.SerializeObject(assnUids)} result.Count:{list.Count} assignmentModifiedDate:{dateTime}");
            return list;
        }

        private static Guid GetResUid(string login)
        {
            Guid result = new Guid();

            string query = "SELECT RES_UID FROM [pjpub].[MSP_RESOURCES] WHERE [WRES_ACCOUNT] = @Login";

            using (SqlConnection sqlConnection = new SqlConnection(csProject))
            {
                try
                {
                    sqlConnection.Open();
                    SqlCommand sqlCommand = new SqlCommand(query, sqlConnection)
                    {
                        CommandTimeout = 500
                    };
                    sqlCommand.Parameters.Add("@Login", SqlDbType.NVarChar, 255).Value = login;
                    SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
                    while(sqlDataReader.Read())
                    {
                        result = Guid.Parse(sqlDataReader["RES_UID"].ToString());
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
            }
            Logger.Log($"For login: {login}, RES_UID: {result}");
            return result;
        }

        
        public static List<TaskDb> GetAllAssnForUserDbTasksStatusManager(string loginName, Guid projUid)
        {
            var start = DateTime.Now;
            List<TaskDb> list = new List<TaskDb>();

            var resUid = GetResUid(loginName);

            using (SqlConnection sqlConnection = new SqlConnection(csProject))
            {
                StringBuilder log = new StringBuilder();
                try
                {
                    Logger.Log($"GetAllAssnForUserDbTasksStatusManager {projUid} {loginName}");
                    string configSettingPercentTeh = ConfigManager.GetConfigSetting("PercentCompleted");
                    string configSetting2 = ConfigManager.GetConfigSetting("KindWork");
                    string configSetting3 = ConfigManager.GetConfigSetting("Stage");
                    string configSetting4 = ConfigManager.GetConfigSetting("Blok");
                    string configSetting5 = ConfigManager.GetConfigSetting("Blok2");
                    string configCapture = ConfigManager.GetConfigSetting("Capture");

                    sqlConnection.Open();
                    string cmdText = "SELECT [TaskParentUID], [ProjectUID], [TaskUID], [TaskName], [TaskIndex], [TaskIsSummary], [TaskOutlineLevel], [TaskStartDate], [TaskFinishDate]," +
                        " [TaskBaseline0StartDate]\r\n\t  ,[TaskBaseline0FinishDate]\r\n\t  ,[TaskBaseline1StartDate]\r\n      ,[TaskBaseline1FinishDate]\r\n\t  ,[TaskBaseline2StartDate]\r\n      ,[TaskBaseline2FinishDate]\r\n\t  ,[TaskBaseline3StartDate]\r\n      ,[TaskBaseline3FinishDate]\r\n\t  ,[TaskBaseline4StartDate]\r\n      ,[TaskBaseline4FinishDate]\r\n\t  ,[TaskBaseline5StartDate]\r\n      ,[TaskBaseline5FinishDate]\r\n\t  ,[TaskBaseline6StartDate]\r\n      ,[TaskBaseline6FinishDate]\r\n\t  ,[TaskBaseline7StartDate]\r\n      ,[TaskBaseline7FinishDate]\r\n\t  ,[TaskBaseline8StartDate]\r\n      ,[TaskBaseline8FinishDate]\r\n\t  ,[TaskBaseline9StartDate]\r\n      ,[TaskBaseline9FinishDate]\r\n\t  ,[TaskBaseline10StartDate]\r\n      ,[TaskBaseline10FinishDate]," +
                        " [TaskPercentCompleted]\r\n,[TaskWBS]," +
                        " [" + configSetting2 + "]\r\n,[" + configSetting3 + "]\r\n,[" + configSetting4 + "]\r\n,[" + configSetting5 + "]\r\n,[" + configCapture + "]\r\n,[" + configSettingPercentTeh + "]\r\n,R.RES_MATERIAL_LABEL,\r\n A.[RES_UID],\r\n A.[ASSN_WORK]/60000 as work,\r\n A.[RES_UID_OWNER],\r\n A.[ASSN_UID]" +
                        " FROM [pjrep].[MSP_EpmTask_UserView] as ET WITH (NOLOCK)" +
                        " LEFT JOIN [pjpub].[MSP_ASSIGNMENTS] AS A WITH (NOLOCK)" +
                        " ON ET.TaskUID =  A.TASK_UID " +
                        " LEFT JOIN [pjpub].[MSP_RESOURCES] AS R WITH (NOLOCK)" +
                        " ON R.[RES_UID] = A.[RES_UID] \r\n  WHERE [ProjectUID] = @ProjUid AND (A.[WRES_UID_MANAGER] = @ResUID OR ET.[TaskIsSummary] = 1)";
                    Logger.Log(cmdText);
                    
                    using (SqlCommand sqlCommand = new SqlCommand(cmdText, sqlConnection))
                    {
                        sqlCommand.CommandTimeout = 500;

                        

                        sqlCommand.Parameters.Add("@ProjUid", SqlDbType.UniqueIdentifier).Value = projUid;
                        sqlCommand.Parameters.Add("@Login", SqlDbType.NVarChar, 255).Value = loginName;
                        sqlCommand.Parameters.Add("@ResUID", SqlDbType.UniqueIdentifier).Value = resUid;

                        using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader())
                        {
                            Logger.Log("ExecuteReader");
                            int current = 0;

                            while (true)
                            {
                                try
                                {
                                    var readStart = DateTime.Now;
                                    bool hasRow = sqlDataReader.Read();
                                    var readTime = DateTime.Now - readStart;

                                    if (!hasRow) break;
                                    current++;

                                    Guid taskUid = Guid.Parse(sqlDataReader["TaskUID"].ToString());

                                    if (readTime.TotalSeconds > 1)
                                    {
                                        log.AppendLine($"[{DateTime.Now}] {current} READ delay: taskId {taskUid}, read time: {readTime.TotalSeconds} sec");
                                    }


                                    Guid guid = Guid.Parse(sqlDataReader["ProjectUID"].ToString());
                                    bool flag = Convert.ToBoolean(sqlDataReader["TaskIsSummary"].ToString());

                                    var dstart = DateTime.Now;

                                    TaskDb taskDb = new TaskDb
                                    {
                                        TaskUid = taskUid,
                                        BaselineStart = new List<DateTime>(),
                                        BaselineFinish = new List<DateTime>(),
                                        IsSummary = flag
                                    };
                                    if (sqlDataReader["TaskName"] != DBNull.Value)
                                    {
                                        taskDb.Name = sqlDataReader["TaskName"].ToString();
                                    }
                                    if (sqlDataReader["TaskWBS"] != DBNull.Value)
                                    {
                                        taskDb.WorkBreakdownStructure = sqlDataReader["TaskWBS"].ToString();
                                    }
                                    if (sqlDataReader["TaskIndex"] != DBNull.Value)
                                    {
                                        taskDb.TaskIndex = Convert.ToInt32(sqlDataReader["TaskIndex"].ToString());
                                    }
                                    if (sqlDataReader["TaskParentUID"] != DBNull.Value)
                                    {
                                        taskDb.Parent = Guid.Parse(sqlDataReader["TaskParentUID"].ToString());
                                    }
                                    if (sqlDataReader["TaskOutlineLevel"] != DBNull.Value)
                                    {
                                        taskDb.OutlineLevel = Convert.ToInt32(sqlDataReader["TaskOutlineLevel"].ToString());
                                    }
                                    if (sqlDataReader["TaskStartDate"] != DBNull.Value)
                                    {
                                        taskDb.Start = Convert.ToDateTime(sqlDataReader["TaskStartDate"].ToString());
                                    }
                                    if (sqlDataReader["TaskFinishDate"] != DBNull.Value)
                                    {
                                        taskDb.Finish = Convert.ToDateTime(sqlDataReader["TaskFinishDate"].ToString());
                                    }
                                    for (int i = 0; i < 11; i++)
                                    {
                                        if (sqlDataReader[$"TaskBaseline{i}StartDate"] != DBNull.Value)
                                        {
                                            taskDb.BaselineStart.Add(Convert.ToDateTime(sqlDataReader[$"TaskBaseline{i}StartDate"].ToString()));
                                        }
                                        else
                                        {
                                            taskDb.BaselineStart.Add(DateTime.MinValue);
                                        }
                                        if (sqlDataReader[$"TaskBaseline{i}FinishDate"] != DBNull.Value)
                                        {
                                            taskDb.BaselineFinish.Add(Convert.ToDateTime(sqlDataReader[$"TaskBaseline{i}FinishDate"].ToString()));
                                        }
                                        else
                                        {
                                            taskDb.BaselineFinish.Add(DateTime.MinValue);
                                        }
                                    }
                                    

                                    if (sqlDataReader[configSettingPercentTeh] != DBNull.Value)
                                    {
                                        taskDb.PercentCompleteCustom = Convert.ToInt32(sqlDataReader[configSettingPercentTeh]); 
                                    }
                                    if (sqlDataReader[configSetting2] != DBNull.Value)
                                    {
                                        taskDb.KindWork = sqlDataReader[configSetting2].ToString();
                                    }
                                    if (sqlDataReader[configSetting3] != DBNull.Value)
                                    {
                                        taskDb.Stage = sqlDataReader[configSetting3].ToString();
                                    }
                                    if (sqlDataReader[configSetting4] != DBNull.Value)
                                    {
                                        taskDb.Blok = sqlDataReader[configSetting4].ToString();
                                    }
                                    if (sqlDataReader[configSetting5] != DBNull.Value)
                                    {
                                        taskDb.Blok2 = sqlDataReader[configSetting5].ToString();
                                    }
                                    if (sqlDataReader[configCapture] != DBNull.Value)
                                    {
                                        taskDb.Capture = sqlDataReader[configCapture].ToString(); 
                                    }
                                    taskDb.Measure = "ч";
                                    if (sqlDataReader["RES_MATERIAL_LABEL"] != DBNull.Value)
                                    {
                                        taskDb.Measure = sqlDataReader["RES_MATERIAL_LABEL"].ToString();
                                    }
                                    if (sqlDataReader["work"] != DBNull.Value)
                                    {
                                        double.TryParse(sqlDataReader["work"].ToString(), out taskDb.VolumePlan);
                                    }
                                    if (!flag && sqlDataReader["ASSN_UID"] != DBNull.Value)
                                    {
                                        taskDb.AssnUid = Guid.Parse(sqlDataReader["ASSN_UID"].ToString());
                                    }
                                    if (sqlDataReader["TaskPercentCompleted"] != DBNull.Value)
                                    {
                                        taskDb.PercentComplete = Convert.ToInt32(sqlDataReader["TaskPercentCompleted"].ToString());
                                    }
                                    list.Add(taskDb);
                                    TimeSpan dts = DateTime.Now - dstart;

                                    if (dts.TotalSeconds > 1)
                                    {
                                        log.AppendLine($"{current} Problem task: {taskDb.Name}, ID: {taskUid} parse time: {dts.TotalSeconds} sec");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.Log(ex);
                                }
                            }
                        }
                    }
                    
                }
                catch (Exception ex2)
                {
                    Logger.Log(ex2);
                }
                System.IO.File.AppendAllText(@"C:\Logs\Log.txt", log.ToString());
            }

            TimeSpan end = DateTime.Now - start;
            Logger.Log($"Get Tasks: {projUid} {loginName} count={list.Count}, in time: {end.TotalMinutes} мин, {end.TotalSeconds} сек");
            return list;
        }

        public static Dictionary<Guid, List<TaskDb>> GetAllAssnForUserDbTasks(Dictionary<Guid, List<AssnDb>> assns, string strProj)
        {
            var start = DateTime.Now;
            Logger.Log("Start получения всех назначений пользователя");

            Dictionary<Guid, List<TaskDb>> dictionary = new Dictionary<Guid, List<TaskDb>>();
            using (SqlConnection sqlConnection = new SqlConnection(csProject))
            {
                try
                {
                    Logger.Log("strProj:" + strProj);
                    string configSetting = ConfigManager.GetConfigSetting("KindWork");
                    string configSetting2 = ConfigManager.GetConfigSetting("Stage");
                    string configSetting3 = ConfigManager.GetConfigSetting("Blok");
                    string configSetting4 = ConfigManager.GetConfigSetting("Blok2");
                    string configCapture = ConfigManager.GetConfigSetting("Capture");
                    string configSettingPercentTeh = ConfigManager.GetConfigSetting("PercentCompleted");

                    sqlConnection.Open();
                    string text = string.Format("\r\nSELECT [TaskParentUID]\r\n      ,[ProjectUID]\r\n      ,[TaskUID]\r\n      ,[TaskName]\r\n      ,[TaskIndex]\r\n      ,[TaskIsSummary]\r\n      ,[TaskOutlineLevel]\r\n      ,[TaskStartDate]\r\n\t  ,[TaskFinishDate]\r\n\t  ,[TaskBaseline0StartDate]\r\n\t  ,[TaskBaseline0FinishDate]\r\n\t  ,[TaskBaseline1StartDate]\r\n      ,[TaskBaseline1FinishDate]\r\n\t  ,[TaskBaseline2StartDate]\r\n      ,[TaskBaseline2FinishDate]\r\n\t  ,[TaskBaseline3StartDate]\r\n      ,[TaskBaseline3FinishDate]\r\n\t  ,[TaskBaseline4StartDate]\r\n      ,[TaskBaseline4FinishDate]\r\n\t  ,[TaskBaseline5StartDate]\r\n      ,[TaskBaseline5FinishDate]\r\n\t  ,[TaskBaseline6StartDate]\r\n      ,[TaskBaseline6FinishDate]\r\n\t  ,[TaskBaseline7StartDate]\r\n      ,[TaskBaseline7FinishDate]\r\n\t  ,[TaskBaseline8StartDate]\r\n      ,[TaskBaseline8FinishDate]\r\n\t  ,[TaskBaseline9StartDate]\r\n      ,[TaskBaseline9FinishDate]\r\n\t  ,[TaskBaseline10StartDate]\r\n      ,[TaskBaseline10FinishDate]\r\n,[TaskPercentCompleted]\r\n,[TaskWBS]\r\n,[TaskWork]\r\n,[TaskActualWork]\r\n,[" + configSetting + "]\r\n,[" + configSetting2 + "]\r\n,[" + configSetting3 + "]\r\n,[" + configSetting4 + "]\r\n,[" + configCapture + "]\r\n,[" + configSettingPercentTeh + "]\r\n  FROM [pjrep].[MSP_EpmTask_UserView]\r\n  WHERE [ProjectUID] " + strProj);
                    Logger.Log("q:" + text);
                    SqlCommand sqlCommand = new SqlCommand(text, sqlConnection)
                    {
                        CommandTimeout = 500
                    };
                    SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
                    while (sqlDataReader.Read())
                    {
                        try
                        {
                            Guid key = Guid.Parse(sqlDataReader["ProjectUID"].ToString());
                            Guid taskUid = Guid.Parse(sqlDataReader["TaskUID"].ToString());
                            bool flag = Convert.ToBoolean(sqlDataReader["TaskIsSummary"].ToString());
                            if (!flag && !assns[key].Any(a => a.TaskUid == taskUid))
                            {
                                continue;
                            }
                            if (!dictionary.ContainsKey(key))
                            {
                                dictionary.Add(key, new List<TaskDb>());
                            }
                            TaskDb taskDb = new TaskDb
                            {
                                TaskUid = taskUid,
                                BaselineStart = new List<DateTime>(),
                                BaselineFinish = new List<DateTime>(),
                                IsSummary = flag
                            };
                            if (sqlDataReader["TaskName"] != DBNull.Value)
                            {
                                taskDb.Name = sqlDataReader["TaskName"].ToString();
                            }
                            if (sqlDataReader["TaskWBS"] != DBNull.Value)
                            {
                                taskDb.WorkBreakdownStructure = sqlDataReader["TaskWBS"].ToString();
                            }
                            if (sqlDataReader["TaskIndex"] != DBNull.Value)
                            {
                                taskDb.TaskIndex = Convert.ToInt32(sqlDataReader["TaskIndex"].ToString());
                            }
                            if (sqlDataReader["TaskParentUID"] != DBNull.Value)
                            {
                                taskDb.Parent = Guid.Parse(sqlDataReader["TaskParentUID"].ToString());
                            }
                            if (sqlDataReader["TaskOutlineLevel"] != DBNull.Value)
                            {
                                taskDb.OutlineLevel = Convert.ToInt32(sqlDataReader["TaskOutlineLevel"].ToString());
                            }
                            if (sqlDataReader["TaskStartDate"] != DBNull.Value)
                            {
                                taskDb.Start = Convert.ToDateTime(sqlDataReader["TaskStartDate"].ToString());
                            }
                            if (sqlDataReader["TaskFinishDate"] != DBNull.Value)
                            {
                                taskDb.Finish = Convert.ToDateTime(sqlDataReader["TaskFinishDate"].ToString());
                            }
                            for (int num = 0; num < 11; num++)
                            {
                                if (sqlDataReader[$"TaskBaseline{num}StartDate"] != DBNull.Value)
                                {
                                    taskDb.BaselineStart.Add(Convert.ToDateTime(sqlDataReader[$"TaskBaseline{num}StartDate"].ToString()));
                                }
                                else
                                {
                                    taskDb.BaselineStart.Add(DateTime.MinValue);
                                }
                                if (sqlDataReader[$"TaskBaseline{num}FinishDate"] != DBNull.Value)
                                {
                                    taskDb.BaselineFinish.Add(Convert.ToDateTime(sqlDataReader[$"TaskBaseline{num}FinishDate"].ToString()));
                                }
                                else
                                {
                                    taskDb.BaselineFinish.Add(DateTime.MinValue);
                                }
                            }
                            if (sqlDataReader[configSetting] != DBNull.Value)
                            {
                                taskDb.KindWork = sqlDataReader[configSetting].ToString();
                            }
                            if (sqlDataReader[configSetting2] != DBNull.Value)
                            {
                                taskDb.Stage = sqlDataReader[configSetting2].ToString();
                            }
                            if (sqlDataReader[configSetting3] != DBNull.Value)
                            {
                                taskDb.Blok = sqlDataReader[configSetting3].ToString();
                            }
                            if (sqlDataReader[configSetting4] != DBNull.Value)
                            {
                                taskDb.Blok2 = sqlDataReader[configSetting4].ToString();
                            }
                            if (sqlDataReader[configCapture] != DBNull.Value)
                            {
                                taskDb.Capture = sqlDataReader[configCapture].ToString(); 
                            }
                            if (sqlDataReader[configSettingPercentTeh] != DBNull.Value)
                            {
                                taskDb.PercentCompleteCustom = Convert.ToInt32(sqlDataReader[configSettingPercentTeh]); 
                            }
                            if (sqlDataReader["TaskPercentCompleted"] != DBNull.Value)
                            {
                                taskDb.PercentComplete = Convert.ToInt16(sqlDataReader["TaskPercentCompleted"]);
                            }
                            if (sqlDataReader["TaskWork"] != DBNull.Value)
                            {
                                taskDb.Work = Convert.ToDouble(sqlDataReader["TaskWork"]);
                            }
                            dictionary[key].Add(taskDb);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(ex, "GetAllAssnForUserDbTasks.ItemDB");
                        }
                    }
                    Logger.Log("end");
                }
                catch (Exception ex2)
                {
                    Logger.Log(ex2);
                }
            }
            TimeSpan end = DateTime.Now - start;
            Logger.Log($"End count:{dictionary.Count}, in time: {end.TotalSeconds} сек");
            return dictionary;
        }

        public static Dictionary<Guid, Guid> GetResourceForAssn(List<Guid> assnUids)
        {
            Dictionary<Guid, Guid> dictionary = new Dictionary<Guid, Guid>();
            using (SqlConnection sqlConnection = new SqlConnection(csProject))
            {
                try
                {
                    sqlConnection.Open();
                    string cmdText = string.Format("\r\nSELECT A.[ASSN_UID], A.[RES_UID_OWNER]\r\nFROM [pjpub].[MSP_ASSIGNMENTS] AS A\r\nWHERE  A.[ASSN_UID] in (" + assnUids.Aggregate("", delegate (string w, Guid n)
                    {
                        Guid guid = n;
                        return w + ",'" + guid.ToString() + "'";
                    }).Remove(0, 1) + ")");
                    SqlCommand sqlCommand = new SqlCommand(cmdText, sqlConnection)
                    {
                        CommandTimeout = 500
                    };
                    SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
                    while (sqlDataReader.Read())
                    {
                        try
                        {
                            dictionary.Add(Guid.Parse(sqlDataReader["ASSN_UID"].ToString()), Guid.Parse(sqlDataReader["RES_UID_OWNER"].ToString()));
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(ex, "GetAllAssnForUserDbAssn.ItemDB");
                        }
                    }
                }
                catch (Exception ex2)
                {
                    Logger.Log(ex2);
                }
            }
            return dictionary;
        }

        public static Dictionary<Guid, Guid> GetAssnPredecessors(List<Guid> assnUids)
        {
            Dictionary<Guid, Guid> dictionary = new Dictionary<Guid, Guid>();
            using (SqlConnection sqlConnection = new SqlConnection(csProject))
            {
                try
                {
                    sqlConnection.Open();
                    string cmdText = string.Format("\r\nSELECT A.[ASSN_UID], A.[RES_UID_OWNER]\r\nFROM [pjpub].[MSP_ASSIGNMENTS] AS A\r\nWHERE  A.[ASSN_UID] in (" + assnUids.Aggregate("", delegate (string w, Guid n)
                    {
                        Guid guid = n;
                        return w + ",'" + guid.ToString() + "'";
                    }).Remove(0, 1) + ")");
                    SqlCommand sqlCommand = new SqlCommand(cmdText, sqlConnection)
                    {
                        CommandTimeout = 500
                    };
                    SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
                    while (sqlDataReader.Read())
                    {
                        try
                        {
                            dictionary.Add(Guid.Parse(sqlDataReader["ASSN_UID"].ToString()), Guid.Parse(sqlDataReader["RES_UID_OWNER"].ToString()));
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(ex, "GetAllAssnForUserDbAssn.ItemDB");
                        }
                    }
                }
                catch (Exception ex2)
                {
                    Logger.Log(ex2);
                }
            }
            return dictionary;
        }

        public static Dictionary<string, string> GetLookupValuesKindWork(List<string> kindWorks)
        {
            Logger.Log("GetLookupValuesKindWork kindWorks:" + JsonConvert.SerializeObject(kindWorks));
            Dictionary<string, string> result = new Dictionary<string, string>();
            Dictionary<string, string> allResult = new Dictionary<string, string>();
            if (!kindWorks.Any())
            {
                return result;
            }
            using (SqlConnection sqlConnection = new SqlConnection(csProject))
            {
                try
                {
                    string configSetting = ConfigManager.GetConfigSetting("LookupValuesKindWork");
                    sqlConnection.Open();
                    string cmdText = string.Format("\r\nSELECT [MemberFullValue], [MemberDescription]\r\nFROM [pjrep].[" + configSetting + "] WHERE MemberFullValue is not null");
                    SqlCommand sqlCommand = new SqlCommand(cmdText, sqlConnection)
                    {
                        CommandTimeout = 5000
                    };
                    SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
                    while (sqlDataReader.Read())
                    {
                        try
                        {
                            allResult.Add(sqlDataReader["MemberFullValue"].ToString(), (sqlDataReader["MemberDescription"] != DBNull.Value) ? sqlDataReader["MemberDescription"].ToString() : "");
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(ex, "GetLookupValuesKindWork.ItemDB");
                        }
                    }
                }
                catch (Exception ex2)
                {
                    Logger.Log(ex2);
                }
            }
            if (!allResult.Any())
            {
                return result;
            }
            kindWorks.ForEach(delegate (string f)
            {
                if (allResult.ContainsKey(f))
                {
                    result.Add(f, allResult[f]);
                }
            });
            Logger.Log($"GetLookupValuesKindWork kindWorks:{JsonConvert.SerializeObject(kindWorks)} result.Count:{result.Count}");
            return result;
        }

        public static Dictionary<Guid, List<AssnDb>> GetAllAssnForUserDbAssn(string loginName, Guid projUid)
        {
            Dictionary<Guid, List<AssnDb>> dictionary = new Dictionary<Guid, List<AssnDb>>();
            using (SqlConnection sqlConnection = new SqlConnection(csProject))
            {
                try
                {
                    Logger.Log($"loginName:{loginName} projUid:{projUid} 1");
                    if (projUid != Guid.Empty)
                    {
                        dictionary.Add(projUid, new List<AssnDb>());
                    }
                    sqlConnection.Open();
                    string text = $"\r\nDECLARE @ResUID uniqueidentifier;\r\nDECLARE @ResMATERIAL nvarchar;\r\nSET @ResUID = (SELECT [RES_UID] FROM [pjpub].[MSP_RESOURCES] WHERE [WRES_ACCOUNT] like @User);\r\nSET @ResMATERIAL = (SELECT [RES_MATERIAL_LABEL] FROM [pjpub].[MSP_RESOURCES] WHERE [RES_UID] = @ResUID);\r\n\r\n\r\nSELECT DISTINCT(A.[ASSN_UID]), A.[PROJ_UID], A.[TASK_UID], [ASSN_PCT_WORK_COMPLETE], A.[RES_UID], A.[ASSN_WORK]/60000 as work, A.[ASSN_ACT_WORK]/60000 as act_work, A.[RES_UID_OWNER],R.RES_MATERIAL_LABEL\r\nFROM [pjpub].[MSP_ASSIGNMENTS] AS A\r\n  INNER JOIN [pjpub].[MSP_RESOURCES] AS R ON R.[RES_UID] = A.[RES_UID] \r\nWHERE  A.[RES_UID_OWNER] = @ResUID AND A.[TASK_IS_SUMMARY] = 0";
                    if (projUid != Guid.Empty)
                    {
                        text += " AND [PROJ_UID] = @ProjUid";
                    }
                    Logger.Log($"loginName:{loginName} projUid:{projUid} 2 q:{text}");
                    SqlCommand sqlCommand = new SqlCommand(text, sqlConnection)
                    {
                        CommandTimeout = 500
                    };
                    sqlCommand.Parameters.AddWithValue("@User", "%" + loginName);
                    if (projUid != Guid.Empty)
                    {
                        sqlCommand.Parameters.AddWithValue("@ProjUid", projUid.ToString());
                    }
                    SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
                    while (sqlDataReader.Read())
                    {
                        try
                        {
                            Guid guid = Guid.Parse(sqlDataReader["RES_UID"].ToString());
                            Guid guid2 = Guid.Parse(sqlDataReader["RES_UID_OWNER"].ToString());
                            AssnDb assnDb = new AssnDb
                            {
                                AssnUid = Guid.Parse(sqlDataReader["ASSN_UID"].ToString()),
                                TaskUid = Guid.Parse(sqlDataReader["TASK_UID"].ToString()),
                                ResUid = guid2
                            };
                            if (guid == guid2)
                            {
                                assnDb.Measure = "ч";
                            }
                            else if (sqlDataReader["RES_MATERIAL_LABEL"] != DBNull.Value)
                            {
                                assnDb.Measure = sqlDataReader["RES_MATERIAL_LABEL"].ToString();
                            }
                            if (sqlDataReader["work"] != DBNull.Value)
                            {
                                double.TryParse(sqlDataReader["work"].ToString(), out assnDb.VolumePlan);
                            }
                            if (sqlDataReader["act_work"] != DBNull.Value)
                            {
                                double.TryParse(sqlDataReader["act_work"].ToString(), out assnDb.ActualWork);
                            }
                            if (sqlDataReader["ASSN_PCT_WORK_COMPLETE"] != DBNull.Value)
                            {
                                assnDb.PercentCompleteWork = Convert.ToInt16(sqlDataReader["ASSN_PCT_WORK_COMPLETE"]);
                            }
                            if (projUid != Guid.Empty)
                            {
                                dictionary[projUid].Add(assnDb);
                                continue;
                            }
                            Guid key = Guid.Parse(sqlDataReader["PROJ_UID"].ToString());
                            if (!dictionary.ContainsKey(key))
                            {
                                dictionary.Add(key, new List<AssnDb>());
                            }
                            dictionary[key].Add(assnDb);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(ex, "GetAllAssnForUserDbAssn.ItemDB");
                        }
                    }
                    Logger.Log($"loginName:{loginName} projUid:{projUid} 3");
                }
                catch (Exception ex2)
                {
                    Logger.Log(ex2);
                }
            }
            return dictionary;
        }

        public static List<string> GetViewForUser(string loginName)
        {
            List<string> result = new List<string>();
            using (VolumeContext volumeContext = new VolumeContext())
            {
                result = (from u in volumeContext.UserSettingss
                          where u.Spreading == "*" || u.Spreading.Contains(loginName)
                          select u.Name).ToList();
            }
            return result;
        }

        public static List<int> GetBasePlans(Guid projUid)
        {
            using (VolumeContext volumeContext = new VolumeContext())
            {
                try
                {

                    UserSettingsBasicPlansProject userSettingsBasicPlansProject = volumeContext.UserSettingsBasicPlansProjects.FirstOrDefault(u => u.ProjectUid == projUid);
                    if (userSettingsBasicPlansProject != null && !string.IsNullOrEmpty(userSettingsBasicPlansProject.BasicPlans))
                    {
                        return userSettingsBasicPlansProject.BasicPlans?.Split(';')?.Select(int.Parse)?.ToList();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
            }
            return new List<int>
            {
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10
            };
        }

        public static List<BasePlanProject> GetBasePlansProjects(string loginName)
        {
            List<BasePlanProject> list = new List<BasePlanProject>();
            try
            {
                Dictionary<Guid, List<int>> usbpp = new Dictionary<Guid, List<int>>();
                using (VolumeContext volumeContext = new VolumeContext())
                {
                    volumeContext.UserSettingsBasicPlansProjects.ForEach(delegate (UserSettingsBasicPlansProject f)
                    {
                        if (!string.IsNullOrEmpty(f.BasicPlans))
                        {
                            usbpp.Add(f.ProjectUid, f.BasicPlans?.Split(';')?.Select(int.Parse)?.ToList());
                        }
                    });
                }
                using (SqlConnection sqlConnection = new SqlConnection(csProject))
                {
                    try
                    {
                        sqlConnection.Open();
                        string cmdText = $"\r\nDECLARE @ResUID uniqueidentifier;\r\nSET @ResUID = (SELECT [RES_UID] FROM [pjpub].[MSP_RESOURCES] WHERE [WRES_ACCOUNT] like @User);\r\nSELECT [PROJ_UID],[PROJ_NAME],[WRES_UID]\r\nFROM [pjpub].[MSP_PROJECTS]\r\nWHERE WRES_UID = @ResUID";
                        SqlCommand sqlCommand = new SqlCommand(cmdText, sqlConnection)
                        {
                            CommandTimeout = 500
                        };
                        sqlCommand.Parameters.AddWithValue("@User", "%" + loginName);
                        SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
                        while (sqlDataReader.Read())
                        {
                            Guid guid = Guid.Parse(sqlDataReader["PROJ_UID"].ToString());
                            string projName = sqlDataReader["PROJ_NAME"].ToString();
                            List<int> basePlan = (usbpp.ContainsKey(guid) ? usbpp[guid] : new List<int>());
                            list.Add(new BasePlanProject
                            {
                                ProjUid = guid,
                                ProjName = projName,
                                BasePlan = basePlan
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                    }
                }
            }
            catch (Exception ex2)
            {
                Logger.Log(ex2);
            }
            return list;
        }

        public static void SavingBaselinePlansProjects(List<BasePlanProject> basePlanProjects)
        {
            using (VolumeContext ctx = new VolumeContext())
            {
                try
                {

                    basePlanProjects.ForEach(delegate (BasePlanProject b)
                    {
                        string basicPlans = string.Empty;
                        if (b.BasePlan.Any())
                        {
                            basicPlans = string.Join(";", b.BasePlan);
                        }
                        if (ctx.UserSettingsBasicPlansProjects.Any(a => a.ProjectUid == b.ProjUid))
                        {
                            ctx.UserSettingsBasicPlansProjects.FirstOrDefault(a => a.ProjectUid == b.ProjUid).BasicPlans = basicPlans;
                        }
                        else
                        {
                            ctx.UserSettingsBasicPlansProjects.Add(new UserSettingsBasicPlansProject
                            {
                                ProjectUid = b.ProjUid,
                                BasicPlans = basicPlans
                            });
                        }
                    });
                    ctx.SaveChanges();
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
            }
        }

        public static UserSettings GetViewForUser(string loginName, string viewName)
        {
            using (VolumeContext volumeContext = new VolumeContext())
            {
                try
                {
                    if (!volumeContext.UserSettingss.Any())
                    {
                        return null;
                    }
                    return volumeContext.UserSettingss.FirstOrDefault(u => (u.Spreading == "*" || u.Spreading.Contains(loginName)) && u.Name == viewName);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
            }
            return null;
        }

        public static string GetHeightForUser(string loginName)
        {
            using (VolumeContext volumeContext = new VolumeContext())
            {
                try
                {
                    if (!volumeContext.UserScales.Any(a => a.Login == loginName))
                    {
                        return null;
                    }
                    return volumeContext.UserScales.FirstOrDefault(a => a.Login == loginName).Height;
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
            }
            return null;
        }

        public static void SetHeightForUser(string loginName, string height)
        {
            using (VolumeContext volumeContext = new VolumeContext())
            {
                try
                {
                    if (!volumeContext.UserScales.Any(a => a.Login == loginName))
                    {
                        volumeContext.UserScales.Add(new UserScale
                        {
                            Uid = Guid.NewGuid(),
                            Height = height,
                            Login = loginName,
                            TimeStamp = DateTime.Now
                        });
                    }
                    else
                    {
                        UserScale userScale = volumeContext.UserScales.FirstOrDefault(a => a.Login == loginName);
                        userScale.TimeStamp = DateTime.Now;
                        userScale.Height = height;
                    }
                    SaveDbExceptionLog(volumeContext);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
            }
        }

        public static List<FieldInfo> GetFieldsForUser(string loginName, string viewName)
        {
            using (VolumeContext volumeContext = new VolumeContext())
            {
                Logger.Log("loginName:" + loginName + " viewName:" + viewName + " 1");
                if (!volumeContext.UserSettingss.Any())
                {
                    Logger.Log("Создаем представление по умолчанию");
                    List<FieldInfo> list = new List<FieldInfo>
                    {
                        new FieldInfo
                        {
                            Name = "Id",
                            Width = 40,
                            IsVisible = true
                        },
                        new FieldInfo
                        {
                            Name = "Title",
                            Width = 515,
                            IsVisible = true
                        },
                        new FieldInfo
                        {
                            Name = "Blok",
                            Width = 100,
                            IsVisible = true
                        },
                        new FieldInfo
                        {
                            Name = "Blok2",
                            Width = 100,
                            IsVisible = true
                        },
                        new FieldInfo
                        {
                            Name = "Capture",
                            Width = 100,
                            IsVisible = true
                        },
                        new FieldInfo
                        {
                            Name = "KindWork",
                            Width = 100,
                            IsVisible = true
                        },
                        new FieldInfo
                        {
                            Name = "PercentCompleteWork",
                            Width = 127,
                            IsVisible = true
                        },
                        new FieldInfo
                        {
                            Name = "Start",
                            Width = 100,
                            IsVisible = true
                        },
                        new FieldInfo
                        {
                            Name = "Finish",
                            Width = 100,
                            IsVisible = true
                        },
                        new FieldInfo
                        {
                            Name = "VolumePlan",
                            Width = 100,
                            IsVisible = true
                        },
                        new FieldInfo
                        {
                            Name = "VolumeFact",
                            Width = 100,
                            IsVisible = true
                        },
                        new FieldInfo
                        {
                            Name = "VolumeLeft",
                            Width = 100,
                            IsVisible = true
                        },
                        new FieldInfo
                        {
                            Name = "Measure",
                            Width = 50,
                            IsVisible = true
                        },
                        new FieldInfo
                        {
                            Name = "Status",
                            Width = 100,
                            IsVisible = true
                        },
                        new FieldInfo
                        {
                            Name = "Comments",
                            Width = 100,
                            IsVisible = true
                        },
                        new FieldInfo
                        {
                            Name = "Stage",
                            Width = 100,
                            IsVisible = true
                        }
                    };
                    UserSettings entity = new UserSettings
                    {
                        Spreading = "*",
                        Uid = Guid.NewGuid(),
                        Fields = JsonConvert.SerializeObject(list),
                        TimeStamp = DateTime.Now,
                        Name = "Все задачи"
                    };
                    volumeContext.UserSettingss.Add(entity);
                    Logger.Log("loginName:" + loginName + " viewName:" + viewName + " 2");
                    volumeContext.SaveChanges();
                }
                Logger.Log("loginName:" + loginName + " viewName:" + viewName + " 3");
                UserSettings userSettings = volumeContext.UserSettingss.FirstOrDefault(u => (u.Spreading == "*" || u.Spreading.Contains(loginName)) && u.Name == viewName);
                List<FieldInfo> result = JsonConvert.DeserializeObject<List<FieldInfo>>(userSettings.Fields);
                if (!result.Where(x => x.Name == "Blok2").Any())
                {
                    result.Add(new FieldInfo() { Name = "Blok2", IsVisible = false, Width = 100 });
                }
                if (!result.Where(x => x.Name == "Capture").Any())
                {
                    result.Add(new FieldInfo() { Name = "Capture", IsVisible = false, Width = 100 });
                }
                return result;
            }
        }

        
        public static void SaveSubmittedApproval(SubmittedApprovalGridInfo data, SPUser user)
        {
            var res = JsonConvert.SerializeObject(data);
            Logger.Log(res);
            StringBuilder log = new StringBuilder();
            var web = SPContext.Current.Web;

            try
            {
                using (VolumeContext ctx = new VolumeContext())
                {
                    try
                    {
                        data.Assns.ForEach(delegate (SubmittedApprovalGridInfo.Assn assn)
                        {
                            Logger.Log("Assn: " + JsonConvert.SerializeObject(assn));

                            bool flag = false;
                            VolumeData volumeData = ctx.VolumeDatas.FirstOrDefault(r => r.AssnUid == assn.AssnUid && r.TaskUid == assn.TaskUid && r.ProjectUid == data.ProjectGuid);
                            if (volumeData == null)
                            {
                                flag = true;
                                volumeData = new VolumeData
                                {
                                    AssnUid = assn.AssnUid,
                                    TaskUid = assn.TaskUid,
                                    ProjectUid = data.ProjectGuid
                                };
                                if (assn.Task.Start != DateTime.MinValue)
                                {
                                    volumeData.StartDate = assn.Task.Start;
                                }
                                if (assn.Task.Finish != DateTime.MinValue)
                                {
                                    volumeData.FinishDate = assn.Task.Finish;
                                }
                            }
                            if (assn.PercentCompleteWork.HasValue)
                            {
                                volumeData.PercentCompleted = assn.PercentCompleteWork.Value;
                            }
                            if (assn.StartDate != DateTime.MinValue)
                            {
                                volumeData.StartDate = assn.StartDate;
                            }
                            else
                            {
                                if(assn.Task?.Start != DateTime.MinValue)
                                {
                                    volumeData.StartDate = assn.Task.Start;
                                }
                            }
                            if (assn.FinishDate != DateTime.MinValue)
                            {
                                volumeData.FinishDate = assn.FinishDate;
                            }
                            else
                            {
                                if (assn.Task?.Finish != DateTime.MinValue)
                                {
                                    volumeData.FinishDate = assn.Task.Finish;
                                }
                            }
                            volumeData.Status = 2;
                            volumeData.Comments = "";
                            volumeData.TimeStamp = DateTime.Now;
                            volumeData.UserLogin = user.LoginName;
                            volumeData.UserName = user.Name;
                            volumeData.SenderEmail = user.Email;
                            Logger.Log(JsonConvert.SerializeObject(volumeData));
                            if (flag)
                            {
                                ctx.VolumeDatas.Add(volumeData);
                            }
                            foreach (VolumeInDay item in assn.VolumeInDay)
                            {
                                VolumeInDay volumeInDay = ctx.VolumeInDays.FirstOrDefault(v => v.AssnUid == assn.AssnUid && v.Date == item.Date);
                                if (volumeInDay == null)
                                {
                                    volumeInDay = item;
                                    volumeInDay.Uid = Guid.NewGuid();
                                    volumeInDay.AssnUid = assn.AssnUid;
                                    ctx.VolumeInDays.Add(volumeInDay);
                                }
                                volumeInDay.Volume = item.Volume;
                                volumeInDay.Status = 2;
                                volumeInDay.TimeStamp = data.TimeStamp;
                            }
                        });
                        SaveDbExceptionLog(ctx);
                        bool nofailure = true;

                        StringBuilder sbTasks = new StringBuilder();

                        foreach (var assn in data.Assns)
                        {
                            var taskInfo = DbManager.GetTaskShortInfo(assn.TaskUid.ToString());
                            bool _percentChange = false;
                            bool _StartChange = false;
                            bool _FinishChange = false;

                            
                            DateTime dStart = DateTime.MinValue;
                            DateTime dFinish = DateTime.MinValue;
                            if (assn.StartDate != DateTime.MinValue)
                            {
                                dStart = assn.StartDate;
                            }
                            else
                            {
                                if (assn.Task?.Start != DateTime.MinValue)
                                {
                                    dStart = assn.Task.Start;
                                }
                            }
                            if (assn.FinishDate != DateTime.MinValue)
                            {
                                dFinish = assn.FinishDate;
                            }
                            else
                            {
                                if (assn.Task?.Finish != DateTime.MinValue)
                                {
                                    dFinish = assn.Task.Finish;
                                }
                            }

                            string sDate = dStart == DateTime.MinValue ? "" : dStart.ToString("dd.MM.yyyy");
                            string eDate = dFinish == DateTime.MinValue ? "" : dFinish.ToString("dd.MM.yyyy");

                            sbTasks.AppendLine($"<tr><td style=\"border-bottom: 1px solid #eeeeee;\">{assn.Task.Name}</td><td align=\"right\" style=\"border-bottom: 1px solid #eeeeee;\">{assn.PercentCompleteWork}</td><td align=\"right\" style=\"border-bottom: 1px solid #eeeeee;\">{sDate}</td><td align=\"right\" style=\"border-bottom: 1px solid #eeeeee;\">{eDate}</td></tr>");
                        }

                        List<EmailTemplate> emailList = SpEmailService.GetEmailTemplate("SENT", "USER");
                        var projInfo = DbManager.GetProjectInfo(data.ProjectGuid.ToString());
                        if (emailList.Count > 0)
                        {
                            var emailTo = "";
                            try
                            {
                                emailTo = user.Email;
                                if (!string.IsNullOrWhiteSpace(emailTo))
                                {

                                    var email = emailList.FirstOrDefault();
                                    if (email.IsActive)
                                    {
                                        TemplateManager tplm = new TemplateManager();
                                        tplm.AddKeyValue("PROJECT", projInfo.Proj_Name);
                                        tplm.AddKeyValue("TASKS", sbTasks.ToString());
                                        tplm.AddKeyValue("FIO", "<a href='mailto:" + user.Email + "'>" + user.Name + "</a>");
                                        
                                        email.Body = tplm.ReplaceParse(email.Body);
                                        email.Subject = tplm.ReplaceParse(email.Subject);
                                        EmailSendService.SendEmail(new MailAddress(email.FromEmail, email.FromName), new MailAddress(emailTo, user.Name), email.Subject, email.Body);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Log(ex, "Send email");
                                nofailure = false;
                            }
                        }

                        if (nofailure)
                        {
                            emailList = SpEmailService.GetEmailTemplate("SENT", "MANAGER");
                            if (emailList.Count > 0)
                            {
                                var emailTo = "";
                                try
                                {
                                    emailTo = projInfo.Wres_Email;
                                    if (!string.IsNullOrWhiteSpace(emailTo))
                                    {
                                        var email = emailList.FirstOrDefault();
                                        if (email.IsActive)
                                        {
                                            TemplateManager tplm = new TemplateManager();
                                            tplm.AddKeyValue("PROJECT", projInfo.Proj_Name);
                                            tplm.AddKeyValue("TASKS", sbTasks.ToString());
                                            tplm.AddKeyValue("FIO", "<a href='mailto:" + user.Email + "'>" + user.Name + "</a>");

                                            email.Body = tplm.ReplaceParse(email.Body);
                                            email.Subject = tplm.ReplaceParse(email.Subject);
                                            EmailSendService.SendEmail(new MailAddress(email.FromEmail, email.FromName), new MailAddress(emailTo, projInfo.Res_Name), email.Subject, email.Body);
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
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public static void SaveDbExceptionLog(VolumeContext ctx)
        {
            try
            {
                ctx.SaveChanges();
            }
            catch (DbEntityValidationException ex)
            {
                foreach (DbEntityValidationResult entityValidationError in ex.EntityValidationErrors)
                {
                    Logger.Log($"Entity of type {entityValidationError.Entry.Entity.GetType().Name} in state {entityValidationError.Entry.State} has the following validation errors:");
                    foreach (DbValidationError validationError in entityValidationError.ValidationErrors)
                    {
                        Logger.Log("- Property: " + validationError.PropertyName + ", Error: " + validationError.ErrorMessage);
                    }
                }
                Logger.Log("- Share: Error: " + ex.Message);
                throw;
            }
        }

        public static List<TaskDb> GetParentTasks(List<TaskDb> allTasks, TaskDb curTask)
        {
            List<TaskDb> list = new List<TaskDb>();
            if (!curTask.Parent.HasValue)
            {
                return list;
            }
            TaskDb taskDb = allTasks.FirstOrDefault(t => t.TaskUid == curTask.Parent.Value);
            if (taskDb == null)
            {
                return list;
            }
            list.Add(taskDb);
            if (taskDb.OutlineLevel != 0)
            {
                list.AddRange(GetParentTasks(allTasks, taskDb));
            }
            return list;
        }

        public static List<TaskDb> GetChildTasks(List<TaskDb> allTasks, TaskDb curTask)
        {
            List<TaskDb> result = new List<TaskDb>();
            IEnumerable<TaskDb> enumerable = allTasks.Where(t => t.Parent.Value == curTask.TaskUid);
            if (!enumerable.Any())
            {
                return result;
            }
            enumerable.ForEach(delegate (TaskDb t)
            {
                result.Add(t);
                List<TaskDb> childTasks = GetChildTasks(allTasks, t);
                if (childTasks.Any())
                {
                    result.AddRange(childTasks);
                }
            });
            return result;
        }

        public static void SaveView(CallbackArgs callbackArgs, SPUser user)
        {
            using (VolumeContext volumeContext = new VolumeContext())
            {
                UserSettings userSettings = volumeContext.UserSettingss.FirstOrDefault(a => a.Name == callbackArgs.ViewName && a.Spreading == user.LoginName);
                bool flag = false;
                if (userSettings == null)
                {
                    userSettings = new UserSettings
                    {
                        Uid = Guid.NewGuid(),
                        Spreading = user.LoginName,
                        Name = callbackArgs.ViewName
                    };
                    flag = true;
                }
                userSettings.Fields = JsonConvert.SerializeObject(callbackArgs.GridColumn);
                userSettings.TimeStamp = DateTime.Now;
                userSettings.FilterProjectUid = callbackArgs.ProjUid;
                if (callbackArgs.TaskUids != null && callbackArgs.TaskUids.Any())
                {
                    userSettings.FilterTaskUids = callbackArgs.TaskUids.Aggregate("", delegate (string w, Guid n)
                    {
                        Guid guid = n;
                        return w + ";" + guid.ToString();
                    }).Remove(0, 1);
                }
                userSettings.Layout = (int)callbackArgs.Layouts;
                if (callbackArgs.BasePlan.HasValue)
                {
                    userSettings.FilterBasePlan = callbackArgs.BasePlan.Value;
                }
                if (callbackArgs.KindWork.Any())
                {
                    userSettings.FilterTypeWork = string.Join(";", callbackArgs.KindWork);
                }
                if (callbackArgs.Stages.Any())
                {
                    userSettings.Stages = string.Join(";", callbackArgs.Stages);
                }
                if (callbackArgs.Bloks.Any())
                {
                    userSettings.Bloks = string.Join(";", callbackArgs.Bloks);
                }
                if (callbackArgs.Bloks2.Any())
                {
                    userSettings.Bloks2 = string.Join(";", callbackArgs.Bloks2);
                }
                if (flag)
                {
                    volumeContext.UserSettingss.Add(userSettings);
                }
                volumeContext.SaveChanges();
            }
        }

        internal static void Accept(Guid projUid, List<TaskDb> tasks, List<Guid> accept, string currentUserName, string currentUserLogin, string currentUserEmail)
        {
            if (DbManager.ProjectCheckedOut(projUid).IsCheckedOut)
            {

                return;
            }
            List<ApprovalInfo> listAssn = new List<ApprovalInfo>();
            List<ApprovalWorkInfo> listAssnWork = new List<ApprovalWorkInfo>();
            Dictionary<Guid, Guid> resourceUids = GetResourceForAssn(accept);
            Dictionary<Guid, int> percentTasks = GetPercentTasks(projUid);
            Logger.Log($"Accept старт сохранения accept={JsonConvert.SerializeObject(accept)} currentUserName={currentUserName} projUid:{projUid}");
            using (VolumeContext ctx = new VolumeContext())
            {
                try
                {
                    accept.ForEach(delegate (Guid r)
                    {
                        VolumeData volumeData = ctx.VolumeDatas.FirstOrDefault(a => a.AssnUid == r);
                        TaskDb taskDb = tasks.FirstOrDefault(t => t.AssnUid == r);
                        IQueryable<VolumeInDay> queryable = ctx.VolumeInDays.Where(v => v.AssnUid == r && v.Status == 2);
                        queryable.ForEach(delegate (VolumeInDay e)
                        {
                            e.Status = 4; 
                            e.TimeStamp = DateTime.Now;
                        });
                        VolumeData volumeData2 = ctx.VolumeDatas.FirstOrDefault(a => a.AssnUid == r);
                        volumeData2.Status = 4;
                        volumeData2.TimeStamp = DateTime.Now;
                        if (!string.IsNullOrEmpty(currentUserName))
                        {
                            volumeData2.Comments = currentUserName + ": " + DateTime.Now.ToShortDateString();
                        }
                        ctx.VolumeDatas.AddOrUpdate(volumeData2);
                        if (taskDb.Measure != "ч")
                        {
                            listAssn.Add(new ApprovalInfo
                            {
                                AssnUid = r,
                                ResUid = resourceUids[r],
                                VolumeInDays = queryable.ToList()
                            });
                        }
                        ApprovalWorkInfo approvalWorkInfo = new ApprovalWorkInfo
                        {
                            AssnUid = r,
                            TaskUid = taskDb.TaskUid,
                            ExceptionsWork = new List<DateTime>()
                        };
                        if (taskDb.Measure != "ч")
                        {
                            approvalWorkInfo.IsMaterial = true;
                        }
                        if (taskDb.Start != volumeData.StartDate)
                        {
                            approvalWorkInfo.Start = volumeData.StartDate;
                        }
                        if (taskDb.Finish != volumeData.FinishDate)
                        {
                            approvalWorkInfo.Finish = volumeData.FinishDate;
                            if (volumeData.StartDate != DateTime.MinValue)
                            {
                                approvalWorkInfo.Start = volumeData.StartDate;
                            }
                            Logger.Log($"Accept work.Finish {approvalWorkInfo.Finish} 1");
                        }
                        if (taskDb.PercentCompleteCustom != (int)volumeData.PercentCompleted)
                        {
                            approvalWorkInfo.PercentComplete = (int)volumeData.PercentCompleted;
                            if (taskDb.PercentCompleteCustom == 0)
                            {
                                approvalWorkInfo.Finish = volumeData.FinishDate;
                                Logger.Log($"Accept work.Finish {approvalWorkInfo.Finish} 2");
                            }
                        }
                        listAssnWork.Add(approvalWorkInfo);
                    });
                    SaveDbExceptionLog(ctx);
                    Logger.Log($"Accept listAssn={listAssn.Any()} listAssnWor={listAssnWork.Any()}");
                    if (listAssn.Any(a => a.VolumeInDays.Any()))
                    {
                        ProjectManager.ApproveMaterial(projUid, listAssn);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
            }

            
            Logger.Log($"Accept до отдельного потока _accept={JsonConvert.SerializeObject(accept)} currentUserName={currentUserName} {currentUserLogin} projUid:{projUid}");
            List<PercentTask> jsonPercentTasks = new List<PercentTask>();
            percentTasks.ForEach(delegate (KeyValuePair<Guid, int> f)
            {
                jsonPercentTasks.Add(new PercentTask
                {
                    Uid = f.Key,
                    Percent = f.Value
                });
            });
            List<ResourceUid> jsonResourceUids = new List<ResourceUid>();
            resourceUids.ForEach(delegate (KeyValuePair<Guid, Guid> f)
            {
                jsonResourceUids.Add(new ResourceUid
                {
                    AssnUid = f.Key,
                    RUid = f.Value
                });
            });
            DataThreadRePublish info = new DataThreadRePublish
            {
                ProjUid = projUid,
                CurrentUserName = currentUserName,
                CurrentUserLogin = currentUserLogin,
                CurrentUserEmail = currentUserEmail,
                Accept = accept,
                ListAssnWork = listAssnWork,
                PercentTasks = jsonPercentTasks,
                ResourceUids = jsonResourceUids
            };
            Logger.Log("CallExecuteAcceptDoWork info: " + JsonConvert.SerializeObject(info));
            var url = ProjSpaceSettings.Settings.SiteCollectionUrl;
            PsService.CallExecuteAcceptDoWork(url, info);
            Logger.Log($"Accept после отдельного потока _accept={JsonConvert.SerializeObject(accept)} currentUserName={currentUserName} projUid:{projUid}");
        }

        internal static void AcceptNew(Guid projUid, List<TaskDb> tasks, List<Guid> accept, string currentUserName, string currentUserLogin, string currentUserEmail)
        {
            Logger.Log("START");
            if (DbManager.ProjectCheckedOut(projUid).IsCheckedOut)
            {
                
                Logger.Log($"Проект {projUid} извлечен, утверждение не возможно");
                return;
            }

            List<ApprovalInfo> listAssn = new List<ApprovalInfo>();
            List<ApprovalWorkInfo> listAssnWork = new List<ApprovalWorkInfo>();
            Dictionary<Guid, Guid> resourceUids = GetResourceForAssn(accept);
            Dictionary<Guid, int> percentTasks = GetPercentTasks(projUid);
           
            using (VolumeContext ctx = new VolumeContext())
            {
                try
                {
                    Logger.Log("Переводим задачи в статус на публикации");
                    accept.ForEach(delegate (Guid r)
                    {
                        VolumeData volumeData = ctx.VolumeDatas.FirstOrDefault(a => a.AssnUid == r);
                        TaskDb taskDb = tasks.FirstOrDefault(t => t.AssnUid == r);
                        IQueryable<VolumeInDay> queryable = ctx.VolumeInDays.Where(v => v.AssnUid == r && v.Status == 2);
                        queryable.ForEach(delegate (VolumeInDay e)
                        {
                            e.Status = 4;
                            e.TimeStamp = DateTime.Now;
                        });
                        VolumeData volumeData2 = ctx.VolumeDatas.FirstOrDefault(a => a.AssnUid == r);
                        volumeData2.Status = 4;
                        volumeData2.TimeStamp = DateTime.Now;
                        if (!string.IsNullOrEmpty(currentUserName))
                        {
                            volumeData2.Comments = currentUserName + ": " + DateTime.Now.ToShortDateString();
                        }
                        ctx.VolumeDatas.AddOrUpdate(volumeData2);
                        if (taskDb.Measure != "ч")
                        {
                            listAssn.Add(new ApprovalInfo
                            {
                                AssnUid = r,
                                ResUid = resourceUids[r],
                                VolumeInDays = queryable.ToList()
                            });
                        }
                        ApprovalWorkInfo approvalWorkInfo = new ApprovalWorkInfo
                        {
                            AssnUid = r,
                            TaskUid = taskDb.TaskUid,
                            ExceptionsWork = new List<DateTime>()
                        };
                        if (taskDb.Measure != "ч")
                        {
                            approvalWorkInfo.IsMaterial = true;
                        }
                        if (taskDb.Start != volumeData.StartDate)
                        {
                            approvalWorkInfo.Start = volumeData.StartDate;
                        }
                        if (taskDb.Finish != volumeData.FinishDate)
                        {
                            approvalWorkInfo.Finish = volumeData.FinishDate;
                            if (volumeData.StartDate != DateTime.MinValue)
                            {
                                approvalWorkInfo.Start = volumeData.StartDate;
                            }
                            Logger.Log($"Accept work.Finish {approvalWorkInfo.Finish} 1");
                        }
                        if (taskDb.PercentCompleteCustom != (int)volumeData.PercentCompleted)
                        {
                            approvalWorkInfo.PercentComplete = (int)volumeData.PercentCompleted;
                            if (taskDb.PercentCompleteCustom == 0)
                            {
                                approvalWorkInfo.Finish = volumeData.FinishDate;
                                Logger.Log($"Accept work.Finish {approvalWorkInfo.Finish} 2");
                            }
                        }
                        listAssnWork.Add(approvalWorkInfo);
                    });
                    SaveDbExceptionLog(ctx);
                    Logger.Log($"Accept listAssn={listAssn.Any()} listAssnWor={listAssnWork.Any()}");
                   
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
            }


            Logger.Log($"Accept до вызова отдельного потока _accept={JsonConvert.SerializeObject(accept)} currentUserName={currentUserName} {currentUserLogin} projUid:{projUid}");
            List<PercentTask> jsonPercentTasks = new List<PercentTask>();
            percentTasks.ForEach(delegate (KeyValuePair<Guid, int> f)
            {
                jsonPercentTasks.Add(new PercentTask
                {
                    Uid = f.Key,
                    Percent = f.Value
                });
            });
            List<ResourceUid> jsonResourceUids = new List<ResourceUid>();
            resourceUids.ForEach(delegate (KeyValuePair<Guid, Guid> f)
            {
                jsonResourceUids.Add(new ResourceUid
                {
                    AssnUid = f.Key,
                    RUid = f.Value
                });
            });
            DataThreadRePublish info = new DataThreadRePublish
            {
                ProjUid = projUid,
                CurrentUserName = currentUserName,
                CurrentUserLogin = currentUserLogin,
                CurrentUserEmail = currentUserEmail,
                Accept = accept,
                ListAssnWork = listAssnWork,
                PercentTasks = jsonPercentTasks,
                ResourceUids = jsonResourceUids
            };
            Logger.Log("CallExecuteAcceptDoWork info: " + JsonConvert.SerializeObject(info));
            var url = ProjSpaceSettings.Settings.SiteCollectionUrl;
            PsService.CallExecuteAcceptDoWorkNew(url, info);
            Logger.Log($"Accept после вызова отдельного потока _accept={JsonConvert.SerializeObject(accept)} currentUserName={currentUserName} projUid:{projUid}");
        }

        
        [Obsolete("This method is gone. Use NewMethod.")]
        private static void DoWork(object data)
        {
            Logger.Log("Accept_v2 DoWork data=" + JsonConvert.SerializeObject((DataThreadRePublish)data));
            DataThreadRePublish d = data as DataThreadRePublish;
            if (d == null)
            {
                return;
            }
            Thread.Sleep(2000); 
            PauseAfterProjectChange(d.ProjUid, 3600);
            using (WindowsImpersonationContext windowsImpersonationContext = WindowsIdentity.Impersonate(IntPtr.Zero))
            {
                try
                {
                    List<AssnNewFinish> list = new List<AssnNewFinish>();
                    Dictionary<Guid, int> percentTasks = new Dictionary<Guid, int>();
                    if (!d.ListAssnWork.Any())
                    {
                        return;
                    }
                    d.PercentTasks.ForEach(delegate (PercentTask f)
                    {
                        percentTasks.Add(f.Uid, f.Percent);
                    });
                    Tuple<bool, List<AssnNewFinish>> tuple = ProjectManager.ApproveNonMaterial(d.ProjUid, d.ListAssnWork, percentTasks); 
                    list = tuple.Item2;
                    list.ForEach(delegate (AssnNewFinish f)
                    {
                        f.ResUid = d.ResourceUids.FirstOrDefault(df => df.AssnUid == f.AssnUid).RUid;
                    });
                    Logger.Log($"Accept_v2 DoWork Item1={tuple.Item1} Item2:{JsonConvert.SerializeObject(tuple.Item2)}");
                    if (tuple.Item1)
                    {
                        ProjectManager.RePublish(d.ProjUid, list, d.CurrentUserName, d.CurrentUserLogin, d.CurrentUserEmail, d.Accept);
                    }
                    else
                    {
                        ProjectManager.RePublish(d.ProjUid, list, d.CurrentUserName, d.CurrentUserLogin, d.CurrentUserEmail, new List<Guid>());
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex, "The Thread PauseAfterProjectChange");
                }
            }
        }

        
        private static void DoWorkNew(object data)
        {
            List<string> senderEmail = new List<string>();

            var start = DateTime.Now;
            Logger.Log("Accept_v4 DoWorkNEW data=" + JsonConvert.SerializeObject((DataThreadRePublish)data));
            DataThreadRePublish d = data as DataThreadRePublish;
            if (d == null)
            {
                return;
            }

            Logger.Log($"Accepted task count: {d.Accept.Count}, All count: {d.ListAssnWork.Count}");

           
            using (WindowsImpersonationContext windowsImpersonationContext = WindowsIdentity.Impersonate(IntPtr.Zero))
            {
                DraftProject dprj = null;
                try
                {
                   
                    using (ProjectContext ctx = new ProjectContext(ProjectManager.SiteUrl))
                    {
                        Logger.Log("Project context opened");
                        try
                        {
                            ctx.RequestTimeout = 1000 * 60 * 10; 

                            Logger.Log($"Get project by guid: {d.ProjUid}");
                            var prj = ctx.Projects.GetByGuid(d.ProjUid);
                            try
                            {
                                ctx.Load(prj,
                                    p => p.Id,
                                    p => p.Name,
                                    p => p.IsCheckedOut
                                    );
                                Logger.Log("Get project query");
                                ctx.ExecuteQuery();
                            }
                            catch (Exception ex)
                            {
                                Logger.Log(ex, "Get project error");
                                throw (ex);
                            }
                            Logger.Log($"Проект загружен: {prj.Name}");
                            
                            if (prj.IsCheckedOut)
                            {
                                Logger.Log($"Проект извлечен");
                            }
                            else
                            {
                                dprj = prj.CheckOut();
                                                              

                                var assnIds = new List<Guid>();
                                foreach (var a in d.ListAssnWork)
                                {
                                    assnIds.Add(a.TaskUid);
                                }

                                var settings = ProjSpaceSettings.Settings;
                                var percentTehName = settings.PercentageCustom;                               
                                var startCustom = settings.StartCustom;                           
                                var finishCustom = settings.FinishCustom; 

                                Logger.Log($"PercentageCustom: {percentTehName}");
                                Logger.Log($"StartCustom: {startCustom}");
                                Logger.Log($"FinishCustom: {finishCustom}");

                                var taskDict = new Dictionary<Guid, DraftTask>(); 

                                List<Guid> _updatedMaterialsGuids = new List<Guid>(); 

                               
                                Logger.Log("Start get all Tasks");
                                var taskGetStart = DateTime.Now;

                                foreach (var id in assnIds)
                                {
                                    try
                                    {
                                        DraftTask task = dprj.Tasks.GetByGuid(id);
                                        ctx.Load(task,
                                            t => t.Name,
                                            t => t.Id,
                                            t => t.IsManual,
                                            t => t.Start,
                                            t => t.Finish,
                                            t => t.PercentComplete,
                                            t => t.Assignments,
                                            t => t.Predecessors
                                            );
                                        ctx.ExecuteQuery();
                                        taskDict[id] = task;
                                    }
                                    catch(Exception ex)
                                    {
                                        Logger.Log(ex);
                                    }
                                }
                                TimeSpan taskGetEnd = DateTime.Now - taskGetStart;
                                Logger.Log($"Tasks loaded: {taskDict.Count} in {taskGetEnd.TotalMinutes} min");

                               

                                foreach (var id in assnIds)
                                {
                                    if (!taskDict.TryGetValue(id, out var task))
                                    {
                                        continue;
                                    }
                                    Logger.Log($"Обновление задачи: {task.Id} {task.Name}");
                                    var info = d.ListAssnWork.Where(x => x.TaskUid == task.Id).FirstOrDefault();
                                    
                                    bool startUpdated = false;
                                    bool finishUpdated = false;
                                    if (info.Start.HasValue)
                                    {
                                        task[startCustom] = info.Start.Value;
                                        Logger.Log("Start custom: " + info.Start.Value);
                                        if (task.Start != info.Start.Value)
                                        {                                            
                                            Logger.Log($"New Start date: {info.Start.Value.ToShortDateString()}, Old: {task.Start.ToShortDateString()}");
                                            task.Start = info.Start.Value;
                                            startUpdated = true;
                                        }
                                    }
                                    else
                                    {
                                        if(task.Start != DateTime.MinValue)
                                        {
                                            task[startCustom] = task.Start;
                                            Logger.Log("Start custom from task: " + task.Start);
                                        }
                                    }

                                    if (info.Finish.HasValue)
                                    {
                                        task[finishCustom] = info.Finish.Value;
                                        Logger.Log("Finish custom: " + info.Finish.Value);
                                        if (task.Finish != info.Finish.Value)
                                        {
                                            Logger.Log($"New Finish date: {info.Finish.Value.ToShortDateString()}, Old: {task.Finish.ToShortDateString()}");
                                            task.Finish = info.Finish.Value;
                                            finishUpdated = true;
                                        }
                                    }
                                    else
                                    {
                                        if(task.Finish != DateTime.MinValue)
                                        {
                                            task[finishCustom] = task.Finish;
                                            Logger.Log("Finish custom from task: " + task.Finish);
                                        }
                                    }

                                    if ((startUpdated || finishUpdated) && !task.IsManual)
                                    {
                                        task.IsManual = true; 
                                        Logger.Log($"Перевели задачу {task.Id} на ручное планирование");
                                    }

                                    if (info.PercentComplete.HasValue)
                                    {
                                        Logger.Log($"Update task {task.Id}, IsManual={task.IsManual}, New percent: {info.PercentComplete.Value}");
                                        task.PercentComplete = info.PercentComplete.Value;
                                        task[percentTehName] = info.PercentComplete.Value; 

                                       
                                        if (info.IsMaterial)
                                        {
                                            try
                                            {
                                                Logger.Log("IsMaterial");
                                                var taskAss = task.Assignments.FirstOrDefault();
                                                taskAss.PercentWorkComplete = info.PercentComplete.Value;
                                                taskAss[percentTehName] = info.PercentComplete.Value;
                                            }
                                            catch (Exception ex)
                                            {
                                                Logger.Log(ex);
                                            }
                                        }
                                    }
                                }

                                
                                Logger.Log("End update tasks");

                                Logger.Log("Update start");

                                var job = dprj.Update();
                                var state = ctx.WaitForQueue(job, 600);
                                Logger.Log($"Update {state}");

                                

                                Logger.Log("Publish start");
                                job = dprj.Publish(true);
                                state = ctx.WaitForQueue(job, 600);
                                Logger.Log($"Publish {state}");

                                

                                
                                ProjectManager.UpdateStatuses(d.ListAssnWork, 5);

                                TimeSpan end = DateTime.Now - start;
                                Logger.Log($"Finish in {end.TotalMinutes} мин, {end.TotalSeconds} сек");

                                
                                List<TaskShortInfo> approvedTasks = new List<TaskShortInfo>();
                                d.Accept.ForEach((r) =>
                                {
                                    var tmp = DbManager.GetTaskShortInfo(r.ToString());
                                    approvedTasks.Add(tmp);
                                });
                                StringBuilder sbTasks = new StringBuilder();
                                if (approvedTasks.Count > 0)
                                {
                                    foreach (var t in approvedTasks)
                                    {
                                        string apline = "<tr><td style=\"border-bottom: 1px solid #eeeeee;\">" + t.Name + "</td>" +
                                            "<td align =\"right\" style=\"border-bottom: 1px solid #eeeeee;\">" + t.PercentComplete + "</td></tr>";

                                        sbTasks.AppendLine(apline);
                                    }
                                }
                                else
                                {
                                    sbTasks.AppendLine("<tr><td></td></tr>");
                                }

                                Logger.Log("Отправляем письма утвердившему");
                                var projInfo = DbManager.GetProjectInfo(d.ProjUid.ToString());
                                List<EmailTemplate> emailList = SpEmailService.GetEmailTemplate("PUBLISHED", "MANAGER");
                                if (emailList.Count > 0)
                                {
                                    var emailTo = "";
                                    try
                                    {
                                        emailTo = d.CurrentUserEmail;
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

                                
                            }
                        }
                        catch(Exception ex)
                        {
                            Logger.Log(ex);

                            
                            Logger.Log("В процессе утверждения возникли ошибки, откатываем VolumeData на статус 'На утверждении'");
                            ProjectManager.UpdateStatuses(d.ListAssnWork, 2);
                            DbManager.UnDelegateUser();

                           

                            if (dprj == null)
                            {
                                Logger.Log("CRITICAL error, draft project is null");
                            }
                            else
                            {
                                Logger.Log("Update start");
                                var job = dprj.Update();
                                var state = ctx.WaitForQueue(job, 600);
                                Logger.Log($"Update {state}");

                                Logger.Log("Publish start");
                                job = dprj.Publish(true);
                                state = ctx.WaitForQueue(job, 600);
                                Logger.Log($"Publish {state}");
                            }
                        }
                    }               
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("ERROR in global process");
                    Logger.Log(ex);                    
                }
            }
        }

        [Obsolete("This method is gone. Use NewMethod.")]
        public static void AcceptDoWork(object data)
        {
            Thread thread = new Thread(DoWork);
            thread.Start(data);
        }

        public static void AcceptDoWorkNew(object data)
        {
            Thread thread = new Thread(DoWorkNew);
            thread.Start(data);
        }

        internal static void Reject(List<Guid> reject)
        {
            using (VolumeContext ctx = new VolumeContext())
            {
                List<string> emailsto = new List<string>();
                try
                {
                    string _guid = "";
                    reject.ForEach(delegate (Guid r)
                    {
                        IQueryable<VolumeInDay> items = ctx.VolumeInDays.Where(v => v.AssnUid == r && v.Status == 2);
                        items.ForEach(delegate (VolumeInDay e)
                        {
                            e.Status = 3;
                            e.TimeStamp = DateTime.Now;
                        });
                        VolumeData volumeData = ctx.VolumeDatas.FirstOrDefault(a => a.AssnUid == r);
                        _guid = volumeData.ProjectUid.ToString();
                        volumeData.Status = 3;
                        volumeData.TimeStamp = DateTime.Now;
                        if (!string.IsNullOrWhiteSpace(volumeData.SenderEmail))
                        {
                            if (!emailsto.Contains(volumeData.SenderEmail))
                            {
                                emailsto.Add(volumeData.SenderEmail);
                            }
                        }
                        Logger.Log("REJECTED task with GUID: " + r);
                    });
                    SaveDbExceptionLog(ctx);

                    List<TaskShortInfo> rejectedTasks = new List<TaskShortInfo>();
                    reject.ForEach((r) =>
                    {
                        var tmp = DbManager.GetTaskShortInfo(r.ToString());
                        rejectedTasks.Add(tmp);
                    });
                    StringBuilder sbTasks = new StringBuilder();
                    if (rejectedTasks.Count > 0)
                    {
                        foreach (var t in rejectedTasks)
                        {
                            sbTasks.AppendLine($"<tr><td style=\"border-bottom: 1px solid #eeeeee;\">{t.Name}</td><td align=\"right\" style=\"border-bottom: 1px solid #eeeeee;\">{t.PercentComplete}</td></tr>");
                        }
                    }
                    else
                    {
                        sbTasks.AppendLine("<tr><td></td></tr>");
                    }

                         
                    var projInfo = DbManager.GetProjectInfo(_guid);

                    var tplm = new TemplateManager();
                    tplm.AddKeyValue("PROJECT", projInfo.Proj_Name);
                    tplm.AddKeyValue("TASKS", sbTasks.ToString());
                    foreach (var userEmail in emailsto) 
                    {
                        var email = SpEmailService.GetEmailTemplate("REJECT", "USER");
                        var emF = email.FirstOrDefault();
                        emF.Body = tplm.ReplaceParse(emF.Body);
                        emF.Subject = tplm.ReplaceParse(emF.Subject);

                        EmailSendService.SendEmail(new MailAddress(emF.FromEmail, emF.FromName), new MailAddress(userEmail), emF.Subject, emF.Body);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
            }
        }

        public static void PauseAfterProjectChange(Guid projUid, int stepsSeconds)
        {
            try
            {
                for (int i = 0; i < stepsSeconds; i++)
                {
                    if (!IsProgressJobProject(projUid))
                    {
                        Logger.Log($"PauseAfterProjectChange projUid={projUid} i:{i}");
                        break;
                    }
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public static bool IsProgressJobProject(Guid projUid)
        {
            using (SqlConnection sqlConnection = new SqlConnection(csProject))
            {
                try
                {
                    sqlConnection.Open();
                    string cmdText = $"SELECT [JOB_INFO_UID] FROM [pjdraft].[MSP_QUEUE_PROJECT_GROUP]\r\n                Where [JOB_INFO_UID] = @projUid";
                    SqlCommand sqlCommand = new SqlCommand(cmdText, sqlConnection)
                    {
                        CommandTimeout = 500
                    };
                    sqlCommand.Parameters.AddWithValue("@projUid", projUid);
                    SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
                    if (sqlDataReader.Read())
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                    return true;
                }
            }
            return false;
        }

        public static bool DelegateUser(Guid resUid)
        {
            bool result = false;
            WindowsIdentity current = WindowsIdentity.GetCurrent();
            string name = current.Name;
            using (SqlConnection sqlConnection = new SqlConnection(csProject))
            {
                try
                {                    
                    sqlConnection.Open();

                    
                    string cmdText = $"SELECT * FROM [pjpub].[MSP_RESOURCES]\r\n  Where WRES_ACCOUNT like @iisLoginName and RES_ACTING_AS_UID IS NULL";
                    SqlCommand sqlCommand = new SqlCommand(cmdText, sqlConnection)
                    {
                        CommandTimeout = 500
                    };
                    sqlCommand.Parameters.AddWithValue("@iisLoginName", "%" + name);
                    int num = sqlCommand.ExecuteNonQuery();

                    if (num == 0)
                    {
                        
                        try
                        {
                            cmdText = "\r\nDECLARE @resDelUid uniqueidentifier;\r\nDECLARE @siteId uniqueidentifier;\r\nSET @siteId = (SELECT [SiteId] FROM [pjpub].[MSP_RESOURCES] WHERE WRES_ACCOUNT like @iisLoginName);\r\nSET @resDelUid = (SELECT [RES_UID] FROM [pjpub].[MSP_RESOURCES] WHERE WRES_ACCOUNT like @iisLoginName);\r\n            INSERT INTO [pjpub].[MSP_RESOURCE_DELEGATIONS]\r\n           ([SiteId]\r\n           ,[DELEGATION_UID]\r\n           ,[RES_UID]\r\n           ,[DELEGATE_UID]\r\n           ,[DELEGATION_START]\r\n           ,[DELEGATION_FINISH])\r\n     VALUES\r\n           (@siteId,\r\n            @uid,\r\n           @resUid,\r\n            @resDelUid,\r\n            @dayeStart,\r\n           @dayeFinish)";
                            SqlCommand sqlCommand1 = new SqlCommand(cmdText, sqlConnection);
                            sqlCommand = sqlCommand1;
                            sqlCommand.Parameters.AddWithValue("@uid", Guid.NewGuid());
                            sqlCommand.Parameters.AddWithValue("@dayeStart", DateTime.Now.AddMonths(-3));
                            sqlCommand.Parameters.AddWithValue("@dayeFinish", DateTime.Now.AddMonths(3));
                            sqlCommand.Parameters.AddWithValue("@resUid", resUid);
                            sqlCommand.Parameters.AddWithValue("@iisLoginName", "%" + name);
                            num = sqlCommand.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            Logger.Log("Запись создана");
                        }
                    }

                    
                    Logger.Log("Delegate to resUid: " + resUid);
                    cmdText = $"Update [pjpub].[MSP_RESOURCES] set RES_ACTING_AS_UID = @resUid\r\n  Where WRES_ACCOUNT like @iisLoginName";
                    sqlCommand = new SqlCommand(cmdText, sqlConnection);
                    sqlCommand.Parameters.AddWithValue("@resUid", resUid);
                    sqlCommand.Parameters.AddWithValue("@iisLoginName", "%" + name);
                    num = sqlCommand.ExecuteNonQuery();
                    
                    result = true;
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
            }
            return result;
        }

        public static bool UnDelegateUser()
        {
            bool result = false;
            WindowsIdentity current = WindowsIdentity.GetCurrent();
            string name = current.Name;
            using (SqlConnection sqlConnection = new SqlConnection(csProject))
            {
                try
                {
                    sqlConnection.Open();
                    string cmdText = $"\r\nUpdate [pjpub].[MSP_RESOURCES] set RES_ACTING_AS_UID = null\r\n  Where WRES_ACCOUNT like @iisLoginName\r\nDECLARE @resUid uniqueidentifier;\r\nSET @resUid = (SELECT [RES_UID] FROM [pjpub].[MSP_RESOURCES] WHERE WRES_ACCOUNT like @iisLoginName);\r\nDELETE  FROM [pjpub].[MSP_RESOURCE_DELEGATIONS] WHERE DELEGATE_UID = @resUid\r\n";
                    SqlCommand sqlCommand = new SqlCommand(cmdText, sqlConnection)
                    {
                        CommandTimeout = 500
                    };
                    sqlCommand.Parameters.AddWithValue("@iisLoginName", "%" + name);
                    
                    int num = sqlCommand.ExecuteNonQuery();
                    Logger.Log($"UnDelegateUser {name} {num}");
                    result = true;
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
            }
            return result;
        }

        

        public static Dictionary<Guid, int> GetPercentTasks(Guid projUid)
        {
            Dictionary<Guid, int> dictionary = new Dictionary<Guid, int>();
            using (SqlConnection sqlConnection = new SqlConnection(csProject))
            {
                try
                {
                    sqlConnection.Open();
                    string cmdText = $"\r\n                SELECT [TaskUID],[TaskPercentCompleted]\r\n                FROM [pjrep].[MSP_EpmTask_UserView]\r\n                WHERE [ProjectUID] = @projUid";
                    SqlCommand sqlCommand = new SqlCommand(cmdText, sqlConnection)
                    {
                        CommandTimeout = 500
                    };
                    sqlCommand.Parameters.AddWithValue("@projUid", projUid);
                    SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
                    while (sqlDataReader.Read())
                    {
                        try
                        {
                            Guid key = Guid.Parse(sqlDataReader["TaskUID"].ToString());
                            if (sqlDataReader["TaskPercentCompleted"] != DBNull.Value)
                            {
                                dictionary.Add(key, Convert.ToInt16(sqlDataReader["TaskPercentCompleted"]));
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(ex, "GetPercentTasks.Item");
                        }
                    }
                }
                catch (Exception ex2)
                {
                    Logger.Log(ex2);
                }
            }
            return dictionary;
        }
    }

}