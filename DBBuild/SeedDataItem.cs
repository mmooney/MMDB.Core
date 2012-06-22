using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MMDB.Core.DBBuild
{
    public class SeedDataItem
    {
        public SeedDataItem()
        {
        }
        public SeedDataItem(string seeddataelement)
        {
            SeedDataElement = seeddataelement;
        }
        public string SeedDataElement { get; set; }
    }
}