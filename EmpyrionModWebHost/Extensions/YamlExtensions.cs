using Newtonsoft.Json;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace EmpyrionModWebHost.Extensions
{
    public static class YamlExtensions
    {
        public static YamlNode GetChild(this YamlMappingNode aNode, string aChildName)
        {
            return aNode?.Children.FirstOrDefault(C => C.Key.ToString() == aChildName).Value;
        }
        public static string ObjectToYaml(object aObjectData)
        {
            return new SerializerBuilder()
                .Build()
                .Serialize(aObjectData);
        }

        public static T YamlToObject<T>(TextReader aYamlData)
        {
            var deserializer = new DeserializerBuilder().Build();
            var yamlObject = deserializer.Deserialize(aYamlData);

            var serializer = new SerializerBuilder()
                .JsonCompatible()
                .Build();

            var json = serializer.Serialize(yamlObject);

            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
