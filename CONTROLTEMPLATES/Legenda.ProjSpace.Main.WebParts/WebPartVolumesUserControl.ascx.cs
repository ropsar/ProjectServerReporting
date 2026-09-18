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
using Legenda.ProjSpace.Main.Extensions;
using Legenda.ProjSpace.Main.Logging;
using Legenda.ProjSpace.Main.Model;
using Legenda.ProjSpace.Main.Model.DB;
using Legenda.ProjSpace.Main.Model.Enums;
using Legenda.ProjSpace.Main.Services;
using Legenda.ProjSpace.Main.WebParts.WebPartVolumes;

namespace Legenda.ProjSpace.Main.CONTROLTEMPLATES.Legenda.ProjSpace.Main.WebParts
{
    public partial class WebPartVolumesUserControl : UserControl, ICallbackEventHandler
    {
        private CallbackArgs _callbackArgs;

        private GridData _data;

        private SPUser _currentUser;

        private List<ProjectDb> _projects = null;

        private ProjectDb _currentProject = null;

        protected JSGrid JsGridControl;

        public WebPartVolumes WebPartControl { get; set; }

        public Properties.TypeForm TypeFormEnum => Properties.TypeForm.InsertValue;

        protected void Page_Load(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            try
            {
                Logger.Log($"WebPartVolumesUserControl Start Page.IsPostBack:{Page.IsPostBack}");
                if (!Page.IsPostBack)
                {
                    JsGridControl.JsInitObject = new
                    {
                        callbackScript = Page.ClientScript.GetCallbackEventReference(this, "args", "WGM.DisplayProjectsData", "true", true)
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
            Logger.Log($"WebPartVolumesUserControl. Finish затраченное время: {(DateTime.Now - now).TotalSeconds} секунд");
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
            Logger.Log("Start WebPartVolumesUserControl.GetCallbackResult LoginName:" + _currentUser.LoginName);
            WindowsImpersonationContext windowsImpersonationContext = null;
            windowsImpersonationContext = WindowsIdentity.Impersonate(IntPtr.Zero);
            try
            {
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
                if (_callbackArgs.Command == "SaveView")
                {
                    DbManager.SaveView(_callbackArgs, _currentUser);
                }
                Logger.Log("WebPartVolumesUserControl.GetCallbackResult LoginName:" + _currentUser.LoginName + " 2");
                if (_projects == null)
                {
                    _projects = DbManager.GetAllProjectsForUser(_currentUser.LoginName);
                }
                Logger.Log("WebPartVolumesUserControl.GetCallbackResult LoginName:" + _currentUser.LoginName + " 3");
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
                        Logger.Log("WebPartVolumesUserControl.GetCallbackResult LoginName:" + _currentUser.LoginName + " 4");
                        goto IL_034d;
                    }
                }
                Logger.Log($"WebPartVolumesUserControl.GetCallbackResult LoginName:{_currentUser.LoginName} 4 _callbackArgs.ProjUid:{_callbackArgs.ProjUid}");
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
                    _currentProject.Tasks = DbManager.GetAllAssnForUser(_currentUser.LoginName, _currentProject.Id, TypeFormEnum);
                }
                goto IL_034d;
            IL_034d:
                if (_callbackArgs.Command == "SubmittedApproval")
                {
                    SubmittedApprovalGridInfo dataToSave = new SubmittedApprovalGridInfo
                    {
                        TimeStamp = DateTime.Now,
                        ProjectGuid = _currentProject.Id,
                        UserLogin = _currentUser.LoginName,
                        UserName = _currentUser.Name,
                        Assns = new List<SubmittedApprovalGridInfo.Assn>()
                    };
                    _currentProject.Tasks = DbManager.GetAllAssnForUser(_currentUser.LoginName, _currentProject.Id, TypeFormEnum);
                    Logger.Log("WebPartVolumesUserControl.GetCallbackResult LoginName:" + _currentUser.LoginName + " 4.1");
                    SubmittedApproval.SaveGridDate(_callbackArgs.SubmittedApproval, dataToSave, _callbackArgs, _currentProject.Tasks, _currentUser, _currentProject.Id);
                    Logger.Log("WebPartVolumesUserControl.GetCallbackResult LoginName:" + _currentUser.LoginName + " 4.2");
                    _currentProject.Tasks = DbManager.GetAllAssnForUser(_currentUser.LoginName, _currentProject.Id, TypeFormEnum);
                    Logger.Log("WebPartVolumesUserControl.GetCallbackResult LoginName:" + _currentUser.LoginName + " 4.3");
                }
                Logger.Log("WebPartVolumesUserControl.GetCallbackResult LoginName:" + _currentUser.LoginName + " 5");
                DateManager.SetDate(_callbackArgs, _currentProject);
                Logger.Log("WebPartVolumesUserControl.GetCallbackResult LoginName:" + _currentUser.LoginName + " 6");
                if (_callbackArgs.Command == "GetView")
                {
                    UserSettings viewForUser = DbManager.GetViewForUser(_currentUser.LoginName, _callbackArgs.ViewName);
                    if (viewForUser != null)
                    {
                        if (viewForUser.FilterProjectUid.HasValue && viewForUser.FilterProjectUid != Guid.Empty)
                        {
                            _currentProject.Id = viewForUser.FilterProjectUid.Value;
                        }
                        _currentProject.Tasks = DbManager.GetAllAssnForUser(_currentUser.LoginName, _currentProject.Id, TypeFormEnum);
                        _callbackArgs.KindWork = ((!string.IsNullOrEmpty(viewForUser.FilterTypeWork)) ? viewForUser.FilterTypeWork.Split(';').ToList() : new List<string>());
                        _callbackArgs.Stages = ((!string.IsNullOrEmpty(viewForUser.Stages)) ? viewForUser.Stages.Split(';').ToList() : new List<string>());
                        _callbackArgs.Bloks = ((!string.IsNullOrEmpty(viewForUser.Bloks)) ? viewForUser.Bloks.Split(';').ToList() : new List<string>());
                        _callbackArgs.Bloks2 = ((!string.IsNullOrEmpty(viewForUser.Bloks2)) ? viewForUser.Bloks2.Split(';').ToList() : new List<string>());
                        _callbackArgs.Capture = ((!string.IsNullOrEmpty(viewForUser.Capture)) ? viewForUser.Capture.Split(';').ToList() : new List<string>());
                        _callbackArgs.TaskUids = ((!string.IsNullOrEmpty(viewForUser.FilterTaskUids)) ? (from e in viewForUser.FilterTaskUids.Split(';')
                                                                                                         select Guid.Parse(e)).ToList() : new List<Guid>());
                        _callbackArgs.BasePlan = viewForUser.FilterBasePlan;
                        _callbackArgs.Layouts = (PaneLayout)viewForUser.Layout;
                    }
                }
                Logger.Log("WebPartVolumesUserControl.GetCallbackResult LoginName:" + _currentUser.LoginName + " 7 _callbackArgs.ViewName:" + _callbackArgs.ViewName);
                if (_callbackArgs.ViewName == "")
                {
                    _callbackArgs.ViewName = "Все задачи";
                }
                List<FieldInfo> fieldsForUser = DbManager.GetFieldsForUser(_currentUser.LoginName, _callbackArgs.ViewName);
                Logger.Log("WebPartVolumesUserControl.GetCallbackResult LoginName:" + _currentUser.LoginName + " 8");
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
                outGrid.Bloks2 = (from t in _currentProject.Tasks
                                  where t.Blok2 != null && !t.IsSummary
                                  select t.Blok2).Distinct().ToList();
                outGrid.Bloks2.Sort();
                outGrid.Capture = (from t in _currentProject.Tasks
                                  where t.Capture != null && !t.IsSummary
                                  select t.Capture).Distinct().ToList();
                outGrid.Capture.Sort();
                Logger.Log("WebPartVolumesUserControl.GetCallbackResult LoginName:" + _currentUser.LoginName + " 9");
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
                Logger.Log("WebPartVolumesUserControl.GetCallbackResult LoginName:" + _currentUser.LoginName + " 10");
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
                if (_callbackArgs.Bloks2 != null && _callbackArgs.Bloks2.Any())
                {
                    _currentProject.Tasks = _currentProject.Tasks.Where(t => _callbackArgs.Bloks2.Contains(t.Blok2) && !t.IsSummary).ToList();
                }
                if (_callbackArgs.Capture != null && _callbackArgs.Capture.Any())
                {
                    _currentProject.Tasks = _currentProject.Tasks.Where(t => _callbackArgs.Capture.Contains(t.Capture) && !t.IsSummary).ToList();
                }
                Logger.Log("WebPartVolumesUserControl.GetCallbackResult LoginName:" + _currentUser.LoginName + " 11");
                if (_callbackArgs.Period.HasValue)
                {
                    _currentProject.Tasks = DateManager.FilterTasksForPeriod(_currentProject.Tasks, _callbackArgs);
                }
                Logger.Log("WebPartVolumesUserControl.GetCallbackResult LoginName:" + _currentUser.LoginName + " 12");
                List<TaskDb> AllParentTask = new List<TaskDb>();
                if (_callbackArgs.KindWork.Any() || _callbackArgs.Stages.Any() || _callbackArgs.Bloks.Any() || _callbackArgs.Bloks2.Any() || _callbackArgs.Capture.Any() || (_callbackArgs.Period.HasValue && _callbackArgs.Period != PeriodType.All && (_callbackArgs.Period != PeriodType.Custom || (_callbackArgs.Period == PeriodType.Custom && _callbackArgs.PeriodStart.HasValue && _callbackArgs.PeriodStart != DateTime.MinValue && _callbackArgs.PeriodFinish.HasValue && _callbackArgs.PeriodFinish != DateTime.MinValue && _callbackArgs.PeriodStart < _callbackArgs.PeriodFinish))))
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
                Logger.Log("WebPartVolumesUserControl.GetCallbackResult LoginName:" + _currentUser.LoginName + " 13");
                _data = new GridData(TypeViewDate.week, TypeFormEnum, _callbackArgs.Properties, _currentProject, fieldsForUser);
                Logger.Log("WebPartVolumesUserControl.GetCallbackResult LoginName:" + _currentUser.LoginName + " 14");
                text = GetProjectsJson(answerFlags);
                Logger.Log("WebPartVolumesUserControl.GetCallbackResult LoginName:" + _currentUser.LoginName + " 14.1");
                outGrid.gridS = text;
                outGrid.Views = DbManager.GetViewForUser(_currentUser.LoginName);
                outGrid.Views.Sort();
                Logger.Log($"WebPartVolumesUserControl.GetCallbackResultLoginName:{_currentUser.LoginName} 15 outGrid.Views:{outGrid.Views.Count}");
                outGrid.BasePlans = DbManager.GetBasePlans(_currentProject.Id);
                Logger.Log($"WebPartVolumesUserControl.GetCallbackResultLoginName:{_currentUser.LoginName} 16 outGrid.BasePlans:{outGrid.BasePlans.Count}");
                outGrid.TaskUids = _callbackArgs.TaskUids;
                outGrid.Layouts = _callbackArgs.Layouts;
                outGrid.TypeView = _callbackArgs.TypeView;
                outGrid.Period = _callbackArgs.Period;
                outGrid.PeriodStart = _callbackArgs.PeriodStart;
                outGrid.PeriodFinish = _callbackArgs.PeriodFinish;
                outGrid.CurrentKindWork = _callbackArgs.KindWork;
                outGrid.CurrentStages = _callbackArgs.Stages;
                outGrid.CurrentBloks = _callbackArgs.Bloks;
                outGrid.CurrentBloks2 = _callbackArgs.Bloks2;
                outGrid.CurrentCapture = _callbackArgs.Capture;
                outGrid.ViewName = _callbackArgs.ViewName;
                outGrid.ProjUid = _currentProject.Id;
                outGrid.BasePlan = _callbackArgs.BasePlan;
                outGrid.Projects = _projects;
                Logger.Log($"Finish WebPartVolumesUserControl.GetCallbackResult LoginName:{_currentUser.LoginName} затраченное время: {(DateTime.Now - now).TotalSeconds} секунд");
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
                Logger.Log($"WebPartVolumesUserControl.GetProjectsJson Start LoginName:{_currentUser.LoginName} 1 AnswerFlags:{AnswerFlags.Count}");
                DateTime now = DateTime.Now;
                DataTable dataTable = _data.Data(AnswerFlags, _callbackArgs, false);
                DataTable table = _data.DataLeft(AnswerFlags);
                FieldOrderCollection sortedColumns = new FieldOrderCollection(new string[1] { "Id" });
                if (!_callbackArgs.OrderByColumnName.IsNullOrEmpty())
                {
                    sortedColumns = new FieldOrderCollection(new string[1] { _callbackArgs.OrderByColumnName }, new bool[1] { _callbackArgs.IsDescending });
                }
                GridSerializer gridSerializer = new GridSerializer(SerializeMode.Full, dataTable, "Key", sortedColumns, GridService.GetGridFields(dataTable, TypeFormEnum), GridService.GetGridColumns(table, _data));
                gridSerializer.EnableHierarchy(null, "HierarchyParentKey", "Title", false);
                Logger.Log("LoginName:" + _currentUser.LoginName + " 2");
                gridSerializer.PaneLayout = (PaneLayout)_callbackArgs.Properties["Layout"];
                if (gridSerializer.PaneLayout == PaneLayout.GridAndPivotedGrid)
                {
                    gridSerializer.EnablePivotedGridPane(GetPivotedGridColumns(dataTable, _data.PropsFilter));
                }
                else if (gridSerializer.PaneLayout == PaneLayout.GridAndGantt)
                {
                    gridSerializer.EnableGantt(DateTime.Now.AddDays(0.0), DateTime.Now.AddDays(10.0), GridService.GetStyleInfo(), null);
                }
                Logger.Log("LoginName:" + _currentUser.LoginName + " 3");
                gridSerializer.EnableAutoFilterEntryGeneration = false;
                gridSerializer.RowAutoFilter = null;
                Serializer s = new Serializer();
                Logger.Log($"WebPartVolumesUserControl.GetProjectsJson затраченное время: {(DateTime.Now - now).TotalSeconds} секунд");
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
                Logger.Log("eventArgument:" + eventArgument + " 1");
                _callbackArgs = new JavaScriptSerializer().Deserialize<CallbackArgs>(eventArgument);
                Logger.Log("eventArgument:" + eventArgument + " 2");
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }
    }


}
