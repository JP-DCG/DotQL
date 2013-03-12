using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ancestry.QueryProcessor;
using Ancestry.QueryProcessor.Compile;

namespace Ancestry.QueryProcessor.Type
{
	public class FunctionType : BaseType
	{
		private List<FunctionParameter> _parameters = new List<FunctionParameter>();
		public List<FunctionParameter> Parameters { get { return _parameters; } set { _parameters = value; } }

		// TODO: Type parameters
		//private List<FunctionTypeParameter> _parameters = new List<FunctionTypeParameter>();
		//public List<FunctionTypeParameter> Parameters { get { return _parameters; } }

		public BaseType Type { get; set; }


		protected override void EmitBinaryOperator(MethodContext method, Compiler compiler, ExpressionContext left, ExpressionContext right, Parse.BinaryExpression expression)
		{
			switch (expression.Operator)
			{
				default: throw NotSupported(expression);
			}
		}

		protected override void EmitUnaryOperator(MethodContext method, Compiler compiler, ExpressionContext inner, Parse.UnaryExpression expression)
		{
			switch (expression.Operator)
			{
				default: throw NotSupported(expression);
			}
		}

		public override System.Type GetNative(Compile.Emitter emitter)
		{
			var types = (from p in Parameters select p.Type.GetNative(emitter)).ToList();
			types.Add(Type.GetNative(emitter));
			return System.Linq.Expressions.Expression.GetDelegateType(types.ToArray());
		}

		public override int GetHashCode()
		{
			var running = 0;
			foreach (var p in _parameters)
				running = running * 83 + p.Name.GetHashCode() * 83 + p.Type.GetHashCode();
			return running * 83 + Type.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj is FunctionType)
				return (FunctionType)obj == this;
			else
				return base.Equals(obj);
		}

		public static bool operator ==(FunctionType left, FunctionType right)
		{
			return Object.ReferenceEquals(left, right) 
				|| 
				(
					!Object.ReferenceEquals(right, null) 
						&& !Object.ReferenceEquals(left, null)
						&& left.GetType() == right.GetType() 
						&& left.Parameters.SequenceEqual(right.Parameters) 
						&& left.Type == right.Type
				);
		}

		public static bool operator !=(FunctionType left, FunctionType right)
		{
			return !(left == right);
		}

		public override Parse.Expression BuildDefault()
		{
			return
				new Parse.FunctionSelector 
				{ 
					Expression = Type.BuildDefault(),
					Parameters =
					(
						from p in Parameters 
						select new Parse.FunctionParameter { Name = p.Name.ToID(), Type = p.Type.BuildDOM() }
					).ToList()
				};
		}

		public override Parse.TypeDeclaration BuildDOM()
		{
			return 
				new Parse.FunctionType 
				{ 
					ReturnType = Type.BuildDOM(),
					Parameters = 
					(
						from p in Parameters
						select new Parse.FunctionParameter { Name = p.Name.ToID(), Type = p.Type.BuildDOM() }
					).ToList()
					// TODO: type parameters
					// TypeParameters =
				};
		}
	}
}
