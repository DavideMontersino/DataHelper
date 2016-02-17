using System;
using System.Data;

namespace Nts.DataHelper
{
    /// <summary>
    /// Summary description for CurrentDateTime
    /// </summary>
    public class CurDateTime
    {
        [MappedField]
        public DateTime Value { get; set; }

        public static CurDateTime Now()
        {
            var ret = DataHelper.Load<CurDateTime>("SELECT GetDate() AS Value", CommandType.Text);
            return ret;
        }
    }
}