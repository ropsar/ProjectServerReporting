using System;
using System.Collections.Generic;
using System.Linq;
using Legenda.ProjSpace.Main.Extensions;
using Legenda.ProjSpace.Main.Logging;
using Legenda.ProjSpace.Main.Model;
using Legenda.ProjSpace.Main.Model.DB;
using Legenda.ProjSpace.Main.Model.Enums;
//using Legenda.ProjSpace.Core.Services.Logging;
using Microsoft.SharePoint;

namespace Legenda.ProjSpace.Main.Services
{

    public static class SubmittedApproval
    {
        /// <summary>
        /// Отправить задачи на согласование
        /// </summary>
        /// <param name="data"></param>
        /// <param name="dataToSave"></param>
        /// <param name="args"></param>
        /// <param name="tasks"></param>
        /// <param name="user"></param>
        /// <param name="projUid"></param>
        public static void SaveGridDate(Dictionary<string, Dictionary<string, object>> data, SubmittedApprovalGridInfo dataToSave, CallbackArgs args, List<TaskDb> tasks, SPUser user, Guid projUid)
        {
            try
            {
                DbManager.ClearingDeletedTasks(projUid);
                List<AssnByDay> assnsByDay = DbManager.GetAssnsByDay(data.Select(s => Guid.Parse(s.Key)).ToList());
                if (assnsByDay.Any())
                {
                    UpdateGridVolumes(assnsByDay, tasks, projUid);
                }
                List<VolumeData> oldVolumesDB = new List<VolumeData>();
                List<VolumeInDay> list = new List<VolumeInDay>();
                using (VolumeContext volumeContext = new VolumeContext())
                {
                    oldVolumesDB = volumeContext.VolumeDatas.Where(w => w.ProjectUid == projUid).ToList();
                    if (oldVolumesDB.Any())
                    {
                        List<Guid> guids = oldVolumesDB.Select(s => s.AssnUid).ToList();
                        list = volumeContext.VolumeInDays.Where(w => guids.Contains(w.AssnUid)).ToList();
                    }
                }
                data.ForEach(delegate (KeyValuePair<string, Dictionary<string, object>> assn)
                {
                    SubmittedApprovalGridInfo.Assn ds = new SubmittedApprovalGridInfo.Assn
                    {
                        AssnUid = Guid.Parse(assn.Key)
                    };
                    ds.Task = tasks.FirstOrDefault(t => t.AssnUid == ds.AssnUid);
                    KeyValuePair<string, object> keyValuePair = assn.Value.FirstOrDefault(f => f.Key == "Finish");
                    DateTime finish = DateTime.MinValue;
                    if (keyValuePair.Value != null)
                    {
                        finish = DateTime.Parse(((Dictionary<string, object>)keyValuePair.Value)["data"].ToString());
                    }
                    KeyValuePair<string, object> keyValuePair2 = assn.Value.FirstOrDefault(f => f.Key == "Start");
                    DateTime start = DateTime.MinValue;
                    if (keyValuePair2.Value != null)
                    {
                        DateTime dateTime = DateTime.Parse(((Dictionary<string, object>)keyValuePair2.Value)["data"].ToString());
                        start = (ds.StartDate = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 9, 0, 0));
                    }
                    assn.Value.ForEach(delegate (KeyValuePair<string, object> change)
                    {
                        Logger.Log("SaveGridDate" + change.Key);
                        switch (change.Key)
                        {
                            case "TaskUid":
                                ds.TaskUid = Guid.Parse(change.Value.ToString());
                                break;
                            case "PercentCompleteWork":
                                ds.PercentCompleteWork = Convert.ToInt16(((Dictionary<string, object>)change.Value)["data"].ToString().Replace("%", ""));
                                break;
                            case "Start":
                                {
                                    DateTime dateTime2 = DateTime.Parse(((Dictionary<string, object>)change.Value)["data"].ToString());
                                    ds.StartDate = new DateTime(dateTime2.Year, dateTime2.Month, dateTime2.Day, 9, 0, 0);
                                    Logger.Log("StartDate =" + ((Dictionary<string, object>)change.Value)["data"].ToString() + " ds.StartDate=" + ds.StartDate.ToString());
                                    break;
                                }
                            case "Finish":
                                ds.FinishDate = DateTime.Parse(((Dictionary<string, object>)change.Value)["data"].ToString()).AddHours(18.0);
                                Logger.Log("FinishDate =" + ((Dictionary<string, object>)change.Value)["data"].ToString());
                                break;
                        }
                        if ((change.Key == "PercentCompleteWork" || change.Key == "SumFact") && ds.Task.Measure != "ч")
                        {
                            List<VolumeInDay> source = (from o in DbManager.GetVolumeByDay(ds.AssnUid)
                                                        orderby o.Date
                                                        select o).ToList();
                            double volumePlan = ds.Task.VolumePlan;
                            double num = 0.0;
                            double num2 = 0.0;
                            num2 = source.Where(v => v.Date != Convert.ToDateTime(DateTime.Now.ToShortDateString())).Sum(v => v.Volume);
                            if (change.Key == "PercentCompleteWork")
                            {
                                ds.PercentCompleteWork = Convert.ToInt16(((Dictionary<string, object>)change.Value)["data"].ToString().Replace("%", ""));
                                if (oldVolumesDB.Any())
                                {
                                    VolumeData volumeData = oldVolumesDB.FirstOrDefault(a => a.AssnUid == ds.AssnUid);
                                    if (volumeData != null && volumeData.PercentCompleted > (double?)ds.PercentCompleteWork)
                                    {
                                        num2 = source.Sum(v => v.Volume);
                                    }
                                }
                                num = volumePlan * Convert.ToDouble(((Dictionary<string, object>)change.Value)["data"].ToString().Replace("%", "")) / 100.0 - num2;
                            }
                            if (change.Key == "SumFact")
                            {
                                num = Convert.ToDouble(((Dictionary<string, object>)change.Value)["data"]);
                            }
                            bool flag = false;
                            if (num > 0.0)
                            {
                                DateTime lastDate;
                                DateTime dateTime3 = (lastDate = Convert.ToDateTime(DateTime.Today.ToShortDateString()));
                                if (source.Any())
                                {
                                    List<VolumeInDay> source2 = source.Where(w => w.Volume > 0.0).ToList();
                                    if (source2.Any())
                                    {
                                        lastDate = source2.Max(x => x.Date).AddDays(1.0);
                                    }
                                    else if (oldVolumesDB.Any(a => a.AssnUid == ds.AssnUid))
                                    {
                                        VolumeData volumeData2 = oldVolumesDB.FirstOrDefault(a => a.AssnUid == ds.AssnUid);
                                        DateTime dateTime4 = volumeData2.FinishDate;
                                        DateTime dateTime5 = volumeData2.StartDate;
                                        if (finish != DateTime.MinValue && finish > dateTime4)
                                        {
                                            dateTime4 = finish;
                                        }
                                        if (start != DateTime.MinValue && start < dateTime5)
                                        {
                                            dateTime5 = start;
                                        }
                                        if (dateTime4 < dateTime3)
                                        {
                                            lastDate = dateTime5;
                                            flag = true;
                                            ds.FinishDate = dateTime4;
                                        }
                                    }
                                }
                                int num3 = DateManager.GetCountColumns(lastDate, dateTime3, TypeViewDate.day);
                                if (oldVolumesDB.Any(a => a.AssnUid == ds.AssnUid))
                                {
                                    VolumeData volumeData3 = oldVolumesDB.FirstOrDefault(a => a.AssnUid == ds.AssnUid);
                                    DateTime dateTime6 = volumeData3.FinishDate;
                                    if (assn.Value.Any(f => f.Key == "Finish"))
                                    {
                                        KeyValuePair<string, object> keyValuePair3 = assn.Value.FirstOrDefault(f => f.Key == "Finish");
                                        if (keyValuePair3.Value != null)
                                        {
                                            DateTime dateTime7 = DateTime.Parse(((Dictionary<string, object>)keyValuePair3.Value)["data"].ToString());
                                            if (dateTime7 != DateTime.MinValue && dateTime6 < dateTime7)
                                            {
                                                dateTime6 = dateTime7;
                                            }
                                        }
                                    }
                                    if (dateTime6 < dateTime3)
                                    {
                                        num3 = 1;
                                        if (source.Any(a => a.Volume > 0.0))
                                        {
                                            lastDate = lastDate.AddDays(-1.0);
                                            num += source.FirstOrDefault(f => f.Date == lastDate).Volume;
                                            ds.FinishDate = dateTime6;
                                        }
                                    }
                                }
                                if (flag)
                                {
                                    num3 = 1;
                                }
                                if (num3 <= 0)
                                {
                                    num3 = 1;
                                    lastDate = dateTime3;
                                }
                                double num4 = Math.Round(num / (double)num3, 2);
                                double num5 = num - num4 * (double)(num3 - 1);
                                for (int num6 = 0; num6 < num3; num6++)
                                {
                                    ds.VolumeInDay.Add(new VolumeInDay
                                    {
                                        Date = Convert.ToDateTime(lastDate.AddDays(num6).ToShortDateString()),
                                        Volume = ((num6 == num3 - 1) ? num5 : num4),
                                        Status = 2
                                    });
                                }
                            }
                            else if (source.Sum(v => v.Volume) + num >= 0.0 && change.Key == "PercentCompleteWork")
                            {
                                num *= -1.0;
                                List<VolumeInDay> list2 = source.Where(w => w.Volume > 0.0).ToList();
                                if (list2.Any())
                                {
                                    while (num > 0.0)
                                    {
                                        VolumeInDay el = list2.LastOrDefault();
                                        double value = 0.0;
                                        if (el.Volume > num)
                                        {
                                            value = el.Volume - num;
                                        }
                                        num -= el.Volume;
                                        if (ds.VolumeInDay.Any(a => a.Date == el.Date))
                                        {
                                            ds.VolumeInDay.FirstOrDefault(a => a.Date == el.Date).Volume = Math.Round(value, 2);
                                            ds.VolumeInDay.FirstOrDefault(a => a.Date == el.Date).Status = 2;
                                        }
                                        else
                                        {
                                            ds.VolumeInDay.Add(new VolumeInDay
                                            {
                                                Date = el.Date,
                                                Volume = Math.Round(value, 2),
                                                Status = 2
                                            });
                                        }
                                        list2.Remove(el);
                                    }
                                }
                            }
                            else
                            {
                                Logger.Log($"{ds.AssnUid} %завершения меньше предыдущего или введеный объем меньше 0 или отрицательный, при этом в катомной базе недостатаочно факта для уменьшения");
                            }
                        }
                        if (change.Key.StartsWith("DateFact") && args.TypeView == TypeViewDate.day)
                        {
                            int num7 = Convert.ToInt32(change.Key.Replace("DateFact", ""));
                            DateTime dateTime8 = args.PeriodStart.Value;
                            DateTime dateTime9 = DateManager.NextDate(dateTime8, args.TypeView);
                            for (int num8 = 0; num8 < num7; num8++)
                            {
                                dateTime8 = dateTime9;
                                dateTime9 = DateManager.NextDate(dateTime8, args.TypeView);
                            }
                            int num9 = DateManager.GetCountColumns(dateTime8, dateTime9, args.TypeView) - 1;
                            double num10 = double.Parse(Convert.ToDouble(((Dictionary<string, object>)change.Value)["data"]).ToString());
                            int num11 = (int)(num10 * 100.0 / (double)num9) / 100;
                            double num12 = num10 - (double)(num11 * (num9 - 1));
                            for (int num13 = 0; num13 < num9; num13++)
                            {
                                ds.VolumeInDay.Add(new VolumeInDay
                                {
                                    Date = dateTime8.AddDays(num13),
                                    Volume = ((num13 == num9 - 1) ? num12 : ((double)num11))
                                });
                            }
                            List<VolumeInDay> volumeByDay = DbManager.GetVolumeByDay(ds.AssnUid);
                            double volumePlan2 = ds.Task.VolumePlan;
                            double volumeFact = 0.0;
                            volumeFact += ds.VolumeInDay.Sum(v => v.Volume);
                            volumeByDay.ForEach(delegate (VolumeInDay vs)
                            {
                                if (!ds.VolumeInDay.Any(v => v.Date == vs.Date))
                                {
                                    volumeFact += vs.Volume;
                                }
                            });
                            ds.PercentCompleteWork = Convert.ToInt32(Math.Round(volumeFact * 100.0 / volumePlan2, 2));
                        }
                        else if (change.Key.StartsWith("DateFact") && args.TypeView != TypeViewDate.day)
                        {
                            int num14 = Convert.ToInt32(change.Key.Replace("DateFact", ""));
                            DateTime dateTime10 = args.PeriodStart.Value;
                            DateTime dateTime11 = DateManager.NextDate(dateTime10, args.TypeView);
                            for (int num15 = 0; num15 < num14; num15++)
                            {
                                dateTime10 = dateTime11;
                                dateTime11 = DateManager.NextDate(dateTime10, args.TypeView);
                            }
                            if (ds.Task.Start > dateTime10 && ds.Task.Start < dateTime11)
                            {
                                dateTime10 = ds.Task.Start;
                            }
                            if (ds.Task.Finish > dateTime10 && ds.Task.Finish < dateTime11)
                            {
                                dateTime10 = ds.Task.Finish;
                            }
                            DateTime dateTime12 = Convert.ToDateTime(DateTime.Now.ToShortDateString());
                            if (dateTime12 > dateTime10 && dateTime12 < dateTime11)
                            {
                                dateTime11 = dateTime12;
                            }
                            int num16 = DateManager.GetCountColumns(dateTime10, dateTime11, TypeViewDate.day) - 1;
                            double num17 = double.Parse(Convert.ToDouble(((Dictionary<string, object>)change.Value)["data"]).ToString());
                            int num18 = (int)(num17 * 100.0 / (double)num16) / 100;
                            double num19 = num17 - (double)(num18 * (num16 - 1));
                            for (int num20 = 0; num20 < num16; num20++)
                            {
                                ds.VolumeInDay.Add(new VolumeInDay
                                {
                                    Date = dateTime10.AddDays(num20),
                                    Volume = ((num20 == num16 - 1) ? num19 : ((double)num18))
                                });
                            }
                            List<VolumeInDay> volumeByDay2 = DbManager.GetVolumeByDay(ds.AssnUid);
                            double volumePlan3 = ds.Task.VolumePlan;
                            double volumeFact2 = 0.0;
                            volumeFact2 += ds.VolumeInDay.Sum(v => v.Volume);
                            volumeByDay2.ForEach(delegate (VolumeInDay vs)
                            {
                                if (!ds.VolumeInDay.Any(v => v.Date == vs.Date))
                                {
                                    volumeFact2 += vs.Volume;
                                }
                            });
                            ds.PercentCompleteWork = Convert.ToInt32(Math.Round(volumeFact2 * 100.0 / volumePlan3, 2));
                        }
                    });
                    if (ds.PercentCompleteWork > 100)
                    {
                        ds.PercentCompleteWork = 100;
                    }
                    dataToSave.Assns.Add(ds);
                });
                if (!dataToSave.Assns.Any())
                {
                    return;
                }
                try
                {
                    Dictionary<Guid, int> percentTasks = DbManager.GetPercentTasks(projUid);
                    dataToSave.Assns.ForEach(delegate (SubmittedApprovalGridInfo.Assn assn)
                    {
                        KeyValuePair<string, Dictionary<string, object>> keyValuePair = data.FirstOrDefault(an => Guid.Parse(an.Key) == assn.AssnUid);
                        int num = percentTasks[assn.TaskUid];
                        VolumeData volumeData = oldVolumesDB.FirstOrDefault(t => t.AssnUid == assn.AssnUid);
                        if (num == 0 && !keyValuePair.Value.ContainsKey("PercentCompleteWork"))
                        {
                            if (assn.FinishDate == DateTime.MinValue && volumeData != null)
                            {
                                assn.FinishDate = volumeData.FinishDate;
                            }
                            assn.VolumeInDay.ForEach(delegate (VolumeInDay v)
                            {
                                if (assn.FinishDate != DateTime.MinValue && assn.FinishDate < v.Date)
                                {
                                    assn.FinishDate = v.Date;
                                }
                            });
                            if (assn.FinishDate != DateTime.MinValue && assn.StartDate != DateTime.MinValue && !keyValuePair.Value.ContainsKey("Start") && volumeData != null)
                            {
                                assn.StartDate = volumeData.StartDate;
                            }
                        }
                    });
                    DbManager.SaveSubmittedApproval(dataToSave, user);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
            }
            catch (Exception ex2)
            {
                Logger.Log(ex2);
            }
        }

