using System;
using System.Collections;
using System.Reflection;

namespace Ancestry.QueryProcessor.Parse
{
    /// <summary>DotQL reserved words that are not keywords (keywords are assumed reserved).</summary>    
    public class ReservedWords
    {
		// Reserved only
		public const string Args = "args";
		public const string Value = "value";
		public const string Index = "index";
		public const string Self = "self";

		// Also keywords
		public const string TypeOf = "typeof";
		public const string Enum = "enum";
		public const string Const = "const";
		public const string Var = "var";
		public const string TypeDef = "typedef";
		public const string Ref = "ref";
		public const string Key = "key";
        public const string Set = "set";
		public const string IntervalType = "interval";
		public const string Try = "try";
		public const string Catch = "catch";
        public const string For = "for";
        public const string Let = "let";
        public const string Where = "where";
		public const string Order = "order";
		public const string Return = "return";
        public const string In = "in";
        public const string Asc = "asc";
        public const string Desc = "desc";
        public const string Or = "or";
        public const string Xor = "xor";
        public const string Like = "like";
        public const string Matches = "matches";
        public const string And = "and";
        public const string Not = "not";
		public const string Exists = "exists";
        public const string True = "true";
        public const string False = "false";
        public const string Null = "null";
        public const string Void = "void";
        public const string Of = "of";
        public const string Case = "case";
        public const string When = "when";
        public const string Then = "then";
		public const string Strict = "strict";
        public const string If = "if";
		public const string Else = "else";
		public const string End = "end";

        private static string[] FReservedWords;
        
        private static void PopulateReservedWords()
        {
			FieldInfo[] LFields = typeof(ReservedWords).GetFields();

			int LFieldCount = 0;
			foreach (FieldInfo LField in LFields)
				if (LField.FieldType.Equals(typeof(string)) && LField.IsLiteral)
					LFieldCount++;

			FReservedWords = new string[LFieldCount];

			int LFieldCounter = 0;
			foreach (FieldInfo LField in LFields)
				if (LField.FieldType.Equals(typeof(string)) && LField.IsLiteral)
				{
					FReservedWords[LFieldCounter] = (string)LField.GetValue(null);
					LFieldCounter++;
				}
        }
        
        public static bool Contains(string AIdentifier)
        {
			if (FReservedWords == null)
				PopulateReservedWords();
				
			return ((IList)FReservedWords).Contains(AIdentifier);
        }
    }
}