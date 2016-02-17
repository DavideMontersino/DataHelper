using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;
using System.Threading;
using System.Data;
using System.Reflection;

namespace Nts.DataHelper
{
    public static class Ensure
    {


        private static void DropDatabase()
        {
            using (var conn2 = new SqlConnection(ConnectionStringUtils.CleanConnectionString))
            {
                var comm = new SqlCommand("DROP DATABASE [" + ConnectionStringUtils.DbName + "]", conn2);
                conn2.Open();
                comm.ExecuteNonQuery();
                conn2.Close();
                Thread.Sleep(5000);
            }
        }

        private static void CreateDatabase()
        {

            using (var conn2 = new SqlConnection(ConnectionStringUtils.CleanConnectionString))
            {
                var comm = new SqlCommand("CREATE DATABASE [" + ConnectionStringUtils.DbName + "]", conn2);
                conn2.Open();
                comm.ExecuteNonQuery();
                conn2.Close();
                Thread.Sleep(5000);
            }
        }


        public static void EnsureDatabase()
        {
            using (var conn = new SqlConnection(ConnectionStringUtils.ConnectionString))
            {
                try
                {
                    conn.Open();
                }
                catch (Exception ex)
                {
                    if (!ex.Message.Contains("Cannot open database"))
                        throw;
                    CreateDatabase();
                    Thread.Sleep(5000);
                }
            }
        }
        public static void EnsureTables(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes().Where(x => x.GetCustomAttributes(typeof(MappedTableAttribute), true).Any()))
            {
                string tableName = type.GetTableName();

                if (!ExistTable(tableName))
                {
                    CreateTableForType(type, false);
                }

                UpdateTableColumns(type);

            }
            UpdateAllComputedFields(assembly);
            UpdateAllForeignKeys(assembly);

        }



        private static void UpdateTableColumns(Type type)
        {
            var tableName = type.GetTableName();
            var dt = DataHelper.QueryToDataTable("SELECT * FROM [" + tableName + "]", CommandType.Text);

            new List<PropertyInfo>();
            foreach (var pi in type.GetProperties().Where(x => x.GetCustomAttributes(typeof(MappedFieldAttribute), true).Any() && x.CanWrite))
            {

                var columnName = pi.GetColumnName();
                var columnType = pi.GetDbType();

                if (dt.Columns.Contains(columnName))
                    continue;

                var alterCommand = "ALTER TABLE [" + tableName + "] ADD [" + columnName + "] " + columnType;

                DataHelper.NonQuery(alterCommand, CommandType.Text);
                
            }
            
        }

        public static bool IsIdentityField(this PropertyInfo pi)
        {
            return pi.GetCustomAttributes(typeof(IdentityFieldAttribute), true).Any();

        }

        public static void CreateTableForType(Type type, bool recreate)
        {

            var ret = "";
            var tableName = type.GetTableName();

            if (recreate && ExistTable(tableName))
            {
                DropTable(tableName);
            }


            string identityField = type.GetIdentityName();

            ret += "CREATE TABLE [" + tableName + "] (\n";
            ret += "[" + identityField + "] [int] IDENTITY(1,1) NOT NULL,\n";

            foreach (PropertyInfo pi in type.GetProperties().Where(pi => pi.CanWrite))
            {
                if (pi.IsIdentityField())
                    continue;
                var mta = pi.GetCustomAttributes(typeof (MappedFieldAttribute), true).FirstOrDefault() as MappedFieldAttribute;
                if (mta == null  )
                    continue;
                if (mta.Computed)
                    continue;
                var columnName = pi.GetColumnName();



                string dbType = pi.GetDbType();

                ret += "[" + columnName + "] " + dbType + ",\n";

            }

            ret += "CONSTRAINT [PK_" + tableName + "] PRIMARY KEY CLUSTERED (\n";
            ret += "\t[" + identityField + "] ASC\n)";

            foreach (PropertyInfo pi in type.GetProperties().Where(pi => pi.IsUnique()))
            {
                var columnName = pi.GetColumnName();
                ret += ", CONSTRAINT [Unique_" + tableName + "_" + columnName + "] UNIQUE NONCLUSTERED  (\n";
                ret += "\t[" + columnName + "] ASC\n)";
            }



            ret += "\n)";

            DataHelper.NonQuery(ret, CommandType.Text);

            


        }
        
