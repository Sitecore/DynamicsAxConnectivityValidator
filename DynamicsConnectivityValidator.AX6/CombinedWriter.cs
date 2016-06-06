// ------------------------------------------------------------------------------------------
// <copyright file="CombinedWriter.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2016 
// </copyright>
// ------------------------------------------------------------------------------------------
// Copyright 2016 Sitecore Corporation A/S
// Licensed under the SITECORE SHARED SOURCE LICENSE, you may not use this file 
// except in compliance with the License. You may obtain a copy of the License at
//       https://marketplace.sitecore.net/Shared_Source_License.aspx
// -------------------------------------------------------------------------------------------

namespace DynamicsConnectivityValidator
{
    using System.IO;
    using System.Text;

    /// <summary>
    /// The StremWriter to write to file and console
    /// </summary>   
    public class CombinedWriter : StreamWriter
    {
        readonly TextWriter _console;

        /// <summary>
        /// Initializes a new instance of the <see cref="CombinedWriter"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="append">if set to <c>true</c> [append].</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="bufferSize">Size of the buffer.</param>
        /// <param name="console">The console.</param>
        public CombinedWriter(string path, bool append, Encoding encoding, int bufferSize, TextWriter console)
            : base(path, append, encoding, bufferSize)
        {
            this._console = console;
            base.AutoFlush = true;
        }

        /// <summary>
        /// Writes a string to the stream.
        /// </summary>
        /// <param name="value">The string to write to the stream. If <paramref name="value" /> is null, nothing is written.</param>
        public override void Write(string value)
        {
            _console.WriteLine(value);
            base.WriteLine(value);
        }
    }
}
