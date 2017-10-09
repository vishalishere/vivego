using System;

using Orleans.Providers;

namespace vivego.Orleans.Providers.Common
{
	public static class ProviderConfigurationExtensions
	{
		public static int GetParamInt(this IProviderConfiguration config, string paramName)
		{
			if (!config.Properties.TryGetValue(paramName, out string paramValuePreParsed))
			{
				throw new ArgumentException($"{paramName} could not be resolved from configuration");
			}

			if (!int.TryParse(paramValuePreParsed, out int paramValue))
			{
				throw new ArgumentException($"{paramName} invalid. Must be int");
			}

			return paramValue;
		}

		public static string GetParamString(this IProviderConfiguration config, string paramName)
		{
			if (!config.Properties.TryGetValue(paramName, out string paramValuePreParsed))
			{
				throw new ArgumentException($"{paramName} could not be resolved from configuration");
			}

			return paramValuePreParsed;
		}

		public static string GetParamString(this IProviderConfiguration config, string paramName, string defaultValue)
		{
			if (!config.Properties.TryGetValue(paramName, out string paramValuePreParsed))
			{
				return defaultValue;
			}

			return paramValuePreParsed;
		}

		public static bool GetOptionalParamBool(this IProviderConfiguration config, string paramName, bool defaultValue)
		{
			bool paramValue = defaultValue;
			if (!config.Properties.TryGetValue(paramName, out string paramValuePreParsed))
			{
				return paramValue;
			}

			if (!bool.TryParse(paramValuePreParsed, out paramValue))
			{
				throw new ArgumentException($"{paramName} invalid. Must be bool");
			}

			return paramValue;
		}

		public static int GetOptionalParamInt(this IProviderConfiguration config, string paramName, int defaultValue)
		{
			int paramValue = defaultValue;
			if (!config.Properties.TryGetValue(paramName, out string paramValuePreParsed))
			{
				return paramValue;
			}

			if (!int.TryParse(paramValuePreParsed, out paramValue))
			{
				throw new ArgumentException($"{paramName} invalid. Must be int");
			}

			return paramValue;
		}

		public static TimeSpan GetOptionalParamTimeSpan(this IProviderConfiguration config,
			string paramName,
			TimeSpan defaultValue)
		{
			TimeSpan paramValue = defaultValue;
			if (!config.Properties.TryGetValue(paramName, out string paramValuePreParsed))
			{
				return paramValue;
			}

			if (!TimeSpan.TryParse(paramValuePreParsed, out paramValue))
			{
				throw new ArgumentException($"{paramName} invalid. Must be TimeSpan");
			}

			return paramValue;
		}
	}
}