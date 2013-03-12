using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Ancestry.QueryProcessor.Type;

namespace Ancestry.QueryProcessor.Compile
{
	// TODO: Make most compiler helpers and methods public so they are available to types etc.

	public class Compiler
	{
		private CompilerOptions _options;
		private Emitter _emitter;
		public Emitter Emitter { get { return _emitter; } }

		// Scope management
		private Dictionary<Parse.Statement, Frame> _frames = new Dictionary<Parse.Statement, Frame>();
		/// <summary> Stack frames by DOM object. </summary>
		public Dictionary<Parse.Statement, Frame> Frames { get { return _frames; } }

		private Dictionary<object, ExpressionContext> _contextsBySymbol = new Dictionary<object, ExpressionContext>();
		/// <summary> Contexts with emitters necessary to reference the given symbol. </summary>
		public Dictionary<object, ExpressionContext> ContextsBySymbol { get { return _contextsBySymbol; } }

		private Dictionary<object, List<object>> _references = new Dictionary<object, List<object>>();
		/// <summary> References to a given symbol. </summary>
		public Dictionary<object, List<object>> References { get { return _references; } }

		public Frame _importFrame;
		public Frame _scriptFrame;
		private HashSet<Parse.Statement> _recursions = new HashSet<Parse.Statement>();
		private Dictionary<Parse.ModuleMember, Func<MemberInfo>> _uncompiledMembers = new Dictionary<Parse.ModuleMember, Func<MemberInfo>>();
		private Dictionary<Parse.ModuleMember, object> _compiledMembers = new Dictionary<Parse.ModuleMember, object>();

		// Using private constructor pattern because state spans single static call
		private Compiler() { }

		public static CompilerResult CreateExecutable(CompilerOptions options, Parse.Script script)
		{
			return new Compiler().InternalCreateExecutable(options, script);
		}

		private CompilerResult InternalCreateExecutable(CompilerOptions options, Parse.Script script)
		{
			_options = options;
			if (_options.ScalarTypes == null)
				_options.ScalarTypes = 
					new Dictionary<string, BaseType>
					{
						{ "System.String", SystemTypes.String },
						{ "System.Int32", SystemTypes.Int32 },
						{ "System.Int64", SystemTypes.Int64 },
						{ "System.Boolean", SystemTypes.Boolean },
						{ "System.DateTime", SystemTypes.DateTime },
						{ "System.TimeSpan", SystemTypes.TimeSpan },
						{ "System.Double", SystemTypes.Double },
						{ "System.Char", SystemTypes.Char },
						{ "System.Void", SystemTypes.Void }
					};
			_emitter =
				new Emitter
				(
					new EmitterOptions
					{
						DebugOn = options.DebugOn,
						AssemblyName = options.AssemblyName,
						SourceFileName = options.SourceFileName,
						ScalarTypes = options.ScalarTypes
					}
				);

			var main = _emitter.DeclareMain();
			var type = CompileScript(main, script);
			var program = _emitter.CompleteMain(main);

			return new CompilerResult { Execute = _emitter.Complete(program), Type = type };
		}

		private IEnumerable<Runtime.ModuleTuple> GetModules()
		{
			return Runtime.Runtime.GetModulesRepository(_options.Factory).Get(null, null);
		}

		private BaseType CompileScript(MethodContext method, Parse.Script script)
		{
			_importFrame = new Frame();
			_scriptFrame = AddFrame(_importFrame, script);

			// Create temporary frame for resolution of used modules from all modules
			var modulesFrame = new Frame();
			foreach (var module in GetModules())
				modulesFrame.Add(script, module.Name, module);

			// Usings
			foreach (var u in script.Usings.Union(_options.DefaultUsings).Distinct(new UsingComparer()))
				CompileUsing(method, _importFrame, modulesFrame, u);

			// Module declarations
			foreach (var m in script.Modules)
				CompileModule(method, _scriptFrame, m);

			// Vars
			foreach (var v in script.Vars)
			{
				CompileVar(method, _scriptFrame, v);
				_scriptFrame.Add(v.Name, v);
			}

			// Assignments
			foreach (var a in script.Assignments)
				CompileAssignment(method, _scriptFrame, a);

			// Return expression
			if (script.Expression != null)
				return CompileResult(method, _scriptFrame, script.Expression);
			else
			{
				method.IL.Emit(OpCodes.Ldnull);
				return SystemTypes.Void;
			}
		}

		public void ResolveListReferences(Frame frame, IEnumerable<Parse.ID> list)
		{
			foreach (var item in list)
				ResolveReference<object>(frame, item); 
		}

		/// <summary> Resolves a given reference and logs the reference. </summary>
		public T ResolveReference<T>(Frame frame, Parse.ID item)
		{
			var target = frame.Resolve<T>(item);
			GetTargetSources(target).Add(item);
			return target;
		}

		/// <summary> Resolves a given reference and logs the reference. </summary>
		public T ResolveReference<T>(Frame frame, Parse.Statement statement, Name name)
		{
			var target = frame.Resolve<T>(statement, name);
			GetTargetSources(target).Add(statement);
			return target;
		}

		public ExpressionContext ResolveContext(Frame frame, Parse.Statement statement, Name name)
		{
			var symbol = ResolveReference<object>(frame, statement, name);
			return _contextsBySymbol[symbol];
		}

		private List<object> GetTargetSources(object target)
		{
			List<object> sources;
			if (!_references.TryGetValue(target, out sources))
			{
				sources = new List<object>();
				_references.Add(target, sources);
			}
			return sources;
		}

