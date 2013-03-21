using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Compile
{
	public class MethodContext
	{
		public MethodContext(MethodBuilder builder)
		{
			 Builder = builder;
			 IL = builder.GetILGenerator();
		}

		public MethodContext(DynamicMethod method)
		{
			IL = method.GetILGenerator();
		}

		public MethodBuilder Builder { get; private set; }
		public ILGenerator IL { get; private set; }

		public LocalBuilder DeclareLocal(Parse.Statement statement, System.Type type, string name)
		{
			var local = IL.DeclareLocal(type);
			if (!String.IsNullOrEmpty(name) && statement != null)
				local.SetLocalSymInfo(name, statement.Line, statement.LinePos);
			return local;
		}

		public void EmitVersion(Version version)
		{
			var types = new List<System.Type>() { typeof(int), typeof(int) };
			IL.Emit(OpCodes.Ldc_I4, version.Major);
			IL.Emit(OpCodes.Ldc_I4, version.Minor);
			if (version.Build >= 0)
			{
				IL.Emit(OpCodes.Ldc_I4, version.Build);
				types.Add(typeof(int));
			}
			if (version.Revision >= 0)
			{
				IL.Emit(OpCodes.Ldc_I4, version.Revision);
				types.Add(typeof(int));
			}
			
			IL.Emit(OpCodes.Newobj, typeof(Version).GetConstructor(types.ToArray()));
		}

		public void EmitName(Parse.Statement statement, string[] components)
		{
			// var nameVar = new Name();
			var nameVar = DeclareLocal(statement, typeof(Name), null);
			IL.Emit(OpCodes.Ldloca, nameVar);
			IL.Emit(OpCodes.Initobj, typeof(Name));

			// <stack> = new string[components.Length];
			IL.Emit(OpCodes.Ldloca, nameVar);
			IL.Emit(OpCodes.Ldc_I4, components.Length);
			IL.Emit(OpCodes.Newarr, typeof(string));

			for (int i = 0; i < components.Length; i++)
			{
				// <stack>[i] = components[i]
				IL.Emit(OpCodes.Dup);
				IL.Emit(OpCodes.Ldc_I4, i);
				IL.Emit(OpCodes.Ldstr, components[i]);
				IL.Emit(OpCodes.Stelem_Ref);
			}

			// nameVar.Components = <stack>
			IL.Emit(OpCodes.Stfld, ReflectionUtility.NameComponents);

			// return nameVar;
			IL.Emit(OpCodes.Ldloc, nameVar);
		}

	}
}
