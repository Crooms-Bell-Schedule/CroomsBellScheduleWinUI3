using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CroomsBellScheduleCS.Utils
{
    public class HtmlWriter
    {
        private StringBuilder result = new();
        private Stack<string> tags = [];

        public string GetHTML()
        {
            if (tags.Count != 0)
            {
                throw new Exception("unclosed tag " + tags.First());
            }
            return result.ToString();
        }

        public void BeginTag(string name, List<HtmlAttrib>? attributes = null)
        {
            result.Append($"<{name} ");
            if (attributes != null)
                foreach (var item in attributes)
                {
                    result.Append($"{item.Name}={item.Value}");
                }
            result.Append(">");
            tags.Push(name);
        }
        public void AppendString(string text)
        {
            result.Append(text);
        }

        public void EndTag(string name)
        {
            string tag = tags.Pop();
            if (tag != name) throw new Exception("unclosed tag " + name+", expected " + tag);

            result.Append($"</{name}>");
        }
    }
    public record HtmlAttrib(string Name, string Value);
}
