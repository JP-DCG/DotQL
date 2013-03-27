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

		public void Add(ID id, object symbol)
		{
			Add(id, Name.FromID(id), symbol);
		}

		private class Ambiguity : List<object> {}

		public void AddFunction(Parse.Statement statement, Name name, object symbol)
		{
			if (name.IsRooted)
				throw new CompilerException(statement, CompilerException.Codes.InvalidRootedIdentifier);

			object group;
			if (_items.TryGetValue(name, out group))
			{
				// Check for a conflict with another type
				if (!(group is List<object>))
					throw new CompilerException(statement, CompilerException.Codes.IdentifierConflict, name);
				else
					((List<object>)group).Add(symbol);
			}
			else
				_items.Add(name, new List<object> { symbol });
		}

		public void Add(Parse.Statement statement, Name name, object symbol)
		{
			if (name.IsRooted)
				throw new CompilerException(statement, CompilerException.Codes.InvalidRootedIdentifier);
			
			// Check for a conflict
			if (_items.ContainsKey(name))
				throw new CompilerException(statement, CompilerException.Codes.IdentifierConflict, name);
			_items.Add(name, symbol);

			InternalAdd(name, symbol);
		}

		private void InternalAdd(Name name, object symbol)
		{
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

		public T Resolve<T>(ID id)
		{
			return Resolve<T>(id, Name.FromID(id));
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

		/// <summary> Attempts to resolve the given symbol as a function; throws if unable. </summary>
		/// <remarks> If a function group is found; this returns all matching functions in all frames.  If a non-function symbol is found first, an exception is thrown. </remarks>
		public List<object> ResolveFunction(Parse.Statement statement, Name id)
		{
			var functions = new List<object>();
			var rooted = id.IsRooted;
			var current = this;
			while (current != null)
			{
				object result;
				if (!current._items.TryGetValue(id, out result) && !rooted)
					current._fragments.TryGetValue(id, out result);
				if (result != null)
				{
					if (result is Ambiguity)
						throw new CompilerException(statement, CompilerException.Codes.AmbiguousReference, id);
					if (result is List<object>)
						functions.AddRange((List<object>)result);
					else if (functions.Count == 0)
						throw new CompilerException(statement, CompilerException.Codes.AmbiguousReference, id);
				}
				current = current.BaseFrame;
			}

			if (functions.Count() == 0)
				throw new CompilerException(statement, CompilerException.Codes.UnknownIdentifier, id.ToString());

			return functions;
		}
	}
}
