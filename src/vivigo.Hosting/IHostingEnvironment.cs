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

using Microsoft.Extensions.FileProviders;

namespace vivigo.Hosting
{
	/// <summary>
	///     Provides information about the hosting environment in which the
	///     application is running.
	/// </summary>
	public interface IHostingEnvironment
	{
		/// <summary>
		///     The environment name.
		/// </summary>
		string Name { get; }

		/// <summary>
		///     The application name.
		/// </summary>
		string ApplicationName { get; }

		/// <summary>
		///     The <see cref="IFileProvider" /> pointing at <see cref="ContentRootPath" />.
		/// </summary>
		IFileProvider ContentRootFileProvider { get; }

		/// <summary>
		///     The absolute path to the directory that contains the
		///     application files.
		/// </summary>
		string ContentRootPath { get; }
	}
}