using System;
using System.Collections.Generic;
using System.Text;

namespace MMDB.Core.DBBuild
{
    public class DBBuildException : Exception
    {
        public DBBuildException(string message) : base(message) { }
        public DBBuildException(string message, Exception ex) : base(message, ex) { }
        public DBBuildException() : base() { }

    }
}
