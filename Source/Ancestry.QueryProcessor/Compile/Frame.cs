using Ancestry.QueryProcessor.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ancestry.QueryProcessor.Compile
{
	public class Frame
	{
		/// <summary> Fully qualified symbols. </summary>
		private Dictionary<Name, object> _items = new Dictionary<Name, object>();
		/// <summary> Name fragments. </summary>
		private Dictionary<Name, object> _fragments = new Dictionary<Name, object>();

		private Dictionary<object, List<Parse.Statement>> _references = new Dictionary<object,List<Statement>>();
		private Dictionary<object, List<Parse.Statement>> References { get { return _references; } }

		private Frame _baseFrame;
		public Frame BaseFrame { get { return _baseFrame; } }

		public Frame(Frame baseFrame = null)
		{
			_baseFrame = baseFrame;
		}

		public void Add(QualifiedIdentifier id, object symbol)
		{
			Add(id, Name.FromQualifiedIdentifier(id), symbol);
		}

		private class Ambiguity : List<object> {}

		public void Add(Parse.Statement statement, Name name, object symbol)
		{
			if (name.IsRooted)
				throw new CompilerException(statement, CompilerException.Codes.InvalidRootedIdentifier);
			
			// Check for a conflict
			if (_items.ContainsKey(name))
				throw new CompilerException(statement, CompilerException.Codes.IdentifierConflict, name);
			_items.Add(name, symbol);

			// Add each fragment recursively
			for (var i = 1; i < name.Components.Length; i++)
			{
				object existing;
				var attempt = Name.FromComponents(name.Components.Skip(i).ToArray());
				if (_fragments.TryGetValue(attempt, out existing))
				{
					Ambiguity ambiguity;
					if (existing is Ambiguity)
						ambiguity = (Ambiguity)existing;
					else
						ambiguity = new Ambiguity { symbol, existing };
					_fragments[attempt] = ambiguity; 
				}
				else
					_fragments.Add(attempt, symbol);
			}
		}

		/// <summary> Attempts to resolve the given symbol; return null if unable. </summary>
		public object this[Parse.Statement statement, Name id]
		{
			get
			{
				object result = null;
				var rooted = id.IsRooted;
				var current = this;
				while (result == null && current != null)
				{
					if (!current._items.TryGetValue(id, out result) && !rooted)
						current._fragments.TryGetValue(id, out result);
					current = current.BaseFrame;
				}
				if (result is Ambiguity)
					throw new CompilerException(statement, CompilerException.Codes.AmbiguousReference, id);
				return result;
			}
		}

		public T Resolve<T>(QualifiedIdentifier id)
		{
			return Resolve<T>(id, Name.FromQualifiedIdentifier(id));
		}

		/// <summary> Attempts to resolve the given symbol; throws if unable. </summary>
		public T Resolve<T>(Parse.Statement statement, Name id)
		{
			var result = this[statement, id];
			if (result == null)
				throw new CompilerException(statement, CompilerException.Codes.UnknownIdentifier, id.ToString());
			if (!(result is T))
				throw new CompilerException(statement, CompilerException.Codes.IncorrectTypeReferenced, typeof(T), result.GetType());
			return (T)result;
		}
	}
}
