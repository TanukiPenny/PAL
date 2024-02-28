namespace PAL.DataObjects;

public class FieldData(string name, string fieldType, string declaringType, object value)
{
    public string Name { get; set; } = name;
    public string FieldType { get; set; } = fieldType;
    public string DeclaringType { get; set; } = declaringType;
    public object Value { get; set; } = value;
}