using System;

namespace Glimpse.Package
{
    public class ConfigProcessor : IConfigProcessor
    {
        private readonly IConfigProvider _configProvider;

        public ConfigProcessor(IConfigProvider configProvider)
        {
            _configProvider = configProvider; 
        }

        public void Process(ISettings settings)
        {
            var config = RetrieveConfig();
            if (config != null)
            { 
                if (config.Debug.HasValue)
                    settings.Debug = config.Debug.GetValueOrDefault();

                var logging = config.Logging;
                if (logging != null)
                {
                    if (logging.Enabled.HasValue)
                        settings.LoggingEnabled = logging.Enabled.GetValueOrDefault();
                    if (logging.LogEverything.HasValue)
                        settings.LogEverything = logging.LogEverything.GetValueOrDefault();
                    if (!String.IsNullOrEmpty(logging.LoggingPath))
                        settings.LoggingPath = logging.LoggingPath;
                }
                
                if (String.IsNullOrEmpty(settings.LoggingPath))
                    settings.LoggingPath = @".\logging.xml";

                var services = config.Services;
                if (services != null)
                {
                    if (services.MinTriggerInterval.HasValue)
                        settings.MinServiceTriggerInterval = services.MinTriggerInterval.GetValueOrDefault(); 
                }

                var dataSource = config.DataSource;
                if (dataSource != null)
                {
                    if (dataSource.DisabledAutoBuild.HasValue)
                        settings.DisableAutoBuild = dataSource.DisabledAutoBuild.GetValueOrDefault();
                }
            }
        }

        protected ConfigSectionGlimpse RetrieveConfig()
        {
            return _configProvider.GetSection<ConfigSectionGlimpse>("glimpsePackage");
        }
    }
}