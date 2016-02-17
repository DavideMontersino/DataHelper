using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Threading;
using System.Web;
using System.Globalization;

namespace Nts.DataHelper
{

    public static class DataHelper
    {

        public static Dictionary<Type, string> DefaultTypeMapping = new Dictionary<Type, string>
            {
            {typeof(string),"nvarchar(250) NOT NULL DEFAULT('')"},
            {typeof(DateTime),"datetime NOT NULL DEFAULT GETDATE()"},
            {typeof(int), "int NOT NULL DEFAULT(0)"},
            {typeof(bool), "bit NOT NULL DEFAULT(0)" },
            {typeof(DateTime ? ),"datetime NULL"},
            {typeof(int ? ), "int NULL"},
            {typeof(bool ? ), "bit NULL" },
            {typeof(TimeSpan), "time(7)"},
            {typeof(TimeSpan?), "time(7)"}
        };

        public static int NonQuery(string commandText, CommandType commandType, params SqlParameter[] parameters)
        {
            return NonQuery(commandText, commandType, false, parameters);
        }

        public static int NonQuery(string commandText, CommandType commandType, bool returnsId, params SqlParameter[] parameters)
        {
            using (var conn = new SqlConnection(
                ConnectionStringUtils.ConnectionString))
            {
                var ctxComm = GetContextCommand(conn);
                if (returnsId) commandText += "; SELECT SCOPE_IDENTITY()";
                var comm = new SqlCommand(commandText, conn) { CommandType = commandType };

                foreach (var par in parameters)
                {
                    comm.Parameters.Add(par);
                }
                try
                {
                    conn.Open();
                }
                catch (Exception ex)
                {
                    if (!ex.Message.Contains("Cannot open database"))
                        throw;
                    Ensure.EnsureDatabase();
                    Thread.Sleep(5000);
                }
                ctxComm.ExecuteNonQuery();

                return returnsId ? int.Parse("0" + comm.ExecuteScalar()) : comm.ExecuteNonQuery();

            }
        }
        private static int UserId
        {
            get
            {
                
                if (HttpContext.Current == null)
                    return 0; //TODO dirty
                if (HttpContext.Current.Session["UserId"] == null)
                    HttpContext.Current.Session["UserId"] = GetUserIdFromDB();
                return (int)HttpContext.Current.Session["UserId"];
            }
        }

        // TODO eliminare hardcoding utente
        private static int GetUserIdFromDB()
        {
            using (var conn = new SqlConnection(
                ConnectionStringUtils.ConnectionString))
            {
                conn.Open();
                var comm = new SqlCommand("SELECT IdPerson FROM Person WHERE Username=@username", conn) { CommandType = CommandType.Text };
                comm.Parameters.Add(new SqlParameter("username", HttpContext.Current.User.Identity.Name));
                try
                {
                    return int.Parse("0" + comm.ExecuteScalar());
                }
                catch (SqlException)
                {
                    return 0;
                }
            }
        }
        private static SqlCommand GetContextCommand(SqlConnection conn)
        {

            var ctxComm =
                new SqlCommand("DECLARE @ctx varbinary(MAX) = convert (varbinary(128),@userId); SET CONTEXT_INFO @ctx",
                               conn);
            ctxComm.Parameters.AddWithValue("userId", UserId);
            return ctxComm;
        }

        public static DateTime CurrentDateTime
        {
            get
            {

                return CurDateTime.Now().Value;
            }
        }
        public static List<T> GetAll<T>()
        {
            return GetList<T>("SELECT * FROM " + typeof(T).GetTableName(), CommandType.Text);
        }



        public static T Load<T>(string commandText, CommandType commandType, params SqlParameter[] parameters)
        {
            return GetList<T>(commandText, commandType, parameters).FirstOrDefault();
        }

