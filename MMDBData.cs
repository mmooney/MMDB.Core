using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Common;   
using System.Data.Sql;
using System.Data.SqlClient;
using Microsoft.Practices.EnterpriseLibrary.Data;
using Microsoft.Practices.EnterpriseLibrary.Data.Sql;
using MMDB.Core.ExtensionMethods;


namespace MMDB.Core
{
    public static class MMDBData
    {
        public static DataSet RunProcedure(string ConnectionString, string ProcedureName, ref List<MMDBDataParameter> Parameters, ProcedureReturnType returntype)
        {
            try
            {
                Database db = DatabaseFactory.CreateDatabase(ConnectionString);

                DbCommand dbCommand = db.GetStoredProcCommand(ProcedureName);

                //We could add more parms if ever needed.
                foreach(MMDBDataParameter dp in Parameters)
                {
                    //Code for no default date.
                    DateTime dt;
                    if ((dp.DBType == DbType.Date || dp.DBType == DbType.DateTime || dp.DBType == DbType.DateTime2) && (dp.ParameterValue.ToDateTime() <= DateTime.MinValue))
                    {
                        dp.ParameterValue = "01/01/1799";
                    }

                    db.AddParameter(dbCommand, dp.ParameterName,dp.DBType, dp.ParameterSize, dp.Direction, true, 10, 10, dp.ParameterName, DataRowVersion.Default, dp.ParameterValue);
                }

                DataSet ds = null;
                switch (returntype)
                {
                    case ProcedureReturnType.DataSet:
                        ds = db.ExecuteDataSet(dbCommand);
                        break;
                    case ProcedureReturnType.ExecuteOnly:
                    case ProcedureReturnType.ParameterOnly:
                        db.ExecuteNonQuery(dbCommand);
                        break;

                }

                foreach(MMDBDataParameter dp in Parameters)
                {
                    if(dp.Direction == ParameterDirection.Output || dp.Direction == ParameterDirection.InputOutput)
                    {
                        //We will update the value
                        dp.ParameterValue = dbCommand.Parameters[@"@"+dp.ParameterName].Value.ToString();
                    }
                }

                if(returntype == ProcedureReturnType.DataSet)
                {
                    return ds;
                }
                else
                {
                    return null;
                }
                //Send out Parms
            }
            catch(Exception err)
            {
                MMDBExceptionHandler.HandleException(err);
                Parameters = null;
                return null;
            }
       }
    }
}
