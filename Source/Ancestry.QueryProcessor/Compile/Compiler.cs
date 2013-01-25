using Ancestry.QueryProcessor.Execute;
using Ancestry.QueryProcessor.Plan;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace Ancestry.QueryProcessor.Compile
{
	public class Compiler
	{
		private AssemblyName _assemblyName;
		private AssemblyBuilder _assembly;
		private ModuleBuilder _module;

		private Dictionary<Type.TupleType, System.Type> _tupleToNative;
		private Dictionary<object, Expression> _paramsBySymbol = new Dictionary<object, Expression>();

		private ParameterExpression _args;
		private ParameterExpression _factory;
		private ParameterExpression _cancelToken;

		private QueryOptions _options;
		private bool _debugOn;
		private SymbolDocumentInfo _symbolDocument;
		private ISymbolDocumentWriter _symbolWriter;

		public Compiler(QueryOptions actualOptions, bool debugOn, string assemblyName, string sourceFileName)
		{
			_options = actualOptions;
			_debugOn = debugOn;
			_assemblyName = new AssemblyName(assemblyName);
			_symbolDocument = Expression.SymbolDocument(sourceFileName);

			_assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(_assemblyName, AssemblyBuilderAccess.RunAndSave);// TODO: temp for debugging .RunAndCollect);
			if (_debugOn)
				_assembly.SetCustomAttribute
				(
					new CustomAttributeBuilder
					(
						typeof(DebuggableAttribute).GetConstructor
						(
							new System.Type[] { typeof(DebuggableAttribute.DebuggingModes) }
						),
						new object[] 
						{ 
							DebuggableAttribute.DebuggingModes.DisableOptimizations | 
							DebuggableAttribute.DebuggingModes.Default 
						}
					)
				);
			_module = _assembly.DefineDynamicModule(_assemblyName.Name + ".dll", _debugOn);
			if (_debugOn)
				_symbolWriter = _module.DefineDocument(sourceFileName, Guid.Empty, Guid.Empty, Guid.Empty);

			_tupleToNative = new Dictionary<Type.TupleType, System.Type>();
		}

		public ExecuteHandler CreateExecutable(ScriptPlan plan)
		{
			//// TODO: setup app domain with appropriate cache path, shadow copying etc.
			//var domainName = "plan" + DateTime.Now.Ticks.ToString();
			//var domain = AppDomain.CreateDomain(domainName);
			//var an = new AssemblyName("Dynamic." + domainName);

			//// TODO: use RunAndCollect for transient execution
			//var assembly = domain.DefineDynamicAssembly(an, AssemblyBuilderAccess.RunAndSave);

			//var module = assembly.DefineDynamicModule(an.Name + ".dll");

			//var type = module.DefineType("Executable", TypeAttributes.Public, typeof(ExecutableBase));

			//var method = type.DefineMethod("Execute", MethodAttributes.Static | MethodAttributes.Public);

			_args = Expression.Parameter(typeof(Dictionary<string, object>), "args");
			_factory = Expression.Parameter(typeof(Storage.IRepositoryFactory), "factory");
			_cancelToken = Expression.Parameter(typeof(CancellationToken), "cancelToken");

			var execute = 
				Expression.Lambda<ExecuteHandler>
				(
					CompileScript(plan),
					_args,
					_factory,
					_cancelToken
				);

			_module.CreateGlobalFunctions();
			//_assembly.Save("qpdebug.dll");

			//var pdbGenerator = _debugOn ? System.Runtime.CompilerServices.DebugInfoGenerator.CreatePdbGenerator() : null;
			return execute.Compile();//pdbGenerator);

			//// TODO: Pass debug info
			//execute.CompileToMethod(method);
		}

		private Expression CompileScript(ScriptPlan plan)
		{
			var script = plan.Script;
			var vars = new List<ParameterExpression>();
			var block = new List<Expression>();

			foreach (var u in script.Usings)
			{
				var module = FindReference<System.Type>(plan, u.Target);
			}

			// Compute result and convert to object (box if needed)
			if (script.Expression != null)
			{
				var result = CompileClausedExpression(plan, script.Expression);
				if (result.Type.IsValueType)
					result = Expression.Convert(result, typeof(object));

				if (_debugOn)
					block.Add(GetDebugInfo(script));
				block.Add(result);

				return Expression.Block(vars, result);
			}
			else
				return Expression.Constant(null, typeof(object));
		}

		private static T FindReference<T>(ScriptPlan plan, Parse.QualifiedIdentifier id)
		{
			object module;
			if (!plan.References.TryGetValue(id, out module))
				throw new CompilerException(CompilerException.Codes.IdentifierNotFound, id.ToString());
			if (!(module is T))
				throw new CompilerException(CompilerException.Codes.IncorrectType, module.GetType(), typeof(T));
			return (T)module;
		}

		private DebugInfoExpression GetDebugInfo(Parse.Statement statement)
		{
			return Expression.DebugInfo
			(
				_symbolDocument, 
				statement.Line + 1, 
				statement.LinePos + 1, 
				(statement.EndLine < 0 ? statement.Line : statement.EndLine) + 1, 
				(statement.EndLinePos < 0 ? statement.LinePos : statement.EndLinePos) + 1
			);
		}

		private Expression CompileClausedExpression(ScriptPlan plan, Parse.ClausedExpression clausedExpression)
		{
			var vars = new List<ParameterExpression>();

			if (clausedExpression.ForClauses.Count > 0)
			//// TODO: foreach (var forClause in clausedExpression.ForClauses)
			{
				var forClause = clausedExpression.ForClauses[0];
				var forExpression = CompileExpression(plan, forClause.Expression);
				var elementType = 
					forExpression.Type.IsConstructedGenericType
						? forExpression.Type.GetGenericArguments()[0]
						: forExpression.Type.GetElementType();
				var enumerableType = typeof(IEnumerable<>).MakeGenericType(elementType);
				var enumeratorType = typeof(IEnumerator<>).MakeGenericType(elementType);
				var enumerator = Expression.Parameter(enumeratorType, "enumerator");
				vars.Add(enumerator);
				var forVariable = Expression.Variable(elementType, forClause.Name.ToString());
				_paramsBySymbol.Add(forClause, forVariable);
				vars.Add(forVariable);

				var returnBlock = CompileClausedReturn(plan, clausedExpression, vars);
				var resultIsSet = clausedExpression.OrderDimensions.Count == 0
					&& forExpression.Type.IsConstructedGenericType
					&& (forExpression.Type.GetGenericTypeDefinition() == typeof(HashSet<>));
				var resultType = resultIsSet 
					? typeof(HashSet<>).MakeGenericType(returnBlock.Type)
					: typeof(List<>).MakeGenericType(returnBlock.Type);
				var resultVariable = Expression.Variable(resultType, "result");
				vars.Add(resultVariable);
				var resultAddMethod = resultType.GetMethod("Add");
				var breakLabel = Expression.Label("break");


				return Expression.Block
				(
					vars,
					GetDebugInfo(clausedExpression),
					Expression.Assign(enumerator, Expression.Call(forExpression, enumerableType.GetMethod("GetEnumerator"))),
					Expression.Assign(resultVariable, Expression.New(resultType)),
					Expression.Loop
					(
						Expression.IfThenElse
						(
							Expression.Call(enumerator, typeof(IEnumerator).GetMethod("MoveNext")),
							Expression.Block
							(
								Expression.Assign(forVariable, Expression.Property(enumerator, enumeratorType.GetProperty("Current"))),
								
								clausedExpression.WhereClause == null
									? (Expression)Expression.Call(resultVariable, resultAddMethod, returnBlock)
									: Expression.IfThen
									(
										CompileExpression(plan, clausedExpression.WhereClause, typeof(bool)), 
										Expression.Call(resultVariable, resultAddMethod, returnBlock)
									)
							),
							Expression.Break(breakLabel)
						),
						breakLabel
					),
					resultVariable
				);
			}

			return Expression.Block(vars, CompileClausedReturn(plan, clausedExpression, vars));
		}

		private Expression CompileClausedReturn(ScriptPlan plan, Parse.ClausedExpression clausedExpression, List<ParameterExpression> vars)
		{
			var blocks = new List<Expression> { GetDebugInfo(clausedExpression) };

			// Create a variable for each let and initialize
			foreach (var let in clausedExpression.LetClauses)
			{
				var compiledExpression = CompileExpression(plan, let.Expression);
				var variable = Expression.Variable(compiledExpression.Type, let.Name.ToString());
				blocks.Add(Expression.Assign(variable, compiledExpression));
				_paramsBySymbol.Add(let, variable);
				vars.Add(variable);
			}

			// Add the expression to the body
			blocks.Add(CompileExpression(plan, clausedExpression.Expression));

			return Expression.Block(blocks);
		}

		private Expression CompileExpression(ScriptPlan plan, Parse.Expression expression, System.Type typeHint = null)
		{
			switch (expression.GetType().Name)
			{
				case "LiteralExpression": return CompileLiteral(plan, (Parse.LiteralExpression)expression);
				case "BinaryExpression": return CompileBinaryExpression(plan, (Parse.BinaryExpression)expression);
				case "ClausedExpression": return CompileClausedExpression(plan, (Parse.ClausedExpression)expression);
				case "IdentifierExpression": return CompileIdentifierExpression(plan, (Parse.IdentifierExpression)expression);
				case "TupleSelector": return CompileTupleSelector(plan, (Parse.TupleSelector)expression);
				case "ListSelector": return CompileListSelector(plan, (Parse.ListSelector)expression);
				case "SetSelector": return CompileSetSelector(plan, (Parse.SetSelector)expression);
				case "CallExpression": return CompileCallExpression(plan, (Parse.CallExpression)expression);
				default : throw new NotSupportedException(String.Format("Expression type {0} is not supported", expression.GetType().Name));
			}
		}

		private Expression CompileCallExpression(ScriptPlan plan, Parse.CallExpression callExpression)
		{
			var expression = CompileExpression(plan, callExpression.Expression);
			var args = new Expression[callExpression.Arguments.Count];
			for (var i = 0; i < callExpression.Arguments.Count; i++)
				args[i] = CompileExpression(plan, callExpression.Arguments[i]);
			return Expression.Invoke(expression, args);
		}

		private Expression CompileSetSelector(ScriptPlan plan, Parse.SetSelector setSelector)
		{
			// Compile each item's expression
			var initializers = new ElementInit[setSelector.Items.Count];
			System.Type type = null;
			System.Type setType = null;
			MethodInfo addMethod = null;
			for (var i = 0; i < setSelector.Items.Count; i++)
			{
				var expression = CompileExpression(plan, setSelector.Items[i], type);
				if (type == null)
				{
					type = expression.Type;
					GetSetTypeAndAddMethod(type, ref setType, ref addMethod);
				}
				else if (type != expression.Type)
					expression = Convert(expression, type);
				initializers[i] = Expression.ElementInit(addMethod, expression);
			}
			if (type == null)
			{
				type = typeof(void);
				GetSetTypeAndAddMethod(type, ref setType, ref addMethod);
			}

			return Expression.ListInit(Expression.New(setType), initializers);
		}

		private static void GetSetTypeAndAddMethod(System.Type type, ref System.Type setType, ref MethodInfo addMethod)
		{
			setType = typeof(HashSet<>).MakeGenericType(type);
			addMethod = setType.GetMethod("Add");
		}

		private Expression CompileListSelector(ScriptPlan plan, Parse.ListSelector listSelector)
		{
			// Compile each item's expression
			var initializers = new Expression[listSelector.Items.Count];
			System.Type type = null;
			for (var i = 0; i < listSelector.Items.Count; i++)
			{
				var expression = CompileExpression(plan, listSelector.Items[i], type);
				if (type == null)
					type = expression.Type;
				else if (type != expression.Type)
					expression = Convert(expression, type);
				initializers[i] = expression;
			}
			if (type == null)
				type = typeof(void);

			return Expression.NewArrayInit(type, initializers);
		}

		private Expression Convert(Expression expression, System.Type type)
		{
			throw new NotImplementedException();
		}

		private Expression CompileTupleSelector(ScriptPlan plan, Parse.TupleSelector tupleSelector)
		{
			var bindings = new List<MemberBinding>();
			var expressions = new Dictionary<Parse.AttributeSelector, Expression>();
			
			// Compile attributes
			foreach (var attribute in tupleSelector.Attributes)
				expressions.Add(attribute, CompileExpression(plan, attribute.Value));

			var tupleType = TupeTypeFromTupleSelector(tupleSelector, expressions);
			var type = FindOrCreateNativeFromTupleType(tupleType);
			
			// Create initialization bindings for each field
			foreach (var attr in tupleSelector.Attributes)
			{
				var binding = Expression.Bind(type.GetField(QualifiedID.FromQualifiedIdentifier(attr.Name).ToString()), expressions[attr]);
				bindings.Add(binding);
			}

			return Expression.MemberInit(Expression.New(type), bindings);
		}

		private System.Type FindOrCreateNativeFromTupleType(Type.TupleType tupleType)
		{
			System.Type nativeType;
			if (!_tupleToNative.TryGetValue(tupleType, out nativeType))
			{
				nativeType = TupleMaker.TypeTypeToNative(_module, tupleType);
				_tupleToNative.Add(tupleType, nativeType);
			}
			return nativeType;
		}

		private static Type.TupleType TupeTypeFromTupleSelector(Parse.TupleSelector tupleSelector, Dictionary<Parse.AttributeSelector, Expression> expressions)
		{
			// Create a tuple type for the given selector
			var tupleType = new Type.TupleType();
			foreach (var attribute in tupleSelector.Attributes)
				tupleType.Attributes.Add(QualifiedID.FromQualifiedIdentifier(attribute.Name), expressions[attribute].Type);
			foreach (var reference in tupleSelector.References)
				tupleType.References.Add(QualifiedID.FromQualifiedIdentifier(reference.Name), Type.TupleReference.FromParseReference(reference));
			foreach (var key in tupleSelector.Keys)
				tupleType.Keys.Add(Type.TupleKey.FromParseKey(key));
			return tupleType;
		}

		private string QualifiedIdentifierToName(Parse.QualifiedIdentifier qualifiedIdentifier)
		{
			return String.Join("_", qualifiedIdentifier.Components);
		}

		private Expression CompileIdentifierExpression(ScriptPlan plan, Parse.IdentifierExpression identifierExpression)
		{
			var symbol = FindReference<object>(plan, identifierExpression.Target);
			Expression result;
			if (_paramsBySymbol.TryGetValue(symbol, out result))
				return result;

			switch (symbol.GetType().Name)
			{
				case "RuntimeMethodInfo": 
					var method = (MethodInfo)symbol;
					// TODO: type parameter determination!!
					method = method.MakeGenericMethod(typeof(int));
					var paramTypes = new List<System.Type>(from p in method.GetParameters() select p.ParameterType);
					paramTypes.Add(method.ReturnType);
					var delegateType = Expression.GetFuncType(paramTypes.ToArray());
					return Expression.Constant(Delegate.CreateDelegate(delegateType, method));
				case "RuntimeFieldInfo":
					var field = (FieldInfo)symbol;
					return Expression.Call
					(
						_factory, 
						ReflectionUtility.IRepositoryFactoryGetRepository.MakeGenericMethod(new System.Type[] { field.FieldType }),
						Expression.Constant(field.DeclaringType),
						QualifiedIdentifierToQualifiedIDExpression(identifierExpression.Target)
					);
				// TODO: enums and typedefs
				default:
					throw new CompilerException(CompilerException.Codes.IdentifierNotFound, identifierExpression.Target);
			}
		}

		private static Expression QualifiedIdentifierToQualifiedIDExpression(Parse.QualifiedIdentifier id)
		{
			return Expression.MemberInit
			(
				Expression.New(typeof(QualifiedID)),
				Expression.Bind
				(
					typeof(QualifiedID).GetField("Components"),
					Expression.NewArrayInit(typeof(string), from t in id.Components select Expression.Constant(t))
				)
			);
		}

		private Expression CompileBinaryExpression(ScriptPlan plan, Parse.BinaryExpression expression)
		{
			// TODO: if intrinsic type...
			var result = CompileExpression(plan, expression.Left);
			switch (expression.Operator)
			{
				case Parse.Operator.Addition : return Expression.Add(result, CompileExpression(plan, expression.Right));
				case Parse.Operator.Subtract : return Expression.Subtract(result, CompileExpression(plan, expression.Right));
				case Parse.Operator.Multiply : return Expression.Multiply(result, CompileExpression(plan, expression.Right));
				case Parse.Operator.Modulo: return Expression.Modulo(result, CompileExpression(plan, expression.Right));
				case Parse.Operator.Divide: return Expression.Divide(result, CompileExpression(plan, expression.Right));
				case Parse.Operator.BitwiseAnd:
				case Parse.Operator.And : return Expression.And(result, CompileExpression(plan, expression.Right));
				case Parse.Operator.BitwiseOr:
				case Parse.Operator.Or: return Expression.Or(result, CompileExpression(plan, expression.Right));
				case Parse.Operator.BitwiseXor:
				case Parse.Operator.Xor: return Expression.ExclusiveOr(result, CompileExpression(plan, expression.Right));
				case Parse.Operator.Equal: return Expression.Equal(result, CompileExpression(plan, expression.Right));
				case Parse.Operator.NotEqual : return Expression.NotEqual(result, CompileExpression(plan, expression.Right));
				case Parse.Operator.ShiftLeft: return Expression.LeftShift(result, CompileExpression(plan, expression.Right));
				case Parse.Operator.ShiftRight: return Expression.RightShift(result, CompileExpression(plan, expression.Right));
				case Parse.Operator.Power: return Expression.Power(result, CompileExpression(plan, expression.Right));
				case Parse.Operator.InclusiveGreater: return Expression.GreaterThanOrEqual(result, CompileExpression(plan, expression.Right));
				case Parse.Operator.InclusiveLess: return Expression.LessThanOrEqual(result, CompileExpression(plan, expression.Right));
				case Parse.Operator.Greater: return Expression.GreaterThan(result, CompileExpression(plan, expression.Right));
				case Parse.Operator.Less: return Expression.LessThan(result, CompileExpression(plan, expression.Right));
				default: throw new NotSupportedException(String.Format("Operator {0} is not supported.", expression.Operator));
			}
		}

		private Expression CompileLiteral(ScriptPlan plan, Parse.LiteralExpression expression)
		{
			return Expression.Constant(expression.Value);
		}
	}
}

