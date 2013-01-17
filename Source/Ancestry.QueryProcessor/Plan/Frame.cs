using Ancestry.QueryProcessor.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ancestry.QueryProcessor.Plan
{
	public class Frame
	{
		private Dictionary<QualifiedIdentifier, ISymbol> _items = new Dictionary<QualifiedIdentifier, ISymbol>();

		private Dictionary<ISymbol, List<Parse.Statement>> _references = new Dictionary<ISymbol,List<Statement>>();
		private Dictionary<ISymbol, List<Parse.Statement>> References { get { return _references; } }

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
			_items.Add(name, symbol);
		}

		/// <summary> Attempts to resolve the given symbol; return null if unable. </summary>
		public ISymbol this[QualifiedIdentifier id]
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

		/// <summary> Attempts to resolve the given symbol; throws if unable. </summary>
		public ISymbol Resolve(QualifiedIdentifier id, Statement statement)
		{
			var result = this[id];
			if (result == null)
				throw new PlanningException(PlanningException.Codes.UnknownIdentifier, id.ToString());
			return result;
		}

		/// <summary> Attempts to resolve the given symbol; throws if unable. </summary>
		public T Resolve<T>(QualifiedIdentifier id)
		{
			var result = this[id];
			if (result == null)
				throw new PlanningException(PlanningException.Codes.UnknownIdentifier, id.ToString());
			if (!(result is T))
				throw new PlanningException(PlanningException.Codes.IncorrectTypeReferenced, typeof(T).Name, result.Name);
			return (T)result;
		}

		public List<T> ResolveEach<T>(IEnumerable<Parse.QualifiedIdentifier> list)
		{
			var resolved = new List<T>();
			foreach (var i in list)
				resolved.Add(Resolve<T>(i));
			return resolved;
		}

		public void AddNonRooted(QualifiedIdentifier id, ISymbol symbol)
		{
			if (id.IsRooted)
				throw new PlanningException(PlanningException.Codes.InvalidRootedIdentifier);
			Add(id, symbol); 
		}
	}
}
