using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MMDB.Core.DBBuild
{
    public class ScriptingException : Exception
    {
        public ScriptingException(string message, Exception err) : base(message, err)
        {
        }
    }
    public class PartitionSchemeScriptingException : Exception
    {
        public PartitionSchemeScriptingException(string message, Exception err)
            : base(message, err)
        {
        }
    }
    public class PartitionFunctionScriptingException : Exception
    {
        public PartitionFunctionScriptingException(string message, Exception err)
            : base(message, err)
        {
        }
    }

    public class IndexScriptingException : ScriptingException
    {
        public IndexScriptingException(string message, Exception err) : base(message, err)
        {
        }
    }
    public class ForeignKeyScriptingException : ScriptingException
    {
        public ForeignKeyScriptingException(string message, Exception err) : base(message, err)
        {
        }
    }
    public class TableScriptingException : Exception
    {
        public TableScriptingException(string message, Exception err) : base(message, err)
        {
        }
    }
    public class ViewScriptingException : ScriptingException
    {
        public ViewScriptingException(string message, Exception err) : base(message, err)
        {
        }
    }
    public class StoredProcedureScriptingException : ScriptingException
    {
        public StoredProcedureScriptingException(string message, Exception err)
            : base(message, err)
        {
        }
    }
    public class FunctionScriptingException : ScriptingException
    {
        public FunctionScriptingException(string message, Exception err) : base(message, err)
        {
        }
    }
    public class TriggerScriptingException : ScriptingException
    {
        public TriggerScriptingException(string message, Exception err) : base(message, err)
        {
        }
    }
    public class DataScriptingException : ScriptingException
    {
        public DataScriptingException(string message, Exception err) : base(message, err)
        {
        }
    }
}
