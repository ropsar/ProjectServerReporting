using Legenda.ProjSpace.Main.Services;
using Legenda.ProjSpace.Main.Settings;
using System.Data.Entity;

namespace Legenda.ProjSpace.Main.Model.DB
{
    /// <summary>
    /// EF DbContext to WSS_Custom_PWA
    /// </summary>
    public class VolumeContext : DbContext
	{
		public virtual DbSet<VolumeData> VolumeDatas { get; set; }

		public virtual DbSet<VolumeInDay> VolumeInDays { get; set; }

		public virtual DbSet<UserSettings> UserSettingss { get; set; }

		public virtual DbSet<UserSettingsDefault> UserSettingsDefaults { get; set; }

		public virtual DbSet<UserSettingsBasicPlansProject> UserSettingsBasicPlansProjects { get; set; }

		public virtual DbSet<UserScale> UserScales { get; set; }

		public VolumeContext()
			: base(ProjSpaceSettings.Settings.CustomConnectionString)
		{
			base.Database.CommandTimeout = 180;
		}
	}
}