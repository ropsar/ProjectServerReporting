using System;
using System.Collections.Generic;
using Legenda.ProjSpace.Main.Model.Enums;
using Microsoft.SharePoint.JSGrid;

namespace Legenda.ProjSpace.Main.Model
{

	public class OutGrid
	{
		public string gridS;

		public List<string> listRoles;

		public string stage;

		public List<WorkTeams> volumeProjectTeams;

		public List<CommentGrid> commentGrid;

		public List<BasePlanProject> BasePlansProjects;

		public bool isAddAvailable;

		public string userName;

		public string startPeriod;

		public string reportMonth;

		public List<TaskBaseInfo> SumTaskNames = new List<TaskBaseInfo>();

		public List<ProjectDb> Projects;

		public Dictionary<string, string> KindWork;

		public List<string> Stages;

		public List<string> Bloks;

        public List<string> Bloks2;

        public List<string> Capture;

        public Guid? ProjUid;

		public List<Guid> TaskUids;

		public List<string> CurrentKindWork;

		public List<string> CurrentStages;

		public List<string> CurrentBloks;

        public List<string> CurrentBloks2;

        public List<string> CurrentCapture;

        public string ViewName;

		public int? BasePlan;

		public List<int> BasePlans;

		public List<string> Views;

		public TypeViewDate TypeView;

		public PaneLayout Layouts;

		public PeriodType? Period;

		public DateTime? PeriodStart;

		public DateTime? PeriodFinish;
	}
}