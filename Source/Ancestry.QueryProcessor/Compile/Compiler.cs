using Ancestry.QueryProcessor.Execute;
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
		private CompilerOptions _options;
		private Emitter _emitter;
		private SymbolDocumentInfo _symbolDocument;

		// Scope management
		private Dictionary<Parse.Statement, Frame> _frames = new Dictionary<Parse.Statement, Frame>();
		public Frame _importFrame;
		public Frame _scriptFrame;
		private Dictionary<Parse.QualifiedIdentifier, object> _references = new Dictionary<Parse.QualifiedIdentifier, object>();
		private Dictionary<object, Expression> _paramsBySymbol = new Dictionary<object, Expression>();
		private HashSet<Parse.Statement> _recursions = new HashSet<Parse.Statement>();

		// Parameters
		private ParameterExpression _argParam;
		private ParameterExpression _factoryParam;
		private ParameterExpression _cancelParam;

		// Using private constructor pattern because state spans single static call
		private Compiler() { }

		public static ExecuteHandler CreateExecutable(CompilerOptions options, Parse.Script script)
		{
			return new Compiler().InternalCreateExecutable(options, script);
		}

		private ExecuteHandler InternalCreateExecutable(CompilerOptions options, Parse.Script script)
		{
			_options = options;
			_emitter =
				new Emitter
				(
					new EmitterOptions
					{
						DebugOn = options.DebugOn,
						AssemblyName = options.AssemblyName,
						SourceFileName = options.SourceFileName
					}
				);
			_symbolDocument = Expression.SymbolDocument(_options.SourceFileName);

			//// TODO: setup separate app domain with appropriate cache path, shadow copying etc.
			//var domainName = "plan" + DateTime.Now.Ticks.ToString();
			//var domain = AppDomain.CreateDomain(domainName);

			_argParam = Expression.Parameter(typeof(Dictionary<string, object>), "args");
			_factoryParam = Expression.Parameter(typeof(Storage.IRepositoryFactory), "factory");
			_cancelParam = Expression.Parameter(typeof(CancellationToken), "cancelToken");

			var execute =
				Expression.Lambda<ExecuteHandler>
				(
					CompileScript(script),
					_argParam,
					_factoryParam,
					_cancelParam
				);

			return execute.Compile();//pdbGenerator);

			//// TODO: Pass debug info
			//execute.CompileToMethod(method);
		}

		private IEnumerable<Runtime.ModuleTuple> GetModules()
		{
			return Runtime.Runtime.GetModulesRepository(_options.Factory).Get(null);
		}

		private Expression CompileScript(Parse.Script script)
		{
			_importFrame = new Frame();
			_scriptFrame = new Frame(_importFrame);
			_frames.Add(script, _scriptFrame);

			var vars = new List<ParameterExpression>();
			var block = new List<Expression>();

			if (_options.DebugOn)
				block.Add(GetDebugInfo(script));

			// Create temporary frame for resolution of used modules from all modules
			var modulesFrame = new Frame();
			foreach (var module in GetModules())
				modulesFrame.Add(module.Name, module);

			// Usings
			foreach (var u in script.Usings.Union(_options.DefaultUsings).Distinct(new UsingComparer()))
			{
				ResolveUsing(_importFrame, u, modulesFrame);
				CompileUsing(u, vars, block);
			}

			// Module declarations
			foreach (var m in script.Modules)
				CompileModule(_scriptFrame, block, m);

			// Vars
			foreach (var v in script.Vars)
			{
				CompileVar(_scriptFrame, v, vars, block);
				_scriptFrame.Add(v.Name, v);
			}

			// Assignments
			foreach (var a in script.Assignments)
				CompileAssignment(_scriptFrame, a, block);

			// Return expression
			if (script.Expression != null)
				CompileResult(_scriptFrame, script.Expression, block);
			else
				block.Add(Expression.Constant(null, typeof(object)));

			return Expression.Block(vars, block);
		}

		private void ResolveUsing(Frame frame, Parse.Using u, Frame modulesFrame)
		{
			var moduleName = Name.FromQualifiedIdentifier(u.Target);
			var module = modulesFrame.Resolve<Runtime.ModuleTuple>(moduleName);
			_references.Add(u.Target, module);
			frame.Add(moduleName, module);
			foreach (var method in module.Class.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
				frame.Add(moduleName + Name.FromNative(method.Name), method);
			foreach (var type in module.Class.GetNestedTypes(BindingFlags.Public | BindingFlags.Static))
				frame.Add(moduleName + Name.FromNative(type.Name), type);
			foreach (var field in module.Class.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
				frame.Add(moduleName + Name.FromNative(field.Name), field);
		}

		private void AddAllReferences(Frame frame, IEnumerable<Parse.QualifiedIdentifier> list)
		{
			foreach (var item in list)
				_references.Add(item, frame.Resolve<object>(item));
		}

		private void CompileAssignment(Frame frame, Parse.ClausedAssignment a, List<Expression> block)
		{
			throw new NotImplementedException();
		}

		private void CompileResult(Frame frame, Parse.ClausedExpression expression, List<Expression> block)
		{
			var result = CompileClausedExpression(frame, expression);

			// Box the result if needed
			if (result.Type.IsValueType)
				result = Expression.Convert(result, typeof(object));

			block.Add(result);
		}

		private void CompileVar(Frame frame, Parse.VarDeclaration v, List<ParameterExpression> vars, List<Expression> block)
		{
			// Compile the (optional) type
			var type = v.Type != null ? CompileTypeDeclaration(frame, v.Type) : null;

			// Compile the (optional) initializer
			var initializer = v.Initializer != null ? CompileExpression(frame, v.Initializer, type) : null;

			// Default the type to the initializer's type
			type = type ?? initializer.Type;

			// Create the variable
			var variable = Expression.Parameter(type, v.Name.ToString());
			vars.Add(variable);
			_paramsBySymbol.Add(v, variable);
			
			// Build the variable initialization logic
			block.Add
			(
				Expression.Assign
				(
					variable,
					Expression.Call
					(
						typeof(Runtime.Runtime).GetMethod("GetInitializer").MakeGenericMethod(type), 
						initializer, 
						_argParam, 
						Expression.Constant(Name.FromQualifiedIdentifier(v.Name))
					)
				)
			);
		}

		private void CompileModule(Frame frame, List<Expression> block, Parse.ModuleDeclaration module)
		{
			// Create the class for the module
			var moduleType = TypeFromModule(frame, module);

			// Build the code to declare the module
			block.Add
			(
				Expression.Call
				(
					typeof(Runtime.Runtime).GetMethod("DeclareModule"),
					Expression.Constant(Name.FromQualifiedIdentifier(module.Name)),
					Expression.Constant(module.Version),
					Expression.Constant(moduleType),
					_factoryParam
				)
			);
		}

		private void CompileUsing(Parse.Using use, List<ParameterExpression> vars, List<Expression> block)
		{
			// Determine the class of the module
			var moduleType = FindReference<Runtime.ModuleTuple>(use.Target).Class;

			// Create a variable to hold the module instance
			var moduleVar = Expression.Parameter(moduleType, (use.Alias ?? use.Target).ToString());
			vars.Add(moduleVar);
			_paramsBySymbol.Add(moduleType, moduleVar);

			// Create initializers for each variable bound to a repository
			var moduleInitializers = new List<MemberBinding>();
			foreach
			(
				var field in
					moduleType.GetFields(BindingFlags.Public | BindingFlags.Instance)
						.Where(f => f.FieldType.GetGenericTypeDefinition() == typeof(Storage.IRepository<>))
			)
				moduleInitializers.Add
				(
					Expression.Bind
					(
						field,
						Expression.Call
						(
							_factoryParam,
							typeof(Storage.IRepositoryFactory).GetMethod("GetRepository").MakeGenericMethod(field.FieldType.GenericTypeArguments),
							Expression.Constant(moduleType),
							Expression.Constant(Name.FromNative(field.Name))
						)
					)
				);

			// Build code to construct instance and assign to variable
			block.Add(Expression.Assign(moduleVar, Expression.MemberInit(Expression.New(moduleType), moduleInitializers)));

			var moduleName = Name.FromQualifiedIdentifier(use.Target);
		}

		private T FindReference<T>(Parse.QualifiedIdentifier id)
		{
			object module;
			if (!_references.TryGetValue(id, out module))
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

		private Expression CompileClausedExpression(Frame frame, Parse.ClausedExpression expression, System.Type typeHint = null)
		{
			var local = new Frame(frame);
			_frames.Add(expression, local);
			var vars = new List<ParameterExpression>();

			if (expression.ForClauses.Count > 0)
			//// TODO: foreach (var forClause in clausedExpression.ForClauses)
			{
				var forClause = expression.ForClauses[0];
				var forExpression = CompileExpression(local, forClause.Expression);
				local.Add(forClause.Name, forClause);
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

				var returnBlock = CompileClausedReturn(local, expression, vars);
				var resultIsSet = expression.OrderDimensions.Count == 0
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
					GetDebugInfo(expression),
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
								
								expression.WhereClause == null
									? (Expression)Expression.Call(resultVariable, resultAddMethod, returnBlock)
									: Expression.IfThen
									(
										CompileExpression(local, expression.WhereClause, typeof(bool)), 
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

			return Expression.Block(vars, CompileClausedReturn(local, expression, vars, typeHint));
		}

		private Expression CompileClausedReturn(Frame frame, Parse.ClausedExpression clausedExpression, List<ParameterExpression> vars, System.Type typeHint = null)
		{
			var blocks = new List<Expression> { GetDebugInfo(clausedExpression) };

			// Create a variable for each let and initialize
			foreach (var let in clausedExpression.LetClauses)
			{
				var compiledExpression = CompileExpression(frame, let.Expression);
				var variable = Expression.Variable(compiledExpression.Type, let.Name.ToString());
				blocks.Add(Expression.Assign(variable, compiledExpression));
				_paramsBySymbol.Add(let, variable);
				vars.Add(variable);
				frame.Add(let.Name, let);
			}

			// Add the expression to the body
			blocks.Add(CompileExpression(frame, clausedExpression.Expression, typeHint));

			return Expression.Block(blocks);
		}

		private Expression CompileExpression(Frame frame, Parse.Expression expression, System.Type typeHint = null)
		{
			switch (expression.GetType().Name)
			{
				case "LiteralExpression": return CompileLiteral(frame, (Parse.LiteralExpression)expression, typeHint);
				case "BinaryExpression": return CompileBinaryExpression(frame, (Parse.BinaryExpression)expression, typeHint);
				case "ClausedExpression": return CompileClausedExpression(frame, (Parse.ClausedExpression)expression, typeHint);
				case "IdentifierExpression": return CompileIdentifierExpression(frame, (Parse.IdentifierExpression)expression, typeHint);
				case "TupleSelector": return CompileTupleSelector(frame, (Parse.TupleSelector)expression, typeHint);
				case "ListSelector": return CompileListSelector(frame, (Parse.ListSelector)expression, typeHint);
				case "SetSelector": return CompileSetSelector(frame, (Parse.SetSelector)expression, typeHint);
				case "FunctionSelector": return CompileFunctionSelector(frame, (Parse.FunctionSelector)expression, typeHint);
				case "CallExpression": return CompileCallExpression(frame, (Parse.CallExpression)expression, typeHint);
				case "RestrictExpression": return CompileRestrictExpression(frame, (Parse.RestrictExpression)expression, typeHint);
				default : throw new NotSupportedException(String.Format("Expression type {0} is not supported", expression.GetType().Name));
			}
		}

		private Expression CompileRestrictExpression(Frame frame, Parse.RestrictExpression restrictExpression, System.Type typeHint)
		{
			var local = new Frame(frame);
			var expression = CompileExpression(frame, restrictExpression.Expression, typeHint);
			if (typeof(IEnumerable).IsAssignableFrom(expression.Type) && expression.Type.IsGenericType)
			{
				var memberType = expression.Type.GenericTypeArguments[0];
				var parameters = new List<ParameterExpression>();

				// Add value param
				var valueParam = Expression.Parameter(memberType, Parse.ReservedWords.Value);
				parameters.Add(valueParam);
				local.Add(Name.FromComponents(Parse.ReservedWords.Value), expression);
				_paramsBySymbol.Add(expression, valueParam);

				// Add index param
				var indexParam = Expression.Parameter(typeof(int), Parse.ReservedWords.Index);
				parameters.Add(indexParam);
				var indexSymbol = new Parse.Statement();	// Dummy symbol; no syntax element generates index
				local.Add(Name.FromComponents(Parse.ReservedWords.Index), indexSymbol);
				_paramsBySymbol.Add(indexSymbol, indexParam);

				// TODO: detect tuple members and push attributes into frame

				// Compile condition
				var condition = 
					Expression.Lambda
					(
						CompileExpression(local, restrictExpression.Condition, typeof(bool)), 
						parameters
					);

				var where = typeof(System.Linq.Enumerable).GetMethodExt("Where", new System.Type[] { typeof(IEnumerable<ReflectionUtility.T>), typeof(Func<ReflectionUtility.T, int, bool>) });
				where = where.MakeGenericMethod(memberType);
				return Expression.Call(where, expression, condition);
			}
			else
			{
				var alreadyOptional = IsOptional(expression.Type);
				var valueParam = Expression.Parameter(expression.Type, Parse.ReservedWords.Value);
				var parameters = new List<ParameterExpression>();
				parameters.Add(valueParam);
				_paramsBySymbol.Add(expression, valueParam);
				local.Add(Name.FromComponents(Parse.ReservedWords.Value), expression);

				var condition = CompileExpression(local, restrictExpression.Condition, typeof(bool));
				return 
					Expression.IfThenElse
					(
						Expression.Block(parameters, condition), 
							alreadyOptional ? (Expression)expression : MakeOptional(expression),
							alreadyOptional ? MakeNullOptional(expression.Type.GenericTypeArguments[0]) : MakeNullOptional(expression.Type)
					);
			} 
		}

		private static Expression MakeOptional(Expression expression)
		{
			return Expression.New(typeof(Runtime.Optional<>).MakeGenericType(expression.Type).GetConstructor(new System.Type[] { expression.Type }), expression);
		}

		private static Expression MakeNullOptional(System.Type type)
		{
			return Expression.New(typeof(Runtime.Optional<>).MakeGenericType(type).GetConstructor(new System.Type[] { typeof(bool) }), Expression.Constant(false));
		}

		private bool IsOptional(System.Type type)
		{
			return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Runtime.Optional<>);
		}

		private Expression CompileFunctionSelector(Frame frame, Parse.FunctionSelector functionSelector, System.Type typeHint)
		{
			var local = new Frame(frame);
			_frames.Add(functionSelector, local);

			var parameters = new ParameterExpression[functionSelector.Parameters.Count];
			var i = 0;
			foreach (var p in functionSelector.Parameters)
			{
				local.Add(p.Name, p);
				parameters[i++] = Expression.Parameter(CompileTypeDeclaration(frame, p.Type), p.Name.ToString());
			}

			var expression = CompileExpression(local, functionSelector.Expression, typeHint);
			return Expression.Lambda(expression, parameters);
		}

		private System.Type TypeFromModule(Frame frame, Parse.ModuleDeclaration moduleDeclaration)
		{
			var local = new Frame(frame);
			_frames.Add(moduleDeclaration, local);

			// Gather the module's symbols
			foreach (var member in moduleDeclaration.Members)
			{
				local.Add(member.Name, member);

				// Populate qualified enumeration members
				if (member is Parse.EnumMember)
					foreach (var e in ((Parse.EnumMember)member).Values)
						local.Add(new Name() { Components = member.Name.Components.Union(e.Components).ToArray() }, member);
			}

			var module = _emitter.BeginModule(moduleDeclaration.Name.ToString());
			
			foreach (var member in moduleDeclaration.Members)
			{
				switch (member.GetType().Name)
				{
					case "VarMember": 
						var varMember = (Parse.VarMember)member;
						_emitter.DeclareVariable(module, member.Name.ToString(), CompileTypeDeclaration(local, varMember.Type));
						break;

					case "TypeMember":
						var typeMember = (Parse.TypeMember)member;
						_emitter.DeclareTypeDef(module, member.Name.ToString(), CompileTypeDeclaration(local, typeMember.Type));
						break;

					case "EnumMember":
						var enumMember = (Parse.EnumMember)member;
						_emitter.DeclareEnum(module, member.Name.ToString(), from v in enumMember.Values select v.ToString());

						break;

					case "ConstMember":
						var constMember = (Parse.ConstMember)member;
						var expression = CompileExpression(local, constMember.Expression);
						var result = CompileTimeEvaluate(expression);
						_emitter.DeclareConst(module, member.Name.ToString(), result, expression.Type);
						break;
						
					default: throw new Exception("Unknown member type " + member.GetType().Name);
				}
			}

			return module.CreateType();
		}

		private static object CompileTimeEvaluate(Expression expression)
		{
			var lambda = Expression.Lambda(expression);
			var compiled = lambda.Compile();
			var result = compiled.DynamicInvoke();
			return result;
		}

		private Expression CompileCallExpression(Frame frame, Parse.CallExpression callExpression, System.Type typeHint)
		{
			// Compile arguments
			var args = new Expression[callExpression.Arguments.Count];
			for (var i = 0; i < callExpression.Arguments.Count; i++)
				args[i] = CompileExpression(frame, callExpression.Arguments[i]);

			var expression = CompileExpression(frame, callExpression.Expression);
			if (typeof(MethodInfo).IsAssignableFrom(expression.Type) && expression is ConstantExpression)
			{
				var method = (MethodInfo)((ConstantExpression)expression).Value;
				if (method.ContainsGenericParameters)
				{
					var genericArgs = method.GetGenericArguments();
					var resolved = new System.Type[genericArgs.Length];
					if (callExpression.TypeArguments.Count > 0)
					{
						for (var i = 0; i < resolved.Length; i++)
							resolved[i] = CompileTypeDeclaration(frame, callExpression.TypeArguments[i]);
					}
					else
					{
						var parameters = method.GetParameters();
						for (var i = 0; i < parameters.Length; i++)
							DetermineTypeParameters(resolved, parameters[i].ParameterType, args[i].Type);
						// TODO: Assert that all type parameters are resolved
					}
					method = method.MakeGenericMethod(resolved);
					// http://msdn.microsoft.com/en-us/library/system.reflection.methodinfo.makegenericmethod.aspx
				}	
				return Expression.Call(method, args);
			}
			else if (typeof(Delegate).IsAssignableFrom(expression.Type))
				return Expression.Invoke(expression, args);
			else
				throw new CompilerException(CompilerException.Codes.IncorrectType, expression.Type, "function");
		}

		private System.Type CompileTypeDeclaration(Frame frame, Parse.TypeDeclaration typeDeclaration)
		{
			switch (typeDeclaration.GetType().Name)
			{
				case "OptionalType": return typeof(Nullable<>).MakeGenericType(CompileTypeDeclaration(frame, ((Parse.OptionalType)typeDeclaration).Type));
				case "ListType": return typeof(IList<>).MakeGenericType(CompileTypeDeclaration(frame, ((Parse.ListType)typeDeclaration).Type));
				case "SetType": return typeof(ISet<>).MakeGenericType(CompileTypeDeclaration(frame, ((Parse.SetType)typeDeclaration).Type));
				case "TupleType": return CompileTupleType(frame, (Parse.TupleType)typeDeclaration);
				case "FunctionType": return CompileFunctionType(frame, (Parse.FunctionType)typeDeclaration);
				case "NamedType": return CompileNamedType(frame, (Parse.NamedType)typeDeclaration);
				default: throw new Exception("Unknown type declaration " + typeDeclaration.GetType().Name); 
			}
		}

		private System.Type CompileNamedType(Frame frame, Parse.NamedType namedType)
		{
			var target = frame.Resolve<object>(namedType.Target);
			_references.Add(namedType.Target, target);
			if (target is System.Type)
				return (System.Type)target;
			else if (target is FieldInfo)
				return ((FieldInfo)target).FieldType;
			else if (target is Parse.Statement)
			{
				if (!_recursions.Add(namedType))
					throw new CompilerException(CompilerException.Codes.RecursiveDeclaration);
				try
				{
					return CompileTypeDeclaration(frame, (Parse.TypeDeclaration)target);	
				}
				finally
				{
					_recursions.Remove(namedType);
				}
			}
			else
				throw new Exception("Internal Error: Named type is not the correct type");
				
		}

		private void ResolveTupleType(Frame frame, Parse.TupleType tupleType)
		{
			var local = new Frame(null);
			_frames.Add(tupleType, local);

			// Resolve all attributes as symbols
			foreach (var a in tupleType.Attributes)
			{
				local.Add(a.Name, a);
			}

			// Resolve source reference columns
			foreach (var k in tupleType.Keys)
			{
				AddAllReferences(local, k.AttributeNames);
			}

			// Resolve key reference columns
			foreach (var r in tupleType.References)
			{
				AddAllReferences(local, r.SourceAttributeNames);
				var target = _scriptFrame.Resolve<Parse.Statement>(r.Target);
				_references.Add(r.Target, target);
				AddAllReferences(_frames[target], r.TargetAttributeNames);
			}
		}

		private System.Type CompileTupleType(Frame frame, Parse.TupleType tupleType)
		{
			ResolveTupleType(frame, tupleType);
			return _emitter.FindOrCreateNativeFromTupleType(TupleTypeFromDomTupleType(frame, tupleType));
		}

		private System.Type CompileFunctionType(Frame frame, Parse.FunctionType functionType)
		{
			throw new NotImplementedException();
		}

		private Type.TupleType TupleTypeFromDomTupleType(Frame frame, Parse.TupleType tupleType)
		{
			var result = new Type.TupleType();

			foreach (var a in tupleType.Attributes)
				result.Attributes.Add(Name.FromQualifiedIdentifier(a.Name), CompileTypeDeclaration(frame, a.Type));

			foreach (var r in tupleType.References)
				result.References.Add
				(
					Name.FromQualifiedIdentifier(r.Name), 
					new Type.TupleReference 
					{ 
						SourceAttributeNames = IdentifiersToNames(r.SourceAttributeNames),  
						Target = Name.FromQualifiedIdentifier(r.Target),
						TargetAttributeNames = IdentifiersToNames(r.TargetAttributeNames)
					}
				);

			foreach (var k in tupleType.Keys)
				result.Keys.Add(new Type.TupleKey { AttributeNames = IdentifiersToNames(k.AttributeNames) });

			return result;
		}

		private static Name[] IdentifiersToNames(IEnumerable<Parse.QualifiedIdentifier> ids)
		{
			return (from n in ids select Name.FromQualifiedIdentifier(n)).ToArray();
		}

		private void DetermineTypeParameters(System.Type[] resolved, System.Type parameterType, System.Type argumentType)
		{
			// If the given parameter contains an unresolved generic type parameter, attempt to resolve using actual arguments
			if (parameterType.ContainsGenericParameters)
			{
				var paramArgs = parameterType.GetGenericArguments();
				var argArgs = argumentType.GetGenericArguments();
				if (paramArgs.Length != argArgs.Length)
					throw new CompilerException(CompilerException.Codes.MismatchedGeneric, parameterType, argumentType);
				for (var i = 0; i < paramArgs.Length; i++)
					if (paramArgs[i].IsGenericParameter && resolved[paramArgs[i].GenericParameterPosition] == null)
						resolved[paramArgs[i].GenericParameterPosition] = argArgs[i];
					else 
						DetermineTypeParameters(resolved, paramArgs[i], argArgs[i]);
			}
		}

		private Expression CompileSetSelector(Frame frame, Parse.SetSelector setSelector, System.Type typeHint)
		{
			// Compile each item's expression
			var initializers = new ElementInit[setSelector.Items.Count];
			System.Type type = null;
			System.Type setType = null;
			MethodInfo addMethod = null;
			for (var i = 0; i < setSelector.Items.Count; i++)
			{
				var expression = CompileExpression(frame, setSelector.Items[i], type);
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

		private Expression CompileListSelector(Frame frame, Parse.ListSelector listSelector, System.Type typeHint)
		{
			// Compile each item's expression
			var initializers = new Expression[listSelector.Items.Count];
			System.Type type = null;
			for (var i = 0; i < listSelector.Items.Count; i++)
			{
				var expression = CompileExpression(frame, listSelector.Items[i], type);
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

		private void ResolveTupleSelector(Frame frame, Parse.TupleSelector tupleSelector)
		{
			var local = new Frame(null);
			_frames.Add(tupleSelector, local);

			// Resolve all attributes as symbols
			foreach (var a in tupleSelector.Attributes)
			{
				local.Add(a.Name, a);
			}

			// Resolve source reference columns
			foreach (var k in tupleSelector.Keys)
			{
				AddAllReferences(local, k.AttributeNames);
			}

			// Resolve key reference columns
			foreach (var r in tupleSelector.References)
			{
				AddAllReferences(local, r.SourceAttributeNames);
				var target = _scriptFrame.Resolve<Parse.Statement>(r.Target);
				_references.Add(r.Target, target);
				AddAllReferences(_frames[target], r.TargetAttributeNames);
			}
		}
		private Expression CompileTupleSelector(Frame frame, Parse.TupleSelector tupleSelector, System.Type typeHint)
		{
			// Resolve internal references for checking purposes
			ResolveTupleSelector(frame, tupleSelector);

			var bindings = new List<MemberBinding>();
			var expressions = new Dictionary<Parse.AttributeSelector, Expression>();
			
			// Compile attributes
			foreach (var attribute in tupleSelector.Attributes)
				expressions.Add(attribute, CompileExpression(frame, attribute.Value));

			var tupleType = TupeTypeFromTupleSelector(tupleSelector, expressions);

			var type = _emitter.FindOrCreateNativeFromTupleType(tupleType);
			
			// Create initialization bindings for each field
			foreach (var attr in tupleSelector.Attributes)
			{
				var binding = Expression.Bind(type.GetField(Name.FromQualifiedIdentifier(attr.Name).ToString()), expressions[attr]);
				bindings.Add(binding);
			}

			return Expression.MemberInit(Expression.New(type), bindings);
		}

		private static Type.TupleType TupeTypeFromTupleSelector(Parse.TupleSelector tupleSelector, Dictionary<Parse.AttributeSelector, Expression> expressions)
		{
			// Create a tuple type for the given selector
			var tupleType = new Type.TupleType();
			foreach (var attribute in tupleSelector.Attributes)
				tupleType.Attributes.Add(Name.FromQualifiedIdentifier(attribute.Name), expressions[attribute].Type);
			foreach (var reference in tupleSelector.References)
				tupleType.References.Add(Name.FromQualifiedIdentifier(reference.Name), Type.TupleReference.FromParseReference(reference));
			foreach (var key in tupleSelector.Keys)
				tupleType.Keys.Add(Type.TupleKey.FromParseKey(key));
			return tupleType;
		}

		private string QualifiedIdentifierToName(Parse.QualifiedIdentifier qualifiedIdentifier)
		{
			return String.Join("_", qualifiedIdentifier.Components);
		}

		private Expression CompileIdentifierExpression(Frame frame, Parse.IdentifierExpression identifierExpression, System.Type typeHint)
		{
			var symbol = frame.Resolve<object>(identifierExpression.Target);
			_references.Add(identifierExpression.Target, symbol); 
			Expression param;
			if (_paramsBySymbol.TryGetValue(symbol, out param))
				return param;

			switch (symbol.GetType().Name)
			{
				case "RuntimeMethodInfo": 
					var method = (MethodInfo)symbol;
					return Expression.Constant(method, typeof(MethodInfo)); 
				case "RtFieldInfo":
					var field = (FieldInfo)symbol;
					
					// Find the module instance
					if (!_paramsBySymbol.TryGetValue(field.DeclaringType, out param))
						throw new Exception("Internal error: unable to find module for field.");

					return 
						Expression.Call
						(
							Expression.Field
							(
								param,
								field
							),
							field.FieldType.GetMethod("Get"),
							Expression.Constant(null, typeof(Parse.Expression))	// Condition
						);

				// TODO: enums and typedefs
				default:
					throw new CompilerException(CompilerException.Codes.IdentifierNotFound, identifierExpression.Target);
			}
		}

		private Expression CompileBinaryExpression(Frame frame, Parse.BinaryExpression expression, System.Type typeHint)
		{
			// TODO: if intrinsic type...
			var result = CompileExpression(frame, expression.Left);
			switch (expression.Operator)
			{
				case Parse.Operator.Addition: return Expression.Add(result, CompileExpression(frame, expression.Right, typeHint));
				case Parse.Operator.Subtract: return Expression.Subtract(result, CompileExpression(frame, expression.Right, typeHint));
				case Parse.Operator.Multiply: return Expression.Multiply(result, CompileExpression(frame, expression.Right, typeHint));
				case Parse.Operator.Modulo: return Expression.Modulo(result, CompileExpression(frame, expression.Right, typeHint));
				case Parse.Operator.Divide: return Expression.Divide(result, CompileExpression(frame, expression.Right, typeHint));
				case Parse.Operator.Power: return Expression.Power(result, CompileExpression(frame, expression.Right, typeHint));

				case Parse.Operator.BitwiseAnd:
				case Parse.Operator.And: return Expression.And(result, CompileExpression(frame, expression.Right, typeHint));
				case Parse.Operator.BitwiseOr:
				case Parse.Operator.Or: return Expression.Or(result, CompileExpression(frame, expression.Right, typeHint));
				case Parse.Operator.BitwiseXor:
				case Parse.Operator.Xor: return Expression.ExclusiveOr(result, CompileExpression(frame, expression.Right, typeHint));
				case Parse.Operator.ShiftLeft: return Expression.LeftShift(result, CompileExpression(frame, expression.Right, typeHint));
				case Parse.Operator.ShiftRight: return Expression.RightShift(result, CompileExpression(frame, expression.Right, typeHint));

				case Parse.Operator.Equal: return Expression.Equal(result, CompileExpression(frame, expression.Right));
				case Parse.Operator.NotEqual: return Expression.NotEqual(result, CompileExpression(frame, expression.Right));
				case Parse.Operator.InclusiveGreater: return Expression.GreaterThanOrEqual(result, CompileExpression(frame, expression.Right));
				case Parse.Operator.InclusiveLess: return Expression.LessThanOrEqual(result, CompileExpression(frame, expression.Right));
				case Parse.Operator.Greater: return Expression.GreaterThan(result, CompileExpression(frame, expression.Right));
				case Parse.Operator.Less: return Expression.LessThan(result, CompileExpression(frame, expression.Right));
				default: throw new NotSupportedException(String.Format("Operator {0} is not supported.", expression.Operator));
			}
		}

		private Expression CompileLiteral(Frame frame, Parse.LiteralExpression expression, System.Type typeHint)
		{
			return Expression.Constant(expression.Value);
		}
	}
}