		private void CompileAssignment(MethodContext method, Frame frame, Parse.ClausedAssignment assignment)
		{
			// TODO: handling of for, let, and where for assignment
			if (assignment.ForClauses.Count > 0 || assignment.LetClauses.Count > 0 || assignment.WhereClause != null)
				throw new NotSupportedException();

			var local = AddFrame(frame, assignment);
			method.IL.BeginScope();
			try
			{
				foreach (var set in assignment.Assignments)
				{
					var target = CompileExpression(local, set.Target);
					var source = CompileExpression(local, set.Source, target.Type);
					
					if (target.EmitSet == null)
						throw new CompilerException(set, CompilerException.Codes.CannotAssignToTarget);
					else
					{
						target.EmitSet
						(
							method,
							m =>
							{
								// Emit the source and convert if necessary
								source.EmitGet(method);
								if (source.Type != target.Type)
									Convert(source, target.Type).EmitGet(m);
							}
						);
					}
				}
			}
			finally
			{
				method.IL.EndScope();
			}
		}

		private BaseType CompileResult(MethodContext method, Frame frame, Parse.ClausedExpression expression)
		{
			var result = CompileClausedExpression(frame, expression);
			result.EmitGet(method);

			// Box the result if needed
			var nativeType = result.NativeType ?? result.Type.GetNative(_emitter);
			if (nativeType.IsValueType)
				method.IL.Emit(OpCodes.Box, nativeType);

			return result.Type;
		}

		private void CompileVar(MethodContext method, Frame frame, Parse.VarDeclaration declaration)
		{
			// Note: the parser will validate that either the type or initializer are given

			var name = Name.FromID(declaration.Name);

			// Compile the (optional) type
			var type = declaration.Type != null 
				? CompileTypeDeclaration(frame, declaration.Type) : 
				null;

			// Compile the (optional) initializer or use default of type
			var initializer = 
				CompileExpression
				(
					frame,
					declaration.Initializer != null
						? declaration.Initializer
						: type.BuildDefault(), 
					type
				);

			// Default the type to the initializer's type
			type = type ?? initializer.Type;
			var nativeType = type.GetNative(_emitter);

			// Create the variable
			var variable = method.DeclareLocal(declaration, nativeType, name.ToString());

			// Setup the symbol context
			var context = 				
				new ExpressionContext
				(
					new Parse.IdentifierExpression { Target = name.ToID() },
					type,
					Characteristic.Default,
					m => { m.IL.Emit(OpCodes.Ldloc, variable); }
				)
				{
					EmitSet = (m, s) =>
					{
						s(m);
						m.IL.Emit(OpCodes.Stloc, variable);
					}
				};
			_contextsBySymbol.Add(declaration, context);

			// Initialize variable, defaulting to passed in arguments if they are provided
			initializer.EmitGet(method);
			if (type != initializer.Type)
				Convert(initializer, type).EmitGet(method);
			method.IL.Emit(OpCodes.Ldarg_0);	// args
			method.EmitName(declaration.Name, name.Components);	// name
			method.IL.EmitCall(OpCodes.Call, ReflectionUtility.RuntimeGetInitializer.MakeGenericMethod(nativeType), null);
			method.IL.Emit(OpCodes.Stloc, variable);
		}

		private void CompileModule(MethodContext method, Frame frame, Parse.ModuleDeclaration module)
		{
			// Create the class for the module
			var moduleType = TypeFromModule(frame, module);

			// Build the code to declare the module
			method.EmitName(module.Name, Name.FromID(module.Name).Components);	// Name
			
			method.EmitVersion(module.Version);	// Version
			
			method.IL.Emit(OpCodes.Ldtoken, moduleType);
			method.IL.EmitCall(OpCodes.Call, ReflectionUtility.TypeGetTypeFromHandle, null);	// Module class

			method.IL.Emit(OpCodes.Ldarg_0);	// factory
			
			method.IL.EmitCall(OpCodes.Call, typeof(Runtime.Runtime).GetMethod("DeclareModule"), null);
		}

		private System.Type TypeFromModule(Frame frame, Parse.ModuleDeclaration moduleDeclaration)
		{
			var local = AddFrame(frame, moduleDeclaration);

			// Gather the module's symbols
			foreach (var member in moduleDeclaration.Members)
			{
				local.Add(member.Name, member);

				// Populate qualified enumeration members
				var memberName = Name.FromID(member.Name);
				if (member is Parse.EnumMember)
					foreach (var e in ((Parse.EnumMember)member).Values)
						local.Add(member, memberName + Name.FromID(e), member);

				//// HACK: Pre-discover sets of tuples (tables) because these may be needed by tuple references.  Would be better to separate symbol discovery from compilation for types.
				//Parse.TypeDeclaration varType;
				//if
				//(
				//	member is Parse.VarMember
				//	&& (varType = ((Parse.VarMember)member).Type) is Parse.SetType
				//	&& ((Parse.SetType)varType).Type is Parse.TupleType
				//)
				//	EnsureTupleTypeSymbols(frame, (Parse.TupleType)((Parse.SetType)varType).Type);
			}

			var module = _emitter.BeginModule(moduleDeclaration.Name.ToString());

			foreach (var member in moduleDeclaration.Members)
			{
				switch (member.GetType().Name)
				{
					case "VarMember":
					{
						var varMember = (Parse.VarMember)member;
						var compiledType = CompileTypeDeclaration(local, varMember.Type);
						var native = compiledType.GetNative(_emitter);
						if (native.ContainsGenericParameters)
							throw new NotImplementedException("Generic types are not currently supported for variables.");
						var field = _emitter.DeclareVariable(module, member.Name.ToString(), native);
						var context = 
							new ExpressionContext
							(
								new Parse.IdentifierExpression { Target = member.Name },
								compiledType,
								Characteristic.Default,
								m =>
								{
									m.IL.Emit(OpCodes.Ldarg_0);	// this
									m.IL.Emit(OpCodes.Ldfld, field);	// Repository
									m.IL.Emit(OpCodes.Ldnull);	// Condition
									m.IL.Emit(OpCodes.Ldnull);	// Order
									m.IL.EmitCall(OpCodes.Callvirt, field.FieldType.GetMethod("Get"), null);
								}
							)
							{
								EmitSet = (m, s) =>
								{
									m.IL.Emit(OpCodes.Ldarg_0);	// this
									m.IL.Emit(OpCodes.Ldfld, field);	// Repository
									m.IL.Emit(OpCodes.Ldnull);	// Condition
									s(m); // Value
									m.IL.EmitCall(OpCodes.Callvirt, field.FieldType.GetMethod("Set"), null);
								}
							};						
						_contextsBySymbol.Add(member, context); 
						break;
					}
					case "TypeMember":
					{
						var typeMember = (Parse.TypeMember)member;
						var compiledType = CompileTypeDeclaration(local, typeMember.Type);
						var result = _emitter.DeclareTypeDef(module, member.Name.ToString(), compiledType.GetNative(_emitter));
						_contextsBySymbol.Add
						(
							member, 
							new ExpressionContext(new Parse.IdentifierExpression { Target = member.Name }, compiledType, Characteristic.Default, null)
						);
						break;
					}
					case "EnumMember":
					{
						// TODO: Enums
						//var enumMember = (Parse.EnumMember)member;
						//var enumType = _emitter.DeclareEnum(module, member.Name.ToString());
						//var i = 0;
						//foreach (var value in from v in enumMember.Values select v.ToString())
						//{
						//	FieldBuilder field = enumType.DefineField(value.ToString(), enumType, FieldAttributes.Public | FieldAttributes.Literal | FieldAttributes.Static);
						//	field.SetConstant(i++);
						//}
						//return enumType.CreateType();
						//_contextsBySymbol.Add
						//(
						//	member, 
						//	new ExpressionContext(new Parse.IdentifierExpression { Target = member.Name }, compiledType, Characteristic.Default, null)
						//);
						break;
					}
					case "ConstMember":
					{
						var constExpression = (Parse.ConstMember)member;
						var expression = CompileExpression(local, constExpression.Expression);
						var native = expression.NativeType ?? expression.Type.GetNative(_emitter);
						var expressionResult = CompileTimeEvaluate(constExpression.Expression, expression, native);
						var field = _emitter.DeclareConst(module, member.Name.ToString(), expressionResult, native);
						break;
					}
					default: throw new Exception("Internal Error: Unknown member type " + member.GetType().Name);
				}
			}

			// Compile in no particular order until all members are resolved
			while (_uncompiledMembers.Count > 0)
				_uncompiledMembers.First().Value();

			return _emitter.EndModule(module);
		}

