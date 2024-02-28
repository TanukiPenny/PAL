using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using PAL.DataObjects;

namespace PAL;

public class PersistenceManager
{
    private string _filePath;
    private Assembly _callingAssembly;

    public PersistenceManager(string filePath)
    {
        _filePath = filePath;
        _callingAssembly = Assembly.GetCallingAssembly();
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class PersistentField : Attribute;

    public void Save()
    {
        List<FieldData> savedFields = new();

        List<Type> types = _callingAssembly.GetTypes().ToList();
        foreach (Type type in types)
        {
            FieldInfo[] fieldInfos =
                type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public |
                               BindingFlags.Static);
            foreach (FieldInfo fieldInfo in fieldInfos)
            {
                if (!fieldInfo.GetCustomAttributes().Contains(new PersistentField())) continue;

                object obj = Activator.CreateInstance(type) ?? throw new InvalidOperationException();

                Console.WriteLine(
                    $"Saved - Name: {fieldInfo.Name}, FieldType: {fieldInfo.FieldType}, DeclaringType: {fieldInfo.DeclaringType}, Value: {fieldInfo.GetValue(obj)}");
                if (fieldInfo.DeclaringType != null)
                    savedFields.Add(new FieldData(fieldInfo.Name, fieldInfo.FieldType.ToString(),
                        fieldInfo.DeclaringType.ToString(),
                        fieldInfo.GetValue(obj) ?? throw new InvalidOperationException()));
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        string saveJson = JsonSerializer.Serialize(savedFields);

        File.WriteAllText(_filePath, saveJson);
    }

    public void Load()
    {
        if (!File.Exists(_filePath)) return;
        string saveJson = File.ReadAllText(_filePath);

        List<FieldData> savedFields = JsonSerializer.Deserialize<List<FieldData>>(saveJson) ??
                                      throw new InvalidOperationException();

        foreach (FieldData savedField in savedFields)
        {
            Type? declaringType = Type.GetType(savedField.DeclaringType);
            if (declaringType == null)
            {
                declaringType = _callingAssembly.GetType(savedField.DeclaringType);
            }
            if (declaringType == null)
            {
                throw new InvalidOperationException();
            }
            
            Type? valueType = Type.GetType(savedField.FieldType);
            if (valueType == null)
            {
                valueType = _callingAssembly.GetType(savedField.FieldType);
            }
            if (valueType == null)
            {
                throw new InvalidOperationException();
            }

            FieldInfo fieldInfo =
                declaringType.GetField(savedField.Name,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static) ??
                throw new InvalidOperationException();

            object obj = Activator.CreateInstance(declaringType) ?? throw new InvalidOperationException();

            JsonElement jsonValue = (JsonElement)savedField.Value;
            object? value = jsonValue.Deserialize(valueType, new JsonSerializerOptions
            {
                IncludeFields = true,
            });

            fieldInfo.SetValue(obj, value);
            Console.WriteLine(
                $"Loaded - Name: {fieldInfo.Name}, FieldType: {fieldInfo.FieldType}, DeclaringType: {fieldInfo.DeclaringType}, Value: {fieldInfo.GetValue(obj)}");
        }
    }
}