namespace Nts.DataHelper
{
    [MappedTable(TableName = "INFORMATION_SCHEMA.TABLES")]
    internal class TableSchema
    {
        [MappedField(ColumnName = "TABLE_CATALOG")]
        public string Catalog { get; set; }

        [MappedField(ColumnName = "TABLE_SCHEMA")]
        public string Schema { get; set; }

        [MappedField(ColumnName = "TABLE_NAME")]
        public string Name { get; set; }

        [MappedField(ColumnName = "TABLE_TYPE")]
        public string Type { get; set; }

    }
}
