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

using Microsoft.Extensions.DependencyInjection;

using vivigo.Hosting.Params;

namespace vivigo.Hosting
{
	/// <inheritdoc />
	public abstract class HostStartup : IHostStartup
	{
		/// <inheritdoc />
		public virtual void ConfigureConfigurationBuilder(IConfigurationBuilderParam param)
		{
		}

		/// <inheritdoc />
		public virtual void ConfigureConfiguration(IConfigurationHandlerParam param)
		{
		}

		/// <inheritdoc />
		public virtual void ConfigureLoggerFactory(ILoggerFactoryHandlerParam param)
		{
		}

		/// <inheritdoc />
		public virtual void ConfigureServiceCollection(IServiceCollectionHandlerParam param)
		{
		}

		/// <inheritdoc />
		public virtual IServiceProvider BuildServiceProvider(IServiceProviderBuilderParam param)
		{
			return param.ServiceCollection.BuildServiceProvider();
		}

		/// <inheritdoc />
		public virtual void Configure(IConfigureHandlerParam param)
		{
		}
	}
}