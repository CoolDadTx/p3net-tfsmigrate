/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfsMigrate.WorkItemTracking
{
    public class WorkItemFieldValue
    {
        #region Construction

        public WorkItemFieldValue () : this("", null)
        { }

        public WorkItemFieldValue ( string key, object value )
        {
            Name = key ?? "";
            Value = value;
        }

        public WorkItemFieldValue ( KeyValuePair<string, object> pair ) : this(pair.Key, pair.Value)
        { }
        #endregion

        public string Name { get; set; }

        public object Value { get; set; }

        public override string ToString () => Name;

        public string ValueAsString ( ) => Value?.ToString() ?? "";
    }
}
