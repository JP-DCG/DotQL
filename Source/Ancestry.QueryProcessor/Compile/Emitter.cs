﻿using System;
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
		private AssemblyName _assemblyName;
		private AssemblyBuilder _assembly;
		private ModuleBuilder _module;
		private ISymbolDocumentWriter _symbolWriter;

		private EmitterOptions _options;

		private Dictionary<Type.TupleType, System.Type> _tupleToNative;

		public Emitter(EmitterOptions options)
		{
			_options = options;
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
			_tupleToNative = new Dictionary<Type.TupleType, System.Type>();
		}

		public void SaveAssembly()
		{
			_module.CreateGlobalFunctions();
			_assembly.Save(_assemblyName + ".dll");

			//var pdbGenerator = _debugOn ? System.Runtime.CompilerServices.DebugInfoGenerator.CreatePdbGenerator() : null;
		}

		public TypeBuilder BeginModule(string name)
		{
			return _module.DefineType(name, TypeAttributes.Class | TypeAttributes.Public);
		}

		public FieldBuilder DeclareVariable(TypeBuilder module, string name, System.Type type)
		{
			return module.DefineField(name, typeof(Storage.IRepository<>).MakeGenericType(type), FieldAttributes.Public);
		}

		public FieldBuilder DeclareTypeDef(TypeBuilder module, string name, System.Type type)
		{
			return module.DefineField(name, type, FieldAttributes.Public | FieldAttributes.Static);
		}

		public System.Type DeclareEnum(TypeBuilder module, string name, IEnumerable<string> values)
		{
			var enumType = module.DefineNestedType(name, TypeAttributes.NestedPublic | TypeAttributes.Sealed, typeof(Enum));
			enumType.DefineField("value__", typeof(int), FieldAttributes.Private | FieldAttributes.SpecialName);
			var i = 0;
			foreach (var value in values)
			{
				FieldBuilder field = enumType.DefineField(value.ToString(), enumType, FieldAttributes.Public | FieldAttributes.Literal | FieldAttributes.Static);
				field.SetConstant(i++);
			}
			return enumType.CreateType();
		}

		public FieldBuilder DeclareConst(TypeBuilder module, string name, object value, System.Type type)
		{
			if (type.IsValueType || typeof(String).IsAssignableFrom(type))
			{
				var constField = module.DefineField(name, type, FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal);
				constField.SetConstant(value);
				return constField;
			}
			else
			{
				//var readOnlyField = module.DefineField(name, type, FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly);
				// construct value types through constructor
				throw new NotImplementedException();
			}
		}

		public MethodBuilder DeclareMethod(TypeBuilder module, string name, LambdaExpression expression)
		{
			var methodBuilder = module.DefineMethod(name, MethodAttributes.Static | MethodAttributes.Public);
			expression.CompileToMethod(methodBuilder);
			return methodBuilder;
		}

		public System.Type EndModule(TypeBuilder module)
		{
			var result = module.CreateType();
			_assembly.SetCustomAttribute
				(
					new CustomAttributeBuilder
					(
						typeof(Type.ModuleAttribute).GetConstructor
						(
							new System.Type[] { typeof(System.Type), typeof(string[]) }
						),
						new object[] { result, Name.FromNative(result.Name).Components }
					)
				); 
			return result;
		}

		public System.Type FindOrCreateNativeFromTupleType(Type.TupleType tupleType)
		{
			System.Type nativeType;
			if (!_tupleToNative.TryGetValue(tupleType, out nativeType))
			{
				nativeType = NativeFromTupleType(tupleType);
				_tupleToNative.Add(tupleType, nativeType);
			}
			return nativeType;
		}

		public System.Type NativeFromTupleType(Type.TupleType tupleType)
		{
			var typeBuilder = _module.DefineType("Tuple" + tupleType.GetHashCode(), TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.Serializable, typeof(ValueType));
			var fieldsByID = new Dictionary<Name, FieldInfo>();

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
						typeof(Type.TupleReferenceAttribute).GetConstructor(new System.Type[] { typeof(string), typeof(string[]), typeof(string), typeof(string[]) }),
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
						typeof(Type.TupleKeyAttribute).GetConstructor(new System.Type[] { typeof(string[]) }),
						new object[] { (from an in key.AttributeNames select an.ToString()).ToArray() }
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

		private static MethodBuilder EmitTupleEquality(Type.TupleType tupleType, TypeBuilder typeBuilder, Dictionary<Name, FieldInfo> fieldsByID)
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

		private static MethodBuilder EmitTupleGetHashCode(Type.TupleType tupleType, TypeBuilder typeBuilder, Dictionary<Name, FieldInfo> fieldsByID)
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

		private Type.TupleType TupleTypeFromNative(System.Type type)
		{
			var tupleType = new Type.TupleType();

			// Get attributes
			foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
				tupleType.Attributes.Add(Name.FromNative(field.Name), field.FieldType);

			// Get references
			foreach (Type.TupleReferenceAttribute r in type.GetCustomAttributes(typeof(Type.TupleReferenceAttribute)))
				tupleType.References.Add
				(
					Name.FromNative(r.Name), 
					new Type.TupleReference 
					{ 
						SourceAttributeNames = (from san in r.SourceAttributeNames select Name.FromNative(san)).ToArray(),
						Target = Name.FromNative(r.Target),
						TargetAttributeNames = (from tan in r.TargetAttributeNames select Name.FromNative(tan)).ToArray()
					}
				);

			// Get keys
			foreach (Type.TupleKeyAttribute k in type.GetCustomAttributes(typeof(Type.TupleKeyAttribute)))
				tupleType.Keys.Add
				(
					new Type.TupleKey { AttributeNames = (from n in k.AttributeNames select Name.FromNative(n)).ToArray() }
				);

			return tupleType;
		}

		public Runtime.ExecuteHandler DeclareProgram(Expression<Runtime.ExecuteHandler> lambda)
		{
			var typeBuilder = _module.DefineType("Program", TypeAttributes.Class | TypeAttributes.Public);
			var methodBuilder = 
				typeBuilder.DefineMethod
				(
					"Main", 
					MethodAttributes.Public | MethodAttributes.Static, 
					typeof(void), 
					new[] { typeof(IDictionary<string, object>), typeof(Storage.IRepositoryFactory), typeof(CancellationToken) }
				);
			lambda.CompileToMethod(methodBuilder);
			var type = typeBuilder.CreateType();
			return (Runtime.ExecuteHandler)type.GetMethod("Main").CreateDelegate(typeof(Runtime.ExecuteHandler));
		}

	}
}
