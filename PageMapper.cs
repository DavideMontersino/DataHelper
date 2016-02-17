using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Reflection;
using System.ComponentModel;

namespace Nts.DataHelper
{
    [DataObject]
    public static class PageMapper
    {
        public static string ToEnumName(Type enumType, string enumValue)
        {
            return ToEnumName(enumType, int.Parse(enumValue));
        }

        private static string ToEnumName(Type enumType, int enumValue)
        {
            var names = EnumToDictionaryInv(enumType, false);

            if (names.All(x => x.Key != enumValue + ""))
            {
                return "";
            }

            return names[enumValue + ""];
        }

        public static Dictionary<string, string> EnumToDictionaryInv(Type enumType, bool addEmpty)
        {
            var ret = new Dictionary<string, string>();

            if (addEmpty)
            {
                ret.Add("", "0");
            }
            // get the names from the enumeration
            string[] names = Enum.GetNames(enumType);
            // get the values from the enumeration
            Array values = Enum.GetValues(enumType);
            // turn it into a hash table

            for (int i = 0; i < names.Length; i++)
                // note the cast to integer here is important
                // otherwise we'll just get the enum string back again
                ret.Add((int)values.GetValue(i) + "", names[i]);
            // return the dictionary to be bound to
            return ret;
        }

        public static Dictionary<string, string> EnumToDictionary(Type enumType, bool addEmpty)
        {
            var ret = new Dictionary<string, string>();

            if (addEmpty)
            {
                ret.Add("", "0");
            }
            // get the names from the enumeration
            string[] names = Enum.GetNames(enumType);
            // get the values from the enumeration
            Array values = Enum.GetValues(enumType);
            // turn it into a hash table

            for (int i = 0; i < names.Length; i++)
                // note the cast to integer here is important
                // otherwise we'll just get the enum string back again
                ret.Add(names[i], (int)values.GetValue(i) + "");
            // return the dictionary to be bound to
            return ret;
        }
        public static void BindToEnum(Type enumType, ListControl lc, bool addEmpty)
        {

            Dictionary<string, string> ht = EnumToDictionary(enumType, addEmpty);

            lc.DataSource = ht.OrderBy(x => x.Key);
            lc.DataTextField = "Key";
            lc.DataValueField = "Value";
            lc.DataBind();
        }
        public static string Prefix
        {
            get
            {
                return "auto_";
            }
        }

        public static Control FindControlRecursive(Control root, string id)
        {
            if (root.ID == id)
                return root;


            return (from Control ctl in root.Controls select FindControlRecursive(ctl, id)).FirstOrDefault(foundCtl => foundCtl != null);
        }

