using System;
using System.Collections.Generic;
using System.Linq;
using Legenda.ProjSpace.Main.Logging;
using Legenda.ProjSpace.Main.Model;
using Legenda.ProjSpace.Main.Model.Enums;
//using Legenda.ProjSpace.Core.Services.Logging;

namespace Legenda.ProjSpace.Main.Services
{
	public static class DateManager
	{
		public static void SetDate(CallbackArgs callbackArgs, ProjectDb currentProject)
		{
			DateTime? periodStart = callbackArgs.PeriodStart;
			DateTime? periodFinish = callbackArgs.PeriodFinish;
			if (periodStart.HasValue)
			{
				periodStart = Convert.ToDateTime(periodStart.Value.ToShortDateString());
			}
			if (periodFinish.HasValue)
			{
				periodFinish = Convert.ToDateTime(periodFinish.Value.ToShortDateString());
			}
			DateTime value = Convert.ToDateTime(DateTime.Now.ToShortDateString());
			if (callbackArgs.Period == PeriodType.CurrentWeek)
			{
				periodStart = value;
				while (periodStart.Value.DayOfWeek != DayOfWeek.Monday)
				{
					periodStart = periodStart.Value.AddDays(-1.0);
				}
				periodFinish = periodStart.Value.AddDays(6.0);
			}
			else if (callbackArgs.Period == PeriodType.CurrentMonth)
			{
				periodStart = new DateTime(value.Year, value.Month, 1);
				periodFinish = periodStart.Value.AddMonths(1).AddDays(-1.0);
			}
			else if (callbackArgs.Period == PeriodType.CurrentQuarter)
			{
				periodStart = new DateTime(value.Year, value.Month, 1);
				while (periodStart.Value.Month != 1 &&
					periodStart.Value.Month != 4 && 
					periodStart.Value.Month != 7 && 
					periodStart.Value.Month != 10)
				{
					periodStart = periodStart.Value.AddMonths(-1);
				}
				periodStart = new DateTime(periodStart.Value.Year, periodStart.Value.Month, 1);
				periodFinish = periodStart.Value.AddMonths(3).AddDays(-1.0);
			}
			else if (callbackArgs.Period == PeriodType.All)
			{
				periodStart = currentProject.Tasks.Min(t => t.Start);
				periodFinish = currentProject.Tasks.Max(t => t.Finish);
				if (periodStart.HasValue)
				{
					periodStart = Convert.ToDateTime(periodStart.Value.ToShortDateString());
				}
				if (periodFinish.HasValue)
				{
					periodFinish = Convert.ToDateTime(periodFinish.Value.ToShortDateString());
				}
			}
			if (callbackArgs.Period == PeriodType.All || callbackArgs.Period == PeriodType.Custom)
			{
				if (callbackArgs.TypeView == TypeViewDate.week)
				{
					while (periodStart.Value.DayOfWeek != DayOfWeek.Monday)
					{
						periodStart = periodStart.Value.AddDays(-1.0);
					}
					while (periodFinish.Value.DayOfWeek != DayOfWeek.Sunday)
					{
						periodFinish = periodFinish.Value.AddDays(1.0);
					}
				}
				else if (callbackArgs.TypeView == TypeViewDate.month)
				{
					periodStart = new DateTime(periodStart.Value.Year, periodStart.Value.Month, 1);
					periodFinish = periodFinish.Value.AddMonths(1).AddDays(-1.0);
				}
				else if (callbackArgs.TypeView == TypeViewDate.quarter)
				{
					while (periodStart.Value.Month != 1 && periodStart.Value.Month != 4 && periodStart.Value.Month != 7 && periodStart.Value.Month != 10)
					{
						periodStart = periodStart.Value.AddMonths(-1);
					}
					periodStart = new DateTime(periodStart.Value.Year, periodStart.Value.Month, 1);
					while (periodFinish.Value.Month != 3 && periodFinish.Value.Month != 6 && periodFinish.Value.Month != 9 && periodFinish.Value.Month != 12)
					{
						periodFinish = periodFinish.Value.AddMonths(1);
					}
					periodFinish = periodFinish.Value.AddMonths(1).AddDays(-1.0);
				}
				else if (callbackArgs.TypeView == TypeViewDate.year)
				{
					periodStart = new DateTime(periodStart.Value.Year, 1, 1);
					periodFinish = new DateTime(periodFinish.Value.Year, 12, 31);
				}
			}
			callbackArgs.PeriodStart = periodStart;
			callbackArgs.PeriodFinish = periodFinish;
		}

