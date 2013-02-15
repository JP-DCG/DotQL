using System;
using System.Configuration;

namespace Ancestry.QueryProcessor.Service
{
	public class QueryConfiguration : ConfigurationSection
	{
		[ConfigurationProperty("factoryClass", IsRequired = true)]
		public string FactoryClass
		{
			get { return (string)base["factoryClass"]; }
			set { base["factoryClass"] = value; }
		}

		[ConfigurationProperty("defaultUsings", IsDefaultCollection = true)]
		[ConfigurationCollection(typeof(UsingConfigurationCollection), AddItemName = "addUsing", ClearItemsName = "clearUsing", RemoveItemName = "removeUsing")]
		public UsingConfigurationCollection DefaultUsings
		{
			get { return (UsingConfigurationCollection)base["defaultUsings"]; }
		}

		public override string ToString()
		{
			return String.Format
			(
				"FactoryClass: {0}",
				FactoryClass
			);
		}
	}

	[ConfigurationCollection(typeof(UsingConfiguration), AddItemName = "using", CollectionType = ConfigurationElementCollectionType.BasicMap)]
	public class UsingConfigurationCollection : ConfigurationElementCollection
	{
		public override ConfigurationElementCollectionType CollectionType
		{
			get { return ConfigurationElementCollectionType.BasicMap; }
		}

		protected override string ElementName
		{
			get { return "using"; }
		}

		public UsingConfiguration this[int index]
		{
			get { return (UsingConfiguration)base.BaseGet(index); }
			set
			{
				if (base.BaseGet(index) != null)
					base.BaseRemoveAt(index);
				base.BaseAdd(index, value);
			}
		}

		public UsingConfiguration this[char code]
		{
			get { return (UsingConfiguration)base.BaseGet(code); }
		}

		public void Add(UsingConfiguration denomination)
		{
			base.BaseAdd(denomination);
		}

		public void Remove(string name)
		{
			base.BaseRemove(name);
		}

		public void Remove(UsingConfiguration denomination)
		{
			base.BaseRemove(GetElementKey(denomination));
		}

		public void Clear()
		{
			base.BaseClear();
		}

		public void RemoveAt(int index)
		{
			base.BaseRemoveAt(index);
		}

		public char GetKey(int index)
		{
			return (char)base.BaseGetKey(index);
		}

		protected override ConfigurationElement CreateNewElement()
		{
			return new UsingConfiguration();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			return ((UsingConfiguration)element).Name;
		}
	}

	public class UsingConfiguration : ConfigurationElement
	{
		[ConfigurationProperty("name", IsRequired = true)]
		public string Name
		{
			get { return (string)base["name"]; }
			set { base["name"] = value; }
		}

		[ConfigurationProperty("version", IsRequired = true)]
		public string Version
		{
			get { return (string)base["version"]; }
			set { base["version"] = value.ToString(); }
		}

		public override string ToString()
		{
			return String.Format
			(
				"using {0} {1}", 
				Name, 
				Version
			);
		}
	}
}