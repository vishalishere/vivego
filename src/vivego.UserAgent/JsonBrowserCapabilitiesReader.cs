using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace vivego.UserAgent
{
	public class JsonBrowserCapabilitiesReader
	{
		public static IEnumerable<BrowserCapabilities> Read(string fileName)
		{
			if (fileName == null)
			{
				throw new ArgumentNullException(nameof(fileName));
			}

			if (!File.Exists(fileName))
			{
				throw new FileNotFoundException("File not found", fileName);
			}

			using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
			{
				return Read(fileStream);
			}
		}

		public static IEnumerable<BrowserCapabilities> Read(Stream stream)
		{
			if (stream == null)
			{
				throw new ArgumentNullException(nameof(stream));
			}

			return ReadJson(stream);
		}

		public static IEnumerable<BrowserCapabilities> ReadJson(Stream stream)
		{
			if (stream == null)
			{
				throw new ArgumentNullException(nameof(stream));
			}

			List<BrowserCapabilities> browserCapabilities = new List<BrowserCapabilities>();
			IDictionary<string, BrowserCapabilities> browserCapabilitiesLookup = new Dictionary<string, BrowserCapabilities>();

			JsonSerializer serializer = new JsonSerializer();
			using (StreamReader sr = new StreamReader(stream))
			using (JsonReader reader = new JsonTextReader(sr))
			{
				// Position reader to first object
				while (reader.Read())
				{
					// deserialize only when there's "{" character in the stream
					if (reader.TokenType == JsonToken.StartObject)
					{
						break;
					}
				}

				while (reader.Read())
				{
					switch (reader.TokenType)
					{
						case JsonToken.StartObject:
							serializer.Deserialize<JObject>(reader);
							break;
						case JsonToken.PropertyName when !reader.Path.Equals("comments", StringComparison.OrdinalIgnoreCase) && !reader.Path.Equals("GJK_Browscap_Version", StringComparison.OrdinalIgnoreCase):
							BrowserCapabilities browserCapability = new BrowserCapabilities {Pattern = reader.Value.ToString()};
							if (reader.Read())
							{
								string value = reader.Value.ToString();
								using (StringReader localSr = new StringReader(value))
								using (JsonReader localReader = new JsonTextReader(localSr))
								{
									JObject properties = serializer.Deserialize<JObject>(localReader);
									browserCapability.Properties = properties
										.OfType<JProperty>()
										.ToDictionary(jProperty => jProperty.Path, jProperty => jProperty.Value.ToString());

									BrowserCapabilities capability = browserCapability;
									while (!string.IsNullOrEmpty(capability.Parent)
										&& browserCapabilitiesLookup.TryGetValue(capability.Parent, out capability))
									{
										browserCapability.Merge(capability);
									}
									
									browserCapabilities.Add(browserCapability);
									browserCapabilitiesLookup.Add(browserCapability.Pattern, browserCapability);
								}

								yield return browserCapability;
							}

							break;
					}
				}
			}
		}
	}
}