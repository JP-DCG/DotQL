using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Parse
{
	public enum Operator
	{
		Unknown,
		In,
		Or,
		Xor,
		Like,
		Matches,
		And,
		Addition,
		BitwiseAnd,
		BitwiseNot,
		BitwiseOr,
		BitwiseXor,
		Compare,
		Divide,
		Equal,
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
		Dereference,
		Successor,
		Predicessor
	}
}