        /// <summary>
        /// Riempe i controlli di una pagina p con i valori delle properties dell'oggetto o
        /// </summary>
        /// <param name="o"></param>
        /// <param name="p"></param>
        public static void LoadPage(object o, Control p)
        {
            Type t = o.GetType();

            PropertyInfo[] pi = t.GetProperties();
            foreach (PropertyInfo prop in pi)
            {

                string prefix = Prefix;

                Type propType = prop.PropertyType;
                if (propType == typeof(string))
                {

                    string controlName = prefix + "TextBox" + prop.Name;
                    var txtBox = FindControlRecursive(p, controlName) as TextBox;

                    var str = "";
                    var mfa = prop.GetCustomAttributes(typeof(MappedFieldAttribute), true).FirstOrDefault() as MappedFieldAttribute;
                    if (mfa != null && !string.IsNullOrEmpty(mfa.DefaultFormatString))
                        str = (string.Format(mfa.DefaultFormatString, prop.GetValue(o, new object[0])));
                    else
                        str = (string)prop.GetValue(o, new object[0]);

                    if (txtBox != null)
                    {
                        txtBox.Text = str;
                        continue;
                    }

                    controlName = prefix + "DropDownList" + prop.Name;
                    var ddl = FindControlRecursive(p, controlName) as DropDownList;
                    if (ddl != null)
                    {
                        ddl.SelectedValue = (string)prop.GetValue(o, new object[0]);
                        continue;
                    }

                    controlName = prefix + "Label" + prop.Name;
                    var lbl = FindControlRecursive(p, controlName) as Label;
                }
                /*
                if (propType.IsEnum) {

                    var val = (int)prop.GetValue(o, new object[0]);
                    foreach (FieldInfo fInfo in prop.PropertyType.GetFields(BindingFlags.Public | BindingFlags.Static)) {
                        var c= fInfo.GetValue(prop);
                    }
                }*/
                if (propType == typeof(int) | propType == typeof(int?) | propType.IsEnum)
                {
                    var controlName = prefix + "TextBox" + prop.Name;
                    int? intVal = null;

                    if (propType == typeof(int?))
                        intVal = ((int?)prop.GetValue(o, new object[0]));
                    else
                        intVal = ((int)prop.GetValue(o, new object[0]));
                    var str = "0";
                    if (intVal.HasValue)
                    {
                        var mfa = prop.GetCustomAttributes(typeof(MappedFieldAttribute), true).FirstOrDefault() as MappedFieldAttribute;
                        if (mfa != null && !string.IsNullOrEmpty(mfa.DefaultFormatString))
                            str = (string.Format(mfa.DefaultFormatString, intVal));
                        else
                            str = intVal.Value + "";
                    }

                    var txtBox = FindControlRecursive(p, controlName) as TextBox;
                    if (txtBox != null)
                    {
                        if (intVal.HasValue)
                            txtBox.Text = str;
                        continue;
                    }
                    controlName = prefix + "DropDownList" + prop.Name;
                    var ddl = FindControlRecursive(p, controlName) as DropDownList;
                    if (ddl != null)
                    {
                        if (intVal.HasValue)
                            ddl.SelectedValue = intVal.Value + "";
                        continue;
                    }
                    controlName = prefix + "HiddenField" + prop.Name;
                    var hlf = FindControlRecursive(p, controlName) as HiddenField;
                    if (hlf != null)
                    {
                        if (intVal.HasValue)
                            hlf.Value = intVal.Value + "";
                        continue;
                    }
                }

                if (propType == typeof(decimal))
                {

                    var str = "";
                    var mfa = prop.GetCustomAttributes(typeof(MappedFieldAttribute), true).FirstOrDefault() as MappedFieldAttribute;
                    if (mfa != null && !string.IsNullOrEmpty(mfa.DefaultFormatString))
                        str = (string.Format(mfa.DefaultFormatString, (decimal)prop.GetValue(o, new object[0])));
                    else
                        str = ((decimal)prop.GetValue(o, new object[0])) + "";

                    string controlName = prefix + "TextBox" + prop.Name;
                    var txtBox = FindControlRecursive(p, controlName) as TextBox;
                    if (txtBox != null)
                    {
                        txtBox.Text = str;
                        continue;
                    }
                }
                if (propType == typeof(DateTime) | propType == typeof(DateTime?))
                {
                    string controlName = prefix + "TextBox" + prop.Name;
                    var txtBox = FindControlRecursive(p, controlName) as TextBox;
                    if (txtBox != null)
                    {
                        if (propType == typeof(DateTime?))
                        {
                            var val = ((DateTime?)prop.GetValue(o, new object[0]));
                            if (val.HasValue)
                                txtBox.Text = val.Value.ToShortDateString();

                        }
                        else
                            txtBox.Text = ((DateTime)prop.GetValue(o, new object[0])).ToShortDateString();
                        continue;
                    }
                }

                if (propType == typeof(bool?) || propType == typeof(bool?) || propType == typeof(System.Boolean) || propType == typeof(System.Boolean?))
                {
                    string controlName = prefix + "CheckBox" + prop.Name;
                    var chkBox = FindControlRecursive(p, controlName) as CheckBox;
                    if (chkBox != null)
                    {
                        var tmp = (bool?)(prop.GetValue(o, new object[0]));
                        chkBox.Checked = tmp.HasValue && tmp.Value;
                        continue;
                    }
                }
            }
        }

