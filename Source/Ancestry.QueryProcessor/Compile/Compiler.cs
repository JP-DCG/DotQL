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
		private Dictionary<Parse.ISymbol, ParameterExpression> _paramsBySymbol = new Dictionary<Parse.ISymbol, ParameterExpression>();

		private ParameterExpression _cancelToken;
		private QueryOptions _options;
		private SymbolDocumentInfo _symbolDocument;
		private ISymbolDocumentWriter _symbolWriter;

		public Compiler(QueryOptions actualOptions, string assemblyName, string sourceFileName)
		{
			_options = actualOptions;
			_assemblyName = new AssemblyName(assemblyName);
			_symbolDocument = Expression.SymbolDocument(sourceFileName);

			_assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(_assemblyName, AssemblyBuilderAccess.RunAndSave);// TODO: temp for debugging .RunAndCollect);
			if (_options.DebugOn)
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
			_module = _assembly.DefineDynamicModule(_assemblyName.Name + ".dll", _options.DebugOn);
			if (_options.DebugOn)
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

			var args = Expression.Parameter(typeof(Dictionary<string, object>), "args");
			_cancelToken = Expression.Parameter(typeof(CancellationToken), "cancelToken");

			var execute = 
				Expression.Lambda<ExecuteHandler>
				(
					CompileScript(plan, args),
					args,
					_cancelToken
				);

			_module.CreateGlobalFunctions();
			_assembly.Save("qpdebug.dll");

			//var pdbGenerator = _options.DebugOn ? System.Runtime.CompilerServices.DebugInfoGenerator.CreatePdbGenerator() : null;
			return execute.Compile();//pdbGenerator);

			//// TODO: Pass debug info
			//execute.CompileToMethod(method);
		}

		private Expression CompileScript(ScriptPlan plan, Expression args)
		{
			var script = plan.Script;
			var vars = new List<ParameterExpression>();

			foreach (var u in script.Usings)
			{
				// TODO: add variables for each symbol in each module
				//_paramsBySymbol.Add(
				//vars.Add(
			}

			// Compute result and convert to object (box if needed)
			if (script.Expression != null)
			{
				var result = CompileClausedExpression(plan, script.Expression);
				if (result.Type.IsValueType)
					result = Expression.Convert(result, typeof(object));

				return 
					Expression.Block
					(
						GetDebugInfo(script),
						// TODO: usings, modules, vars (with arg overrides), assignments
						script.Expression != null ? result : null
					);
			}
			else
				return Expression.Constant(null, typeof(object));
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
				nativeType = TypeTypeToNative(tupleType);
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

		private System.Type TypeTypeToNative(Type.TupleType tupleType)
		{
			var typeBuilder = _module.DefineType("Tuple" + tupleType.GetHashCode(), TypeAttributes.Public);
			var fieldsByID = new Dictionary<QualifiedID, FieldInfo>();

			// Add attributes
			foreach (var attribute in tupleType.Attributes)
			{
				var field = typeBuilder.DefineField(attribute.Key.ToString(), attribute.Value, FieldAttributes.Public);
				fieldsByID.Add(attribute.Key, field);
			}

			// Add references
			foreach (var reference in tupleType.References)
			{
				var cab =
					new CustomAttributeBuilder
					(
						typeof(Type.TupleReferenceAttribute).GetConstructor(new System.Type[] { typeof(string[]), typeof(string), typeof(string[]) }),
						new object[] 
							{ 
								(from san in reference.Value.SourceAttributeNames select san.ToString()).ToArray(),
								reference.Value.Target.ToString(),
								(from tan in reference.Value.TargetAttributeNames select tan.ToString()).ToArray(),
							}
					);
				typeBuilder.SetCustomAttribute(cab);
			}

			// Add tuple attribute
			var attributeBuilder =
				new CustomAttributeBuilder
				(
					typeof(Type.TupleAttribute).GetConstructor(new System.Type[] { }),
					new object[] { }
				);
			typeBuilder.SetCustomAttribute(attributeBuilder);

			// Add comparison and hash methods based on key(s)
			EmitTupleGetHashCode(tupleType, typeBuilder, fieldsByID);
			var equalityMethod = EmitTupleEquality(tupleType, typeBuilder, fieldsByID);
			EmitTupleInequality(typeBuilder, equalityMethod);
			EmitTupleEquals(typeBuilder, equalityMethod);

			// Create the type
			return typeBuilder.CreateType();
		}

		private static MethodBuilder EmitTupleInequality(TypeBuilder typeBuilder, MethodBuilder equalityMethod)
		{
			var inequalityMethod = typeBuilder.DefineMethod("op_Inequality", MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, CallingConventions.Standard, typeof(bool), new System.Type[] { typeBuilder, typeBuilder });
			var il = inequalityMethod.GetILGenerator();
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.EmitCall(OpCodes.Call, equalityMethod, null);
			il.Emit(OpCodes.Not);
			il.Emit(OpCodes.Ret);
			return inequalityMethod;
		}

		private static MethodBuilder EmitTupleEquality(Type.TupleType tupleType, TypeBuilder typeBuilder, Dictionary<QualifiedID, FieldInfo> fieldsByID)
		{
			var equalityMethod = typeBuilder.DefineMethod("op_Equality", MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, CallingConventions.Standard, typeof(bool), new System.Type[] { typeBuilder, typeBuilder });
			var il = equalityMethod.GetILGenerator();
			bool first = true;
			foreach (var keyItem in tupleType.GetKeyAttributes())
			{
				var field = fieldsByID[keyItem];
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldfld, field);
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Ldfld, field);
				var fieldEqualityMethod = field.FieldType.GetMethod("op_Equality", new System.Type[] { field.FieldType, field.FieldType });
				if (fieldEqualityMethod != null)
					il.EmitCall(OpCodes.Call, fieldEqualityMethod, null);
				else
					il.Emit(OpCodes.Ceq);
				if (first)
					first = false;
				else
					il.Emit(OpCodes.And);
			}
			il.Emit(OpCodes.Ret);
			return equalityMethod;
		}

		private static MethodBuilder EmitTupleGetHashCode(Type.TupleType tupleType, TypeBuilder typeBuilder, Dictionary<QualifiedID, FieldInfo> fieldsByID)
		{
			var getHashCodeMethod = typeBuilder.DefineMethod("GetHashCode", MethodAttributes.Virtual | MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot, CallingConventions.HasThis, typeof(Int32), new System.Type[] { });
			var il = getHashCodeMethod.GetILGenerator();
			// result = 83
			il.Emit(OpCodes.Ldc_I4, 83);
			foreach (var keyItem in tupleType.GetKeyAttributes())
			{
				var field = fieldsByID[keyItem];
				var hashMethod = field.FieldType.GetMethod("GetHashCode");

				// result ^= this.<field>.GetHashCode();
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldflda, field);
				if (hashMethod != null)
					il.EmitCall(OpCodes.Call, hashMethod, null);
				else
					il.EmitCall(OpCodes.Callvirt, ReflectionUtility.ObjectGetHashCode, null);
 				il.Emit(OpCodes.Xor);
			}
			il.Emit(OpCodes.Ret);
			typeBuilder.DefineMethodOverride(getHashCodeMethod, ReflectionUtility.ObjectGetHashCode);
			return getHashCodeMethod;
		}

		private static MethodBuilder EmitTupleEquals(TypeBuilder typeBuilder, MethodBuilder equalityMethod)
		{
			var equalsMethod = typeBuilder.DefineMethod("Equals", MethodAttributes.Virtual | MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot, CallingConventions.HasThis, typeof(bool), new System.Type[] { typeof(object) });
			var il = equalsMethod.GetILGenerator();
			var baseLabel = il.DefineLabel();
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Isinst, typeBuilder);
			il.Emit(OpCodes.Brfalse, baseLabel);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Castclass, typeBuilder);
			il.Emit(OpCodes.Ldarg_0);
			il.EmitCall(OpCodes.Call, equalityMethod, null);
			il.Emit(OpCodes.Ret);
			il.MarkLabel(baseLabel);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.EmitCall(OpCodes.Call, ReflectionUtility.ObjectEquals, null);
			il.Emit(OpCodes.Ret);
			typeBuilder.DefineMethodOverride(equalsMethod, ReflectionUtility.ObjectEquals);
			return equalsMethod;
		}

		private string QualifiedIdentifierToName(Parse.QualifiedIdentifier qualifiedIdentifier)
		{
			return String.Join("_", qualifiedIdentifier.Components);
		}

		private Expression CompileIdentifierExpression(ScriptPlan plan, Parse.IdentifierExpression identifierExpression)
		{
			Parse.ISymbol symbol;
			if (!plan.References.TryGetValue(identifierExpression.Target, out symbol))
				throw new Exception("Identifier Not found."); // TODO: CompilerException(CompilerException.Codes.IdentifierNotFound, identifierExpression.Target.ToString());
			return _paramsBySymbol[symbol];
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

