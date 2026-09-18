using Legenda.ProjSpace.Main.Extensions;
using Legenda.ProjSpace.Main.Logging;
using Legenda.ProjSpace.Main.Model;
using Legenda.ProjSpace.Main.Model.DB;
using Legenda.ProjSpace.Main.Model.Enums;
//using Legenda.ProjSpace.Core.Services.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Web.UI.WebControls;

namespace Legenda.ProjSpace.Main.Services
{
    public class GridData
    {
        private double loadCustomFields = 0.0;

        private double loadParentTasks = 0.0;

        private double loadValueInDB = 0.0;

        public int deltaWBS = 0;

        public int TaskIndex = 0;

        private double quality = 0.01;

        private Random _rand = new Random();

        public DateTime StartDate;

        public DateTime TaskStart;

        public DateTime TaskFinish;

        public TypeViewDate TypeView;

        private Properties.TypeForm _typeForm;

        public Dictionary<string, object> PropsFilter;

        private static ProjectDb _project = new ProjectDb();

        private static List<ProjectDb> _projects = new List<ProjectDb>();

        private static ProjectDb _projectCurrent = new ProjectDb();

        public List<FieldInfo> FieldsProp = new List<FieldInfo>();

        public static List<string> StatusList = new List<string> { "ActualValue", "ReportedValue", "RejectedValue", "ApprovedValue" };

        public GridData()
        {
        }

        public GridData(TypeViewDate typeView, Properties.TypeForm typeForm, Dictionary<string, object> propsFilter, ProjectDb project, List<FieldInfo> fields)
        {
            if (propsFilter.Any())
            {
                _project = project;
                FieldsProp = fields;
                StartDate = ((propsFilter.ContainsKey("StartPeriod") && propsFilter["StartPeriod"].ToString() != "") ? Convert.ToDateTime(propsFilter["StartPeriod"]) : DateTime.Now);
                TaskStart = ((propsFilter.ContainsKey("ShowTaskStart") && propsFilter["ShowTaskStart"].ToString() != "") ? Convert.ToDateTime(propsFilter["ShowTaskStart"]) : DateTime.Now);
                TaskFinish = ((propsFilter.ContainsKey("ShowTaskFinish") && propsFilter["ShowTaskFinish"].ToString() != "") ? Convert.ToDateTime(propsFilter["ShowTaskFinish"]) : DateTime.Now.AddDays(28.0));
                TypeView = typeView;
                _typeForm = typeForm;
                PropsFilter = propsFilter;
                if (!(project.Id == Guid.Empty))
                {
                }
            }
        }

        public static Unit GetHeight(string _callbackArgsHeight)
        {
            double result = 600.0;
            if (!string.IsNullOrEmpty(_callbackArgsHeight))
            {
                if (!_callbackArgsHeight.Contains("%"))
                {
                    double.TryParse(_callbackArgsHeight, out result);
                    return new Unit(result, UnitType.Pixel);
                }
                if (double.TryParse(_callbackArgsHeight.Replace("%", ""), out result))
                {
                    return new Unit(result, UnitType.Percentage);
                }
            }
            return new Unit(result, UnitType.Pixel);
        }

        public static Unit GetWidth(string _callbackArgsWidth)
        {
            double result = 100.0;
            if (!string.IsNullOrEmpty(_callbackArgsWidth))
            {
                if (!_callbackArgsWidth.Contains("%"))
                {
                    double.TryParse(_callbackArgsWidth, out result);
                    return new Unit(result, UnitType.Pixel);
                }
                if (double.TryParse(_callbackArgsWidth.Replace("%", ""), out result))
                {
                    return new Unit(result, UnitType.Percentage);
                }
            }
            return new Unit(result, UnitType.Percentage);
        }

