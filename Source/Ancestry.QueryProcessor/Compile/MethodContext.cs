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
			var i = 1;
			IL.Emit(OpCodes.Ldc_I4, version.Major);
			if (version.Minor >= 0)
				IL.Emit(OpCodes.Ldc_I4, version.Minor);
			if (version.Build >= 0)
				IL.Emit(OpCodes.Ldc_I4, version.Build);
			if (version.Revision >= 0)
				IL.Emit(OpCodes.Ldc_I4, version.Revision);

			var types = new List<System.Type>() { typeof(int) };
			while (types.Count < i)
				types.Add(typeof(int));
			
			IL.Emit(OpCodes.Newobj, typeof(Version).GetConstructor(types.ToArray()));
		}
	}
}
