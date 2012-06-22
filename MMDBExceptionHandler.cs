using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MMDB.Core
{
    public static class MMDBExceptionHandler
    {
        public static void HandleException(Exception ex)
        {
            try //by nature, if this fails, its already handling an exception!
            {
                MMDBLogFile.Log(ex);
            }
            catch//(Exception x)
            {
            }
        }
    }
}
