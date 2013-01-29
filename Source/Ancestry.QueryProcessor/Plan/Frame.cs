﻿using Ancestry.QueryProcessor.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ancestry.QueryProcessor.Plan
{
	public class Frame
	{
		private Dictionary<Name, object> _items = new Dictionary<Name, object>();

		private Dictionary<object, List<Parse.Statement>> _references = new Dictionary<object,List<Statement>>();
		private Dictionary<object, List<Parse.Statement>> References { get { return _references; } }

		private Frame _baseFrame;
		public Frame BaseFrame { get { return _baseFrame; } }

		public Frame(Frame baseFrame = null)
		{
			_baseFrame = baseFrame;
		}

		public void Add(Name name, object symbol)
		{
			var existing = this[name];
			if (existing != null)
				throw new PlanningException(PlanningException.Codes.IdentifierConflict, name);
			_items.Add(name, symbol);
		}

		/// <summary> Attempts to resolve the given symbol; return null if unable. </summary>
		public object this[Name id]
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
			return Resolve<T>(Name.FromQualifiedIdentifier(id));
		}

		/// <summary> Attempts to resolve the given symbol; throws if unable. </summary>
		public T Resolve<T>(Name id)
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
			AddNonRooted(Name.FromQualifiedIdentifier(id), symbol);
		}

		public void AddNonRooted(Name id, object symbol)
		{
			if (id.IsRooted)
				throw new PlanningException(PlanningException.Codes.InvalidRootedIdentifier);
			Add(id, symbol); 
		}
	}
}
