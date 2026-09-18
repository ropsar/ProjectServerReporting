using System.Configuration;

namespace Legenda.ProjSpace.Main.Services
{

	public static class ConfigManager
	{
		public static string GetConfigSetting(string name)
		{
			var confPath = "C:\\Program Files\\Common Files\\microsoft shared\\Web Server Extensions\\16\\ISAPI\\Legenda\\ProjSpace\\web.config";
            ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap
			{
				ExeConfigFilename = confPath
            };
			Configuration configuration = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
			return configuration.AppSettings.Settings[name].Value;
		}
	}
}