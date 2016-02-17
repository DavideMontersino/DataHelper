using System;

namespace Nts.DataHelper
{
    public class IdentityFieldAttribute : Attribute
    {

    }

    public class MappedFieldAttribute : Attribute
    {
        public string ColumnName { get; set; }
        public bool Unique { get; set; }
        public Type References { get; set; }
        public bool Computed { get; set; }
        public bool Nullable { get; set; }
        public string DefaultFormatString { get; set; }
        public string ComputedFormula { get; set; }
        public string ImportField { get; set; }
        public ImportParsers.ParseRowColumn ImportParser { get; set; }
        public bool ImportKey { get; set; }
        public bool CompulsoryImport { get; set; }
    }

    public class GridViewFieldAttribute : Attribute
    {

    }

    public class FilterFieldAttribute : Attribute
    {

    }

    public class ImportFieldAttribute : Attribute
    {

    }



    public class MappedTableAttribute : Attribute
    {
        public string TableName { get; set; }

    }


    /// <summary>
    /// Da usare per tabelle che partono da 0 anzichè 1
    /// </summary>
    public class ZeroIndexAttribute : Attribute
    {


    }
}