		private static object CompileTimeEvaluate(Parse.Statement statement, ExpressionContext expression, System.Type native)
		{
			if (expression.Characteristics != Characteristic.Constant)
				throw new CompilerException(statement, CompilerException.Codes.ConstantExpressionExpected);
			var dynamicMethod = new DynamicMethod("", native, null);
			var method = new MethodContext(dynamicMethod);
			expression.EmitGet(method);
			if (native.IsValueType)
				method.IL.Emit(OpCodes.Box, native);
			method.IL.Emit(OpCodes.Ret);
			return dynamicMethod.Invoke(null, null);
		}

		private void CompileUsing(MethodContext method, Frame frame, Frame modulesFrame, Parse.Using use)
		{
			var moduleName = Name.FromID(use.Target);
			var module = ResolveReference<Runtime.ModuleTuple>(modulesFrame, use, moduleName);

			// Determine the class of the module
			var moduleType = module.Class;

			// Create a variable to hold the module instance
			var moduleVar = method.DeclareLocal(use, moduleType, (use.Alias ?? use.Target).ToString());

			// Discover methods
			foreach (var methodInfo in module.Class.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
			{
				frame.Add(use, moduleName + Name.FromNative(methodInfo.Name), methodInfo);
				_emitter.ImportType(methodInfo.ReturnType);
				foreach (var parameter in methodInfo.GetParameters())
					_emitter.ImportType(parameter.ParameterType);
			}

			// Discover enums
			foreach (var type in module.Class.GetNestedTypes(BindingFlags.Public))
			{
				var enumName = moduleName + Name.FromNative(type.Name);
				frame.Add(use, enumName, type);
				foreach (var enumItem in type.GetFields(BindingFlags.Public | BindingFlags.Static))
					frame.Add(use, enumName + Name.FromNative(enumItem.Name), enumItem);
			}

			// Discover consts
			foreach (var field in module.Class.GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => (f.Attributes & FieldAttributes.Literal) == FieldAttributes.Literal))
			{
				var name = moduleName + Name.FromNative(field.Name);
				frame.Add(use, name, field);
				_emitter.ImportType(field.FieldType);
				var type = _emitter.TypeFromNative(field.FieldType);
				_contextsBySymbol.Add
				(
					field,
					new ExpressionContext
					(
						new Parse.IdentifierExpression { Target = name.ToID() },
						type,
						Characteristic.Constant,
						m =>
						{
							var expression = new Parse.LiteralExpression { Value = field.GetValue(null) };
							var context = CompileLiteral(frame, expression, type);
							context.EmitGet(m);
						}
					)
				);
			}

			// Discover variables
			foreach (var field in module.Class.GetFields(BindingFlags.Public | BindingFlags.Instance))
			{
				var fullName = moduleName + Name.FromNative(field.Name);
				frame.Add(use, fullName, field);
				// Variables are IRepository<T> - determine the T
				var native = field.FieldType.GenericTypeArguments[0];
				_emitter.ImportType(native);
				var type = _emitter.TypeFromNative(native);

				var context =
					new ExpressionContext
					(
						new Parse.IdentifierExpression { Target = fullName.ToID() }, 
						type, 
						Characteristic.Default,
						m => 
						{
							m.IL.Emit(OpCodes.Ldloc, moduleVar); 
							m.IL.Emit(OpCodes.Ldfld, field); // Repository
							m.IL.Emit(OpCodes.Ldnull);	// Condition
							m.IL.Emit(OpCodes.Ldnull);	// Order
							m.IL.EmitCall(OpCodes.Callvirt, field.FieldType.GetMethod("Get"), null);
						}
					)
					{
						EmitSet = (m, s) =>
						{
							m.IL.Emit(OpCodes.Ldloc, moduleVar); 
							m.IL.Emit(OpCodes.Ldfld, field); // Repository
							m.IL.Emit(OpCodes.Ldnull);	// Condition
							s(m); // Value
							m.IL.EmitCall(OpCodes.Callvirt, field.FieldType.GetMethod("Set"), null);
						}
					};
				_contextsBySymbol.Add(field, context);
			}

			// Discover typedefs
			foreach (var field in module.Class.GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => (f.Attributes & FieldAttributes.Literal) != FieldAttributes.Literal))
			{
				frame.Add(use, moduleName + Name.FromNative(field.Name), field);
				_emitter.ImportType(field.FieldType);
				var type = _emitter.TypeFromNative(field.FieldType);
				var context =
					new ExpressionContext
					(
						null,
						type,
						Characteristic.Constant,
						null
					);
			}

