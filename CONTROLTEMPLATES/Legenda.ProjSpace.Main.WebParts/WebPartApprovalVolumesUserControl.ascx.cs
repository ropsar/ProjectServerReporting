using Legenda.ProjSpace.Main.EmailService;
using Legenda.ProjSpace.Main.Extensions;
using Legenda.ProjSpace.Main.Logging;
using Legenda.ProjSpace.Main.Model;
using Legenda.ProjSpace.Main.Model.DB;
using Legenda.ProjSpace.Main.Model.Enums;
using Legenda.ProjSpace.Main.Services;
using Legenda.ProjSpace.Main.WebParts.WebPartApprovalVolumes;
using Microsoft.SharePoint;
using Microsoft.SharePoint.JSGrid;
using Microsoft.SharePoint.JsonUtilities;
using Microsoft.SharePoint.WebControls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Mail;
using System.Security.Principal;
using System.Web.Script.Serialization;
using System.Web.UI;

namespace Legenda.ProjSpace.Main.CONTROLTEMPLATES.Legenda.ProjSpace.Main.WebParts
{
    public partial class WebPartApprovalVolumesUserControl : UserControl, ICallbackEventHandler
    {
        private CallbackArgs _callbackArgs;

        private GridData _data;

        private SPUser _currentUser;

        private List<ProjectDb> _projects = null;

        private ProjectDb _currentProject = null;

        protected JSGrid JsGridControl;

        public WebPartApprovalVolumes WebPartApprovalControl { get; set; }

        public Properties.TypeForm TypeFormEnum => Properties.TypeForm.AgreeValue;

        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                if (!Page.IsPostBack)
                {
                    JsGridControl.JsInitObject = new
                    {
                        callbackScript = Page.ClientScript.GetCallbackEventReference(this, "args", "WGMA.DisplayProjectsData", "true", true)
                    };
                    string loginName = SPContext.Current.Web.CurrentUser.LoginName;
                    Page.ClientScript.RegisterHiddenField("gridVolumeViewType", TypeFormEnum.ToString());
                    string clientQueryString = Page.ClientQueryString;
                    WindowsImpersonationContext windowsImpersonationContext = null;
                    windowsImpersonationContext = WindowsIdentity.Impersonate(IntPtr.Zero);
                    try
                    {
                        if (!string.IsNullOrEmpty(clientQueryString))
                        {
                            string text = clientQueryString.Replace("height=", "");
                            JsGridControl.Height = GridData.GetHeight(text);
                            DbManager.SetHeightForUser(loginName, text);
                        }
                        else
                        {
                            string heightForUser = DbManager.GetHeightForUser(loginName);
                            if (!string.IsNullOrEmpty(heightForUser))
                            {
                                JsGridControl.Height = GridData.GetHeight(heightForUser);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                    }
                    finally
                    {
                        windowsImpersonationContext.Undo();
                    }
                }
            }
            catch (Exception ex2)
            {
                Logger.Log(ex2);
            }
            Logger.Log("Finish");
        }

