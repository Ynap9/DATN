using System.Text;

namespace sh.be.infrastructure.external.Storage.Base
{
    public class PackageXmlBuilder(string declaration)
    {
        private static readonly UTF8Encoding Utf8WithoutBom = new(false);
        private readonly StringBuilder _content = new(declaration);
        private int _depth;

        public void Open(string name, params (string Name, string? Value)[] attributes)
        {
            AppendStartTag(name, attributes, ">");
            _depth++;
        }

        public void Close(string name)
        {
            _depth--;
            AppendNewLine();
            _content.Append("</").Append(name).Append('>');
        }

        public void Empty(string name, params (string Name, string? Value)[] attributes)
        {
            AppendStartTag(name, attributes, "/>");
        }

        public void Element(string name, string value, params (string Name, string? Value)[] attributes)
        {
            if (value.Length == 0)
            {
                Empty(name, attributes);
                return;
            }
            AppendStartTag(name, attributes, ">");
            _content.Append(Escape(value, false)).Append("</").Append(name).Append('>');
        }

        public byte[] ToBytes()
        {
            return Utf8WithoutBom.GetBytes($"{_content}\n");
        }

        // Thuộc tính giá trị null thì bỏ (vd ADMID chỉ có ở AIP, DIP); chuỗi rỗng vẫn phát như OTHERTYPE="" của gói mẫu
        private void AppendStartTag(string name, (string Name, string? Value)[] attributes, string tagEnd)
        {
            AppendNewLine();
            _content.Append('<').Append(name);
            foreach (var (attributeName, attributeValue) in attributes.Where(a => a.Value != null))
            {
                _content.Append(' ').Append(attributeName).Append("=\"").Append(Escape(attributeValue!, true)).Append('"');
            }
            _content.Append(tagEnd);
        }

        private void AppendNewLine()
        {
            _content.Append('\n').Append(' ', _depth * 4);
        }

        private static string Escape(string value, bool isAttribute)
        {
            var escaped = value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
            return isAttribute ? escaped.Replace("\"", "&quot;") : escaped;
        }
    }
}
