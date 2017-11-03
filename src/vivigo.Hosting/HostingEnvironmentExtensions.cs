﻿#region License

// The MIT License (MIT)
// 
// Copyright (c) 2017 SimpleSoft.pt
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

#endregion

using System;

using vivigo.Hosting.Exceptions;

namespace vivigo.Hosting
{
	/// <summary>
	///     Extensions for <see cref="IHostingEnvironment" /> instances.
	/// </summary>
	public static class HostingEnvironmentExtensions
	{
		/// <summary>
		///     Checks if the current hosting environment name matches the given string.
		///     The comparison is case insensitive.
		/// </summary>
		/// <param name="env">The hosting environment</param>
		/// <param name="environmentName">The environment name</param>
		/// <returns>True if the specified name is the same as the current environment, otherwise false.</returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="WhitespaceStringException"></exception>
		public static bool IsEnvironment(this IHostingEnvironment env, string environmentName)
		{
			if (env == null)
			{
				throw new ArgumentNullException(nameof(env));
			}
			if (environmentName == null)
			{
				throw new ArgumentNullException(nameof(environmentName));
			}
			if (string.IsNullOrWhiteSpace(environmentName))
			{
				throw new WhitespaceStringException(nameof(environmentName));
			}

			return environmentName.Equals(env.Name, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		///     Checks if the current hosting environment name is "Production".
		/// </summary>
		/// <param name="env">The hosting environment</param>
		/// <returns>True if the environment name is "Production", otherwise false.</returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static bool IsProduction(this IHostingEnvironment env)
		{
			return env.IsEnvironment(Constants.EnvironmentNameProduction);
		}

		/// <summary>
		///     Checks if the current hosting environment name is "Staging".
		/// </summary>
		/// <param name="env">The hosting environment</param>
		/// <returns>True if the environment name is "Staging", otherwise false.</returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static bool IsStaging(this IHostingEnvironment env)
		{
			return env.IsEnvironment(Constants.EnvironmentNameStaging);
		}

		/// <summary>
		///     Checks if the current hosting environment name is "Development".
		/// </summary>
		/// <param name="env">The hosting environment</param>
		/// <returns>True if the environment name is "Development", otherwise false.</returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static bool IsDevelopment(this IHostingEnvironment env)
		{
			return env.IsEnvironment(Constants.EnvironmentNameDevelopment);
		}
	}
}