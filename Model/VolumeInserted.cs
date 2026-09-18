using Legenda.ProjSpace.Main.Logging;
using System;
using System.Data.SqlClient;
using System.Reflection;

//using Legenda.ProjSpace.Core.Services.Logging;

namespace Legenda.ProjSpace.Main.Model
{

	public class VolumeInserted : VolumeUpdate
	{
		public Guid GuidProject { get; set; }

		public int NumberWeek { get; set; }

		public string NameProject { get; set; }

		public string NameTask { get; set; }

		public string Measure { get; set; }

		public double ActualValue { get; set; }

		public double RejectedValue { get; set; }

		public double ApprovedValue { get; set; }

		public DateTime TimeStamp { get; set; }

		public string UserLogin { get; set; }

		public string UserName { get; set; }

		public string CommentPlan { get; set; }

		public string CommentBase { get; set; }

		public string Comment { get; set; }

		public void ExecuteInsert(SqlConnection conn)
		{
			Type type = GetType();
			PropertyInfo[] properties = type.GetProperties();
			string text = "";
			string text2 = "";
			PropertyInfo[] array = properties;
			foreach (PropertyInfo propertyInfo in array)
			{
				text = text + propertyInfo.Name + ", ";
				text2 = text2 + "@" + propertyInfo.Name + ", ";
			}
			text = text.Remove(text.Length - 2);
			text2 = text2.Remove(text2.Length - 2);
			string text3 = string.Format("INSERT INTO {0} ({1}) VALUES ({2})", "VolumeData", text, text2);
			Logger.Log("[sqlQuery]:" + text3);
			SqlCommand sqlCommand = new SqlCommand(text3, conn);
			sqlCommand.CommandTimeout = 500;
			PropertyInfo[] array2 = properties;
			foreach (PropertyInfo propertyInfo2 in array2)
			{
				sqlCommand.Parameters.AddWithValue("@" + propertyInfo2.Name, propertyInfo2.GetValue(this));
			}
			try
			{
				int num = sqlCommand.ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				Logger.Log(ex);
			}
		}
	}
}