			// Build code to construct instance and assign to variable
			method.IL.Emit(OpCodes.Newobj, moduleType.GetConstructor(new System.Type[] { }));
			// Initialize each variable bound to a repository
			foreach (var field in moduleType.GetFields(BindingFlags.Public | BindingFlags.Instance))
			{
				// <instance>.<field> = factory.GetRepository(moduleType, Name.FromNative(new string[] { field.Name }))
				method.IL.Emit(OpCodes.Dup);
				method.IL.Emit(OpCodes.Ldarg_1);
				method.IL.Emit(OpCodes.Ldtoken, moduleType);
				method.IL.EmitCall(OpCodes.Call, ReflectionUtility.TypeGetTypeFromHandle, null);
				method.IL.Emit(OpCodes.Ldc_I4_1);
				method.IL.Emit(OpCodes.Newarr, typeof(string));
				method.IL.Emit(OpCodes.Dup);
				method.IL.Emit(OpCodes.Ldc_I4_0);
				method.IL.Emit(OpCodes.Ldstr, field.Name);
				method.IL.Emit(OpCodes.Stelem_Ref);
				method.IL.EmitCall(OpCodes.Call, ReflectionUtility.NameFromComponents, null);
				method.IL.EmitCall(OpCodes.Callvirt, typeof(Storage.IRepositoryFactory).GetMethod("GetRepository").MakeGenericMethod(field.FieldType.GenericTypeArguments), null);
				method.IL.Emit(OpCodes.Stfld, field);
			}
			method.IL.Emit(OpCodes.Stloc, moduleVar);
		}

		//private DebugInfoExpression GetDebugInfo(Parse.Statement statement)
		//{
		//	return Expression.DebugInfo
		//	(
		//		_symbolDocument, 
		//		statement.Line + 1, 
		//		statement.LinePos + 1, 
		//		(statement.EndLine < 0 ? statement.Line : statement.EndLine) + 1, 
		//		(statement.EndLinePos < 0 ? statement.LinePos : statement.EndLinePos) + 1
		//	);
		//}

		private ExpressionContext CompileClausedExpression(Frame frame, Parse.ClausedExpression expression, BaseType typeHint = null)
		{
			if (expression.ForClauses.Count > 0)
			{
				var local = AddFrame(frame, expression);

				// Declare result variable 
				LocalBuilder resultVariable = null;
				local.Add(expression, Name.FromComponents("@result"), expression);
				var resultContext = new ExpressionContext(null, null, Characteristic.Default, m => m.IL.Emit(OpCodes.Ldloc, resultVariable));
				_contextsBySymbol.Add(expression, resultContext);

				var result = CompileForClause(local, 0, expression);
				resultContext.Type = result.Type;

				return
					new ExpressionContext
					(
						expression,
						result.Type,
						result.Characteristics,
						m =>
						{
							m.IL.BeginScope();

							// Define the result variable
							var resultNative = result.ActualNative(_emitter);
							resultVariable = m.DeclareLocal(expression, resultNative, "@result");

							// Initialize the result collection 
							m.IL.Emit(OpCodes.Newobj, resultNative.GetConstructor(new System.Type[] { }));
							m.IL.Emit(OpCodes.Stloc, resultVariable);

							result.EmitGet(m);

							m.IL.EndScope();
						}
					);
			}
			else
				return CompileClausedReturn(frame, expression, typeHint);
		}

