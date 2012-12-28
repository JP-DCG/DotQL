using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Parse
{
	public enum Operator
	{
		In,
		Or,
		Xor,
		Like,
		Matches,
		And,
		Add,
		BitwiseAnd,
		BitwiseNot,
		BitwiseOr,
		BitwiseXor,
		Compare,
		Divide,
		Equals,
		Exists,
		Greater,
		InclusiveGreater,
		Modulus,
		Negate,
		Not,
		NotEqual,
		InclusiveLess,
		Less,
		Multiply,
		Power,
		ShiftLeft,
		ShiftRight,
		Subtract,
		IsNull,
		IfNull,
		Dereference
	}
}
