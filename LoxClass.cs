namespace LoxSharp
{
	class LoxClass
	{
		readonly string Name;

		public LoxClass(string name)
		{
			Name = name;
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