        public static void UpdateGridVolumes(List<AssnByDay> assnsByDays, List<TaskDb> tasks, Guid projUid)
        {
            try
            {
                VolumeContext ctx = new VolumeContext();
                try
                {
                    assnsByDays.ForEach(delegate (AssnByDay abd)
                    {
                        TaskDb taskDb = tasks.FirstOrDefault(t => t.AssnUid == abd.AssnUid);
                        if (taskDb != null)
                        {
                            bool flag = false;
                            VolumeData volumeData = ctx.VolumeDatas.FirstOrDefault(r => r.AssnUid == abd.AssnUid);
                            if (volumeData == null)
                            {
                                flag = true;
                                volumeData = new VolumeData
                                {
                                    AssnUid = abd.AssnUid,
                                    TaskUid = taskDb.TaskUid,
                                    ProjectUid = projUid,
                                    PercentCompleted = taskDb.PercentCompleteWork
                                };
                                if (taskDb.Start != DateTime.MinValue)
                                {
                                    volumeData.StartDate = taskDb.Start;
                                }
                                if (taskDb.Finish != DateTime.MinValue)
                                {
                                    volumeData.FinishDate = taskDb.Finish;
                                }
                                volumeData.Status = 1;
                                volumeData.Comments = "";
                                volumeData.TimeStamp = DateTime.Now;
                            }
                            if (flag)
                            {
                                ctx.VolumeDatas.Add(volumeData);
                            }
                            foreach (AssnByDay.DayFact item in abd.DaysFacts)
                            {
                                VolumeInDay volumeInDay = ctx.VolumeInDays.FirstOrDefault(v => v.AssnUid == abd.AssnUid && v.Date == item.Date);
                                if (volumeInDay == null || (volumeInDay.Status != 2 && volumeInDay.Status != 4))
                                {
                                    if (volumeInDay == null)
                                    {
                                        volumeInDay = new VolumeInDay
                                        {
                                            Uid = Guid.NewGuid(),
                                            AssnUid = abd.AssnUid,
                                            Date = new DateTime(item.Date.Year, item.Date.Month, item.Date.Day, 0, 0, 0, 0)
                                        };
                                        ctx.VolumeInDays.Add(volumeInDay);
                                    }
                                    volumeInDay.Volume = Math.Round(item.Fact, 5);
                                    volumeInDay.Status = 1;
                                    volumeInDay.TimeStamp = DateTime.Now;
                                }
                            }
                        }
                    });
                    DbManager.SaveDbExceptionLog(ctx);
                }
                finally
                {
                    if (ctx != null)
                    {
                        ((IDisposable)ctx).Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }
    }
}