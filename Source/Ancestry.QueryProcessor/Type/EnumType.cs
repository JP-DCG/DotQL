using Ancestry.QueryProcessor.Compile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	public class EnumType: BaseType
	{
		public EnumType(Name name)
		{
			Name = name;
		}

		public Name Name { get; private set; }
		public System.Type Native { get; set; }

		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj is EnumType)
				return (EnumType)obj == this;
			else
				return base.Equals(obj);
		}

		public static bool operator ==(EnumType left, EnumType right)
		{
			return Object.ReferenceEquals(left, right)
				||
				(
					!Object.ReferenceEquals(right, null)
						&& !Object.ReferenceEquals(left, null)
						&& left.GetType() == right.GetType()
						&& left.Name == right.Name
				);
		}

		public static bool operator !=(EnumType left, EnumType right)
		{
			return !(left == right);
		}

		public override string ToString()
		{
			return Name.ToString();
		}
		
		public override System.Type GetNative(Compile.Emitter emitter)
		{
			return Native;
		}

		public override Parse.Expression BuildDefault()
		{
			return new Parse.LiteralExpression { Value = ReflectionUtility.GetDefaultValue(Native) };
		}

		public override Parse.TypeDeclaration BuildDOM()
		{
			return new Parse.NamedType { Target = Name.ToID() };
		}

		// TODO: define operators for Enum

		public override void EmitLiteral(Compile.MethodContext method, object value)
		{
			method.IL.Emit(OpCodes.Ldc_I4, (int)value);
		}
	}
}
