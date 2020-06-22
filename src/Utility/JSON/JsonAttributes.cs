using System;
namespace TinyJson
{

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
	public class JsonPropertyAttribute : Attribute {
		public string Name { get; private set; }

		public JsonPropertyAttribute(string name) {
			Name = name;
		}
	}

	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public class MatchSnakeCaseAttribute : Attribute {
	}
}
