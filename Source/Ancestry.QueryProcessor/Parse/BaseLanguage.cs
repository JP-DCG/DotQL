using System;
using System.Text;
using System.Collections.Generic;
using System.ComponentModel;

namespace Ancestry.QueryProcessor.Parse
{
    public class LineInfo
    {
		public LineInfo() : this(-1, -1, -1, -1) { }
		public LineInfo(int line, int linePos, int endLine, int endLinePos)
		{
			Line = line;
			LinePos = linePos;
			EndLine = endLine;
			EndLinePos = endLinePos;
		}
		
		public LineInfo(LineInfo lineInfo)
		{
			SetFromLineInfo(lineInfo == null ? StartingOffset : lineInfo);
		}
		
		public int Line;
		public int LinePos;
		public int EndLine;
		public int EndLinePos;
		
		public void SetFromLineInfo(LineInfo lineInfo)
		{
			Line = lineInfo.Line;
			LinePos = lineInfo.LinePos;
			EndLine = lineInfo.EndLine;
			EndLinePos = lineInfo.EndLinePos;
		}

		public static LineInfo Empty = new LineInfo();		
		public static LineInfo StartingOffset = new LineInfo(0, 0, 0, 0);
		public static LineInfo StartingLine = new LineInfo(1, 1, 1, 1);
    }
    
	public abstract class Statement : Object 
	{
		public Statement() : base() {}
		public Statement(Lexer lexer) : base()
		{
			SetPosition(lexer);
		}
		
		// The line and position of the starting token for this element in the syntax tree
		private LineInfo _lineInfo;
		public LineInfo LineInfo { get { return _lineInfo; } set { _lineInfo = value; } }
		
		public int Line
		{
			get { return _lineInfo == null ? -1 : _lineInfo.Line; }
			set
			{
				if (_lineInfo == null)
					_lineInfo = new LineInfo();
				_lineInfo.Line = value;
			}
		}
		
		public int LinePos
		{
			get { return _lineInfo == null ? -1 : _lineInfo.LinePos; }
			set
			{
				if (_lineInfo == null)
					_lineInfo = new LineInfo();
				_lineInfo.LinePos = value;
			}
		}
		
		public int EndLine
		{
			get { return _lineInfo == null ? -1 : _lineInfo.EndLine; }
			set
			{
				if (_lineInfo == null)
					_lineInfo = new LineInfo();
				_lineInfo.EndLine = value;
			}
		}
		
		public int EndLinePos
		{
			get { return _lineInfo == null ? -1 : _lineInfo.EndLinePos; }
			set
			{
				if (_lineInfo == null)
					_lineInfo = new LineInfo();
				_lineInfo.EndLinePos = value;
			}
		}
		
		public void SetPosition(Lexer lexer)
		{
			SetPosition(lexer[0, false]);
		}

		public void SetPosition(LexerToken token)
		{
			Line = token.Line;
			LinePos = token.LinePos;
		}
		
		public void SetEndPosition(Lexer lexer)
		{
			SetEndPosition(lexer[0, false]);
		}

		public void SetEndPosition(LexerToken token)
		{
			EndLine = token.Line;
			EndLinePos = token.LinePos + (token.Token == null ? 0 : token.Token.Length);
		}

		public void SetLineInfo(LineInfo lineInfo)
		{
			if (lineInfo != null)
			{
				Line = lineInfo.Line;
				LinePos = lineInfo.LinePos;
				EndLine = lineInfo.EndLine;
				EndLinePos = lineInfo.EndLinePos;
			}
		}

		public virtual IEnumerable<Statement> GetChildren()
		{
			yield break;
		}
	}
}
