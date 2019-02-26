using System.Linq;
using YamlDotNet.RepresentationModel;

namespace EWAModClient
{
    public static class YamlExtensions
    {
        public static YamlNode GetChild(this YamlMappingNode aNode, string aChildName)
        {
            return aNode?.Children.FirstOrDefault(C => C.Key.ToString() == aChildName).Value;
        }
    }
}
