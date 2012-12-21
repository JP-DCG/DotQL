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
		Addition,
		BitwiseAnd,
		BitwiseNot,
		BitwiseOr,
		Compare,
		Div,
		Division,
		Equal,
		Exists,
		Greater,
		InclusiveGreater,
		Lestt,
		Mod,
		Multiplications,
		Negate,
		Not,
		NotEqual,
		BitwiseXor,
		InclusiveLess,
		Less,
		Multiplication,
		Power,
		ShiftLeft,
		ShiftRight,
		Subtraction,
		IsNull,
		IsNotNull,
		Concat
	}
}
