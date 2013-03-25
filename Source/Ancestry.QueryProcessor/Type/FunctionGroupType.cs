using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ancestry.QueryProcessor;
using Ancestry.QueryProcessor.Compile;
using System.Reflection;
using System.Reflection.Emit;

namespace Ancestry.QueryProcessor.Type
{
    public class FunctionGroupType : BaseType
    {
        private List<MethodInfo> _methods = new List<MethodInfo>();
        public List<MethodInfo> Methods { get { return _methods; } set { _methods = value; } }

        public BaseType Type { get; set; }

        public override ExpressionContext CompileCallExpression(Compiler compiler, Frame frame, ExpressionContext functionGroup, Parse.CallExpression callExpression, BaseType typeHint)
        {
            var potential = _methods.Where(m => m.GetParameters().Count() == callExpression.Arguments.Count).ToList();
            if (potential.Count == 0)
                throw new Exception("No matching functions");

            //Compile arguments...
            var args = new ExpressionContext[callExpression.Arguments.Count];
            for (var i = 0; i < callExpression.Arguments.Count; i++)
                args[i] = compiler.CompileExpression(frame, callExpression.Arguments[i]);

           MethodInfo function = null;

            if (args.Count() == 0)
                    function = potential[0];
            else
            {
                for(int i = 0; i < potential.Count; ++i)
                {
                    if (compiler.Emitter.TypeFromNative(potential[i].GetParameters()[0].ParameterType) == args[0].Type)
                    {
                        function = potential[i];
                        break;
                    }                        
                }  
            }

            if (function == null)
                throw new Exception("No matching functions");

            var functionType = FunctionType.FromMethod(function, compiler.Emitter);

            return functionType.CompileCallExpression
                (
                    compiler,
                    frame,
                    new ExpressionContext
                    (
                        new Parse.IdentifierExpression { Target = Name.FromNative(function.Name).ToID() },
                        functionType,
                        Characteristic.Constant,
                        null
                    )
                    {
                        Member = function
                    },
                    callExpression,
                    typeHint
                );
        }

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

        public static FunctionGroupType FromMethodGroup(MethodInfo[] methods, Emitter emitter)
        {
            return new FunctionGroupType
            {
                Methods =
                methods.ToList(),
                Type = emitter.TypeFromNative(methods.GetType())
            };
        }

        public override System.Type GetNative(Compile.Emitter emitter)
        {
            throw new NotImplementedException();
        }

        public override int GetHashCode()
        {
            var running = 0;
            foreach (var m in _methods)
                running = running * 83 + m.GetHashCode();
            return running * 83 + Type.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is FunctionGroupType)
                return (FunctionGroupType)obj == this;
            else
                return base.Equals(obj);
        }

        public static bool operator ==(FunctionGroupType left, FunctionGroupType right)
        {
            return Object.ReferenceEquals(left, right)
                ||
                (
                    !Object.ReferenceEquals(right, null)
                        && !Object.ReferenceEquals(left, null)
                        && left.GetType() == right.GetType()
                        && left.Methods.SequenceEqual(right.Methods)
                        && left.Type == right.Type
                );
        }

        public static bool operator !=(FunctionGroupType left, FunctionGroupType right)
        {
            return !(left == right);
        }

        public override Parse.Expression BuildDefault()
        {
            return
                new Parse.FunctionSelector
                {
                    Expression = new Parse.ClausedExpression { Expression = Type.BuildDefault() }
                };
        }

        public override Parse.TypeDeclaration BuildDOM()
        {
            return
                new Parse.FunctionType
                {
                    ReturnType = Type.BuildDOM()
                    // TODO: type parameters
                    // TypeParameters =
                };
        }
    }
}
