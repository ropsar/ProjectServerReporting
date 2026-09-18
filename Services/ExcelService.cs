using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using Legenda.ProjSpace.Main.Logging;
using Legenda.ProjSpace.Main.Model;
using Legenda.ProjSpace.Main.Model.Enums;
//using Legenda.ProjSpace.Core.Services.Logging;

namespace Legenda.ProjSpace.Main.Services
{
    public class ExcelService
    {
        public Properties.TypeForm _type;

        public Stream ExportProjectToExcel(string loginName, CallbackArgs filter)
        {
            return ExportProjectToExcelStream(loginName, filter);
        }

        private Stream ExportProjectToExcelStream(string loginName, CallbackArgs filter)
        {
            try
            {
                List<TaskDb> projectTasks = GetProjectTasks(loginName, filter);
                string text = filter.ViewName;
                if (string.IsNullOrEmpty(text))
                {
                    text = "Все задачи";
                }
                List<FieldInfo> fieldsForUser = DbManager.GetFieldsForUser(loginName, text);
                return GenerateExcelFileFromTemplate(projectTasks, fieldsForUser);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        private Stream GenerateExcelFileFromTemplate(List<TaskDb> tasks, List<FieldInfo> fields)
        {
            try
            {
                MemoryStream memoryStream = new MemoryStream();
                using (XLWorkbook xLWorkbook = new XLWorkbook())
                {
                    IXLWorksheet worksheet = xLWorkbook.Worksheets.Add("Проект");
                    worksheet = FillExcelTemplate(worksheet, tasks, fields);
                    xLWorkbook.SaveAs(memoryStream);
                    memoryStream.Seek(0L, SeekOrigin.Begin);
                }
                return memoryStream;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        private IXLWorksheet FillExcelTemplate(IXLWorksheet worksheet, List<TaskDb> tasks, List<FieldInfo> fields)
        {
            try
            {
                int rowCounter = 0;
                int headRow = 1;
                string emptyBeginString = string.Empty;
                tasks = tasks.OrderBy(t => t.TaskIndex).ToList();
                tasks.ForEach(delegate (TaskDb task)
                {
                    int num = 0;
                    for (int i = 0; i < fields.Count; i++)
                    {
                        FieldInfo fieldInfo = fields[i];
                        if (fieldInfo.IsVisible)
                        {
                            num++;
                            switch (fieldInfo.Name)
                            {
                                case "Id":
                                    if (rowCounter == 0)
                                    {
                                        DrawHeaderCell(worksheet, headRow, num, "ИД", 5);
                                    }
                                    DrawValueCell(worksheet, headRow + rowCounter + 1, num, task.TaskIndex.ToString(), false, XLAlignmentHorizontalValues.Right);
                                    break;
                                case "Title":
                                    {
                                        if (rowCounter == 0)
                                        {
                                            DrawHeaderCell(worksheet, headRow, num, "Название задачи", 50);
                                        }
                                        emptyBeginString = string.Empty;
                                        for (int num3 = 0; num3 < task.OutlineLevel; num3++)
                                        {
                                            emptyBeginString += "          "; // ExcelTasksDelimeter
                                        }
                                        bool isFontBold = false;
                                        if (task.TaskIndex == 0)
                                        {
                                            isFontBold = true;
                                        }
                                        DrawValueCell(worksheet, headRow + rowCounter + 1, num, emptyBeginString + task.Name, isFontBold, XLAlignmentHorizontalValues.Left, null, XLDataType.Text);
                                        break;
                                    }
                                case "KindWork":
                                    if (rowCounter == 0)
                                    {
                                        DrawHeaderCell(worksheet, headRow, num, "Вид работ", 20);
                                    }
                                    DrawValueCell(worksheet, headRow + rowCounter + 1, num, task.KindWork, false, XLAlignmentHorizontalValues.Right, null, XLDataType.Text);
                                    break;
                                case "PercentCompleted":
                                    if (rowCounter == 0)
                                    {
                                        DrawHeaderCell(worksheet, headRow, num, "% завершения", 10);
                                    }
                                    DrawValueCell(worksheet, headRow + rowCounter + 1, num, task.PercentComplete.ToString(), false, XLAlignmentHorizontalValues.Right);
                                    break;
                                case "PercentCompleteWork":
                                    if (rowCounter == 0)
                                    {
                                        DrawHeaderCell(worksheet, headRow, num, "% завершения по трудозатратам", 22);
                                    }
                                    DrawValueCell(worksheet, headRow + rowCounter + 1, num, task.PercentCompleteWork.ToString(), false, XLAlignmentHorizontalValues.Right);
                                    break;
                                case "Start":
                                    if (rowCounter == 0)
                                    {
                                        DrawHeaderCell(worksheet, headRow, num, "Начало работ", 12);
                                    }
                                    DrawValueCell(worksheet, headRow + rowCounter + 1, num, task.Start.ToString(), false, XLAlignmentHorizontalValues.Right, "dd.mm.yyyy");
                                    break;
                                case "Finish":
                                    if (rowCounter == 0)
                                    {
                                        DrawHeaderCell(worksheet, headRow, num, "Окончание работ", 12);
                                    }
                                    DrawValueCell(worksheet, headRow + rowCounter + 1, num, task.Finish.ToString(), false, XLAlignmentHorizontalValues.Right, "dd.mm.yyyy");
                                    break;
                                case "VolumePlan":
                                    if (rowCounter == 0)
                                    {
                                        DrawHeaderCell(worksheet, headRow, num, "Объем работ(всего)", 20);
                                    }
                                    DrawValueCell(worksheet, headRow + rowCounter + 1, num, task.VolumePlan.ToString(), false, XLAlignmentHorizontalValues.Right, "0.00", XLDataType.Number);
                                    break;
                                case "VolumeFact":
                                    {
                                        if (rowCounter == 0)
                                        {
                                            DrawHeaderCell(worksheet, headRow, num, "Объем работ(выполнено)", 20);
                                        }
                                        string value = string.Empty;
                                        if (task.AssnUid.HasValue)
                                        {
                                            List<VolumeInDay> volumeByDay = DbManager.GetVolumeByDay(task.AssnUid.Value);
                                            value = volumeByDay.Sum(v => v.Volume).ToString();
                                        }
                                        DrawValueCell(worksheet, headRow + rowCounter + 1, num, value, false, XLAlignmentHorizontalValues.Right, "0.00", XLDataType.Number);
                                        break;
                                    }
                                case "VolumeLeft":
                                    {
                                        if (rowCounter == 0)
                                        {
                                            DrawHeaderCell(worksheet, headRow, num, "Объем работ(Осталось)", 20);
                                        }
                                        string value2 = string.Empty;
                                        if (task.AssnUid.HasValue)
                                        {
                                            List<VolumeInDay> volumeByDay2 = DbManager.GetVolumeByDay(task.AssnUid.Value);
                                            double num2 = volumeByDay2.Sum(v => v.Volume);
                                            value2 = (task.VolumePlan - num2).ToString();
                                        }
                                        DrawValueCell(worksheet, headRow + rowCounter + 1, num, value2, false, XLAlignmentHorizontalValues.Right, "0.00", XLDataType.Number);
                                        break;
                                    }
                                case "Measure":
                                    if (rowCounter == 0)
                                    {
                                        DrawHeaderCell(worksheet, headRow, num, "Ед. изм.", 10);
                                    }
                                    DrawValueCell(worksheet, headRow + rowCounter + 1, num, task.Measure, false, XLAlignmentHorizontalValues.Right, null, XLDataType.Text);
                                    break;
                                case "Status":
                                    {
                                        if (rowCounter == 0)
                                        {
                                            DrawHeaderCell(worksheet, headRow, num, "Состояние процесса", 20);
                                        }
                                        string value3 = string.Empty;
                                        if (task.AssnUid.HasValue)
                                        {
                                            VolumeData volumeData = DbManager.GetVolumeData(task.AssnUid.Value);
                                            value3 = ((volumeData != null) ? ((StatusTask)volumeData.Status).GetEnumDescription() : "");
                                        }
                                        DrawValueCell(worksheet, headRow + rowCounter + 1, num, value3, false, XLAlignmentHorizontalValues.Right, null, XLDataType.Text);
                                        break;
                                    }
                                case "Stage":
                                    if (rowCounter == 0)
                                    {
                                        DrawHeaderCell(worksheet, headRow, num, "Этаж", 10);
                                    }
                                    DrawValueCell(worksheet, headRow + rowCounter + 1, num, task.Stage, false, XLAlignmentHorizontalValues.Right, null, XLDataType.Text);
                                    break;
                                case "Blok":
                                    if (rowCounter == 0)
                                    {
                                        DrawHeaderCell(worksheet, headRow, num, "Блок", 10);
                                    }
                                    DrawValueCell(worksheet, headRow + rowCounter + 1, num, task.Blok, false, XLAlignmentHorizontalValues.Right, null, XLDataType.Text);
                                    break;
                                case "Blok2":
                                    if (rowCounter == 0)
                                    {
                                        DrawHeaderCell(worksheet, headRow, num, "Блок 2", 10);
                                    }
                                    DrawValueCell(worksheet, headRow + rowCounter + 1, num, task.Blok2, false, XLAlignmentHorizontalValues.Right, null, XLDataType.Text);
                                    break;
                                case "Capture":
                                    if (rowCounter == 0)
                                    {
                                        DrawHeaderCell(worksheet, headRow, num, "Захватки", 10);
                                    }
                                    DrawValueCell(worksheet, headRow + rowCounter + 1, num, task.Capture, false, XLAlignmentHorizontalValues.Right, null, XLDataType.Text);
                                    break;
                                case "Comments":
                                    if (rowCounter == 0)
                                    {
                                        DrawHeaderCell(worksheet, headRow, num, "Примечание", 50);
                                    }
                                    DrawValueCell(worksheet, headRow + rowCounter + 1, num, task.Comments, false, XLAlignmentHorizontalValues.Right, null, XLDataType.Text);
                                    break;
                            }
                        }
                    }
                    rowCounter++;
                });
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return worksheet;
        }

        private List<TaskDb> GetProjectTasks(string loginName, CallbackArgs filter)
        {
            ProjectDb project = new ProjectDb();
            Guid guid = filter.ProjUid ?? Guid.Empty;
            if (guid == Guid.Empty)
            {
                return null;
            }
            if (_type == Properties.TypeForm.InsertValue)
            {
                project.Tasks = DbManager.GetAllAssnForUser(loginName, guid, Properties.TypeForm.InsertValue);
            }
            else
            {
                project.Tasks = DbManager.GetAllAssnForUserStatusManager(loginName, guid);
            }
            if (filter.TaskUids != null && filter.TaskUids.Any())
            {
                List<TaskDb> newTasks = new List<TaskDb>();
                filter.TaskUids.ForEach(delegate (Guid tUid)
                {
                    TaskDb taskDb = project.Tasks.FirstOrDefault(t => tUid == t.TaskUid);
                    newTasks.Add(taskDb);
                    List<TaskDb> parentTasks = DbManager.GetParentTasks(project.Tasks, taskDb);
                    List<TaskDb> childTasks = DbManager.GetChildTasks(project.Tasks, taskDb);
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
                project.Tasks = newTasks;
            }
            List<TaskDb> AllTasks = project.Tasks;
            if (filter.KindWork != null && filter.KindWork.Any())
            {
                project.Tasks = project.Tasks.Where(t => filter.KindWork.Contains(t.KindWork)).ToList();
            }
            if (filter.Stages != null && filter.Stages.Any())
            {
                project.Tasks = project.Tasks.Where(t => filter.Stages.Contains(t.Stage)).ToList();
            }
            if (filter.Bloks != null && filter.Bloks.Any())
            {
                project.Tasks = project.Tasks.Where(t => filter.Bloks.Contains(t.Blok)).ToList();
            }
            List<TaskDb> AllParentTask = new List<TaskDb>();
            if (filter.KindWork.Any() || filter.Stages.Any() || filter.Bloks.Any())
            {
                project.Tasks.ForEach(delegate (TaskDb t)
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
                project.Tasks.AddRange(AllParentTask);
            }
            return project.Tasks;
        }

        private void DrawHeaderCell(IXLWorksheet worksheet, int coordX, int coordY, string headerName, int columnWidth)
        {
            IXLColumns iXLColumns = worksheet.Columns(coordY, coordY);
            iXLColumns.Width = columnWidth;
            IXLCell iXLCell = worksheet.Cell(coordX, coordY);
            iXLCell.Value = headerName;
            iXLCell.Style.Font.Bold = true;
            iXLCell.Style.Font.FontName = "Calibri";
            iXLCell.Style.Font.FontSize = 8.0;
            iXLCell.Style.Font.FontColor = XLColor.White;
            iXLCell.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
            iXLCell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            iXLCell.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#4169E1"));
            iXLCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            iXLCell.Style.Border.OutsideBorderColor = XLColor.Black;
        }

        private void DrawValueCell(IXLWorksheet worksheet, int coordX, int coordY, string value, bool isFontBold, XLAlignmentHorizontalValues horizontalAligment, string numberFormat = null, XLDataType? xLDataType = null)
        {
            IXLCell iXLCell = worksheet.Cell(coordX, coordY);
            if (numberFormat != null)
            {
                iXLCell.Style.NumberFormat.Format = numberFormat;
            }
            if (xLDataType.HasValue)
            {
                iXLCell.SetDataType(xLDataType.Value);
                iXLCell.SetValue(value);
            }
            else
            {
                iXLCell.Value = value;
            }
            iXLCell.Style.Font.Bold = isFontBold;
            iXLCell.Style.Font.FontName = "Calibri";
            iXLCell.Style.Font.FontSize = 8.0;
            iXLCell.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
            iXLCell.Style.Alignment.SetHorizontal(horizontalAligment);
            iXLCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            iXLCell.Style.Border.OutsideBorderColor = XLColor.Black;
        }
    }
}