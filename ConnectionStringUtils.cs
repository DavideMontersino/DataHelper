using System.Collections.Generic;
using System.Linq;
using System.Configuration;

namespace Nts.DataHelper
{
    public static class ConnectionStringUtils
    {
        public static string ConnectionString
        {
            get
            {
                return ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            }
        }

        public static string CleanConnectionString
        {
            get
            {
                return ParsedConnectionString.Where(kvp => kvp.Key != "Initial Catalog").Aggregate("", (current, kvp) => current + (kvp.Key + "=" + kvp.Value + ";"));
            }
        }
        public static Dictionary<string, string> ParsedConnectionString
        {
            get
            {
                char[] sep1 = { ';' };
                char[] sep2 = { '=' };
                var keyValuePairs = ConnectionString.Split(sep1);

                return keyValuePairs.Select(keyValuePair => keyValuePair.Split(sep2)).Where(pair => pair.Count() > 1).ToDictionary(pair => pair[0], pair => pair[1]);
            }
        }
        public static string DbName
        {
            get
            {
                return ParsedConnectionString["Initial Catalog"];
            }
        }
        
    }
}