        public IList<PivotedGridColumn> GetPivotedGridColumns(DataTable table, Dictionary<string, object> prop)
        {
            List<PivotedGridColumn> list = new List<PivotedGridColumn>();
            try
            {
                DateTime? periodStart = _callbackArgs.PeriodStart;
                DateTime? periodFinish = _callbackArgs.PeriodFinish;
                int countColumns = DateManager.GetCountColumns(periodStart.Value, periodFinish.Value, _callbackArgs.TypeView);
                List<string> list2 = new List<string>
                {
                    "DescPlan",
                    "DescFact"
                };
                PivotedGridColumn pivotedGridColumn = new PivotedGridColumn
                {
                    ColumnKey = "Desc",
                    FieldKeys = list2,
                    Name = "",
                    Width = 46
                };
                list.Add(pivotedGridColumn);
                list2 = new List<string>
                {
                    "SumPlan",
                    "SumFact"
                };
                pivotedGridColumn = new PivotedGridColumn
                {
                    ColumnKey = "Sum",
                    FieldKeys = list2,
                    Name = "Итого за период",
                    Width = 100
                };
                list.Add(pivotedGridColumn);
                DateTime date = periodStart.Value;
                for (int i = 0; i < countColumns; i++)
                {
                    string columnKey = $"Date{i}";
                    List<string> list3 = new List<string>
                    {
                        $"DatePlan{i}",
                        $"DateFact{i}"
                    };
                    pivotedGridColumn = new PivotedGridColumn
                    {
                        ColumnKey = columnKey,
                        FieldKeys = list3,
                        Width = 100,
                        Name = DateManager.getColumnName(date, _callbackArgs.TypeView)
                    };
                    list.Add(pivotedGridColumn);
                    date = DateManager.NextDate(date, _callbackArgs.TypeView);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return list;
        }

        public string GetCallbackResult()
        {
            DateTime now = DateTime.Now;
            _currentUser = SPContext.Current.Web.CurrentUser;
            Logger.Log("WebPartApprovalVolumesUserControl.GetCallbackResult start LoginName:" + _currentUser.LoginName + "  new 20.09");
            WindowsImpersonationContext windowsImpersonationContext = null;
            windowsImpersonationContext = WindowsIdentity.Impersonate(IntPtr.Zero);
            try
            {
                JsGridControl.Height = GridData.GetHeight(_callbackArgs.Height);
                JsGridControl.Width = GridData.GetWidth(_callbackArgs.Width);
                List<AnswerFlag> answerFlags = new List<AnswerFlag>();
                List<string> list = new List<string>();
                OutGrid outGrid = new OutGrid();
                if (!string.IsNullOrEmpty(_callbackArgs.PeriodStartStr) && !string.IsNullOrEmpty(_callbackArgs.PeriodFinishStr))
                {
                    List<int> list2 = _callbackArgs.PeriodStartStr.Split('.').Select(int.Parse).ToList();
                    _callbackArgs.PeriodStart = new DateTime(list2[0], list2[1], list2[2], 0, 0, 0, 0);
                    List<int> list3 = _callbackArgs.PeriodFinishStr.Split('.').Select(int.Parse).ToList();
                    _callbackArgs.PeriodFinish = new DateTime(list3[0], list3[1], list3[2], 0, 0, 0, 0);
                }
                if (_callbackArgs.Command == "SavingBaselinePlansProjects" && _callbackArgs.BasePlansProjects != null && _callbackArgs.BasePlansProjects.Any())
                {
                    DbManager.SavingBaselinePlansProjects(_callbackArgs.BasePlansProjects);
                }
                if (_projects == null)
                {
                    _projects = DbManager.GetAllProjectsForUserStatusManager(_currentUser.LoginName);
                }
                if (!_projects.Any())
                {
                    return null;
                }
                if (_currentProject != null)
                {
                    
                    Guid id = _currentProject.Id;
                    Guid? projUid = _callbackArgs.ProjUid;
                    if (!(id != projUid))
                    {
                        goto IL_0318;
                    }
                }
                _currentProject = new ProjectDb();
                if (!_callbackArgs.ProjUid.HasValue)
                {
                    _currentProject.Id = _projects.FirstOrDefault().Id;
                }
                else
                {
                    _currentProject.Id = _callbackArgs.ProjUid.Value;
                }
                if (_callbackArgs.Command != "SubmittedApproval")
                {
                    _currentProject.Tasks = DbManager.GetAllAssnForUserStatusManager(_currentUser.LoginName, _currentProject.Id);
                }
                goto IL_0318;
            IL_0318:
                DateManager.SetDate(_callbackArgs, _currentProject);
                
                if (_callbackArgs.Command == "Accept")
                {
                    Logger.Log($"ACCEPT: [{_currentUser.LoginName}] [{_currentUser.Email}] [{_currentUser.Name}]");
                    if (DbManager.ProjectCheckedOut(_currentProject.Id).IsCheckedOut)
                    {
                        var projInfo = DbManager.GetProjectInfo(_currentProject.Id.ToString());
                        Logger.Log($"Проект {projInfo.Proj_Name} извлечен для редактирования. Сохранение и публикация изменений не возможно.");
                        var emails = SpEmailService.GetEmailTemplate("PUBLISHEDERROR", "MANAGER");
                        if (emails.Count > 0)
                        {
                            var email = emails.FirstOrDefault();
                            var tplm = new TemplateManager();
                            tplm.AddKeyValue("PROJECT", projInfo.Proj_Name);
                            tplm.AddKeyValue("ERRORS", "Проект извлечен для редактирования. Сохранение и публикация изменений не возможно.");
                            email.Subject = tplm.ReplaceParse(email.Subject);
                            email.Body = tplm.ReplaceParse(email.Body);
                            EmailSendService.SendEmail(
                                new MailAddress(email.FromEmail, email.FromName),
                                new MailAddress(_currentUser.Email, _currentUser.Name),
                                email.Subject, email.Body);
                        }
                    }
                    else
                    {
                        DbManager.AcceptNew(_callbackArgs.ProjUid.Value, DbManager.GetAllAssnForUserDbTasksStatusManager(_currentUser.LoginName, _currentProject.Id), _callbackArgs.Accept, _currentUser.Name, _currentUser.LoginName, _currentUser.Email);
                    }
                }
                if (_callbackArgs.Command == "Reject")
                {
                    DbManager.Reject(_callbackArgs.Reject);
                }
                if (_callbackArgs.Command == "Accept" || _callbackArgs.Command == "Reject")
                {
                    _currentProject.Tasks = DbManager.GetAllAssnForUserStatusManager(_currentUser.LoginName, _currentProject.Id);
                }
                if (_callbackArgs.Command == "GetView")
                {
                    UserSettings viewForUser = DbManager.GetViewForUser(_currentUser.LoginName, _callbackArgs.ViewName);
                    if (viewForUser != null)
                    {
                        if (viewForUser.FilterProjectUid.HasValue && viewForUser.FilterProjectUid != Guid.Empty)
                        {
                            _currentProject.Id = viewForUser.FilterProjectUid.Value;
                        }
                        _currentProject.Tasks = DbManager.GetAllAssnForUserStatusManager(_currentUser.LoginName, _currentProject.Id);
                        _callbackArgs.KindWork = ((!string.IsNullOrEmpty(viewForUser.FilterTypeWork)) ? viewForUser.FilterTypeWork.Split(';').ToList() : new List<string>());
                        _callbackArgs.Stages = ((!string.IsNullOrEmpty(viewForUser.Stages)) ? viewForUser.Stages.Split(';').ToList() : new List<string>());
                        _callbackArgs.Bloks = ((!string.IsNullOrEmpty(viewForUser.Bloks)) ? viewForUser.Bloks.Split(';').ToList() : new List<string>());
                        _callbackArgs.TaskUids = ((!string.IsNullOrEmpty(viewForUser.FilterTaskUids)) ? (from e in viewForUser.FilterTaskUids.Split(';')
                                                                                                         select Guid.Parse(e)).ToList() : new List<Guid>());
                        _callbackArgs.BasePlan = viewForUser.FilterBasePlan;
                        _callbackArgs.Layouts = (PaneLayout)viewForUser.Layout;
                    }
                }
                if (_callbackArgs.ViewName == "")
                {
                    _callbackArgs.ViewName = "Все задачи";
                }
                List<FieldInfo> fieldsForUser = DbManager.GetFieldsForUser(_currentUser.LoginName, _callbackArgs.ViewName);
                _currentProject.Tasks.OrderBy(o => o.OutlineLevel).ForEach(delegate (TaskDb t)
                {
                    outGrid.SumTaskNames.Add(new TaskBaseInfo
                    {
                        Uid = t.TaskUid,
                        Name = t.Name,
                        IsSummary = !t.AssnUid.HasValue
                    });
                });
                List<string> list4 = (from t in _currentProject.Tasks
                                      where t.KindWork != null && !t.IsSummary
                                      select t.KindWork).Distinct().ToList();
                list4.Sort();
                outGrid.KindWork = DbManager.GetLookupValuesKindWork(list4);
                outGrid.Stages = (from t in _currentProject.Tasks
                                  where t.Stage != null && !t.IsSummary
                                  select t.Stage).Distinct().ToList();
                outGrid.Bloks = (from t in _currentProject.Tasks
                                 where t.Blok != null && !t.IsSummary
                                 select t.Blok).Distinct().ToList();
                outGrid.Stages.Sort();
                outGrid.Bloks.Sort();
                if (_callbackArgs.TaskUids != null && _callbackArgs.TaskUids.Any())
                {
                    List<TaskDb> newTasks = new List<TaskDb>();
                    _callbackArgs.TaskUids.ForEach(delegate (Guid tUid)
                    {
                        TaskDb taskDb = _currentProject.Tasks.FirstOrDefault(t => tUid == t.TaskUid);
                        newTasks.Add(taskDb);
                        List<TaskDb> parentTasks = DbManager.GetParentTasks(_currentProject.Tasks, taskDb);
                        List<TaskDb> childTasks = DbManager.GetChildTasks(_currentProject.Tasks, taskDb);
                        parentTasks.ForEach(delegate (TaskDb pi)
                        {
                            if (!newTasks.Any(t => t.TaskUid == pi.TaskUid))
                            {
                                newTasks.Add(pi);
                            }
                        });
                        childTasks.ForEach(delegate (TaskDb pi)
                        {
                            if (!newTasks.Any(t => t.TaskUid == pi.TaskUid))
                            {
                                newTasks.Add(pi);
                            }
                        });
                    });
                    _currentProject.Tasks = newTasks;
                }
                List<TaskDb> AllTasks = _currentProject.Tasks;
                if (_callbackArgs.KindWork != null && _callbackArgs.KindWork.Any())
                {
                    _currentProject.Tasks = _currentProject.Tasks.Where(t => _callbackArgs.KindWork.Contains(t.KindWork) && !t.IsSummary).ToList();
                }
                if (_callbackArgs.Stages != null && _callbackArgs.Stages.Any())
                {
                    _currentProject.Tasks = _currentProject.Tasks.Where(t => _callbackArgs.Stages.Contains(t.Stage) && !t.IsSummary).ToList();
                }
                if (_callbackArgs.Bloks != null && _callbackArgs.Bloks.Any())
                {
                    _currentProject.Tasks = _currentProject.Tasks.Where(t => _callbackArgs.Bloks.Contains(t.Blok) && !t.IsSummary).ToList();
                }
                if (_callbackArgs.Period.HasValue)
                {
                    _currentProject.Tasks = DateManager.FilterTasksForPeriod(_currentProject.Tasks, _callbackArgs);
                }
                List<TaskDb> AllParentTask = new List<TaskDb>();
                if (_callbackArgs.KindWork.Any() || _callbackArgs.Stages.Any() || _callbackArgs.Bloks.Any() || (_callbackArgs.Period.HasValue && _callbackArgs.Period != PeriodType.All && (_callbackArgs.Period != PeriodType.Custom || (_callbackArgs.Period == PeriodType.Custom && _callbackArgs.PeriodStart.HasValue && _callbackArgs.PeriodStart != DateTime.MinValue && _callbackArgs.PeriodFinish.HasValue && _callbackArgs.PeriodFinish != DateTime.MinValue && _callbackArgs.PeriodStart < _callbackArgs.PeriodFinish))))
                {
                    _currentProject.Tasks.ForEach(delegate (TaskDb t)
                    {
                        List<TaskDb> parentTasks = DbManager.GetParentTasks(AllTasks, t);
                        parentTasks.ForEach(delegate (TaskDb p)
                        {
                            if (!AllParentTask.Any(at => at.TaskUid == p.TaskUid))
                            {
                                AllParentTask.Add(p);
                            }
                        });
                    });
                    _currentProject.Tasks.AddRange(AllParentTask);
                }                

                string text = "";
                DateTime now2 = DateTime.Now;
                _data = new GridData(TypeViewDate.week, TypeFormEnum, _callbackArgs.Properties, _currentProject, fieldsForUser);
                text = GetProjectsJson(answerFlags);
                outGrid.gridS = text;
                outGrid.Views = DbManager.GetViewForUser(_currentUser.LoginName);
                outGrid.Views.Sort();
                outGrid.BasePlans = DbManager.GetBasePlans(_currentProject.Id);
                outGrid.TaskUids = _callbackArgs.TaskUids;
                outGrid.Layouts = _callbackArgs.Layouts;
                outGrid.TypeView = _callbackArgs.TypeView;
                outGrid.Period = _callbackArgs.Period;
                outGrid.PeriodStart = _callbackArgs.PeriodStart;
                outGrid.PeriodFinish = _callbackArgs.PeriodFinish;
                outGrid.CurrentKindWork = _callbackArgs.KindWork;
                outGrid.CurrentStages = _callbackArgs.Stages;
                outGrid.CurrentBloks = _callbackArgs.Bloks;
                outGrid.ViewName = _callbackArgs.ViewName;
                outGrid.ProjUid = _currentProject.Id;
                outGrid.BasePlan = _callbackArgs.BasePlan;
                outGrid.BasePlansProjects = DbManager.GetBasePlansProjects(_currentUser.LoginName);
                outGrid.Projects = _projects;
                Logger.Log($"Finish WebPartApprovalVolumesUserControl.GetCallbackResult LoginName:{_currentUser.LoginName} затраченное время: {(DateTime.Now - now).TotalSeconds} секунд");
                return JsonConvert.SerializeObject(outGrid);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                return null;
            }
            finally
            {
                windowsImpersonationContext.Undo();
            }
        }

        private string GetProjectsJson(List<AnswerFlag> AnswerFlags)
        {
            try
            {
                DateTime now = DateTime.Now;
                DataTable dataTable = _data.Data(AnswerFlags, _callbackArgs, true);
                DataTable table = _data.DataLeft(AnswerFlags);
                FieldOrderCollection sortedColumns = new FieldOrderCollection(new string[1] { "Id" });
                if (!_callbackArgs.OrderByColumnName.IsNullOrEmpty())
                {
                    sortedColumns = new FieldOrderCollection(new string[1] { _callbackArgs.OrderByColumnName }, new bool[1] { _callbackArgs.IsDescending });
                }
                GridSerializer gridSerializer = new GridSerializer(SerializeMode.Full, dataTable, "Key", sortedColumns, GridService.GetGridFields(dataTable, TypeFormEnum), GridService.GetGridColumns(table, _data));
                gridSerializer.EnableHierarchy(null, "HierarchyParentKey", "Title", false);
                gridSerializer.PaneLayout = (PaneLayout)_callbackArgs.Properties["Layout"];
                if (gridSerializer.PaneLayout == PaneLayout.GridAndPivotedGrid)
                {
                    gridSerializer.EnablePivotedGridPane(GetPivotedGridColumns(dataTable, _data.PropsFilter));
                }
                else if (gridSerializer.PaneLayout == PaneLayout.GridAndGantt)
                {
                    gridSerializer.EnableGantt(DateTime.Now.AddDays(0.0), DateTime.Now.AddDays(10.0), GridService.GetStyleInfo(), null);
                }
                gridSerializer.EnableAutoFilterEntryGeneration = false;
                gridSerializer.RowAutoFilter = null;
                Serializer s = new Serializer();
                Logger.Log("before gds.ToJson(serializer)");
                return gridSerializer.ToJson(s);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                return string.Empty;
            }
        }

        public void RaiseCallbackEvent(string eventArgument)
        {
            try
            {               
                _callbackArgs = new JavaScriptSerializer().Deserialize<CallbackArgs>(eventArgument);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }
    }
}
