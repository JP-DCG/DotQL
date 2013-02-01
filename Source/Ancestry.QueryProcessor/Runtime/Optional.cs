using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Runtime
{
	public struct Optional<T>
	{
		public Optional(T value)
		{
			_value = value;
			_hasValue = true;
		}

		public Optional(bool hasValue)
		{
			_value = default(T);
			_hasValue = hasValue;
		}

		private T _value;
		private bool _hasValue;

		public bool HasValue { get { return _hasValue; } }

		public T Value 
		{ 
			get
			{
				if (!_hasValue)
					throw new NullReferenceException();
				return _value;
			} 
		}

		public override bool Equals(object obj)
		{
			if (obj is Optional<T>)
			{
				var other = (Optional<T>)obj;
				return this.HasValue == other.HasValue && (!this.HasValue || this.Value.Equals(other.Value));
			}
			else if (obj is T)
			{
				var other = (T)obj;
				return this.HasValue && this.Value.Equals(other);
			}
			else
				return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			var result = HasValue.GetHashCode();
			return result * 83 + (HasValue ? Value.GetHashCode() : 0);
		}

		public override string ToString()
		{
			return HasValue ? Value.ToString() : "";
		}
	}
}
