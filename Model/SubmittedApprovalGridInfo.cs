using System;
using System.Collections.Generic;

namespace Legenda.ProjSpace.Main.Model
{

	public class SubmittedApprovalGridInfo
	{
		public class Assn
		{
			public Guid AssnUid;

			public Guid TaskUid;

			public int Status;

			public DateTime StartDate;

			public DateTime FinishDate;

			public int? PercentCompleteWork;

			public int? PercentCompleted;

			public double VolumePlan;

			public double VolumeFact;

			public TaskDb Task;

			public List<VolumeInDay> VolumeInDay = new List<VolumeInDay>();
		}

		public Guid ProjectGuid;

		public DateTime TimeStamp;

		public string UserLogin;

		public string UserName;

		public List<Assn> Assns;
	}
}