        public static List<T> GetList<T>(string commandText, CommandType commandType, params SqlParameter[] parameters)
        {
            var list = new List<T>();
            using (var conn = new SqlConnection(
                ConnectionStringUtils.ConnectionString))
            {
                var command = new SqlCommand(commandText, conn) { CommandType = commandType, CommandTimeout = 180 };
                command.Parameters.AddRange(parameters);
                conn.Open();
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var obj = Activator.CreateInstance<T>();
                    foreach (PropertyInfo pi in typeof(T).GetProperties())
                    {
                        if (pi.GetCustomAttributes(typeof(MappedFieldAttribute), true).Length > 0
                            ||
                            pi.GetCustomAttributes(typeof(IdentityFieldAttribute), true).Length > 0)
                        {
                            string columnName = pi.GetColumnName();

                            object val = reader[columnName];


                            if (val is DBNull)
                                val = null;

                            if (val is int && pi.PropertyType == typeof(bool))
                            {
                                val = ((int)val) != 0;
                            }
                            pi.SetValue(obj, val, new object[0]);
                        }
                    }
                    list.Add(obj);
                }

            }
            return list;
        }

        public static T Load<T>(int id)
        {
            var zeroIndexAttributes = typeof(T).GetCustomAttributes(typeof(ZeroIndexAttribute), false);

            if (id == 0 && zeroIndexAttributes.Length == 0)
                return default(T);

            /* se è un zeroindex invece voglio caricarlo!
            if (id == 0)
                return default(T);*/

            string idName = "";
            foreach (PropertyInfo pi in typeof(T).GetProperties())
            {
                if (pi.GetCustomAttributes(typeof(IdentityFieldAttribute), true).Length > 0)
                {
                    idName = pi.Name;
                }
            }

            if (idName == "")
                throw new NotImplementedException("No Identity Field found for type " + typeof(T).GetTableName());

            return GetList<T>("SELECT * FROM [" + typeof(T).GetTableName() + "] WHERE " + idName + "=@" + idName, CommandType.Text
               , new SqlParameter(idName, id)).FirstOrDefault();
        }

        public static int GetIdentity(object obj)
        {
            foreach (PropertyInfo pi in obj.GetType().GetProperties())
            {
                if (pi.GetCustomAttributes(typeof(IdentityFieldAttribute), true).Length > 0)
                {
                    return Convert.ToInt32(pi.GetValue(obj, new object[0]));
                }
            }
            throw new InvalidOperationException();
        }

        public static void SetIdentity(object obj, int idValue)
        {
            foreach (PropertyInfo pi in obj.GetType().GetProperties())
            {
                if (pi.GetCustomAttributes(typeof(IdentityFieldAttribute), true).Length > 0)
                {
                    pi.SetValue(obj, idValue, new object[0]);
                    return;
                }
            }
            throw new InvalidOperationException();
        }




        public static List<SqlParameter> GetParameters(bool includeId, object obj)
        {
            var type = obj.GetType();
            string idPropertyName = type.GetIdentityName();

            var parameters = new List<SqlParameter>();
            foreach (PropertyInfo pi in type.GetProperties().Where(pi => pi.CanWrite))
            {
                if (pi.Name == idPropertyName)
                {
                    if (!includeId)
                        continue;
                }
                else
                {
                    //if (pi.GetCustomAttributes(typeof(MappedFieldAttribute), true).Length == 0)
                    //continue;
                    var mta = pi.GetCustomAttributes(typeof(MappedFieldAttribute), true).FirstOrDefault() as MappedFieldAttribute;
                    if (mta == null)
                        continue;
                    if (mta.Computed)
                        continue;
                }
                object val = pi.GetValue(obj, new object[0]) ?? DBNull.Value;

                //if (pi.Name.EndsWith("ID") && (((int)val) == 0))
                //    val = DBNull.Value;

                parameters.Add(new SqlParameter("@" + pi.Name.ToLower(), val));
            }
            return parameters;
        }

        public static T Save<T>(object obj) where T : class
        {
            Save(obj);
            return obj as T;
        }

        public static void Save(object obj)
        {
            /*Utente u = obj as Utente;
            if (u != null)
            {
                u.DataOraUltimaVariazione = DateTime.Now;
            }*/
            int id = GetIdentity(obj);

            if (id == 0)
            {
                Insert(obj);
            }
            else
            {
                Update(obj);
            }

        }




