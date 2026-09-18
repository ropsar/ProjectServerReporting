using System;
using System.Collections.Generic;

namespace Legenda.ProjSpace.Main.Model
{

	public class ApprovalWorkInfo
	{
		public Guid AssnUid { get; set; }

		public Guid TaskUid { get; set; }

		public DateTime? Start { get; set; }

		public DateTime? Finish { get; set; }

		public DateTime OriginalDateStart { get; set; }

		public DateTime OriginalDateFinish { get; set; }

		public int? PercentComplete { get; set; }

		public bool IsMaterial { get; set; }

		public Guid CalendarId { get; set; }

		public List<DateTime> ExceptionsWork { get; set; }
	}
}