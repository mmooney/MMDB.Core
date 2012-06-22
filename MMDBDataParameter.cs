using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace MMDB.Core
{
    public class MMDBDataParameter
    {
        public string ParameterName { get; set; }
        public DbType DBType { get; set; }
        public ParameterDirection Direction { get; set; }
        public string ParameterValue { get; set; }
        public int ParameterSize { get; set; }
        public MMDBDataParameter(string parametername, string parametervalue, DbType dbtype, int parametersize, ParameterDirection direction)        
        {
            ParameterName = parametername;
            DBType = dbtype;
            ParameterValue = parametervalue;
            ParameterSize = parametersize;
            Direction = direction;
        }
    }
}
