using System;
using System.ComponentModel;
using System.Reflection;

namespace Legenda.ProjSpace.Main.Model.Enums
{
	public static class EnumHelper
	{
		public static string GetEnumDescription(this Enum enumValue)
		{
			System.Reflection.FieldInfo field = enumValue.GetType().GetField(enumValue.ToString());
			if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute descriptionAttribute)
			{
				return descriptionAttribute.Description;
			}
			throw new ArgumentException("Item not found.", "enumValue");
		}
	}
}