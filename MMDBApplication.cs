using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Deployment.Application;

namespace MMDB.Core
{
    public static class MMDBApplication
    {
        public static bool IsNewVersionAvailable()
        {
                    if (ApplicationDeployment.CurrentDeployment.CheckForUpdate())
                    {
                        return true;
                    }
                    return false;
        }
    }
}