        public static Dictionary<Type, ImportParsers.ParseRowColumn> DefaultImportParsers
        {

            get
            {

                return new Dictionary<Type, ImportParsers.ParseRowColumn>(){
                {typeof(int), ImportParsers.ParseInt},
                {typeof(string), ImportParsers.ParseString},
                {typeof(DateTime), ImportParsers.ParseDateTime},
                {typeof(DateTime?), ImportParsers.ParseDateTime},
                {typeof(bool), ImportParsers.ParseBool},
                };
            }
        }

        public static T AssociateFromImport<T>(DataRow row, List<Error> errors) where T : new()
        {
            var p = GetImportKey(typeof(T));
            if (p == null)
                return new T();
            var sql = "SELECT * FROM " + typeof(T).GetTableName() + " WHERE " + p.GetColumnName() + " = @" + p.GetColumnName();

            SqlParameter sp = new SqlParameter(p.GetColumnName(), ReadPropertyFromDataRow(row, p, errors));

            return Load<T>(sql, CommandType.Text, sp);
        }

        private static PropertyInfo GetImportKey(Type type)
        {
            foreach (PropertyInfo pi in type.GetProperties().Where(pi => pi.CanWrite))
            {
                var mfa = pi.GetCustomAttributes(typeof(MappedFieldAttribute), true).FirstOrDefault() as MappedFieldAttribute;
                if (mfa != null && mfa.ImportKey)
                    return pi;
            }
            return null;
        }
        public static T FromDataRow<T>(DataRow row, List<Error> errors) where T : new()
        {
            var ret = AssociateFromImport<T>(row, errors);

            if (ret == null)
                ret = new T();

            foreach (PropertyInfo pi in typeof(T).GetProperties().Where(pi => pi.CanWrite))
            {
                try
                {
                    var parsedValue = ReadPropertyFromDataRow(row, pi, errors);

                    pi.SetValue(ret, parsedValue, new object[0]);
                }
                catch (ArgumentException e)
                {
                    //errors.Add(new Error(ErrorLevel.INFO, "Campo " + pi.GetImportName() + " non presente"));
                }


            }
            return ret;
        }

        private static object ReadPropertyFromDataRow(DataRow row, PropertyInfo pi, List<Error> errors)
        {

            var importName = pi.GetImportName();

            var stringValue = ("" + row.Field<string>(importName)).Trim();
            if (pi.IsCompulsory() && string.IsNullOrEmpty(stringValue))
                errors.Add(new Error(ErrorLevel.ERROR, "Il campo " + pi.GetImportName() + " non è presente o non è interpretabile"));
            var parser = pi.GetImportParser();
            var parsedValue = parser(stringValue);
            return parsedValue;


        }
        private static string CreateInsertNonQuery(object obj)
        {
            string names = "", values = "";
            string idPropertyName = obj.GetType().GetIdentityName();
            foreach (PropertyInfo pi in obj.GetType().GetProperties().Where(pi => pi.CanWrite))
            {

                var objValue = pi.GetValue(obj, new object[0]);
                var type = pi.PropertyType;

                if (type == typeof(string) && objValue == null)
                    continue;
                if (pi.Name == idPropertyName)
                    continue;
                if (pi.GetCustomAttributes(typeof(MappedFieldAttribute), true).Length == 0)
                    continue;
                var mfa = pi.GetCustomAttributes(typeof(MappedFieldAttribute), true).FirstOrDefault() as MappedFieldAttribute;
                if (mfa != null && mfa.Computed)
                    continue;

                var columnName = pi.GetColumnName();

                names += "[" + columnName + "],";
                values += "@" + columnName.ToLower() + ", ";
            }
            names = names.TrimEnd(" ,".ToCharArray());
            values = values.TrimEnd(" ,".ToCharArray());


            return string.Format("INSERT INTO [" + obj.GetType().Name
                + "] ({0}) VALUES ({1})", names, values);
        }

