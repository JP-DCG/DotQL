using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	/// <summary> Describes a type that is composed of a component type, such as a set, list, or interval. </summary>
	public interface IComponentType
	{
		BaseType Of { get; }
	}
}
