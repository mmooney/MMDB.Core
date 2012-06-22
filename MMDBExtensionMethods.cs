using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Windows.Forms;
using System.Web.UI.WebControls;
using System.Collections.Specialized;

namespace MMDB.Core.ExtensionMethods
{
    public static class MMDBExtensionMethods
    {
        public static DateTime ToDateTime(this string value)
        {
            DateTime val = DateTime.MinValue;
            DateTime.TryParse(value, out val);
            return val;
        }
        public static bool ToBoolean(this string value)
        {
            bool val = false;
            Boolean.TryParse(value, out val);
            return val;
        }
        public static List<string> ToSingleFiIeldList(this DataTable dt)
        {
            List<string> returnvalue = new List<string>();

            foreach (DataRow r in dt.Rows)
            {
                returnvalue.Add(r[0].ToString());
            }
            return returnvalue;
        }
        public static int ToInt32(this string value)
        {
            int val = 0;
            Int32.TryParse(value, out val);
            return val;
        }
        public static Dictionary<string, string> ToDictionary(this DataGridView gv)
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            for (int i = 0; i < gv.Rows.Count - 1; i++)
            {
                DataGridViewRow gvr = gv.Rows[i];
                d.Add(gvr.Cells[0].ToString(), gvr.Cells[1].ToString());
            }
            return d;
            
        }
        public static DataTable ToDataTable(this DataGridView gv)
        {
            DataTable dt = new DataTable();
            for (int j = 0; j < gv.Columns.Count; j++)
            {
                dt.Columns.Add(gv.Columns[j].Name);
            }

            for (int i = 0; i < gv.Rows.Count-1;i++)
            {
                DataGridViewRow gvr = gv.Rows[i];
                DataRow dr = dt.NewRow();
                for (int j = 0; j < gv.Columns.Count; j++)
                {
                    dr[j] = gvr.Cells[j].Value;
                }
                dt.Rows.Add(dr);
            }
            return dt;
        }
        public static DataSet ToDataSet<T>(this IList<T> list)
        {
            Type elementType = typeof(T);
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();
            ds.Tables.Add(dt);

            foreach (var propinfo in elementType.GetProperties())
            {
                dt.Columns.Add(propinfo.Name, propinfo.PropertyType);
            }

            foreach (T item in list)
            {
                DataRow row = dt.NewRow();

                foreach (var propInfo in elementType.GetProperties())
                {
                    if (propInfo.Name != "EncryptedPassword")
                    {
                        row[propInfo.Name] = propInfo.GetValue(item, null);
                    }
                }
                dt.Rows.Add(row);

            }
            return ds;
        }
        public static void Add(this StringCollection sc, StringCollection sc1)
        {
            foreach(string s in sc1)
            {
                sc.Add(s);
            }
        }
        public static StringCollection GetRange(this StringCollection sc, int Start, int End)
        {
            int Count = 0;
            StringCollection returncollection = new StringCollection();
            foreach (string s in sc)
            {
                
                if (Count > End){ break; }
                if (Count >= Start){ returncollection.Add(s); }

                Count++;
            }
            return returncollection;
        }
        private static void Sort(ref GridView gv, DataTable dt, string field, string direction)
        {
            if (dt != null)
            {
                DataView dv = new DataView(dt);
                dv.Sort = field + " " + direction;

                gv.DataSource = dv;
                gv.DataBind();
            }
        }
        public static void SortAscending(this GridView gv, DataTable dt, string field)
        {
            Sort(ref gv, dt, field, "ASC");
        }
        public static void SortDescending(this GridView gv, DataTable dt, string field)
        {
            Sort(ref gv, dt, field, "DESC");
        }
    }
}