        public virtual DataTable Data(List<AnswerFlag> AnswerFlags, CallbackArgs callbackArgs, bool approveForm)
        {
            List<DataRow> list = new List<DataRow>();
            DataTable data = DataLeft(AnswerFlags);
            data.Locale = CultureInfo.InvariantCulture;
            if (_project == null)
            {
                return data;
            }
            data.Columns.Add(new DataColumn("IsNewTask", typeof(bool)));
            data.Columns.Add(new DataColumn("IsAssn", typeof(bool)));
            data.Columns.Add(new DataColumn("TaskUid", typeof(string)));
            data.Columns.Add(new DataColumn("WBS", typeof(string)));
            data.Columns.Add(new DataColumn("Key", typeof(Guid)));
            data.Columns.Add(new DataColumn("_GRIDROWSTYLEID", typeof(string)));
            data.Columns.Add(new DataColumn("HierarchyParentKey", typeof(Guid)));
            data.Columns.Add(new DataColumn("IsTask", typeof(bool)));
            data.Columns.Add(new DataColumn("IsTotal", typeof(bool)));
            data.Columns.Add(new DataColumn("IsStartDateClosed", typeof(bool)));
            //data.Columns.Add(new DataColumn("ProjectCheckedOut", typeof(bool)));
            data.Columns.Add(new DataColumn("#", typeof(bool)));
            int numColumn = DateManager.GetCountColumns(callbackArgs.PeriodStart.Value, callbackArgs.PeriodFinish.Value, callbackArgs.TypeView);
            for (int i = 0; i < numColumn; i++)
            {
                data.Columns.Add(new DataColumn("DatePlan" + i, typeof(double)));
                data.Columns.Add(new DataColumn("DateFact" + i, typeof(double)));
                data.Columns.Add(new DataColumn("StatusPlan" + i, typeof(int)));
                data.Columns.Add(new DataColumn("StatusFact" + i, typeof(int)));
            }
            data.Columns.Add(new DataColumn("DescPlan", typeof(string)));
            data.Columns.Add(new DataColumn("DescFact", typeof(string)));
            data.Columns.Add(new DataColumn("SumPlan", typeof(string)));
            data.Columns.Add(new DataColumn("SumFact", typeof(string)));
            data.Columns.Add(new DataColumn("Start Date", typeof(DateTime)));
            data.Columns.Add(new DataColumn("Finish Date", typeof(DateTime)));
            data.Columns.Add(new DataColumn("CompleteThrough", typeof(DateTime)));
            data.Columns.Add(new DataColumn("_GANTTBARSTYLEIDS", typeof(GridService.CustomBarStyle[])));
            DateTime dateTime = DateTime.Now.AddSeconds(_rand.Next(1728000));
            Guid guid = Guid.NewGuid();
            List<VolumeShare> volumesShare = DbManager.GetVolumesShare((from s in _project.Tasks.Where(w => w.AssnUid.HasValue).ToList()
                                                                        select s.AssnUid.Value).ToList(), _project.Id);
            //bool projChecketOut = DbManager.ProjectCheckedOut(_project.Id.ToString());
            Logger.Log($"Data volumesShare:{volumesShare.Count}");
            DataRow dr;
            _project.Tasks.ForEach(delegate (TaskDb task)
            {
                VolumeShare volumeShare = null;
                Guid? guid2 = (task.AssnUid.HasValue ? task.AssnUid : new Guid?(task.TaskUid));
                if (task.AssnUid.HasValue)
                {
                    volumeShare = volumesShare.FirstOrDefault(f => f.AssnUid == task.AssnUid.Value);
                }
                dr = data.NewRow();
                //dr["ProjectCheckedOut"] = projChecketOut;
                dr["Id"] = task.TaskIndex;
                dr["Title"] = task.Name;
                dr["#"] = false;
                dr["Key"] = guid2;
                dr["TaskUid"] = task.TaskUid;
                dr["HierarchyParentKey"] = (task.Parent.HasValue ? task.Parent : guid2);
                dr["WBS"] = task.WorkBreakdownStructure;
                dr["PercentCompleteWork"] = ((task.PercentCompleteWork > 100) ? 100 : task.PercentCompleteWork) + "%";
                dr["Measure"] = task.Measure;
                dr["IsNewTask"] = !task.AssnUid.HasValue;
                dr["IsAssn"] = task.AssnUid.HasValue;
                dr["DescPlan"] = "План";
                dr["DescFact"] = "Факт";
                dr["Start"] = task.Start;
                dr["Finish"] = task.Finish;
                if (callbackArgs.BasePlan.HasValue)
                {
                    if (Convert.ToDateTime(task.BaselineStart[callbackArgs.BasePlan.Value]) != DateTime.MinValue)
                    {
                        dr["StartBasePlan"] = task.BaselineStart[callbackArgs.BasePlan.Value];
                    }
                    if (Convert.ToDateTime(task.BaselineFinish[callbackArgs.BasePlan.Value]) != DateTime.MinValue)
                    {
                        dr["FinishBasePlan"] = task.BaselineFinish[callbackArgs.BasePlan.Value];
                    }
                }
                if (task.IsProjectSource)
                {
                    dr["VolumePlan"] = task.Work;
                }
                else
                {
                    dr["VolumePlan"] = task.VolumePlan;
                }
                if (!string.IsNullOrEmpty(task.KindWork))
                {
                    dr["KindWork"] = task.KindWork;
                }
                if (!string.IsNullOrEmpty(task.Stage))
                {
                    dr["Stage"] = task.Stage;
                }
                if (!string.IsNullOrEmpty(task.Blok))
                {
                    dr["Blok"] = task.Blok;
                }
                if (!string.IsNullOrEmpty(task.Blok2))
                {
                    dr["Blok2"] = task.Blok2;
                }
                if (!string.IsNullOrEmpty(task.Capture))
                {
                    dr["Capture"] = task.Capture;
                }
                if (!string.IsNullOrEmpty(task.Comments))
                {
                    dr["Comments"] = task.Comments;
                }
                dr["Start Date"] = task.Start;
                dr["Finish Date"] = task.Finish;
                if (task.AssnUid.HasValue)
                {
                    dr["IsStartDateClosed"] = IsStartDateClosed(volumeShare, task.Measure != "ч");
                    int num = ((task.Finish - task.Start).Days + 1) * task.PercentCompleteCustom / 100;
                    dr["CompleteThrough"] = task.Start.AddDays(num);
                    dr["_GANTTBARSTYLEIDS"] = new GridService.CustomBarStyle[2]
                    {
                    GridService.CustomBarStyle.Standard,
                    GridService.CustomBarStyle.PctComplete
                    };
                }
                else
                {
                    dr["_GANTTBARSTYLEIDS"] = new GridService.CustomBarStyle[1];
                }
                if (task.AssnUid.HasValue)
                {
                    List<VolumeInDay> source = ((volumeShare == null) ? new List<VolumeInDay>() : volumeShare.VolumeInDays);
                    VolumeData volumeData = volumeShare?.VolumeData;
                    List<VolumeInDay> list2 = new List<VolumeInDay>();
                    bool flag = source.Any(v => v.Status == 2);
                    DateTime dateTime2 = Convert.ToDateTime(task.Start.ToShortDateString());
                    DateTime dateFinish = Convert.ToDateTime(task.Finish.ToShortDateString());
                    double num2 = source.Sum(v => v.Volume);
                    if (task.Measure == "ч")
                    {
                        num2 = task.VolumePlan / 100.0 * (double)task.PercentCompleteWork;
                    }
                    if (task.IsProjectSource)
                    {
                        dr["VolumeFact"] = task.ActualWork;
                        dr["VolumeLeft"] = task.Work - task.ActualWork;
                    }
                    else
                    {
                        double num3 = ((num2 > 0.0) ? (num2 / (task.VolumePlan / 100.0)) : 0.0);
                        if (num3 > 100.0)
                        {
                            num3 = 100.0;
                        }
                        dr["VolumeFact"] = num2;
                        dr["VolumeLeft"] = task.VolumePlan - num2;
                        dr["PercentCompleteWork"] = (int)Math.Round(num3, MidpointRounding.AwayFromZero) + "%";
                    }
                    if (task.Measure.IsNotEmpty())
                    {
                        if (!task.Measure.Contains("ч"))
                        {
                            if (volumeData?.Status == 5)
                            {
                                dr["PercentCompleteWork"] = task.PercentCompleteCustom;
                            }
                        }
                    }
                    if (volumeData?.Status != 5) 
                    {
                        if (volumeData?.Status != null)
                        {
                            dr["Status"] = ((StatusTask)volumeData.Status).GetEnumDescription();
                        }
                    }
                    else dr["Status"] = "";
                    int countColumns = DateManager.GetCountColumns(dateTime2, dateFinish, TypeViewDate.day);
                    double num4 = Math.Round(task.VolumePlan / (double)countColumns, 2);
                    double num5 = task.VolumePlan - num4 * (double)(countColumns - 1);
                    DateTime date = dateTime2;
                    for (int num6 = 0; num6 < countColumns; num6++)
                    {
                        list2.Add(new VolumeInDay
                        {
                            Date = date,
                            Volume = ((num6 < countColumns - 1) ? num4 : num5)
                        });
                        date = date.AddDays(1.0);
                    }
                    if (list2.Any(a => a.Volume > 0.0) || source.Any(a => a.Volume > 0.0))
                    {
                        DateTime dtbuf = callbackArgs.PeriodStart.Value;
                        DateTime dtbufNext = DateManager.NextDate(dtbuf, callbackArgs.TypeView);
                        for (int num7 = 0; num7 < numColumn; num7++)
                        {
                            if (list2.Any())
                            {
                                double num8 = list2.Where(v => v.Date >= dtbuf && v.Date < dtbufNext).Sum(v => v.Volume);
                                if (num8 != 0.0)
                                {
                                    dr["DatePlan" + num7] = num8;
                                }
                            }
                            if (source.Any())
                            {
                                double num9 = source.Where(v => v.Date >= dtbuf && v.Date < dtbufNext).Sum(v => v.Volume);
                                if (num9 != 0.0)
                                {
                                    dr["DateFact" + num7] = num9;
                                }
                            }
                            dtbuf = dtbufNext;
                            dtbufNext = DateManager.NextDate(dtbuf, callbackArgs.TypeView);
                        }
                    }
                }
                data.Rows.Add(dr);
            });
            Logger.Log("Data End");
            return data;
        }

