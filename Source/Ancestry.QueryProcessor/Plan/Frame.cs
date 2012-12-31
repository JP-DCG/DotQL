using Ancestry.QueryProcessor.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ancestry.QueryProcessor.Plan
{
	public class Frame : Dictionary<QualifiedIdentifier, Statement>
	{
		private Frame _baseFrame;
		public Frame BaseFrame { get { return _baseFrame; } }

		public Frame(Frame baseFrame = null)
		{
			_baseFrame = baseFrame;
		}

		public Statement this[QualifiedIdentifier id]
		{
			get
			{
				Statement result = null;
				//var current = id;
				//while (result == null && current.IsQualified
				return result;
			}
		}
	}
}