        public static void ReadPage(object o, Control p)
        {
            Type t = o.GetType();

            PropertyInfo[] pi = t.GetProperties();
            foreach (PropertyInfo prop in pi)
            {

                string prefix = Prefix;

                Type propType = prop.PropertyType;
                if (propType == typeof(string))
                {

                    string controlName = prefix + "TextBox" + prop.Name;
                    TextBox txtBox = FindControlRecursive(p, controlName) as TextBox;
                    if (txtBox != null)
                    {
                        prop.SetValue(o, txtBox.Text, new object[0]);
                        continue;
                    }

                    controlName = prefix + "DropDownList" + prop.Name;
                    DropDownList ddl = FindControlRecursive(p, controlName) as DropDownList;
                    if (ddl != null)
                    {
                        prop.SetValue(o, ddl.SelectedValue, new object[0]);
                        continue;
                    }

                    controlName = prefix + "HiddenField" + prop.Name;
                    HiddenField hf = FindControlRecursive(p, controlName) as HiddenField;
                    if (hf != null)
                    {
                            prop.SetValue(o, hf.Value, new object[0]);
                        continue;
                    }

                }
                if (propType == typeof(int) | propType == typeof(int?) | propType.IsEnum)
                {
                    string controlName = prefix + "TextBox" + prop.Name;
                    TextBox txtBox = FindControlRecursive(p, controlName) as TextBox;
                    if (txtBox != null)
                    {
                        int? intVal = null;
                        if (!string.IsNullOrEmpty(txtBox.Text)) intVal = int.Parse(txtBox.Text);
                        prop.SetValue(o, intVal, new object[0]);
                        continue;
                    }
                    controlName = prefix + "DropDownList" + prop.Name;
                    DropDownList ddl = FindControlRecursive(p, controlName) as DropDownList;
                    if (ddl != null)
                    {
                        var mfa = prop.GetCustomAttributes(typeof(MappedFieldAttribute), true).FirstOrDefault() as MappedFieldAttribute;

                        if (propType.IsEnum)
                            prop.SetValue(o, int.Parse(ddl.SelectedValue), new object[0]);
                        else
                            if (string.IsNullOrEmpty(ddl.SelectedValue))
                                prop.SetValue(o, null, new object[0]);
                            else if (mfa.References != null && int.Parse(ddl.SelectedValue) == 0)
                                prop.SetValue(o, null, new object[0]);
                            else
                                prop.SetValue(o, int.Parse(ddl.SelectedValue), new object[0]);
                        continue;
                    }
                    controlName = prefix + "HiddenField" + prop.Name;
                    HiddenField hf = FindControlRecursive(p, controlName) as HiddenField;
                    if (hf != null)
                    {
                        if (propType.IsEnum)
                            prop.SetValue(o, int.Parse(hf.Value), new object[0]);
                        else
                            if (string.IsNullOrEmpty(hf.Value))
                                prop.SetValue(o, null, new object[0]);
                            else
                                prop.SetValue(o, int.Parse(hf.Value), new object[0]);
                        continue;
                    }
                }

                if (propType == typeof(decimal) | propType == typeof(decimal?))
                {
                    string controlName = prefix + "TextBox" + prop.Name;
                    TextBox txtBox = FindControlRecursive(p, controlName) as TextBox;
                    if (txtBox != null && !string.IsNullOrEmpty(txtBox.Text))
                    {
                        prop.SetValue(o, decimal.Parse(txtBox.Text), new object[0]);
                        continue;
                    }
                }
                if (propType == typeof(DateTime) | propType == typeof(DateTime?))
                {
                    string controlName = prefix + "TextBox" + prop.Name;
                    TextBox txtBox = FindControlRecursive(p, controlName) as TextBox;
                    if (txtBox != null && !string.IsNullOrEmpty(txtBox.Text))
                    {
                        prop.SetValue(o, DateTime.Parse(txtBox.Text), new object[0]);
                        continue;
                    }
                }

                if (propType == typeof(bool?) || propType == typeof(bool?) || propType == typeof(System.Boolean) || propType == typeof(System.Boolean?))
                {
                    string controlName = prefix + "CheckBox" + prop.Name;
                    CheckBox chkBox = FindControlRecursive(p, controlName) as CheckBox;
                    if (chkBox != null)
                    {
                        prop.SetValue(o, chkBox.Checked, new object[0]);
                        continue;
                    }
                }
            }
        }
    }
}