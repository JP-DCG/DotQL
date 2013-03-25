using Ancestry.QueryProcessor.Type;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Compile
{
	public class Emitter
	{
		private static readonly System.Type[] _DelegateConstructorSignature = new System.Type[] { typeof(object), typeof(IntPtr) };

		private AssemblyName _assemblyName;
		private AssemblyBuilder _assembly;
		private ModuleBuilder _module;
		private ISymbolDocumentWriter _symbolWriter;

		private EmitterOptions _options;

		private Dictionary<TupleType, System.Type> _tupleToNative;

		public Emitter(EmitterOptions options)
		{
			_options = options;
			//// TODO: setup separate app domain with appropriate cache path, shadow copying etc.
			//var domainName = "plan" + DateTime.Now.Ticks.ToString();
			//var domain = AppDomain.CreateDomain(domainName);
			_assemblyName = new AssemblyName(_options.AssemblyName);
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
			_module = _assembly.DefineDynamicModule(_assemblyName.Name, _assemblyName.Name + ".dll", _options.DebugOn);
			if (_options.DebugOn)
				_symbolWriter = _module.DefineDocument(_options.SourceFileName, Guid.Empty, Guid.Empty, Guid.Empty);
			_tupleToNative = new Dictionary<TupleType, System.Type>();
		}

		public void SaveAssembly()
		{
			try
			{
				_module.CreateGlobalFunctions();
				if (Debugger.IsAttached)
					_assembly.Save(_assemblyName + ".dll");

				//var pdbGenerator = _debugOn ? System.Runtime.CompilerServices.DebugInfoGenerator.CreatePdbGenerator() : null;
			}
			catch (Exception e)
			{
				System.Diagnostics.Debug.WriteLine(e.ToString());
				// Don't rethrow.  Non-critical.
			}
		}

		public TypeBuilder BeginModule(string name)
		{
			return _module.DefineType(name, TypeAttributes.Class | TypeAttributes.Public);
		}

		public System.Type EndModule(TypeBuilder module)
		{
			var result = module.CreateType();
			_assembly.SetCustomAttribute
				(
					new CustomAttributeBuilder
					(
						typeof(ModuleAttribute).GetConstructor
						(
							new System.Type[] { typeof(System.Type), typeof(string[]) }
						),
						new object[] { result, Name.FromNative(result.Name).Components }
					)
				); 
			return result;
		}

		public System.Type FindOrCreateNativeFromFunctionType(FunctionType functionType)
		{
			/* Custom delegates are needed for DotQL because the parameter names are part of the unique 
			 * function type.  Expression.GetDelegateType, for instance, will give delegates that match 
			 * the type signature, but not the parameter names. */

			// TODO: Need accurate cache key - hash isn't perfect
			var name = "Delegate" + functionType.GetHashCode().ToString();

			var result = _module.GetType(name);
			if (result != null)
				return result;

			var returnType = functionType.Type.GetNative(this);
			var parameterTypes = functionType.Parameters.Select(p => p.Type.GetNative(this)).ToArray();
			TypeBuilder builder = _module.DefineType(name, TypeAttributes.AutoClass | TypeAttributes.Sealed | TypeAttributes.Public, typeof(MulticastDelegate));
			builder.DefineConstructor(MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public, CallingConventions.Standard, _DelegateConstructorSignature).SetImplementationFlags(MethodImplAttributes.CodeTypeMask);
			var invoke = builder.DefineMethod("Invoke", MethodAttributes.NewSlot | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Public, returnType, parameterTypes);
			invoke.SetImplementationFlags(MethodImplAttributes.CodeTypeMask);
			var num = 1;
			foreach (var p in functionType.Parameters)
				invoke.DefineParameter(num++, ParameterAttributes.None, p.Name.ToString());
			return builder.CreateType();
		}

		public System.Type FindOrCreateNativeFromTupleType(TupleType tupleType)
		{
			System.Type nativeType;
			if (!_tupleToNative.TryGetValue(tupleType, out nativeType))
			{
				nativeType = NativeFromTupleType(tupleType);
				_tupleToNative.Add(tupleType, nativeType);
			}
			return nativeType;
		}

		public System.Type NativeFromTupleType(TupleType tupleType)
		{
			var typeBuilder = _module.DefineType("Tuple" + tupleType.GetHashCode(), TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.Serializable, typeof(ValueType));
			var fieldsByID = new Dictionary<Name, FieldInfo>();

			// Add attributes
			foreach (var attribute in tupleType.Attributes)
			{
				var field = typeBuilder.DefineField(attribute.Key.ToString(), attribute.Value.GetNative(this), FieldAttributes.Public);
				fieldsByID.Add(attribute.Key, field);
			}

			// Add references
			foreach (var reference in tupleType.References)
			{
				var cab =
					new CustomAttributeBuilder
					(
						typeof(TupleReferenceAttribute).GetConstructor(new System.Type[] { typeof(string), typeof(string[]), typeof(string), typeof(string[]) }),
						new object[] 
							{ 
								reference.Key.ToString(),
								(from san in reference.Value.SourceAttributeNames select san.ToString()).ToArray(),
								reference.Value.Target.ToString(),
								(from tan in reference.Value.TargetAttributeNames select tan.ToString()).ToArray(),
							}
					);
				typeBuilder.SetCustomAttribute(cab);
			}

			// Add keys
			foreach (var key in tupleType.Keys)
			{
				var cab =
					new CustomAttributeBuilder
					(
						typeof(TupleKeyAttribute).GetConstructor(new System.Type[] { typeof(string[]) }),
						new object[] { (from an in key.AttributeNames select an.ToString()).ToArray() }
					);
				typeBuilder.SetCustomAttribute(cab);
			}

			// Add tuple attribute
			var attributeBuilder =
				new CustomAttributeBuilder
				(
					typeof(TupleAttribute).GetConstructor(new System.Type[] { }),
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

		private static MethodBuilder EmitTupleEquality(TupleType tupleType, TypeBuilder typeBuilder, Dictionary<Name, FieldInfo> fieldsByID)
		{
			var equalityMethod = typeBuilder.DefineMethod("op_Equality", MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, CallingConventions.Standard, typeof(bool), new System.Type[] { typeBuilder, typeBuilder });
			var il = equalityMethod.GetILGenerator();
			var end = il.DefineLabel();
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
				il.Emit(OpCodes.Dup);
				il.Emit(OpCodes.Brfalse_S, end);
				il.Emit(OpCodes.Pop);
			}
			il.Emit(OpCodes.Ldc_I4_1);	// True
			il.MarkLabel(end);
			il.Emit(OpCodes.Ret);
			return equalityMethod;
		}

		private static MethodBuilder EmitTupleGetHashCode(TupleType tupleType, TypeBuilder typeBuilder, Dictionary<Name, FieldInfo> fieldsByID)
		{
			var getHashCodeMethod = typeBuilder.DefineMethod("GetHashCode", MethodAttributes.Virtual | MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot, CallingConventions.HasThis, typeof(Int32), new System.Type[] { });
			var il = getHashCodeMethod.GetILGenerator();
			// result = 83
			il.Emit(OpCodes.Ldc_I4, 83);
			foreach (var keyItem in tupleType.GetKeyAttributes())
			{
				var field = fieldsByID[keyItem];

				// result ^= this.<field>.GetHashCode();
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldflda, field);
				il.Emit(OpCodes.Constrained, field.FieldType);
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
			var theEnd = il.DefineLabel();
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Isinst, typeBuilder);
			il.Emit(OpCodes.Brfalse, baseLabel);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Unbox_Any, typeBuilder);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldobj, typeBuilder);
			il.EmitCall(OpCodes.Call, equalityMethod, null);
			il.Emit(OpCodes.Br_S, theEnd);
			il.MarkLabel(baseLabel);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldobj, typeBuilder);
			il.Emit(OpCodes.Box, typeBuilder);
			il.Emit(OpCodes.Ldarg_1);
			il.EmitCall(OpCodes.Call, ReflectionUtility.ObjectEquals, null);
			il.MarkLabel(theEnd);
			il.Emit(OpCodes.Ret);
			typeBuilder.DefineMethodOverride(equalsMethod, ReflectionUtility.ObjectEquals);
			return equalsMethod;
		}

		public void ImportType(System.Type type)
		{
			if (type.IsGenericType)
				ImportType(type.GenericTypeArguments[0]);
			else
				if (ReflectionUtility.IsTupleType(type))
				{
					var tupleType = TupleTypeFromNative(type);
					_tupleToNative.Add(tupleType, type);
				}
		}

		private TupleType TupleTypeFromNative(System.Type type)
		{
			var tupleType = new TupleType();

			// Get attributes
			foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
				tupleType.Attributes.Add(Name.FromNative(field.Name), TypeFromNative(field.FieldType));

			// Get references
			foreach (TupleReferenceAttribute r in type.GetCustomAttributes(typeof(TupleReferenceAttribute)))
				tupleType.References.Add
				(
					Name.FromNative(r.Name), 
					new TupleReference 
					{ 
						SourceAttributeNames = (from san in r.SourceAttributeNames select Name.FromNative(san)).ToArray(),
						Target = Name.FromNative(r.Target),
						TargetAttributeNames = (from tan in r.TargetAttributeNames select Name.FromNative(tan)).ToArray()
					}
				);

			// Get keys
			foreach (TupleKeyAttribute k in type.GetCustomAttributes(typeof(TupleKeyAttribute)))
				tupleType.Keys.Add
				(
					new TupleKey { AttributeNames = (from n in k.AttributeNames select Name.FromNative(n)).ToArray() }
				);

			return tupleType;
		}

		public BaseType TypeFromNative(System.Type native)
		{
			if (ReflectionUtility.IsTupleType(native))
				return TupleTypeFromNative(native);
			if (ReflectionUtility.IsSet(native))
				return new SetType(TypeFromNative(native.GenericTypeArguments[0]));
			if (ReflectionUtility.IsNary(native))
				return new ListType(TypeFromNative(native.GenericTypeArguments[0]));
			if (ReflectionUtility.IsFunction(native))
				return FunctionType.FromMethod(native.GetMethod("Invoke"), this);
			BaseType scalarType;
			if (_options.ScalarTypes == null || !_options.ScalarTypes.TryGetValue(native.ToString(), out scalarType))
				return new ScalarType(native);
			return scalarType;
		}

		public MethodContext DeclareMain()
		{
			var typeBuilder = _module.DefineType("Program", TypeAttributes.Public);
			var methodBuilder = 
				typeBuilder.DefineMethod
				(
					"Main", 
					MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig, 
					typeof(object), 
					new[] { typeof(IDictionary<string, object>), typeof(Storage.IRepositoryFactory), typeof(CancellationToken) }
				);
			return new MethodContext(methodBuilder);
		}

		public System.Type CompleteMain(MethodContext main)
		{
			main.IL.Emit(OpCodes.Ret);
			return ((TypeBuilder)main.Builder.DeclaringType).CreateType();
		}

		public Runtime.ExecuteHandler Complete(System.Type program)
		{
			if (_options.DebugOn)
				SaveAssembly();
			return (Runtime.ExecuteHandler)program.GetMethod("Main").CreateDelegate(typeof(Runtime.ExecuteHandler));
		}
	}
}
