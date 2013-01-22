using Ancestry.QueryProcessor.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ancestry.QueryProcessor.Plan
{
	public class Frame
	{
		private Dictionary<QualifiedID, object> _items = new Dictionary<QualifiedID, object>();

		private Dictionary<object, List<Parse.Statement>> _references = new Dictionary<object,List<Statement>>();
		private Dictionary<object, List<Parse.Statement>> References { get { return _references; } }

		private Frame _baseFrame;
		public Frame BaseFrame { get { return _baseFrame; } }

		public Frame(Frame baseFrame = null)
		{
			_baseFrame = baseFrame;
		}

		public void Add(QualifiedID name, object symbol)
		{
			var existing = this[name];
			if (existing != null)
				throw new PlanningException(PlanningException.Codes.IdentifierConflict, name);
			_items.Add(name, symbol);
		}

		/// <summary> Attempts to resolve the given symbol; return null if unable. </summary>
		public object this[QualifiedID id]
		{
			get
			{
				// TODO: attempt with dequalified names
				object result = null;
				var current = this;
				while (result == null && current != null)
				{
					current._items.TryGetValue(id, out result);
					current = current.BaseFrame;
				}
				return result;
			}
		}

		public T Resolve<T>(QualifiedIdentifier id)
		{
			return Resolve<T>(QualifiedID.FromQualifiedIdentifier(id));
		}

		/// <summary> Attempts to resolve the given symbol; throws if unable. </summary>
		public T Resolve<T>(QualifiedID id)
		{
			var result = this[id];
			if (result == null)
				throw new PlanningException(PlanningException.Codes.UnknownIdentifier, id.ToString());
			if (!(result is T))
				throw new PlanningException(PlanningException.Codes.IncorrectTypeReferenced, typeof(T), result.GetType());
			return (T)result;
		}

		public void AddNonRooted(QualifiedIdentifier id, object symbol)
		{
			AddNonRooted(QualifiedID.FromQualifiedIdentifier(id), symbol);
		}

		public void AddNonRooted(QualifiedID id, object symbol)
		{
			if (id.IsRooted)
				throw new PlanningException(PlanningException.Codes.InvalidRootedIdentifier);
			Add(id, symbol); 
		}
	}
}
