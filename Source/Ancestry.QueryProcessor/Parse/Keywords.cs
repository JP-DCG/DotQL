using System;
using System.Collections;
using System.Reflection;

namespace Ancestry.QueryProcessor.Parse
{
    /// <summary>DotQL keywords</summary>    
    public class Keywords
    {
        public const string Using = "using";
        public const string Module = "module";
        public const string TypeDef = "typedef";
        public const string Enum = "enum";
		public const string Ref = "ref";
		public const string Const = "const";
        public const string Var = "var";
        public const string Set = "set";
		public const string Function = "function";
		public const string IntervalType = "interval";
        public const string For = "for";
        public const string Let = "let";
        public const string Where = "where";
		public const string Order = "order";
		public const string Return = "return";
		public const string Index = "index";
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
        public const string As = "as";
        public const string Case = "case";
        public const string When = "when";
        public const string Then = "then";
        public const string If = "if";
		public const string Else = "else";
		public const string End = "end";
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
		public const string Successor = "++";
		public const string Predicessor = "--";
        public const string Multiplication = "*";
        public const string Division = "/";
        public const string Mod = "%";
        public const string Power = "**";
        public const string BitwiseNot = "~";
        public const string BeginGroup = "(";
        public const string EndGroup = ")";
        public const string BeginIndexer = "[";
        public const string EndIndexer = "]";
		public const string BeginSet = "{";
		public const string EndSet = "}";
        public const string Dereference = ".";
		public const string Embed = "#";
		public const string Qualifier = "\\";
		public const string IntervalValue = "..";
		public const string Assignment = ":=";
        
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