		private ExpressionContext CompileForClause(Frame frame, int index, Parse.ClausedExpression expression)
		{
			if (index < expression.ForClauses.Count)
			{
				LocalBuilder forVariable = null;
				var forClause = expression.ForClauses[index];

				// Compile target expression
				var forExpression = CompileExpression(frame, forClause.Expression);

				// Validate that we can enumerate this type
				if (!(forExpression.Type is IComponentType))
					throw new CompilerException(forClause, CompilerException.Codes.InvalidForExpressionTarget, forExpression.Type);

				// Determine types
				var elementType = ((IComponentType)forExpression.Type).Of;
				var nativeElementType = elementType.GetNative(_emitter);
				var enumerableType = typeof(IEnumerable<>).MakeGenericType(nativeElementType);
				var enumeratorType = typeof(IEnumerator<>).MakeGenericType(nativeElementType);

				// Add local frame and symbol
				var local = AddFrame(frame, expression);
				local.Add(forClause.Name, forClause);
				_contextsBySymbol.Add
				(
					forClause, 
					new ExpressionContext
					(
						new Parse.IdentifierExpression { Target = forClause.Name },
						elementType,
						Characteristic.Default,
						m => { m.IL.Emit(OpCodes.Ldloc, forVariable); }
					)
				);

				// Compile nested For or Return clause
				var nested = CompileForClause(local, index + 1, expression);

				// Determine the result type
				var resultType = 
					nested.Type is NaryType 
						? // force to list if not iterating a set
						(
							!(forExpression.Type is SetType) 
								? (BaseType)new ListType(((NaryType)nested.Type).Of) 
								: nested.Type
						)
						: new SetType(nested.Type);
				
				ExpressionContext whereResult = null;
				if (expression.WhereClause != null)
				{
					// if (<where expression>) continue
					whereResult = CompileExpression(local, expression.WhereClause, SystemTypes.Boolean);
					if (!(whereResult.Type is BooleanType))
						throw new CompilerException(expression.WhereClause, CompilerException.Codes.IncorrectType, whereResult.Type, SystemTypes.Boolean);
				}

				return
					new ExpressionContext
					(
						expression,
						resultType,
						Characteristic.Default,
						m =>
						{
							// Declare enumerator and item variables
							var enumerator = m.DeclareLocal(forClause, enumeratorType, "enumerator" + Name.FromID(forClause.Name).ToString());
							forVariable = m.DeclareLocal(forClause, nativeElementType, Name.FromID(forClause.Name).ToString());

							// Emit for expression, should result in something enumerable
							forExpression.EmitGet(m);

							// enumerator = GetEnumerator()
							m.IL.EmitCall(OpCodes.Callvirt, enumerableType.GetMethod("GetEnumerator"), null);
							m.IL.Emit(OpCodes.Stloc, enumerator);

							// while (enumerator.MoveNext())
							var loopStart = m.IL.DefineLabel();
							var loopEnd = m.IL.DefineLabel();
							m.IL.MarkLabel(loopStart);
							m.IL.Emit(OpCodes.Ldloc, enumerator);
							m.IL.EmitCall(OpCodes.Callvirt, ReflectionUtility.IEnumerableMoveNext, null);
							m.IL.Emit(OpCodes.Brfalse, loopEnd);

							// forVariable = enumerator.Current
							m.IL.Emit(OpCodes.Ldloc, enumerator);
							m.IL.EmitCall(OpCodes.Callvirt, enumeratorType.GetProperty("Current").GetGetMethod(), null);
							m.IL.Emit(OpCodes.Stloc, forVariable);

							if (whereResult != null)
							{
								whereResult.EmitGet(m);
								m.IL.Emit(OpCodes.Brfalse, loopStart);
							}

							m.IL.Emit(OpCodes.Br, loopStart);
							m.IL.MarkLabel(loopEnd);
						}
					);
			}
			else
			{
				// Compile the return block	and store it in a variable
				var returnBlock = CompileClausedReturn(frame, expression);
				return
					new ExpressionContext
					(
						expression,
						returnBlock.Type,
						Characteristic.Default,
						m =>
						{
							// Emit result collection
							var result = ResolveContext(frame, expression, Name.FromComponents("@result"));
							result.EmitGet(m);

							// Emit the return expression
							returnBlock.EmitGet(m);

							// Add the current item to the result
							var addMethod = result.ActualNative(_emitter).GetMethod("Add");
							m.IL.EmitCall(OpCodes.Call, addMethod, null);
							if (addMethod.ReturnType != typeof(void))
								m.IL.Emit(OpCodes.Pop);	// ignore any add result
						}
					);
			}
		}

		private ExpressionContext CompileClausedReturn(Frame frame, Parse.ClausedExpression expression, BaseType typeHint = null)
		{
			var letContexts = new Dictionary<Parse.LetClause, ExpressionContext>();
			var letVars = new Dictionary<Parse.LetClause, LocalBuilder>();

			// Compile and define a symbol for each let
			foreach (var let in expression.LetClauses)
			{
				var letResult = CompileExpression(frame, let.Expression);
				letContexts.Add(let, letResult);

				_contextsBySymbol.Add
				(
					let, 
					new ExpressionContext
					(
						expression, 
						letResult.Type, 
						letResult.Characteristics, 
						m => { m.IL.Emit(OpCodes.Ldloc, letVars[let]); }
					)
				);
				frame.Add(let.Name, let);
			}

			// Compile main expression
			var main = CompileExpression(frame, expression.Expression, typeHint);

			// Add the expression to the body
			return 
				new ExpressionContext
				(
					expression,
					main.Type,
					main.Characteristics,
					m =>
					{
						// Create a variable for each let and initialize
						foreach (var let in expression.LetClauses)
						{
							// Emit let expression
							var letResult = letContexts[let];
							letResult.EmitGet(m);

							// Emit assignment to var
							var variable = m.DeclareLocal(let, letResult.ActualNative(_emitter), Name.FromID(let.Name).ToString());
							letVars[let] = variable;
							m.IL.Emit(OpCodes.Stloc, variable);
						}

						main.EmitGet(m);
					}
				);
		}

