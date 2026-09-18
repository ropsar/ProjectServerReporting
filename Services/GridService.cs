using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using Legenda.ProjSpace.Main.Logging;
using Legenda.ProjSpace.Main.Model;
//using Legenda.ProjSpace.Core.Services.Logging;
using Microsoft.SharePoint.JSGrid;

namespace Legenda.ProjSpace.Main.Services
{

	public static class GridService
	{
		public enum CustomBarStyle
		{
			Summary,
			Standard,
			Milestone,
			PctComplete
		}

		public static PopulateGroupingRows _fnPopulateGroupingRows;

		public static string groupCollName;

		private static IList<GroupingNode> _slicedGeneratedRows = new List<GroupingNode>();

		public static GanttStyleInfo GetStyleInfo()
		{
            GanttStyleInfo ganttStyleInfo = new GanttStyleInfo();
			ganttStyleInfo.AddBarStyle(new GanttBarStyle(CustomBarStyle.Summary, BarShape.TopHalf, Color.Gray, BarPattern.Solid, BarEndShape.HomePlateDown, Color.Gray, BarShapePattern.Filled, BarEndShape.HomePlateDown, Color.Gray, BarShapePattern.Filled, "Start Date", "Finish Date", 1));
			ganttStyleInfo.AddBarStyle(new GanttBarStyle(CustomBarStyle.Standard, BarShape.Full, Color.Blue, BarPattern.Solid, BarEndShape.None, Color.Black, BarShapePattern.Filled, BarEndShape.None, Color.Black, BarShapePattern.Filled, "Start Date", "Finish Date", 1));
			ganttStyleInfo.AddBarStyle(new GanttBarStyle(CustomBarStyle.Milestone, BarShape.None, Color.Black, BarPattern.Solid, BarEndShape.None, Color.Black, BarShapePattern.Filled, BarEndShape.Diamond, Color.Black, BarShapePattern.Filled, "Finish Date", "Finish Date", 1));
			ganttStyleInfo.AddBarStyle(new GanttBarStyle(CustomBarStyle.PctComplete, BarShape.MidHalf, Color.Black, BarPattern.Solid, BarEndShape.None, Color.Black, BarShapePattern.Filled, BarEndShape.None, Color.Black, BarShapePattern.Filled, "Start Date", "CompleteThrough", 1));
			return ganttStyleInfo;
		}

