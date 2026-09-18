using System;
using System.Collections.Generic;
using Legenda.ProjSpace.Main.Model;
using Legenda.ProjSpace.Main.Model.Enums;
using Microsoft.SharePoint.JSGrid;

namespace Legenda.ProjSpace.Main.Services
{
	public class CallbackArgs
	{
		public string Command;

		public Dictionary<string, Dictionary<string, object>> SubmittedApproval;

		public Dictionary<string, object> Properties;

		public string OrderByColumnName;

		public bool IsDescending;

		public Guid? ProjUid;

		public List<Guid> TaskUids;

		public List<string> KindWork;

		public List<string> Stages;

		public List<string> Bloks;

        public List<string> Bloks2;

        public List<string> Capture;

        public List<BasePlanProject> BasePlansProjects;

		public string ViewName;

		public List<FieldInfo> GridColumn;

		public int? BasePlan;

		public TypeViewDate TypeView;

		public PaneLayout Layouts;

		public PeriodType? Period;

		public string PeriodStartStr;

		public DateTime? PeriodStart;

		public string PeriodFinishStr;

		public DateTime? PeriodFinish;

		public string Height;

		public string Width;

		public List<Guid> Reject;

		public List<Guid> Accept;
	}
}