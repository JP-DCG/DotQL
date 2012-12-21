using System;
using System.Collections.Generic;
using System.Text;

namespace Ancestry.QueryProcessor.Parse
{

	public abstract class TextEmitter
	{
		private StringBuilder _text;
		private int _indent;
		
		protected void IncreaseIndent()
		{
			_indent++;
		}
		
		protected void DecreaseIndent()
		{
			if (_indent > 0)
				_indent--;
		}
		
		protected void NewLine()
		{
			_text.Append("\r\n");
		}
		
		protected void Indent()
		{
			for (int index = 0; index < _indent; index++)
				_text.Append("\t");
		}
		
		protected void Append(string stringValue)
		{
			_text.Append(stringValue);
		}
		
		protected void AppendFormat(string stringValue, params object[] paramsValue)
		{
			_text.AppendFormat(stringValue, paramsValue);
		}
		
		protected void AppendLine(string stringValue)
		{
			Indent();
			Append(stringValue);
			NewLine();
		}
		
		protected void AppendFormatLine(string stringValue, params object[] paramsValue)
		{
			Indent();
			AppendFormat(stringValue, paramsValue);
			NewLine();
		}

		protected abstract void InternalEmit(Statement statement);
		
		public string Emit(Statement statement)
		{
			_text = new StringBuilder();
			_indent = 0;
			InternalEmit(statement);
			return _text.ToString();
		}
	}
}
