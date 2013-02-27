using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Compile
{
	static class ReflectionUtility
	{
		public static readonly MethodInfo ObjectEquals = typeof(object).GetMethod("Equals", new System.Type[] { typeof(object) });
		public static readonly MethodInfo ObjectGetHashCode = typeof(object).GetMethod("GetHashCode");
		public static readonly MethodInfo IRepositoryFactoryGetRepository = typeof(Storage.IRepositoryFactory).GetMethod("GetRepository<>");
		public static readonly MethodInfo NameFromNative = typeof(Name).GetMethod("FromNative");
		public static readonly MethodInfo NameFromComponents = typeof(Name).GetMethod("FromComponents");
		public static readonly PropertyInfo ArrayLength = typeof(Array).GetProperty("Length");
		public static readonly ConstructorInfo DateTimeTicksConstructor = typeof(DateTime).GetConstructor(new[] { typeof(long) });
		public static readonly ConstructorInfo TimeSpanTicksConstructor = typeof(DateTime).GetConstructor(new[] { typeof(long) });
		public static readonly MethodInfo TypeGetTypeFromHandle = typeof(System.Type).GetMethod("GetTypeFromHandle");
		public static readonly MethodInfo IEnumerableMoveNext = typeof(IEnumerator).GetMethod("MoveNext");
		public static readonly MethodInfo StringCompare = typeof(string).GetMethod("Compare", new System.Type[] { typeof(string), typeof(string) });
		public static readonly MethodInfo StringConcat = typeof(string).GetMethod("Concat", new System.Type[] { typeof(string), typeof(string) });
		public static readonly FieldInfo NameComponents = typeof(Name).GetField("Components");
		public static readonly MethodInfo RuntimeGetInitializer = typeof(Runtime.Runtime).GetMethod("GetInitializer");	// Generic

		public static bool IsTupleType(System.Type type)
		{
			return type.GetCustomAttribute(typeof(Type.TupleAttribute), true) != null;
		}

		public static bool IsSet(System.Type type)
		{
			return type.IsGenericType && typeof(ISet<>).IsAssignableFrom(type.GetGenericTypeDefinition());
		}

		public static bool IsNary(System.Type type)
		{
			return type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type);
		}

		public static bool IsRepository(System.Type type)
		{
			return type != null && type.IsGenericType && typeof(Storage.IRepository<>) == type.GetGenericTypeDefinition();
		}

		#region GetMethodExt

		// These GetMethodExt methods are from Ken Beckett's answer on Stack Overflow:
		// http://stackoverflow.com/questions/4035719/getmethod-for-generic-method

		/// <summary>
		/// Search for a method by name and parameter types.  Unlike GetMethod(), does 'loose' matching on generic
		/// parameter types, and searches base interfaces.
		/// </summary>
		/// <exception cref="AmbiguousMatchException"/>
		public static MethodInfo GetMethodExt(this System.Type thisType, string name, params System.Type[] parameterTypes)
		{
			return GetMethodExt(thisType, name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy, parameterTypes);
		}

		/// <summary>
		/// Search for a method by name, parameter types, and binding flags.  Unlike GetMethod(), does 'loose' matching on generic
		/// parameter types, and searches base interfaces.
		/// </summary>
		/// <exception cref="AmbiguousMatchException"/>
		public static MethodInfo GetMethodExt(this System.Type thisType, string name, BindingFlags bindingFlags, params System.Type[] parameterTypes)
		{
			MethodInfo matchingMethod = null;

			// Check all methods with the specified name, including in base classes
			GetMethodExt(ref matchingMethod, thisType, name, bindingFlags, parameterTypes);

			// If we're searching an interface, we have to manually search base interfaces
			if (matchingMethod == null && thisType.IsInterface)
			{
				foreach (System.Type interfaceType in thisType.GetInterfaces())
					GetMethodExt(ref matchingMethod, interfaceType, name, bindingFlags, parameterTypes);
			}

			return matchingMethod;
		}

		private static void GetMethodExt(ref MethodInfo matchingMethod, System.Type type, string name, BindingFlags bindingFlags, params System.Type[] parameterTypes)
		{
			// Check all methods with the specified name, including in base classes
			foreach (MethodInfo methodInfo in type.GetMember(name, MemberTypes.Method, bindingFlags))
			{
				// Check that the parameter counts and types match, with 'loose' matching on generic parameters
				ParameterInfo[] parameterInfos = methodInfo.GetParameters();
				if (parameterInfos.Length == parameterTypes.Length)
				{
					int i = 0;
					for (; i < parameterInfos.Length; ++i)
					{
						if (!parameterInfos[i].ParameterType.IsSimilarType(parameterTypes[i]))
							break;
					}
					if (i == parameterInfos.Length)
					{
						if (matchingMethod == null)
							matchingMethod = methodInfo;
						else
							throw new AmbiguousMatchException("More than one matching method found!");
					}
				}
			}
		}

		/// <summary>
		/// Special type used to match any generic parameter type in GetMethodExt().
		/// </summary>
		public class T
		{ }

		/// <summary>
		/// Determines if the two types are either identical, or are both generic parameters or generic types
		/// with generic parameters in the same locations (generic parameters match any other generic parameter,
		/// but NOT concrete types).
		/// </summary>
		private static bool IsSimilarType(this System.Type thisType, System.Type type)
		{
			// Ignore any 'ref' types
			if (thisType.IsByRef)
				thisType = thisType.GetElementType();
			if (type.IsByRef)
				type = type.GetElementType();

			// Handle array types
			if (thisType.IsArray && type.IsArray)
				return thisType.GetElementType().IsSimilarType(type.GetElementType());

			// If the types are identical, or they're both generic parameters or the special 'T' type, treat as a match
			if (thisType == type || ((thisType.IsGenericParameter || thisType == typeof(T)) && (type.IsGenericParameter || type == typeof(T))))
				return true;

			// Handle any generic arguments
			if (thisType.IsGenericType && type.IsGenericType)
			{
				System.Type[] thisArguments = thisType.GetGenericArguments();
				System.Type[] arguments = type.GetGenericArguments();
				if (thisArguments.Length == arguments.Length)
					return !thisArguments.Where((t, i) => !t.IsSimilarType(arguments[i])).Any();
			}

			return false;
		}

		#endregion
	}
}