        private static string CreateUpdateNonQuery(object obj)
        {
            var sets = "";
            foreach (PropertyInfo pi in obj.GetType().GetProperties().Where(pi => pi.CanWrite))
            {
                var type = pi.PropertyType;
                var objValue = pi.GetValue(obj, new object[0]);

                if (pi.Name == obj.GetType().GetIdentityName())
                    continue;
                if (type == typeof(string) && objValue == null)
                    continue;
                if (pi.GetCustomAttributes(typeof(MappedFieldAttribute), true).Length == 0)
                    continue;
                var mfa = pi.GetCustomAttributes(typeof(MappedFieldAttribute), true).FirstOrDefault() as MappedFieldAttribute;
                if (mfa != null && mfa.Computed)
                    continue;

                var columnName = pi.GetColumnName();
                sets += "[" + columnName + "]=" + "@" + columnName.ToLower() + ", ";
            }
            sets = sets.TrimEnd(" ,".ToCharArray());

            return string.Format("UPDATE [" + obj.GetType().Name
                + "] SET {0} WHERE " + obj.GetType().GetIdentityName() + "=@" + obj.GetType().GetIdentityName(), sets);
        }

        public static void Insert(object obj)
        {
            string commandText = CreateInsertNonQuery(obj);
            List<SqlParameter> parameters = GetParameters(false, obj);
            SetIdentity(obj, NonQuery(commandText, CommandType.Text, true, parameters.ToArray()));
        }
        public static void Update(object obj)
        {
            string commandText = CreateUpdateNonQuery(obj);
            List<SqlParameter> parameters = GetParameters(true, obj);
            NonQuery(commandText, CommandType.Text, false, parameters.ToArray());
        }



        public static void Delete(object obj)
        {
            string commandText = GenerateDeleteNonQuery(obj.GetType());
            NonQuery(commandText, CommandType.Text, new SqlParameter("@id", GetIdentity(obj)));
        }

        public static void Delete<T>(int id)
        {
            var type = typeof(T);
            string commandText = GenerateDeleteNonQuery(type);
            NonQuery(commandText, CommandType.Text, new SqlParameter("@id", id));
        }

        private static string GenerateDeleteNonQuery(Type type)
        {
            string commandText = "DELETE FROM [" + type.GetTableName() + "] WHERE "
                + type.GetIdentityName() + "=@id";
            return commandText;
        }
        public static DataTable QueryToDataTable(string commandText, CommandType commandType, params SqlParameter[] parameters)
        {
            using (var conn = new SqlConnection(ConnectionStringUtils.ConnectionString))
            {
                var comm = new SqlCommand(commandText, conn) { CommandType = commandType };
                foreach (var par in parameters)
                {
                    comm.Parameters.Add(par);
                }
                try
                {
                    conn.Open();
                }
                catch (Exception ex)
                {
                    if (!ex.Message.Contains("Cannot open database"))
                        throw;
                    Ensure.EnsureDatabase();
                    Thread.Sleep(5000);
                }
                var adapter = new SqlDataAdapter(comm);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt;
            }
        }


        public static void AddOrUpdate<TKey, TValue>(this Dictionary<TKey, TValue> @this, TKey key, TValue value)
        {
            if (@this.ContainsKey(key))
            {
                @this[key] = value;
                return;
            }
            @this.Add(key, value);
        }

        public static void RemoveOrIgnore<TKey, TValue>(this Dictionary<TKey, TValue> @this, TKey key)
        {
            if (@this.ContainsKey(key))
            {
                @this.Remove(key);
            }
        }

        public static bool IsUnique(this PropertyInfo @this)
        {
            var attr = @this.GetCustomAttributes(typeof(MappedFieldAttribute), true).FirstOrDefault() as MappedFieldAttribute;

            if (attr == null) return false;

            return attr.Unique;

        }

        public static Type GetReferencedType(this PropertyInfo @this)
        {
            var attr = @this.GetCustomAttributes(typeof(MappedFieldAttribute), true).FirstOrDefault() as MappedFieldAttribute;

            if (attr == null) return null;

            return attr.References;


        }

        #region reflection Extension Methods

        public static string GetIdentityName(this Type @this)
        {
            foreach (PropertyInfo pi in @this.GetProperties())
            {
                if (pi.GetCustomAttributes(typeof(IdentityFieldAttribute), true).Length > 0)
                {
                    return pi.Name;
                }
            }
            throw new InvalidOperationException();
        }

