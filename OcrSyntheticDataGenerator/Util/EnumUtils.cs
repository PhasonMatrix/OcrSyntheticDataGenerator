using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static OcrSyntheticDataGenerator.ImageGeneration.ImageAndLabelGeneratorBase;

namespace OcrSyntheticDataGenerator.Util;

public class EnumUtils
{

    public static TEnum ParseDescription<TEnum>(string stringValue)
    {
        Type type = typeof(TEnum);
        foreach (FieldInfo field in type.GetFields())
        {
            DescriptionAttribute attribute = (DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
            if (attribute != null && attribute.Description == stringValue)
            {
                return (TEnum)Enum.Parse(type, field.Name);
            }
        }
        throw new ArgumentException($"Invalid enum value: {stringValue}");
    }



    public static List<string> GetDescriptions<TEnum>()
    {
        Type type = typeof(TEnum);

        //List<string> descriptions = new List<string>();
        //foreach (var field in type.GetFields())
        //{
        //    var attribute = (DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
        //    if (attribute != null)
        //    {
        //        descriptions.Add(attribute.Description);
        //    }
        //}
        //return descriptions;

        return type.GetFields()
            .Select(field => field.GetCustomAttribute<DescriptionAttribute>())
            .Where(attribute => attribute != null)
            .Select(attribute => attribute.Description)
            .ToList();
    }


}
