using System;

namespace Booru
{
	public abstract class DatabaseReadable
	{
		public bool InitFromReader(DatabaseReader reader)
		{
			var type = this.GetType ();
			var fieldInfos = type.GetFields(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public);

			foreach (var fieldInfo in fieldInfos) {
				var attribs = fieldInfo.GetCustomAttributes (typeof(PropertyAttribute), true);
				foreach (var attrib in attribs) {
					var propAttrib = (PropertyAttribute)attrib;
					var columnName = propAttrib.Column ?? fieldInfo.Name.ToLower();

					Type fieldType = fieldInfo.FieldType;
					object val = reader [columnName];


					if (val == null) {
						if (fieldType.IsClass) {
							fieldInfo.SetValue (this, null);
						} else {
							var fieldCtorEmpty = fieldType.GetConstructor (new Type[] { });
							if (fieldCtorEmpty == null) {
								fieldInfo.SetValue (this, null);
							} else {
								var obj = fieldCtorEmpty.Invoke (new object[] { });
								fieldInfo.SetValue (this, obj);
							}
						}
					} else {
						var valueType = val.GetType ();
						if (fieldType.IsAssignableFrom (valueType)) {
							fieldInfo.SetValue (this, val);
						} else if (valueType == typeof(int) && fieldType == typeof(long)) {
							fieldInfo.SetValue (this, (long)(int)val);
						} else if (valueType == typeof(long) && fieldType == typeof(int)) {
							fieldInfo.SetValue (this, (int)(long)val);
						} else {
							var fieldCtorValue = fieldType.GetConstructor (new Type[] { valueType });
							if (fieldCtorValue == null) {
								throw new InvalidCastException ("Can't initialize field " + type.Name + "." + fieldInfo.Name + " from column " + columnName + ". Type is " + valueType.Name + ".");
							} else {
								var obj = fieldCtorValue.Invoke (new object[] { val });
								fieldInfo.SetValue (this, obj);
							}
						}
					}
				}
			}

			return true;
		}

		[AttributeUsage(AttributeTargets.Field)]
		public class PropertyAttribute : Attribute 
		{
			public readonly string Column;

			public PropertyAttribute(string column = null)
			{
				this.Column = column;
			}
		}
	}
}

