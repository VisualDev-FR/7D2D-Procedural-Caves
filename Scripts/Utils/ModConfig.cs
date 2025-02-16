using System.Collections.Generic;
using System.IO;
using System.Xml;


public class ModConfig
{
    private readonly Dictionary<string, string> properties = new Dictionary<string, string>();

    private readonly XmlDocument document;

    public ModConfig(string modName)
    {
        // modName must equals the one defined in ModInfo.xml
        var mod = ModManager.GetMod(modName);
        var modConfig = Path.GetFullPath($"{mod.Path}/ModConfig.xml");

        document = new XmlDocument();

        using (var reader = new StreamReader(modConfig))
        {
            document.LoadXml(reader.ReadToEnd());
        }

        foreach (XmlNode property in document.GetElementsByTagName("property"))
        {
            string name = property.Attributes["name"]?.Value;
            string value = property.Attributes["value"]?.Value;

            if (name != null && value != null)
            {
                properties[name] = value;
            }
        }
    }

    public string GetProperty(string name)
    {
        if (properties.TryGetValue(name, out var value))
        {
            Logging.Debug($"{name}: {value}");
            return value;
        }

        throw new KeyNotFoundException(name);
    }

    public float GetPropertyFloat(string name)
    {
        return float.Parse(GetProperty(name));
    }

    public int GetPropertyInt(string name)
    {
        return int.Parse(GetProperty(name));
    }

    public bool GetPropertyBool(string name)
    {
        return bool.Parse(GetProperty(name));
    }
}
