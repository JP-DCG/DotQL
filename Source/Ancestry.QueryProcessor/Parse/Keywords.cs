using System;
using System.Collections;
using System.Reflection;

namespace Ancestry.QueryProcessor.Parse
{
    /// <summary>AQL keywords</summary>    
    public class Keywords
    {
        public const string Select = "select";
        public const string Insert = "insert";
        public const string Update = "update";
        public const string Delete = "delete";
        public const string Old = "old";
        public const string New = "new";
        public const string Where = "where";
		public const string Having = "having";
		public const string Group = "group";
        public const string Order = "order";
        public const string By = "by";
        public const string On = "on";
        public const string As = "as";
        public const string Union = "union";
        public const string Intersect = "intersect";
        public const string Minus = "minus";
        public const string Except = "except";
        public const string Cross = "cross";
        public const string Inner = "inner";
        public const string Outer = "outer";
        public const string Left = "left";
        public const string Right = "right";
        public const string Join = "join";
		public const string Table = "table";
        public const string In = "in";
        public const string Or = "or";
        public const string Xor = "xor";
        public const string Like = "like";
        public const string Between = "between";
        public const string Matches = "matches";
        public const string And = "and";
        public const string Not = "not";
        public const string Exists = "exists";
        public const string Is = "is";
        public const string Null = "null";
        public const string BitwiseAnd = "&";
        public const string BitwiseOr = "|";
        public const string BitwiseXor = "^";
        public const string ShiftLeft = "<<";
        public const string ShiftRight = ">>";
        public const string Equal = "=";
        public const string NotEqual = "<>";
        public const string Less = "<";
        public const string Greater = ">";
        public const string InclusiveLess = "<=";
        public const string InclusiveGreater = ">=";
        public const string Compare = "?=";
        public const string Addition = "+";
        public const string Subtraction = "-";
        public const string Multiplication = "*";
        public const string Division = "/";
        public const string Div = "div";
        public const string Mod = "mod";
        public const string Power = "**";
        public const string BitwiseNot = "~";
        public const string BeginGroup = "(";
        public const string EndGroup = ")";
        public const string BeginIndexer = "[";
        public const string EndIndexer = "]";
        public const string From = "from";
        public const string When = "when";
        public const string Then = "then";
        public const string Else = "else";
        public const string Case = "case";
        public const string End = "end";
        public const string Qualifier = ".";
        public const string ListSeparator = ",";
        public const string StatementTerminator = ";";
        public const string Distinct = "distinct";
        public const string All = "all";
        public const string Asc = "asc";
        public const string Desc = "desc";
        public const string Into = "into";
        public const string Set = "set";
        public const string Values = "values";
        public const string Var = "var";
        public const string Star = "*";
        public const string Sum = "sum";
        public const string Min = "min";
        public const string Max = "max";
        public const string Avg = "avg";
        public const string Count = "count";

		public const string Primary = "primary";
		public const string Foreign = "foreign";
		public const string Key = "key";
		public const string Create = "create";
		public const string Alter = "alter";
		public const string Drop = "drop";
		public const string Add = "add";
		public const string View = "view";
		public const string Column = "column";
		public const string Index = "index";
		public const string Unique = "unique";
		public const string Check = "check";
		public const string Constraint = "constraint";
		public const string References = "references";
		public const string Value = "value";
		public const string Cascade = "cascade";
		public const string Default = "default";
		public const string Clustered = "clustered";
        
        private static string[] FKeywords;
        
        private static void PopulateKeywords()
        {
			FieldInfo[] LFields = typeof(Keywords).GetFields();

			int LFieldCount = 0;
			foreach (FieldInfo LField in LFields)
				if (LField.FieldType.Equals(typeof(string)) && LField.IsLiteral)
					LFieldCount++;

			FKeywords = new string[LFieldCount];

			int LFieldCounter = 0;
			foreach (FieldInfo LField in LFields)
				if (LField.FieldType.Equals(typeof(string)) && LField.IsLiteral)
				{
					FKeywords[LFieldCounter] = (string)LField.GetValue(null);
					LFieldCounter++;
				}
        }
        
        public static bool Contains(string AIdentifier)
        {
			if (FKeywords == null)
				PopulateKeywords();
				
			return ((IList)FKeywords).Contains(AIdentifier);
        }
    }
}