		public ExpressionContext CompileExpression(Frame frame, Parse.Expression expression, BaseType typeHint = null)
		{
			switch (expression.GetType().Name)
			{
				case "LiteralExpression": return CompileLiteral(frame, (Parse.LiteralExpression)expression, typeHint);
				case "BinaryExpression": return CompileBinaryExpression(frame, (Parse.BinaryExpression)expression, typeHint);
				case "UnaryExpression": return CompileUnaryExpression(frame, (Parse.UnaryExpression)expression, typeHint);
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

		private ExpressionContext CompileRestrictExpression(Frame frame, Parse.RestrictExpression restrictExpression, BaseType typeHint)
		{
			var left = CompileExpression(frame, restrictExpression.Expression, typeHint);
			return left.Type.CompileRestrictExpression(this, frame, left, restrictExpression, typeHint);
		}

		public ExpressionContext CompileFunctionSelector(Frame frame, Parse.FunctionSelector functionSelector, BaseType typeHint)
		{
			// Extract return type as the type hint
			if (typeHint is FunctionType)
				typeHint = ((FunctionType)typeHint).Type;
			else
				typeHint = null;

			var local = AddFrame(_importFrame, functionSelector);

			var type = new FunctionType();

			// Compile each parameter and define the parameter symbols
			var index = 0;
			foreach (var p in functionSelector.Parameters)
			{
				local.Add(p.Name, p);
				var parameter = new FunctionParameter { Name = Name.FromID(p.Name), Type = CompileTypeDeclaration(_importFrame, p.Type) };
				type.Parameters.Add(parameter);
				_contextsBySymbol.Add
				(
					p,
					new ExpressionContext
					(
						functionSelector,
						parameter.Type,
						Characteristic.Default,
						m => { m.IL.Emit(OpCodes.Ldarg, index); }
					)
				);
				++index;
			}

			// Compile the body
			var expression = CompileExpression(local, functionSelector.Expression, typeHint);
			type.Type = expression.Type;

			// Determine the native param types
			var nativeParamTypes = functionSelector.Parameters.Select((p, i) => type.Parameters[i].Type.GetNative(_emitter)).ToArray();
			var nativeReturnType = expression.NativeType ?? expression.Type.GetNative(_emitter);

			return
				new ExpressionContext
				(
					functionSelector,
					type,
					Characteristic.Default,
					m =>
					{	
						// Create a new private method within the same type as the current method
						var typeBuilder = (TypeBuilder)m.Builder.DeclaringType;
						var innerMethod = new MethodContext
						(
							typeBuilder.DefineMethod("Function" + functionSelector.GetHashCode(), MethodAttributes.Private | MethodAttributes.Static, nativeReturnType, nativeParamTypes)
						);
						expression.EmitGet(innerMethod);
						innerMethod.IL.Emit(OpCodes.Ret);

						// Instantiate a delegate pointing to the new method
						var delegateType = type.GetNative(_emitter);
						m.IL.Emit(OpCodes.Ldnull);	// instance
						m.IL.Emit(OpCodes.Ldftn, innerMethod.Builder);	// method
						m.IL.Emit(OpCodes.Newobj, delegateType.GetConstructor(new[] { typeof(object), typeof(IntPtr) }));
					}
				);
		}

		private ExpressionContext CompileCallExpression(Frame frame, Parse.CallExpression callExpression, BaseType typeHint)
		{
			/* 
			 * Functions are implemented as delegates if referencing a variable, or as methods if referencing a constant.
			 * The logical type will always be a FunctionType, but the native type with either be a MethodInfo
			 * or a Delegate.
			 */

			// Compile expression
			var expression = CompileExpression(frame, callExpression.Expression);
			if (!(expression.Type is FunctionType))
				throw new CompilerException(callExpression.Expression, CompilerException.Codes.CannotInvokeNonFunction);

			// Compile arguments
			var args = new ExpressionContext[callExpression.Arguments.Count];
			for (var i = 0; i < callExpression.Arguments.Count; i++)
				args[i] = CompileExpression(frame, callExpression.Arguments[i]);

			return 
				new ExpressionContext
				(
					callExpression,
					expression.Type,
					expression.Characteristics,
					m =>
					{
						if (expression.Member != null)
						{
							var methodType = (MethodInfo)(expression.Member);

							// Resolve generic arguments
							if (methodType.ContainsGenericParameters)
							{
								var genericArgs = methodType.GetGenericArguments();
								var resolved = new System.Type[genericArgs.Length];
								if (callExpression.TypeArguments.Count > 0)
								{
									for (var i = 0; i < resolved.Length; i++)
										resolved[i] = CompileTypeDeclaration(frame, callExpression.TypeArguments[i]).GetNative(_emitter);
								}
								else
								{
									var parameters = methodType.GetParameters();
									for (var i = 0; i < parameters.Length; i++)
										DetermineTypeParameters(callExpression, resolved, parameters[i].ParameterType, args[i].NativeType ?? args[i].Type.GetNative(_emitter));
									// TODO: Assert that all type parameters are resolved
								}
								methodType = methodType.MakeGenericMethod(resolved);
								// http://msdn.microsoft.com/en-us/library/system.reflection.methodinfo.makegenericmethod.aspx
							}
							m.IL.EmitCall(OpCodes.Call, methodType, null);
						}
						else
						{
							var delegateType = expression.Type.GetNative(_emitter);
							m.IL.EmitCall(OpCodes.Callvirt, delegateType.GetMethod("Invoke"), null);
						}
					}
				);
		}

		private void DetermineTypeParameters(Parse.Statement statement, System.Type[] resolved, System.Type parameterType, System.Type argumentType)
		{
			// If the given parameter contains an unresolved generic type parameter, attempt to resolve using actual arguments
			if (parameterType.ContainsGenericParameters)
			{
				var paramArgs = parameterType.GetGenericArguments();
				var argArgs = argumentType.GetGenericArguments();
				if (paramArgs.Length != argArgs.Length)
					throw new CompilerException(statement, CompilerException.Codes.MismatchedGeneric, parameterType, argumentType);
				for (var i = 0; i < paramArgs.Length; i++)
					if (paramArgs[i].IsGenericParameter && resolved[paramArgs[i].GenericParameterPosition] == null)
						resolved[paramArgs[i].GenericParameterPosition] = argArgs[i];
					else
						DetermineTypeParameters(statement, resolved, paramArgs[i], argArgs[i]);
			}
		}

		private BaseType CompileTypeDeclaration(Frame frame, Parse.TypeDeclaration typeDeclaration)
		{
			switch (typeDeclaration.GetType().Name)
			{
				case "OptionalType": return new OptionalType(CompileTypeDeclaration(frame, ((Parse.OptionalType)typeDeclaration).Type));
				case "ListType": return new ListType(CompileTypeDeclaration(frame, ((Parse.ListType)typeDeclaration).Type));
				case "SetType": return new SetType(CompileTypeDeclaration(frame, ((Parse.SetType)typeDeclaration).Type));
				case "TupleType": return CompileTupleType(frame, (Parse.TupleType)typeDeclaration);
				case "FunctionType": return CompileFunctionType(frame, (Parse.FunctionType)typeDeclaration);
				case "NamedType": return CompileNamedType(frame, (Parse.NamedType)typeDeclaration);
				default: throw new Exception("Internal Error: Unknown type declaration " + typeDeclaration.GetType().Name); 
			}
		}

		private BaseType CompileNamedType(Frame frame, Parse.NamedType namedType)
		{
			var target = ResolveReference<object>(frame, namedType.Target);
			ExpressionContext result;
			if (_contextsBySymbol.TryGetValue(target, out result) && result.EmitGet == null)
				return result.Type;
			else
				throw new CompilerException(namedType, CompilerException.Codes.IncorrectTypeReferenced, "typedef", target.GetType());
		}

		private void EndRecursionCheck(Parse.Statement statement)
		{
			_recursions.Remove(statement);
		}

		private void BeginRecursionCheck(Parse.Statement statement)
		{
			if (!_recursions.Add(statement))
				throw new CompilerException(statement, CompilerException.Codes.RecursiveDeclaration);
		}

		public BaseType CompileTupleType(Frame frame, Parse.TupleType tupleType)
		{
			// Create symbols
			var local = AddFrame(frame, tupleType);

			foreach (var a in tupleType.Attributes)
				local.Add(a.Name, a);

			foreach (var k in tupleType.Keys)
				ResolveListReferences(local, k.AttributeNames);

			foreach (var r in tupleType.References)
				ResolveListReferences(local, r.SourceAttributeNames);

			var normalized = new Type.TupleType();

			// Attributes
			foreach (var a in tupleType.Attributes)
				normalized.Attributes.Add(Name.FromID(a.Name), CompileTypeDeclaration(frame, a.Type));		// uses frame, not local

			// References
			foreach (var k in tupleType.Keys)
				normalized.Keys.Add(new Type.TupleKey { AttributeNames = IdentifiersToNames(k.AttributeNames) });

			// Keys
			foreach (var r in tupleType.References)
			{
				var target = ResolveReference<Parse.Statement>(frame, r.Target);
				if (target is Parse.VarMember)
				{
					// Get the tuple type for the table
					var targetTupleType = CheckedTableType(r.Target, ((Parse.VarMember)target).Type);

					// Add references to each target attribute
					ResolveListReferences(_frames[targetTupleType], r.TargetAttributeNames);
				}
				normalized.References.Add
				(
					Name.FromID(r.Name),
					new Type.TupleReference
					{
						SourceAttributeNames = IdentifiersToNames(r.SourceAttributeNames),
						Target = Name.FromID(r.Target),
						TargetAttributeNames = IdentifiersToNames(r.TargetAttributeNames)
					}
				);
			}

			return normalized;
		}

		/// <summary> Validates that the given target type is a table (set or list of tuples) and returns the tuple type.</summary>
		private static Parse.TypeDeclaration CheckedTableType(Parse.Statement statement, Parse.TypeDeclaration targetType)
		{
			if (!(targetType is Parse.NaryType))
				throw new CompilerException(statement, CompilerException.Codes.IncorrectTypeReferenced, "Set or List of Tuple", targetType.GetType().Name);
			var memberType = ((Parse.NaryType)targetType).Type;
			if (!(memberType is Parse.TupleType))
				throw new CompilerException(statement, CompilerException.Codes.IncorrectTypeReferenced, "Set or List of Tuple", targetType.GetType().Name);
			return memberType;
		}
		
		private BaseType CompileFunctionType(Frame frame, Parse.FunctionType functionType)
		{
			return 
				new FunctionType 
				{ 
					Parameters = 
					(
						from p in functionType.Parameters 
						select new FunctionParameter { Name = Name.FromID(p.Name), Type = CompileTypeDeclaration(frame, p.Type) }
					).ToList(), 
					Type = CompileTypeDeclaration(frame, functionType.ReturnType)
				};
		}

		private static Name[] IdentifiersToNames(IEnumerable<Parse.ID> ids)
		{
			return (from n in ids select Name.FromID(n)).ToArray();
		}

		private ExpressionContext CompileSetSelector(Frame frame, Parse.SetSelector setSelector, BaseType typeHint)
		{
			// Get the component type
			if (typeHint is SetType)
				typeHint = ((SetType)typeHint).Of;
			else
				typeHint = null;

			return EmitNarySelector(frame, of => new SetType(of), setSelector, setSelector.Items, typeHint);
		}

		private ExpressionContext CompileListSelector(Frame frame, Parse.ListSelector listSelector, BaseType typeHint)
		{
			// Get the component type
			if (typeHint is ListType)
				typeHint = ((ListType)typeHint).Of;
			else
				typeHint = null;

			return EmitNarySelector(frame, of => new ListType(of), listSelector, listSelector.Items, typeHint);
		}

		private ExpressionContext EmitNarySelector(Frame frame, Func<BaseType, NaryType> construct, Parse.Expression statement, List<Parse.Expression> items, BaseType elementTypeHint)
		{
			// Compile each item and determine data type
			NaryType type = null;
			List<ExpressionContext> expressions = null;
			if (items.Count > 0)
			{
				expressions = new List<ExpressionContext>();
				// Compile the first item to determine the data type
				foreach (var e in items)
				{
					var expression = CompileExpression(frame, e, elementTypeHint);
					expressions.Add(expression);
					if (type == null && expression.Type != SystemTypes.Void)
						type = construct(expression.Type);
				}
			}
			if (type == null)
				type = construct(elementTypeHint ?? SystemTypes.Void);
			var elementType = type.Of;
			var naryNative = type.GetNative(_emitter);
			
			return
				new ExpressionContext
				(
					statement,
					type,
					Characteristic.Constant,
					m =>
					{
						m.IL.BeginScope();
						if (expressions != null)
						{
							// Construct the set/list			
							//  Attempt to find constructor that takes an initial capacity
							var constructor = naryNative.GetConstructor(new System.Type[] { typeof(int) });
							if (constructor == null)
								constructor = naryNative.GetConstructor(new System.Type[] { });
							else
								m.IL.Emit(OpCodes.Ldc_I4, items.Count);
							m.IL.Emit(OpCodes.Newobj, constructor);

							var addMethod = naryNative.GetMethod("Add");

							// Add items
							foreach (var item in expressions)
							{
								m.IL.Emit(OpCodes.Dup);	// collection
								item.EmitGet(m);
								// Convert the element if needed
								if (item.Type != elementType)
									Convert(item, elementType).EmitGet(m);
								m.IL.Emit(OpCodes.Call, addMethod);
								if (addMethod.ReturnType != typeof(void))
									m.IL.Emit(OpCodes.Pop);	// ignore any add result
							}
						}
						else
						{
							// Construct empty list
							m.IL.Emit(OpCodes.Newobj, naryNative.GetConstructor(new System.Type[] { }));
						}
						m.IL.EndScope();
					}
				);
		}

		private ExpressionContext Convert(ExpressionContext expression, BaseType target)
		{
			return expression.Type.Convert(expression, target);
		}

		private ExpressionContext CompileTupleSelector(Frame frame, Parse.TupleSelector tupleSelector, BaseType typeHint)
		{
			var tupleType = new Type.TupleType();
			var local = AddFrame(frame, tupleSelector);
			var expressions = new Dictionary<Name, ExpressionContext>(tupleSelector.Attributes.Count);

			// Compile and resolve attributes
			foreach (var a in tupleSelector.Attributes)
			{
				// Compile the attribute selector
				var attributeName = Name.FromID(EnsureAttributeName(a.Name, a.Value));
				var attributeNameAsString = attributeName.ToString();
				var valueExpression = CompileExpression(frame, a.Value);		// Use frame not local (attributes aren't visible to each other)
				expressions.Add(attributeName, valueExpression);

				// Contribute to the tuple type
				tupleType.Attributes.Add(attributeName, valueExpression.Type);
				
				// Declare the attribute symbol
				local.Add(a, attributeName, a);
			}

			// Resolve source reference columns
			foreach (var k in tupleSelector.Keys)
			{
				ResolveListReferences(local, k.AttributeNames);

				tupleType.Keys.Add(Type.TupleKey.FromParseKey(k));
			}

			// Resolve key reference columns
			foreach (var r in tupleSelector.References)
			{
				ResolveListReferences(local, r.SourceAttributeNames);
				var target = ResolveReference<Parse.Statement>(_scriptFrame, r.Target);
				ResolveListReferences(_frames[target], r.TargetAttributeNames);

				tupleType.References.Add(Name.FromID(r.Name), Type.TupleReference.FromParseReference(r));
			}		

			return 
				new ExpressionContext
				(
					tupleSelector,
					tupleType,
					Characteristic.Constant,
					m =>
					{
						m.IL.BeginScope();

						var native = _emitter.FindOrCreateNativeFromTupleType(tupleType);
						var instance = m.DeclareLocal(tupleSelector, native, "tuple" + tupleSelector.GetHashCode());

						// Initialize each field
						foreach (var field in native.GetFields(BindingFlags.Public | BindingFlags.Instance))
						{
							m.IL.Emit(OpCodes.Ldloca, instance);
							expressions[Name.FromNative(field.Name)].EmitGet(m);
							m.IL.Emit(OpCodes.Stfld, field);
						}

						m.IL.Emit(OpCodes.Ldloc, instance);

						m.IL.EndScope();
					}
				);
		}

		private static Parse.ID EnsureAttributeName(Parse.ID name, Parse.Expression expression)
		{
			return name == null ? NameFromExpression(expression) : name; 
		}

		private static Parse.ID NameFromExpression(Parse.Expression expression)
		{
			if (expression is Parse.IdentifierExpression)
				return ((Parse.IdentifierExpression)expression).Target;
			else
				throw new CompilerException(expression, CompilerException.Codes.CannotInferNameFromExpression);
		}

		private ExpressionContext CompileIdentifierExpression(Frame frame, Parse.IdentifierExpression identifierExpression, BaseType typeHint)
		{
			var symbol = ResolveReference<object>(frame, identifierExpression.Target);
			ExpressionContext context;
			if (_contextsBySymbol.TryGetValue(symbol, out context))
				return context;

			// Lazy-compile module member if needed
			symbol = LazyCompileModuleMember(identifierExpression, symbol);

			if (_contextsBySymbol.TryGetValue(symbol, out context))
				return context;

			throw new CompilerException(identifierExpression, CompilerException.Codes.IdentifierNotFound, identifierExpression.Target);
		}

		private object LazyCompileModuleMember(Parse.Statement statement, object symbol)
		{
			if (symbol is Parse.ModuleMember)
			{
				var member = (Parse.ModuleMember)symbol;
				BeginRecursionCheck(statement);
				try
				{
					Func<MemberInfo> compilation;
					if (_uncompiledMembers.TryGetValue(member, out compilation))
						compilation();
					symbol = _compiledMembers[member];
				}
				finally
				{
					EndRecursionCheck(statement);
				}
			}
			return symbol;
		}

		private ExpressionContext CompileBinaryExpression(Frame frame, Parse.BinaryExpression expression, BaseType typeHint)
		{
			var left = CompileExpression(frame, expression.Left);
			return left.Type.CompileBinaryExpression(this, frame, left, expression, typeHint);
		}

		private ExpressionContext CompileUnaryExpression(Frame frame, Parse.UnaryExpression expression, BaseType typeHint)
		{
			var inner = CompileExpression(frame, expression.Expression);
			return inner.Type.CompileUnaryExpression(this, frame, inner, expression, typeHint);
		}

		public Frame AddFrame(Frame parent, Parse.Statement statement)
		{
			var newFrame = new Frame(parent);
			_frames.Add(statement, newFrame);
			return newFrame;
		}

		private ExpressionContext CompileLiteral(Frame frame, Parse.LiteralExpression expression, BaseType typeHint)
		{
			if (expression.Value == null)
				return 
					new ExpressionContext
					(
						expression, 
						typeHint ?? SystemTypes.Void, 
						Characteristic.Constant, 
						m => m.IL.Emit(OpCodes.Ldnull)
					);
			else 
			{
				var type = _emitter.TypeFromNative(expression.Value.GetType());
				return
					new ExpressionContext
					(
						expression,
						type,
						Characteristic.Constant,
						m => type.EmitLiteral(m, expression.Value)
					);
			}
		}
	}
}