		public static int GetCountColumns(DateTime dateStart, DateTime dateFinish, TypeViewDate type)
		{
			int num = 1;
			switch (type)
			{
				case TypeViewDate.day:
					num += (dateFinish - dateStart).Days;
					break;
				case TypeViewDate.week:
					num += (dateFinish - dateStart).Days / 7;
					break;
				case TypeViewDate.month:
					{
						int num3 = 0;
						DateTime dateTime2 = dateStart;
						while (dateTime2 < dateFinish)
						{
							dateTime2 = dateTime2.AddMonths(1);
							num3++;
						}
						num = num3;
						break;
					}
				case TypeViewDate.quarter:
					{
						int num4 = 0;
						DateTime dateTime3 = dateStart;
						while (dateTime3 < dateFinish)
						{
							dateTime3 = dateTime3.AddMonths(3);
							num4++;
						}
						num = num4;
						break;
					}
				case TypeViewDate.year:
					{
						int num2 = 0;
						DateTime dateTime = dateStart;
						while (dateTime < dateFinish)
						{
							dateTime = dateTime.AddMonths(12);
							num2++;
						}
						num = num2;
						break;
					}
			}
			if (num > 1024)
			{
				return 1024;
			}
			return num;
		}

		public static DateTime NextDate(DateTime date, TypeViewDate type)
		{
            switch (type)            
            {
				case TypeViewDate.day: return date.AddDays(1.0);
				case TypeViewDate.week: return date.AddDays(7.0);
				case TypeViewDate.month: return date.AddMonths(1);
				case TypeViewDate.quarter: return date.AddMonths(3);
				case TypeViewDate.year: return date.AddYears(1);
				default: throw new Exception("Неправильно задан тип");
			};
		}

		public static string getColumnName(DateTime date, TypeViewDate type)
		{
			if (type == TypeViewDate.day || type == TypeViewDate.week)
			{
				return date.ToString("dd.MM.yy");
			}
            switch (type)            
            {
				case TypeViewDate.month: return date.ToString("MM.yy");
				case TypeViewDate.quarter: return $"Q{(date.Month - 1) / 3 + 1},{date.Year}";
				case TypeViewDate.year: return $"{date.Year}";
				default:  return "";
			};
		}

		/// <summary>
		/// Filter Tasks
		/// </summary>
		/// <param name="tasks"></param>
		/// <param name="callbackArgs"></param>
		/// <returns></returns>
		public static List<TaskDb> FilterTasksForPeriod(List<TaskDb> tasks, CallbackArgs callbackArgs)
		{
			try
			{
				if (callbackArgs.Period == PeriodType.All)
				{
					return tasks;
				}
				DateTime? periodStart = callbackArgs.PeriodStart;
				DateTime? periodFinish = callbackArgs.PeriodFinish;
				return tasks.Where((t) =>
				{
					if (t.Start != DateTime.MinValue && t.Finish != DateTime.MinValue)
					{
						if (periodStart <= t.Start)
						{
							if (t.Start <= periodFinish) return !t.IsSummary;
						}
						if (periodStart <= t.Finish)
						{
							if(t.Finish <= periodFinish) return !t.IsSummary;                   
                        }
						if (periodStart.HasValue && t.Start <= periodStart.GetValueOrDefault() && periodFinish <= t.Finish)
						{
                            return !t.IsSummary;
                        }
					}
					return false;
				}).ToList();
			}
			catch (Exception ex)
			{
				Logger.Log(ex, $"FilterTasksForPeriod.callbackArgs.Period:{callbackArgs.Period}");
			}
			return tasks;
		}
	}
}