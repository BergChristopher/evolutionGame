public class Description : System.Attribute {
	private string _description;
	
	public Description(string description)
	{
		_description = description;
	}
	
	public string Value
	{
		get { return _description; }
	}
}