        public static string GetDbType(this PropertyInfo pi)
        {
            var t = pi.PropertyType;
            //se si tratta di enum, usiamo int (oppure dovremmo mappare tutti i tipi enum..)
            if (pi.PropertyType.IsEnum)
            {
                t = typeof(int);
                //se è un enum nullabile, creo un null
                if (pi.PropertyType.IsGenericType && pi.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    t = typeof(int?);
            }

            return DefaultTypeMapping[t];

        }


        public static string GetTableName(this Type type)
        {
            var ret = type.Name;
            var mta = type.GetCustomAttributes(typeof(MappedTableAttribute), true).FirstOrDefault() as MappedTableAttribute;
            if (mta == null)
                return ret;
            if (!string.IsNullOrEmpty(mta.TableName))
                ret = mta.TableName;
            return ret;
        }

        public static bool IsCompulsory(this PropertyInfo pi)
        {

            var mfa = pi.GetCustomAttributes(typeof(MappedFieldAttribute), true).FirstOrDefault() as MappedFieldAttribute;
            if (mfa != null && (mfa.CompulsoryImport))
                return true;
            return false;
        }

        public static string GetColumnName(this PropertyInfo pi)
        {
            var ret = pi.Name;
            var mfa = pi.GetCustomAttributes(typeof(MappedFieldAttribute), true).FirstOrDefault() as MappedFieldAttribute;
            if (mfa != null && (!string.IsNullOrEmpty(mfa.ColumnName)))
                ret = mfa.ColumnName;
            return ret;
        }


        public static string GetImportName(this PropertyInfo pi)
        {
            var ret = pi.Name;
            var mfa = pi.GetCustomAttributes(typeof(MappedFieldAttribute), true).FirstOrDefault() as MappedFieldAttribute;
            if (mfa != null && (!string.IsNullOrEmpty(mfa.ImportField)))
                ret = mfa.ImportField;
            if (mfa != null && mfa.ImportField == "")
                ret = null;
            return ret;
        }

        public static ImportParsers.ParseRowColumn GetImportParser(this PropertyInfo pi)
        {
            ImportParsers.ParseRowColumn ret = ImportParsers.ParseNull;
            var mfa = pi.GetCustomAttributes(typeof(MappedFieldAttribute), true).FirstOrDefault() as MappedFieldAttribute;
            if (mfa != null && mfa.ImportParser != null)
                ret = mfa.ImportParser;
            else
            {
                if (DefaultImportParsers.ContainsKey(pi.PropertyType) && DefaultImportParsers[pi.PropertyType] != null)
                {
                    ret = DefaultImportParsers[pi.PropertyType];
                }
            }
            return ret;
        }

        #endregion
        /// <summary>
        /// Aggiunta per compatibilità con Ticketing
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T LoadFromQueryString<T>()
        {
            int id = int.Parse("0" + HttpContext.Current.Request.QueryString["id" + typeof(T).Name]);
            return Load<T>(id);
        }


    }


    public class CustomComparer<T> : IComparer<T>
    {
        readonly int _coeff = 1;
        readonly PropertyInfo _pi;
        public CustomComparer(string orderBy)
        {
            var ss = orderBy.Split(' ');
            _pi = typeof(T).GetProperty(ss[0]);
            if (ss.Length > 1 && ss[1] == "DESC")
                _coeff = -1;
        }
        public int Compare(T x, T y)
        {
            var xc = (IComparable)_pi.GetValue(x, new object[0]);
            var yc = (IComparable)_pi.GetValue(y, new object[0]);

            if (xc != null && yc == null)
                return _coeff;
            if (xc == null && yc != null)
                return -_coeff;
            if (xc == null)
                return 0;
            return _coeff * xc.CompareTo(yc);
        }
    }

    public static class ImportParsers
    {
        public delegate object ParseRowColumn(string rowValue);

        public static object ParseInt(string rowValue)
        {
            return Int32.Parse(rowValue);
        }
        public static object ParseBool(string rowValue)
        {
            return bool.Parse(rowValue);
        }
        public static object ParseDateTime(string rowValue)
        {
            string pattern = "dd/MM/yyyy";
            DateTime dt;
            DateTime.TryParseExact(rowValue, pattern, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
            return dt;
        }
        public static object ParseString(string rowValue)
        {
            return rowValue;
        }
        public static object ParseNull(string rowValue)
        {
            return null;
        }
    }
}