		public static IList<GridColumn> GetGridColumns(DataTable table, GridData data)
		{
			List<GridColumn> list = new List<GridColumn>();
			try
			{
				table.DefaultView.ApplyDefaultSort = false;
				foreach (DataColumn iterator in table.Columns)
				{
					if (iterator.ColumnName != "Key" && iterator.ColumnName != "_GRIDROWSTYLEID" && iterator.ColumnName != "_GANTTBARSTYLEIDS" && iterator.ColumnName != "HierarchyParentKey")
					{
                        GridColumn gridColumn = new GridColumn
                        {
                            FieldKey = iterator.ColumnName,
                            Name = iterator.ColumnName,
                            Width = 130,
                            IsHidable = true,
                            IsVisible = false
                        };
                        FieldInfo fieldInfo = data.FieldsProp.FirstOrDefault(f => f.Name == iterator.ColumnName);
						if (fieldInfo != null)
						{
							gridColumn.Width = fieldInfo.Width;
							gridColumn.IsVisible = fieldInfo.IsVisible;
						}
						if (iterator.ColumnName == "Id")
						{
							gridColumn.Name = "ИД";
						}
						if (iterator.ColumnName == "Title")
						{
							gridColumn.Name = "Название задачи";
						}
						if (iterator.ColumnName == "KindWork")
						{
							gridColumn.Name = "Вид работ";
						}
						if (iterator.ColumnName == "PercentCompleted")
						{
							gridColumn.Name = "% завершения";
							gridColumn.IsVisible = false; 
                        }
						if (iterator.ColumnName == "PercentCompleteWork")
						{
							gridColumn.Name = "% завершения по трудозатратам";
						}
						if (iterator.ColumnName == "Start")
						{
							gridColumn.Name = "Начало работ";
						}
						if (iterator.ColumnName == "Finish")
						{
							gridColumn.Name = "Окончание работ";
						}
						if (iterator.ColumnName == "StartBasePlan")
						{
							gridColumn.Name = "Базовая дата начала";
						}
						if (iterator.ColumnName == "FinishBasePlan")
						{
							gridColumn.Name = "Базовая дата окончания";
						}
						if (iterator.ColumnName == "VolumePlan")
						{
							gridColumn.Name = "Объем работ (всего)";
						}
						if (iterator.ColumnName == "VolumeFact")
						{
							gridColumn.Name = "Объем работ (выполнено)";
						}
						if (iterator.ColumnName == "VolumeLeft")
						{
							gridColumn.Name = "Объем работ (осталось)";
						}
						if (iterator.ColumnName == "Measure")
						{
							gridColumn.Name = "Ед. изм.";
						}
						if (iterator.ColumnName == "Status")
						{
							gridColumn.Name = "Состояние процесса";
						}
						if (iterator.ColumnName == "Comments")
						{
							gridColumn.Name = "Примечание";
						}
						if (iterator.ColumnName == "Stage")
						{
							gridColumn.Name = "Этаж";
						}
						if (iterator.ColumnName == "Blok")
						{
							gridColumn.Name = "Блок";
						}
						if (iterator.ColumnName == "Blok2")
						{
							gridColumn.Name = "Блок2";
						}
                        if (iterator.ColumnName == "Capture") 
                        {
                            gridColumn.Name = "Захватки";
                        }
                        if (iterator.ColumnName == "IsTask" || iterator.ColumnName == "#" || iterator.ColumnName == "IsCommentDate" || iterator.ColumnName == "Type" || iterator.ColumnName == "IsNewTask" || iterator.ColumnName == "IsTotal")
						{
							gridColumn.IsHidable = true;
							gridColumn.IsVisible = false;
						}
						if (iterator.ColumnName == "i")
						{
							gridColumn.Width = 37;
							gridColumn.IsResizable = false;
							gridColumn.IsAutoFilterable = false;
						}
						if (iterator.ColumnName == "Status0")
						{
							gridColumn.Width = 200;
						}
						if (iterator.ColumnName == "#")
						{
							gridColumn.Width = 300;
						}
						if (iterator.ColumnName.StartsWith("Date"))
						{
							gridColumn.Width = 70;
						}
						if (iterator.ColumnName.StartsWith("_Date"))
						{
							gridColumn.Width = 70;
						}
						if (iterator.ColumnName == "WBS")
						{
							gridColumn.IsSortable = true;
							gridColumn.IsVisible = false;
						}
						if (iterator.ColumnName == "TaskUid")
						{
							gridColumn.IsSortable = true;
							gridColumn.IsVisible = false;
						}
						if (iterator.ColumnName.StartsWith("Desc"))
						{
							gridColumn.Width = 200;
							gridColumn.Name = "";
						}
						if (iterator.ColumnName == "Date0")
						{
							gridColumn.Width = 200;
						}
						gridColumn.IsSortable = true;
						list.Add(gridColumn);
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Log(ex);
			}
			return list;
		}

		public static void popGroupingRow(IEnumerable<GroupingNode> node)
		{
			try
			{
				foreach (GroupingNode item in node)
				{
					GroupBy groupBy = item.Criteria.FirstOrDefault();
					item.Row["IsTask"] = true;
					item.Row[groupBy.FieldKey] = groupBy.DataValue;
					int i;
					for (i = 1; i <= 5; i++)
					{
						item.Row["Many" + i] = item.Children.Sum(r => Convert.ToDouble(r.Row["Many" + i]));
					}
					double num = item.Children.Sum(r => Convert.ToDouble(r.Row["Date1"]));
					if (num > 0.0)
					{
						item.Row["Date1"] = num;
					}
					item.Row["DescPlan"] = "План";
					item.Row["DescLog"] = "Факт(ЛОГ)";
					item.Row["DescFact"] = "Факт(план-график)";
					item.Row["DescFore"] = "Прогноз";
				}
			}
			catch (Exception ex)
			{
				Logger.Log(ex);
			}
		}

		public static IList<GridField> GetGridFields(DataTable table, Properties.TypeForm typeForm)
		{
			List<GridField> list = new List<GridField>();
			foreach (DataColumn column in table.Columns)
			{
				GridField gf = new GridField();
				gf = formatGridField(gf, column, typeForm);
				list.Add(gf);
			}
			return list;
		}

		public static GridField formatGridField(GridField gf, DataColumn dc, Properties.TypeForm typeForm)
		{
			try
			{
				switch (typeForm)
				{
					case Properties.TypeForm.InsertValue:
						if (dc.ColumnName == "Start" || dc.ColumnName == "Finish")
						{
							gf.PropertyTypeId = "CustomDateTime";
						}
						if (dc.ColumnName.Contains("PercentCompleteWork") || dc.ColumnName == "Start" || dc.ColumnName == "Finish" || dc.ColumnName.Contains("DateFact") || dc.ColumnName.Contains("SumFact"))
						{
							gf.EditMode = EditMode.ReadWrite;
						}
						else
						{
							gf.EditMode = EditMode.ReadOnly;
						}
						break;
					case Properties.TypeForm.AgreeValue:
						gf.EditMode = EditMode.ReadOnly;
						break;
				}
				gf.FieldKey = dc.ColumnName;
				gf.SerializeDataValue = true;
				if (dc.ColumnName != "Key" && dc.ColumnName != "_GRIDROWSTYLEID" && dc.ColumnName != "_GANTTBARSTYLEIDS" && dc.ColumnName != "HierarchyParentKey")
				{
					if (dc.DataType == typeof(string))
					{
						gf.PropertyTypeId = "String";
                        gf.Localizer = (row, toConvert) => (toConvert == null) ? "" : toConvert.ToString();
						gf.SerializeLocalizedValue = true;
						gf.SerializeDataValue = false;
					}
					else if (dc.DataType == typeof(short) || dc.DataType == typeof(int) || dc.DataType == typeof(long) || dc.DataType == typeof(decimal) || dc.DataType == typeof(double))
					{
						gf.PropertyTypeId = "JSNumber";
						gf.Localizer = delegate (DataRow row, object toConvert)
						{
							if (gf.FieldKey.StartsWith("Date") && dc.DataType == typeof(double) && Convert.ToDouble(toConvert) != 0.0)
							{
								return Convert.ToDouble(toConvert).ToString("N2", CultureInfo.GetCultureInfo("ru-RU"));
							}
							if (gf.FieldKey.StartsWith("Many") && dc.DataType == typeof(double) && Convert.ToDouble(toConvert) != 0.0)
							{
								return Convert.ToDouble(toConvert).ToString("N2", CultureInfo.GetCultureInfo("ru-RU"));
							}
							return (dc.DataType == typeof(double) && Convert.ToDouble(toConvert) != 0.0) ? Convert.ToDouble(toConvert).ToString("N2", CultureInfo.GetCultureInfo("ru-RU")) : ((toConvert == null) ? "" : toConvert.ToString());
						};
						gf.DefaultCellStyleId = "TextRightAlign";
						gf.SerializeLocalizedValue = true;
						gf.SerializeDataValue = false;
					}
					else if (dc.DataType == typeof(Hyperlink))
					{
						gf.PropertyTypeId = "Hyperlink";
						gf.Localizer = (row, toConvert) => (toConvert == null) ? "" : toConvert.ToString();
						gf.SerializeLocalizedValue = false;
						gf.SerializeDataValue = true;
					}
					else if (dc.DataType == typeof(bool))
					{
						gf.PropertyTypeId = "CheckBoxBoolean";
						gf.SerializeDataValue = true;
						gf.SerializeLocalizedValue = false;
					}
					else if (dc.DataType == typeof(DateTime) && (dc.ColumnName == "Start" || dc.ColumnName == "Finish") && typeForm == Properties.TypeForm.InsertValue)
					{
						gf.PropertyTypeId = "CustomDateTime";
						gf.DateOnly = true;
						gf.Localizer = (row, toConvert) => (toConvert == null) ? "" : Convert.ToDateTime(toConvert).ToShortDateString();
						gf.SerializeDataValue = true;
						gf.SerializeLocalizedValue = true;
					}
					else
					{
						if (!(dc.DataType == typeof(DateTime)))
						{
							throw new Exception("No PropTypeId defined for this datatype" + dc.DataType);
						}
						gf.PropertyTypeId = "JSDateTime";
						gf.DateOnly = true;
						gf.Localizer = (row, toConvert) => (toConvert == null) ? "" : Convert.ToDateTime(toConvert).ToShortDateString();
						gf.SerializeDataValue = true;
						gf.SerializeLocalizedValue = true;
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Log(ex);
			}
			return gf;
		}
	}
}