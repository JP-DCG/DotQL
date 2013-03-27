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
		private Dictionary<Parse.ModuleMember, Action> _uncompiledMembers = new Dictionary<Parse.ModuleMember, Action>();
		private Dictionary<Parse.ID, Parse.EnumMember> _enumMembers = new Dictionary<Parse.ID,Parse.EnumMember>();

		// Using private constructor pattern because state spans single static call
		private Compiler() { }

        public static Characteristic MergeCharacteristics(Characteristic characteristic1, Characteristic characteristic2)
        {
            return (characteristic1 & (Characteristic.NonDeterministic | Characteristic.SideEffectual))
                | (characteristic2 & (Characteristic.NonDeterministic | Characteristic.SideEffectual))
                |
                (
                    (characteristic1 & Characteristic.Constant)
                        & (characteristic2 & Characteristic.Constant)
                );
        }


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

		private static void EmitFactoryArgument(MethodContext method)
		{
			method.IL.Emit(OpCodes.Ldarg_1);
		}

		private static void EmitArgsArgument(MethodContext method)
		{
			method.IL.Emit(OpCodes.Ldarg_0);
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

        public object ResolveFunction(Frame frame, Parse.CallExpression callExpression, ExpressionContext[] args, IEnumerable<Parse.FunctionMember> functions)
        {
			if (callExpression.Argument != null)
			{
				// TODO: tuple resolution
				throw new NotImplementedException("Tuple call resolution.");
			}
			else
			{
				// Candidate functions don't have fewer arguments than in call
				var potential = functions.Where(m => m.Parameters.Count < callExpression.Arguments.Count).ToList();

				// Validate that at least 1 candidate was found
				if (potential.Count == 0)
					throw new CompilerException(callExpression, CompilerException.Codes.SignatureMismatch, callExpression.Name);

				Parse.FunctionMember function = null;

				for (int f = 0; function == null && f < potential.Count; ++f)
				{
					var candidate = potential[f];
					var candidateTypes = candidate.Parameters.Select(p => CompileTypeDeclaration(frame, p.Type)).ToArray();

					if (candidate.TypeParameters.Count > 0)
					{
						var resolved = new BaseType[candidate.TypeParameters.Count];
						if (callExpression.TypeArguments.Count > 0)
						{
							if (callExpression.TypeArguments.Count != candidate.TypeParameters.Count)
								continue;
							for (var i = 0; i < resolved.Length; i++)
								resolved[i] = CompileTypeDeclaration(frame, callExpression.TypeArguments[i]);
						}
						else
						{
							// TODO: generic type determination for BaseTypes
							throw new NotImplementedException("Generic type determination for BaseTypes.");
							//for (var i = 0; i < parameters.Length; i++)
							//	DetermineTypeParameters(callExpression, resolved, parameters[i].ParameterType, args[i].NativeType ?? args[i].Type.GetNative(Emitter));
						}

						// TODO: generic type resolution
						throw new NotImplementedException("generic type resolution");
						//methodInfo = methodInfo.MakeGenericMethod(resolved);
						//parameters = methodInfo.GetParameters();
					}

					// If the first parameter isn't an exact match continue to the next function and try to match.
					if ((candidateTypes.Length == 0 && args.Length == 0) || candidateTypes.Length > 0 && args.Length > 0 && candidateTypes[0] != args[0].Type)
						continue;

					var actual = new ExpressionContext[Math.Max(args.Length, candidateTypes.Length)];
					// Attempt to convert or default rest of parameters.
					for (int a = 1; a < actual.Length; a++)
					{
						if (a < args.Length)
							if (args[a].Type != candidateTypes[a])
								actual[a] = Convert(args[a], candidateTypes[a]);
							else
								actual[a] = args[a];
						else
							actual[a] = CompileExpression(frame, candidateTypes[a].BuildDefault());
					}

					return potential[f];
				}

				throw new CompilerException(callExpression, CompilerException.Codes.SignatureMismatch, callExpression.Name);
			}
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
								if (source.Type != target.Type)
									Convert(source, target.Type).EmitGet(m);
								else
									source.EmitGet(method);
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
			if (type != initializer.Type)
				Convert(initializer, type).EmitGet(method);
			else
				initializer.EmitGet(method);

			EmitArgsArgument(method);
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

			EmitFactoryArgument(method);	// factory
			
			method.IL.EmitCall(OpCodes.Call, typeof(Runtime.Runtime).GetMethod("DeclareModule"), null);
		}

		private System.Type TypeFromModule(Frame frame, Parse.ModuleDeclaration moduleDeclaration)
		{
			var local = AddFrame(frame, moduleDeclaration);
			var module = _emitter.BeginModule(moduleDeclaration.Name.ToString());

			// HACK: Pre-discover sets of tuples (tables) because these may be needed by tuple references.  Would be better to separate symbol discovery from compilation for types.
			foreach (var member in moduleDeclaration.Members)
			{
				Parse.TypeDeclaration varType;
				if
				(
					member is Parse.VarMember
					&& (varType = ((Parse.VarMember)member).Type) is Parse.SetType
					&& ((Parse.SetType)varType).Type is Parse.TupleType
				)
					EnsureTupleTypeSymbols(frame, (Parse.TupleType)((Parse.SetType)varType).Type);
			}

			// Gather the module's symbols
			foreach (var member in moduleDeclaration.Members)
			{
				local.Add(member.Name, member);

				// Populate qualified enumeration members
				var memberName = Name.FromID(member.Name);

				switch (member.GetType().Name)
				{
					case "VarMember":
						CompileVarMember(local, module, member);
						break;

					case "TypeMember":
						CompileTypeMember(local, module, member);
						break;

					case "EnumMember":
						CompileEnumMember(local, module, (Parse.EnumMember)member, memberName);
						break;

					case "ConstMember":
						CompileConstMember(local, module, member);
						break;

					case "FunctionMember":
						CompileFunctionMember(local, module, (Parse.FunctionMember)member);
						break;
					
					default: throw new Exception("Internal Error: Unknown member type " + member.GetType().Name);
				}
			}

			// Compile in no particular order until all members are resolved
			while (_uncompiledMembers.Count > 0)
				_uncompiledMembers.First().Value();

			return _emitter.EndModule(module);
		}

		private void CompileConstMember(Frame local, TypeBuilder module, Parse.ModuleMember member)
		{
			_uncompiledMembers.Add
			(
				member,
				() =>
				{
					_uncompiledMembers.Remove(member);
					var constExpression = (Parse.ConstMember)member;
					var expression = CompileExpression(local, constExpression.Expression);
					var native = expression.ActualNative(_emitter);
					if (native is TypeBuilder)
					{
						// TODO: handle not yet built enumerations, need to get the value directly rather than emit the constant
					}
					else
					{
						var expressionResult = CompileTimeEvaluate(constExpression.Expression, expression, native);
						var field = DeclareConst(module, member.Name.ToString(), expressionResult, native);
						_contextsBySymbol.Add
						(
							member,
							new ExpressionContext
							(
								new Parse.IdentifierExpression { Target = member.Name },
								expression.Type,
								Characteristic.Constant,
								m =>
								{
									m.IL.Emit(OpCodes.Ldarg_0);	// this
									m.IL.Emit(OpCodes.Ldfld, field);
								}
							)
						);
					}
				}
			);
		}

		private void CompileEnumMember(Frame local, TypeBuilder module, Parse.EnumMember enumMember, Name memberName)
		{
			// Push symbols for each member before compilation
			foreach (var value in enumMember.Values)
			{
				var valueName = memberName + Name.FromID(value);
				local.Add(value, valueName, value);
				_enumMembers.Add(value, enumMember);
			}

			_uncompiledMembers.Add
			(
				enumMember,
				() =>
				{
					_uncompiledMembers.Remove(enumMember);
					var enumBuilder = DeclareEnum(module, memberName.ToString());
					var i = 0;
					var type = new EnumType(memberName);
					foreach (var value in enumMember.Values)
					{
						FieldBuilder field = enumBuilder.DefineField(Name.FromID(value).ToString(), enumBuilder, FieldAttributes.Public | FieldAttributes.Literal | FieldAttributes.Static);
						field.SetConstant(i);
						_contextsBySymbol.Add
						(
							value,
							new ExpressionContext
							(
								null,
								type,
								Characteristic.Constant,
								m => { m.IL.Emit(OpCodes.Ldc_I4, i); }
							)
						);
						++i;
					}
					type.Native = enumBuilder;
					enumBuilder.CreateType();

					// Advertise the enumeration itself as a type symbol
					_contextsBySymbol.Add
					(
						enumMember,
						new ExpressionContext
						(
							null,
							type,
							Characteristic.Constant,
							null
						)
					);
				}
			);
		}

		private void CompileTypeMember(Frame local, TypeBuilder module, Parse.ModuleMember member)
		{
			_uncompiledMembers.Add
			(
				member,
				() =>
				{
					_uncompiledMembers.Remove(member);
					var typeMember = (Parse.TypeMember)member;
					var compiledType = CompileTypeDeclaration(local, typeMember.Type);
					var result = DeclareTypeDef(module, member.Name.ToString(), compiledType.GetNative(_emitter));
					_contextsBySymbol.Add
					(
						member,
						new ExpressionContext
						(
							new Parse.IdentifierExpression { Target = member.Name },
							compiledType,
							Characteristic.Default,
							null
						)
					);
				}
			);
		}

		private void CompileVarMember(Frame local, TypeBuilder module, Parse.ModuleMember member)
		{
			_uncompiledMembers.Add
			(
				member,
				() =>
				{
					_uncompiledMembers.Remove(member);
					var varMember = (Parse.VarMember)member;
					var compiledType = CompileTypeDeclaration(local, varMember.Type);
					var native = compiledType.GetNative(_emitter);
					var field = module.DefineField(member.Name.ToString(), typeof(Storage.IRepository<>).MakeGenericType(native), FieldAttributes.Public);
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
				}
			);
		}

		public void CompileFunctionMember(Frame frame, TypeBuilder module, Parse.FunctionMember functionMember)
		{
			var local = AddFrame(frame, functionMember);

			// Compile each parameter and define the parameter symbols
			var index = 1;	// Skip the "this" param
			var parameterTypes = new List<BaseType>();
			foreach (var p in functionMember.Parameters)
			{
				local.Add(p.Name, p);
				var type = CompileTypeDeclaration(frame, p.Type);
				parameterTypes.Add(type);
				var i = index;	// prevent closure capture of index
				_contextsBySymbol.Add
				(
					p,
					new ExpressionContext
					(
						null,
						type,
						Characteristic.Default,
						m => { m.IL.Emit(OpCodes.Ldarg, i); }
					)
				);
				++index;
			}

			// If a return type is specified, define self
			BaseType returnType = null;
			if (functionMember.ReturnType != null)
			{
				returnType = CompileTypeDeclaration(frame, functionMember.ReturnType);
				var selfSymbol = new Object();
				local.Add(functionMember.ReturnType, Name.FromID(functionMember.Name), selfSymbol);
				_contextsBySymbol.Add
				(
					selfSymbol,
					new ExpressionContext
					(
						null,
						returnType,
						Characteristic.Default,
						m => { m.IL.Emit(OpCodes.Ldarg_0); }	// this instance 
					)
				);
			}

			// Compile the body
			var expression = CompileExpression(local, functionMember.Expression, returnType);

			if (returnType != null)
			{
				// Convert the result if necessary
				if (returnType != expression.Type)
					expression = Convert(expression, returnType);
			}
			else
			{
				// Infer the type
				returnType = expression.Type;
			}

			var nativeParamTypes = functionMember.Parameters.Select((p, i) => parameterTypes[i].GetNative(_emitter)).ToArray();
			var nativeReturnType = expression.ActualNative(_emitter);

			// Create a public method for the function function
			var method = new MethodContext
			(
				module.DefineMethod
				(
					Name.FromID(functionMember.Name).ToString(),
					MethodAttributes.Public,
					nativeReturnType,						// Return type 
					new System.Type[] { module }			// "this" parameter
						.Union(nativeParamTypes).ToArray()	// Remaining parameters
				)
			);
			method.Builder.DefineParameter(1, ParameterAttributes.None, "this");
			var num = 2;
			foreach (var p in functionMember.Parameters)
				method.Builder.DefineParameter(num++, ParameterAttributes.None, Name.FromID(p.Name).ToString());

			expression.EmitGet(method);
			method.IL.Emit(OpCodes.Ret);

			local.AddFunction(functionMember, Name.FromID(functionMember.Name), functionMember);
			_contextsBySymbol.Add
			(
				functionMember,
				new ExpressionContext(null, returnType, Characteristic.Constant, null)
				{
					Member = method.Builder
				}
			);
		}

		public FieldBuilder DeclareTypeDef(TypeBuilder module, string name, System.Type type)
		{
			return module.DefineField(name, type, FieldAttributes.Public | FieldAttributes.Static);
		}

		public TypeBuilder DeclareEnum(TypeBuilder module, string name)
		{
			var enumType = module.DefineNestedType(name, TypeAttributes.NestedPublic | TypeAttributes.Sealed, typeof(Enum));
			enumType.DefineField("value__", typeof(int), FieldAttributes.Private | FieldAttributes.SpecialName);
			return enumType;
		}

		public FieldBuilder DeclareConst(TypeBuilder module, string name, object value, System.Type type)
		{
			if ((type.IsValueType || typeof(String).IsAssignableFrom(type)) && IsConstable(type))
			{
				var constField = module.DefineField(name, type, FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal);
				constField.SetConstant(value);
				return constField;
			}
			else
			{
				var readOnlyField = module.DefineField(name, type, FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly);
				// construct types through constructor

				// TODO: finish - need callback to emit get logic
				var staticConstructor = module.DefineConstructor(MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, new System.Type[] { });
				//staticConstructor
				throw new NotImplementedException();
			}
		}

		private bool IsConstable(System.Type type)
		{
			switch (type.ToString())
			{
				case "System.Boolean":
				case "System.Char":
				case "System.Byte":
				case "System.SByte":
				case "System.Int16":
				case "System.UInt16":
				case "System.Int32":
				case "System.UInt32":
				case "System.Int64":
				case "System.UInt64":
				case "System.Single":
				case "System.Double":
				case "System.DateTime":
				case "System.String":
					return true;
				default:
					return false;
			}
		}

		private static object CompileTimeEvaluate(Parse.Statement statement, ExpressionContext expression, System.Type native)
		{
			if (expression.Characteristics != Characteristic.Constant)
				throw new CompilerException(statement, CompilerException.Codes.ConstantExpressionExpected);
			var dynamicMethod = new DynamicMethod("", typeof(object), null);
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
			foreach 
			(
				var methodInfos in 
					module.Class.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
						.GroupBy(m => m.Name)
			)
			{
                MethodInfo[] methodGroup = methodInfos.ToArray();

                // Import type from methods and arguments.
                foreach (var methodInfo in methodGroup)
                {
                    _emitter.ImportType(methodInfo.ReturnType);
                    foreach (var parameter in methodInfo.GetParameters())
                        _emitter.ImportType(parameter.ParameterType);
                }

				frame.Add(use, moduleName + Name.FromNative(methodGroup[0].Name), methodGroup);
			
				_contextsBySymbol.Add
				(
					methodGroup,
					new ExpressionContext
					(
						null,
						SystemTypes.Void,
						Characteristic.Default,
						m => { m.IL.Emit(OpCodes.Ldloc, moduleVar); }
					)
					{
						Member = methodGroup
					}
				);
			}

			// Discover enums
			foreach (var type in module.Class.GetNestedTypes(BindingFlags.Public))
			{
				var enumName = moduleName + Name.FromNative(type.Name);
				var enumType = new EnumType(enumName) { Native = type };

				// Push the enum symbol as a type
				frame.Add(use, enumName, type);
				_contextsBySymbol.Add
				(
					type,
					new ExpressionContext
					(
						new Parse.IdentifierExpression { Target = enumName.ToID() },
						enumType,
						Characteristic.Constant,
						null
					)
				); 
				
				var i = 0;
				foreach (var enumItem in type.GetFields(BindingFlags.Public | BindingFlags.Static))
				{
					var itemName = enumName + Name.FromNative(enumItem.Name);
					frame.Add(use, itemName, enumItem);
					var num = i;	// Capture within loop
					_contextsBySymbol.Add
					(
						enumItem,
						new ExpressionContext
						(
							new Parse.IdentifierExpression { Target = itemName.ToID() },
							enumType,
							Characteristic.Constant,
							m => { m.IL.Emit(OpCodes.Ldc_I4, num); }
						)
					);
					++i;
				}
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
				_contextsBySymbol.Add
				(
					field,
					new ExpressionContext
					(
						null,
						type,
						Characteristic.Constant,
						null
					)
				);
			}

			// Build code to construct instance and assign to variable
			method.IL.Emit(OpCodes.Newobj, moduleType.GetConstructor(new System.Type[] { }));
			// Initialize each variable bound to a repository
			foreach (var field in moduleType.GetFields(BindingFlags.Public | BindingFlags.Instance))
			{
				// <instance>.<field> = factory.GetRepository(moduleType, Name.FromNative(new string[] { field.Name }))
				method.IL.Emit(OpCodes.Dup);
				EmitFactoryArgument(method);
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

							m.IL.Emit(OpCodes.Ldloc, resultVariable);
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
				var local = AddFrame(frame, forClause);
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
					nested.Type is NaryType && index < expression.ForClauses.Count - 1
						? // force to list if not iterating a set
						(
							!(forExpression.Type is SetType) 
								? (BaseType)new ListType(((NaryType)nested.Type).Of) 
								: nested.Type
						)
						: 
						(
							!(forExpression.Type is SetType) 
								? (BaseType)new ListType(nested.Type) 
								: new SetType(nested.Type)
						);
				
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

							nested.EmitGet(m);

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
			var local = frame;
			var letContexts = new Dictionary<Parse.LetClause, ExpressionContext>();
			var letVars = new Dictionary<Parse.LetClause, LocalBuilder>();

			// Compile and define a symbol for each let
			foreach (var let in expression.LetClauses)
			{
				local = AddFrame(local, let);
				var letResult = CompileExpression(local, let.Expression);
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
				local.Add(let.Name, let);
			}

			// Compile main expression
			var main = CompileExpression(local, expression.Expression, typeHint);

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
                case "CallExpression": return CompileCallExpression(frame, (Parse.CallExpression)expression, typeHint);
                case "IfExpression": return CompileIfExpression(frame, (Parse.IfExpression)expression, typeHint);
                case "CaseExpression": return CompileCaseExpression(frame, (Parse.CaseExpression)expression, typeHint);
				case "ExtractExpression": return CompileExtractExpression(frame, (Parse.ExtractExpression)expression, typeHint);
				default : throw new NotSupportedException(String.Format("Expression type {0} is not supported", expression.GetType().Name));
			}
		}

		private ExpressionContext CompileExtractExpression(Frame frame, Parse.ExtractExpression expression, BaseType typeHint)
		{
			var left = CompileExpression(frame, expression.Expression);
			return left.Type.CompileExtractExpression(this, frame, left, expression, typeHint);
		}

        public ExpressionContext CompileCaseExpression(Frame frame, Parse.CaseExpression expression, BaseType typeHint)
        {
            //TODO: Strict case expressions
            if (expression.IsStrict)
                throw new CompilerException(expression, CompilerException.Codes.InvalidCase, "Strict case expressions not yet supported");

            //TODO: Some of this should be enforced in the parser.
            if (expression.IsStrict && expression.TestExpression == null)
                throw new CompilerException(expression, CompilerException.Codes.InvalidCase, "Strict case requires a test expression");
            if (expression.IsStrict && expression.ElseExpression != null)
                throw new CompilerException(expression, CompilerException.Codes.InvalidCase, "Strict case can not have a default value");
            if (!expression.IsStrict && expression.ElseExpression == null)
                throw new CompilerException(expression, CompilerException.Codes.InvalidCase, "Default expression required in non-strict cases");
            if (expression.Items.Count == 0 && expression.ElseExpression == null)
                throw new CompilerException(expression, CompilerException.Codes.InvalidCase, "Case must have at least one when expression or an else expression");

            //selective case (switch)
            //case X, when Y then, when Z then  
            if (expression.TestExpression != null)
            {
                //Prep to store the result of the test expression as a local variable.
                var local = AddFrame(frame, expression);
                LocalBuilder testLocal = null;

                var test = CompileExpression(frame, expression.TestExpression, typeHint);
                Characteristic characteristic = test.Characteristics;

                local.Add(expression.TestExpression, Name.FromComponents("@test"), expression.TestExpression);
                _contextsBySymbol.Add
                (
                    expression.TestExpression,
                    new ExpressionContext
                    (
                        null,
                        test.Type,
                        test.Characteristics,
                        m => { m.IL.Emit(OpCodes.Ldloc, testLocal); }
                    )
                );

                ExpressionContext def = null;
                BaseType returnType = null;
                if (expression.ElseExpression != null)
                {                    
                    def = CompileExpression(frame, expression.ElseExpression, null);
                    returnType = def.Type;
                    characteristic = Compiler.MergeCharacteristics(characteristic, def.Characteristics);
                }

                List<Tuple<ExpressionContext, ExpressionContext>> caseItemExpressions = new List<Tuple<ExpressionContext, ExpressionContext>>();

                foreach (var caseItem in expression.Items)
                {
                    //Pull the result of the test expression and compare it with the current expression.
                    var whenEx = CompileBinaryExpression(local, new Parse.BinaryExpression() { Left = new Parse.IdentifierExpression() { Target = Parse.ID.FromComponents("@test") }, Right = caseItem.WhenExpression, Operator = Parse.Operator.Equal }, null);

                    var thenEx = CompileExpression(frame, caseItem.ThenExpression, returnType);

                    //In case it hasn't been set by the else (default) case, will be set by the first item in the list.
                    returnType = returnType ?? thenEx.Type;

                    if (thenEx.Type != returnType)
                        thenEx = Convert(thenEx, returnType);

                    characteristic = Compiler.MergeCharacteristics(characteristic, whenEx.Characteristics);
                    characteristic = Compiler.MergeCharacteristics(characteristic, thenEx.Characteristics);

                    caseItemExpressions.Add(new Tuple<ExpressionContext, ExpressionContext>(whenEx, thenEx));
                }

                return
                    new ExpressionContext
                    (
                        expression,
                        returnType,
                        characteristic,
                        m =>
                        {
                            var lblEnd = m.IL.DefineLabel();
                            testLocal = m.DeclareLocal(test.Expression, test.ActualNative(_emitter), "@test");
                            test.EmitGet(m);
                            m.IL.Emit(OpCodes.Stloc, testLocal);
                            foreach (var caseItem in caseItemExpressions)
                            {
                                var lblNext = m.IL.DefineLabel();
                                caseItem.Item1.EmitGet(m);
                                m.IL.Emit(OpCodes.Brfalse, lblNext);
                                caseItem.Item2.EmitGet(m);
                                m.IL.Emit(OpCodes.Br, lblEnd);
                                m.IL.MarkLabel(lblNext);
                            }

                            if (def != null)
                            {
                                def.EmitGet(m);
                            }
                            else
                            {
                                m.IL.Emit(OpCodes.Ldstr, "Internal Error: Unhandled case condition");
                                m.IL.Emit(OpCodes.Newobj, typeof(Exception).GetConstructor(new[] { typeof(string) }));
                                m.IL.Emit(OpCodes.Throw);
                            }

                            m.IL.MarkLabel(lblEnd);
                        }
                    );
            }

            //conditional case (stacked if expression)
            //case when X = Y then, when Y = Z then, else
            else
            {
                ExpressionContext def = CompileExpression(frame, expression.ElseExpression, typeHint);
                BaseType returnType = def.Type;
                Characteristic characteristic = def.Characteristics;

                List<Tuple<ExpressionContext, ExpressionContext>> caseItemExpressions = new List<Tuple<ExpressionContext, ExpressionContext>>();

                foreach (var caseItem in expression.Items)
                {
                    var whenEx = CompileExpression(frame, caseItem.WhenExpression, SystemTypes.Boolean);
                    if (whenEx.Type != SystemTypes.Boolean)
                        throw new CompilerException(expression, CompilerException.Codes.InvalidCase, "When statements in the conditional case must evaluate to a Boolean");

                    //Convert then expressions to either the default type, or if no default is present, the type of the first item in the case items.
                    var thenEx = CompileExpression(frame, caseItem.ThenExpression, returnType);
                    if (thenEx.Type != returnType)
                        thenEx = Convert(thenEx, returnType);

                    characteristic = Compiler.MergeCharacteristics(characteristic, whenEx.Characteristics);
                    characteristic = Compiler.MergeCharacteristics(characteristic, thenEx.Characteristics);

                    caseItemExpressions.Add(new Tuple<ExpressionContext, ExpressionContext>(whenEx, thenEx));
                }

                return
                    new ExpressionContext
                    (
                        expression,
                        returnType,
                        characteristic,
                        m =>
                        {
                            var lblEnd = m.IL.DefineLabel();
                            foreach (var caseItem in caseItemExpressions)
                            {
                                var lblNext = m.IL.DefineLabel();
                                caseItem.Item1.EmitGet(m);
                                m.IL.Emit(OpCodes.Brfalse, lblNext);
                                caseItem.Item2.EmitGet(m);
                                m.IL.Emit(OpCodes.Br, lblEnd);
                                m.IL.MarkLabel(lblNext);
                            }

                            def.EmitGet(m);
                            m.IL.MarkLabel(lblEnd);
                        }
                    );
            }
        }

        public ExpressionContext CompileIfExpression(Frame frame, Parse.IfExpression expression, BaseType typeHint)
        {
            var testExpression = CompileExpression(frame, expression.TestExpression, SystemTypes.Boolean);
            if (!(testExpression.Type is BooleanType))
				throw new CompilerException(expression.TestExpression, CompilerException.Codes.IncorrectType, testExpression.Type, "Boolean");

            var thenExpression = CompileExpression(frame, expression.ThenExpression, typeHint);
            typeHint = typeHint ?? thenExpression.Type;

            var elseExpression = CompileExpression(frame, expression.ElseExpression, typeHint);

            if (thenExpression.Type != elseExpression.Type)
            {
                if (thenExpression.Type == SystemTypes.Void)
                    elseExpression = Convert(elseExpression, thenExpression.Type);
                else
                    thenExpression = Convert(thenExpression, elseExpression.Type);
            }

            Characteristic expressionCharacteristic = MergeCharacteristics(testExpression.Characteristics, thenExpression.Characteristics);
            expressionCharacteristic = MergeCharacteristics(expressionCharacteristic, elseExpression.Characteristics);
            
            return
                new ExpressionContext
                (
                    expression,
                    thenExpression.Type,
                    expressionCharacteristic,
                    m =>
                    {
                        var endLabel = m.IL.DefineLabel();
                        var elseLabel = m.IL.DefineLabel();

                        testExpression.EmitGet(m);
                        m.IL.Emit(OpCodes.Brfalse, elseLabel);
                        thenExpression.EmitGet(m);
                        m.IL.Emit(OpCodes.Br, endLabel);
                        m.IL.MarkLabel(elseLabel);
                        elseExpression.EmitGet(m);
                        m.IL.MarkLabel(endLabel);                     
                    }
                );
        }

		private ExpressionContext CompileCallExpression(Frame frame, Parse.CallExpression callExpression, BaseType typeHint)
		{
			// Find the possible functions
			var symbols = frame.ResolveFunction(callExpression.Name, Name.FromID(callExpression.Name));

			// Compile the arguments
			var args = new ExpressionContext[callExpression.Arguments.Count];
			for (var i = 0; i < callExpression.Arguments.Count; i++)
				args[i] = CompileExpression(frame, callExpression.Arguments[i]);

			// Choose the right function based on signature
			var symbol = ResolveFunction(frame, callExpression, args, symbols.Cast<Parse.FunctionMember>());

			// TODO: module forward referencing and function signature resolution

			// Find the expression context for the function (ensure compiled)
			var context = CompileReference(callExpression.Name, symbol);
			var methodType = (MethodInfo)context.Member;
            
            return
                new ExpressionContext
                (
                    callExpression,
                    context.Type,
                    context.Characteristics,
                    m =>
                    {
                        if (context.EmitGet != null)
                            context.EmitGet(m);	// Instance
                        foreach (var arg in args)
                            arg.EmitGet(m);
                        m.IL.EmitCall(OpCodes.Call, methodType, null);
                    }
                );
        }

		public void DetermineTypeParameters(Parse.Statement statement, System.Type[] resolved, System.Type parameterType, System.Type argumentType)
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

		public BaseType CompileTypeDeclaration(Frame frame, Parse.TypeDeclaration typeDeclaration)
		{
			switch (typeDeclaration.GetType().Name)
			{
				case "OptionalType": return new OptionalType(CompileTypeDeclaration(frame, ((Parse.OptionalType)typeDeclaration).Type));
				case "ListType": return new ListType(CompileTypeDeclaration(frame, ((Parse.ListType)typeDeclaration).Type));
				case "SetType": return new SetType(CompileTypeDeclaration(frame, ((Parse.SetType)typeDeclaration).Type));
				case "TupleType": return CompileTupleType(frame, (Parse.TupleType)typeDeclaration);
				case "NamedType": return CompileNamedType(frame, (Parse.NamedType)typeDeclaration);
				default: throw new Exception("Internal Error: Unknown type declaration " + typeDeclaration.GetType().Name); 
			}
		}

		private BaseType CompileNamedType(Frame frame, Parse.NamedType namedType)
		{
			var context = CompileReference(frame, namedType.Target);
			if (context.EmitGet != null)
				throw new CompilerException(namedType, CompilerException.Codes.IncorrectTypeReferenced, "typedef", context.Type);
			return context.Type;
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
			EnsureTupleTypeSymbols(frame, tupleType);

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
								// Convert the element if needed
								if (item.Type != elementType)
									Convert(item, elementType).EmitGet(m);
								else
									item.EmitGet(m);
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

		private void EnsureTupleTypeSymbols(Frame frame, Parse.TupleType tupleType)
		{
			if (!_frames.ContainsKey(tupleType))
			{
				var local = AddFrame(frame, tupleType);

				foreach (var a in tupleType.Attributes)
					local.Add(a.Name, a);

				foreach (var k in tupleType.Keys)
					ResolveListReferences(local, k.AttributeNames);

				foreach (var r in tupleType.References)
					ResolveListReferences(local, r.SourceAttributeNames);
			}
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
			return CompileReference(frame, identifierExpression.Target);
		}

		private ExpressionContext CompileReference(Frame frame, Parse.ID target)
		{
			var symbol = ResolveReference<object>(frame, target);
			return CompileReference(target, symbol);
		}

		private ExpressionContext CompileReference(Parse.ID target, object symbol)
		{
			// If this is an enum member, ensure that the enclosing enum is compiled
			Parse.EnumMember enumMember;
			if (symbol is Parse.ID && _enumMembers.TryGetValue((Parse.ID)symbol, out enumMember))
				LazyCompileModuleMember(enumMember);

			ExpressionContext context;
			if (_contextsBySymbol.TryGetValue(symbol, out context))
				return context;

			// Lazy-compile module member if needed
			if (symbol is Parse.ModuleMember)
			{
				var member = (Parse.ModuleMember)symbol;
				return ResolveMember(target, member);
			}

			throw new CompilerException(target, CompilerException.Codes.IdentifierNotFound, target);
		}

		private ExpressionContext ResolveMember(Parse.Statement statement, Parse.ModuleMember member)
		{
			LazyCompileModuleMember(member);

			ExpressionContext context;
			if (_contextsBySymbol.TryGetValue(member, out context))
				return context;

			throw new CompilerException(statement, CompilerException.Codes.RecursiveDeclaration);
		}

		private void LazyCompileModuleMember(Parse.ModuleMember member)
		{
			Action compilation;
			if (_uncompiledMembers.TryGetValue(member, out compilation))
				compilation();
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