        internal static object IsStartDateClosed(VolumeShare volumeShare, bool isMaterial)
        {
            bool flag = false;
            if (volumeShare != null)
            {
                if (isMaterial)
                {
                    flag = volumeShare.VolumeInDays.Any(v => v.AssnUid == volumeShare.AssnUid && (v.Status == 4 || v.Status == 5));
                }
                else
                {
                    VolumeData volumeData = volumeShare.VolumeData;
                    if (volumeData != null)
                    {
                        flag = volumeData.PercentCompleted != 0.0;
                    }
                }
            }
            return flag;
        }

        public int GetVolumeStatusForDays(TypeViewDate type, List<VolumeInDay> volumes, DateTime date)
        {
            IEnumerable<VolumeInDay> source = volumes.Where(v => v.Date == date);
            return source.Any() ? source.Min(v => v.Status) : 0;
        }

        public bool UpdateProjectFields()
        {
            return true;
        }

        public virtual List<AnswerFlag> SetData(List<SaveProjects> SaveProjects)
        {
            return new List<AnswerFlag>();
        }

        public DataRow AdditionalTask(DataRow drr, AdditionalTask newTask)
        {
            try
            {
                drr["Key"] = newTask.TaskGuid;
                drr["HierarchyParentKey"] = newTask.ParentGuid;
                drr["Title"] = newTask.Title;
                drr["Start Date"] = DateTime.Today;
                drr["Finish Date"] = DateTime.Today;
                drr["Code"] = "";
                drr["#"] = false;
                drr["Measure"] = newTask.Measure;
                drr["UnitPrice"] = newTask.UnitPrice;
                drr["Justification"] = newTask.Justification;
                drr["BaseVolume"] = newTask.BaseVolume;
                drr["FactCumulative"] = 0;
                drr["ReceivedCumulative"] = 0;
                drr["PercentCompletion"] = 0;
                drr["PercentAccepted"] = 0;
                drr["FactVolume"] = 0;
                drr["AcceptVolume"] = newTask.AcceptVolume;
                drr["RemainingAcceptance"] = 0;
                drr["Type"] = 1;
                drr["IsNewTask"] = true;
                drr["IsTotal"] = newTask.IsTotal;
                drr["IsTask"] = false;
                drr["Sum"] = 0;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return drr;
        }

        internal DataTable DataLeft(List<AnswerFlag> answerFlags)
        {
            DataTable data = new DataTable();
            Dictionary<string, Type> fieldsType = new Dictionary<string, Type>
            {
                { "Id", typeof(int) },
                { "Title", typeof(string) },
                { "KindWork", typeof(string) },
                { "PercentCompleted", typeof(string) },
                { "PercentCompleteWork", typeof(string) },
                { "Start", typeof(DateTime) },
                { "Finish", typeof(DateTime) },
                { "VolumePlan", typeof(double) },
                { "VolumeFact", typeof(double) },
                { "VolumeLeft", typeof(double) },
                { "Measure", typeof(string) },
                { "Status", typeof(string) },
                { "Comments", typeof(string) },
                { "Stage", typeof(string) },
                { "Blok", typeof(string) },
                { "Blok2", typeof(string) },
                { "Capture", typeof(string) },
                { "StartBasePlan", typeof(DateTime) },
                { "FinishBasePlan", typeof(DateTime) }
            };
            FieldsProp.ToList().ForEach(delegate (FieldInfo f)
            {
                data.Columns.Add(new DataColumn(f.Name, fieldsType[f.Name]));
            });
            if (!FieldsProp.Any(f => f.Name == "StartBasePlan"))
            {
                data.Columns.Add(new DataColumn("StartBasePlan", fieldsType["StartBasePlan"]));
            }
            if (!FieldsProp.Any(f => f.Name == "FinishBasePlan"))
            {
                data.Columns.Add(new DataColumn("FinishBasePlan", fieldsType["FinishBasePlan"]));
            }
            return data;
        }
    }

}