        public static void UpdateAllComputedFields(Assembly assembly)
        {
            var types = assembly.GetTypes().Where(x => x.GetCustomAttributes(typeof(MappedTableAttribute), true).Any());

            foreach (var type in types)
            {
                UpdateComputedFields(type);
            }
        }
        public static void UpdateAllForeignKeys(Assembly assembly)
        {
            var types = assembly.GetTypes().Where(x => x.GetCustomAttributes(typeof(MappedTableAttribute), true).Any());

            foreach (var type in types)
            {
                UpdateForeignKeys(type);
            }
        }

        private static void UpdateComputedFields(Type type)
        {
            var tableName = type.GetTableName();
           
            foreach (var pi in type.GetProperties().Where(x => x.GetCustomAttributes(typeof(MappedFieldAttribute), true).Any() && x.CanWrite))
            {
                var mta = pi.GetCustomAttributes(typeof(MappedFieldAttribute), true).FirstOrDefault() as MappedFieldAttribute;
                if (mta == null)
                    throw new NullReferenceException("Property '" + pi.Name + "' in class '" + type.Name + "' has no MappedField attribute..");
                
                if (!mta.Computed || string.IsNullOrEmpty(mta.ComputedFormula)) continue;
                var columnName = pi.GetColumnName();

                var dropCommand = "ALTER TABLE [" + tableName + "] DROP COLUMN [" + columnName + "] " ;
                DataHelper.NonQuery(dropCommand, CommandType.Text);

                var alterCommand = "ALTER TABLE [" + tableName + "] ADD [" + columnName + "] AS " + mta.ComputedFormula;
                DataHelper.NonQuery(alterCommand, CommandType.Text);
            }
        }

        private static void UpdateForeignKeys(Type type)
        {
            foreach (PropertyInfo pi in type.GetProperties().Where(pi => pi.GetReferencedType() != null))
            {
                var type2 = pi.GetReferencedType();

                var foreignKeyName = "dbo.FK_" + type.GetTableName() + "_" + type2.GetTableName();
                var dropQuery = @"
                IF EXISTS (SELECT * 
                          FROM sys.foreign_keys 
                           WHERE name = '" + foreignKeyName + @"'
                           AND parent_object_id = OBJECT_ID(N'dbo." + type.GetTableName() + @"')
                        )
                          ALTER TABLE [" + type.GetTableName() + "] DROP CONSTRAINT [" + foreignKeyName + @"]
                ";
                DataHelper.NonQuery(dropQuery, CommandType.Text);

                var createQuery = @"ALTER TABLE [dbo].[" + type.GetTableName() + @"] 
                          WITH CHECK ADD CONSTRAINT [" + foreignKeyName + @"] FOREIGN KEY([" + pi.GetColumnName() + @"])
                            REFERENCES [dbo].[" + type2.GetTableName() + @"] ([" + type2.GetIdentityName() + @"])";

                DataHelper.NonQuery(createQuery, CommandType.Text);
            }
        }

        private static void DropTable(string tableName)
        {
            DataHelper.NonQuery("DROP TABLE [" + tableName + "]", CommandType.Text);
        }

        private static bool ExistTable(string tableName)
        {

            return
                DataHelper.GetList<TableSchema>("SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @tableName",
                                                CommandType.Text,
                                                new SqlParameter("tableName", tableName)).Any();

        }



        public static void CreateTablesForAssembly(Assembly assembly, bool recreate)
        {
            var types = assembly.GetTypes().Where(x => x.GetCustomAttributes(typeof(MappedTableAttribute), true).Any());

            foreach (var type in types)
            {
                CreateTableForType(type, recreate);
            }
        }

        public static void ResetDatabase()
        {
            DropDatabase();
            EnsureDatabase();
        }
    }
}
