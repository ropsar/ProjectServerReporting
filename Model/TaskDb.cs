using System;
using System.Collections.Generic;

namespace Legenda.ProjSpace.Main.Model
{

	public class TaskDb
	{
		public Guid? Parent;

		public Guid TaskUid;

		public Guid? AssnUid;

		public int TaskIndex;

		public string Name;

		public DateTime Start;

		public DateTime Finish;

		public List<DateTime> BaselineStart;

		public List<DateTime> BaselineFinish;

		public int OutlineLevel;

		public int PercentComplete;

		public int PercentCompleteCustom;

		public int PercentCompleteWork;

		public string WorkBreakdownStructure;

		public string KindWork;

		public double VolumePlan;

		public string Measure;

		public string Stage;

		public string Blok;

		public string Blok2;

		public string Capture;
		
		public string Comments;

		public bool IsSummary;

		public bool IsProjectSource;

		public double Work;

		public double ActualWork;
	}
}