using Ancestry.QueryProcessor.Storage;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace Ancestry.QueryProcessor.Service
{
	public class QueryConfig
	{
		private static ProcessorSettings _settings;

		public static ProcessorSettings Settings
		{
			get
			{
				if (_settings == null)
					_settings = GetSettings();
				return _settings;
			}
		}

		private static ProcessorSettings GetSettings()
		{
			var configuration = (QueryConfiguration)ConfigurationManager.GetSection("query");
			if (configuration == null)
				throw new Exception("'query' configuration section not configured.");

			var factoryType = System.Type.GetType(configuration.FactoryClass);
			var factory = (IRepositoryFactory)Activator.CreateInstance(factoryType);

			var settings = new ProcessorSettings { RepositoryFactory = factory };

			for (int i = 0; i < configuration.DefaultUsings.Count; i++)
			{
				var u = configuration.DefaultUsings[i];
				settings.DefaultOptions.DefaultUsings.Add(new Parse.Using { Target = Name.FromNative(u.Name).ToQualifiedIdentifier(), Version = Version.Parse(u.Version) });
			}

			// TODO: load more settings from configuration
			return settings;
		}
	}
}