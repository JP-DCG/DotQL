using Ancestry.QueryProcessor.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ancestry.QueryProcessor.Plan
{
	public class Frame : Dictionary<QualifiedIdentifier, ISymbol>
	{
		private Frame _baseFrame;
		public Frame BaseFrame { get { return _baseFrame; } }

		public Frame(Frame baseFrame = null)
		{
			_baseFrame = baseFrame;
		}

		public void Add(QualifiedIdentifier name, ISymbol symbol)
		{
			var existing = this[name];
			if (existing != null)
				throw new PlanningException(PlanningException.Codes.IdentifierConflict, existing.Name);
		}

		public ISymbol this[QualifiedIdentifier id]
		{
			get
			{
				// TODO: attempt with dequalified names
				ISymbol result = null;
				var current = this;
				while (result == null && current != null)
				{
					current.TryGetValue(id, out result);
					current = current.BaseFrame;
				}
				return result;
			}
		}
	}
}
