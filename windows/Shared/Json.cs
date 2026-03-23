using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace NowLink.Shared
{
    public static class Json
    {
        private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer();

        public static string Serialize(object value)
        {
            return Serializer.Serialize(value);
        }

        public static T Deserialize<T>(string json)
        {
            return Serializer.Deserialize<T>(json);
        }

        public static T Load<T>(string path) where T : new()
        {
            if (!File.Exists(path))
            {
                return new T();
            }

            return Deserialize<T>(File.ReadAllText(path, Encoding.UTF8));
        }

        public static void Save<T>(string path, T value)
        {
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(path, Serialize(value), Encoding.UTF8);
        }
    }
}
