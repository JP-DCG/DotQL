using Ancestry.QueryProcessor.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ancestry.QueryProcessor.Plan
{
	public class Frame
	{
		private Dictionary<QualifiedID, ISymbol> _items = new Dictionary<QualifiedID, ISymbol>();

		private Dictionary<ISymbol, List<Parse.Statement>> _references = new Dictionary<ISymbol,List<Statement>>();
		private Dictionary<ISymbol, List<Parse.Statement>> References { get { return _references; } }

		private Frame _baseFrame;
		public Frame BaseFrame { get { return _baseFrame; } }

		public Frame(Frame baseFrame = null)
		{
			_baseFrame = baseFrame;
		}

		public void Add(QualifiedID name, ISymbol symbol)
		{
			var existing = this[name];
			if (existing != null)
				throw new PlanningException(PlanningException.Codes.IdentifierConflict, existing.Name);
			_items.Add(name, symbol);
		}

		/// <summary> Attempts to resolve the given symbol; return null if unable. </summary>
		public ISymbol this[QualifiedID id]
		{
			get
			{
				// TODO: attempt with dequalified names
				ISymbol result = null;
				var current = this;
				while (result == null && current != null)
				{
					current._items.TryGetValue(id, out result);
					current = current.BaseFrame;
				}
				return result;
			}
		}

		public ISymbol Resolve(QualifiedIdentifier id)
		{
			return Resolve(QualifiedID.FromQualifiedIdentifier(id));
		}

		/// <summary> Attempts to resolve the given symbol; throws if unable. </summary>
		public ISymbol Resolve(QualifiedID id)
		{
			var result = this[id];
			if (result == null)
				throw new PlanningException(PlanningException.Codes.UnknownIdentifier, id.ToString());
			return result;
		}

		public void AddNonRooted(QualifiedIdentifier id, ISymbol symbol)
		{
			AddNonRooted(QualifiedID.FromQualifiedIdentifier(id), symbol);
		}

		public void AddNonRooted(QualifiedID id, ISymbol symbol)
		{
			if (id.IsRooted)
				throw new PlanningException(PlanningException.Codes.InvalidRootedIdentifier);
			Add(id, symbol); 
		}
	}
}
