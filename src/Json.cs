// 本项目由 AI 编码代理生成（DeepSeek Harness / deepseek-flash），说明见仓库根目录 AI-DISCLOSURE.md。
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SoundWheel
{
    internal static class Json
    {
        public static object Parse(string text)
        {
            int i = 0;
            object value = ParseValue(text, ref i);
            SkipWs(text, ref i);
            if (i < text.Length)
                throw new FormatException("JSON 末尾存在多余字符，位置 " + i.ToString(CultureInfo.InvariantCulture));
            return value;
        }

        public static Dictionary<string, object> Obj(object value)
        {
            Dictionary<string, object> d = value as Dictionary<string, object>;
            if (d == null) return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            return d;
        }

        public static List<object> Arr(object value)
        {
            List<object> list = value as List<object>;
            if (list == null) return new List<object>();
            return list;
        }

        public static string Str(Dictionary<string, object> d, string key, string def)
        {
            if (d == null || !d.ContainsKey(key)) return def;
            object v = d[key];
            if (v == null) return def;
            return Convert.ToString(v, CultureInfo.InvariantCulture);
        }

        public static int Int(Dictionary<string, object> d, string key, int def)
        {
            if (d == null || !d.ContainsKey(key)) return def;
            object v = d[key];
            if (v == null) return def;
            try { return Convert.ToInt32(v, CultureInfo.InvariantCulture); }
            catch (Exception) { return def; }
        }

        public static double Dbl(Dictionary<string, object> d, string key, double def)
        {
            if (d == null || !d.ContainsKey(key)) return def;
            object v = d[key];
            if (v == null) return def;
            try { return Convert.ToDouble(v, CultureInfo.InvariantCulture); }
            catch (Exception) { return def; }
        }

        public static bool Bool(Dictionary<string, object> d, string key, bool def)
        {
            if (d == null || !d.ContainsKey(key)) return def;
            object v = d[key];
            if (v is bool) return (bool)v;
            if (v == null) return def;
            string s = Convert.ToString(v).ToLowerInvariant();
            return s == "true" || s == "1" || s == "yes";
        }

        public static Array GetArr(Dictionary<string, object> d, string key)
        {
            if (d == null || !d.ContainsKey(key)) return new object[0];
            List<object> list = d[key] as List<object>;
            if (list == null) return new object[0];
            return list.ToArray();
        }

        public static string Stringify(object value)
        {
            StringBuilder sb = new StringBuilder();
            WriteValue(sb, value, 0);
            return sb.ToString();
        }

        private static void WriteValue(StringBuilder sb, object value, int indent)
        {
            if (value == null) { sb.Append("null"); return; }
            string s = value as string;
            if (s != null) { WriteString(sb, s); return; }
            if (value is bool) { sb.Append((bool)value ? "true" : "false"); return; }
            if (value is int || value is long || value is uint)
            {
                sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                return;
            }
            if (value is double || value is float || value is decimal)
            {
                double dv = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                if (Math.Abs(dv - Math.Round(dv)) < 0.0000001)
                    sb.Append(((long)Math.Round(dv)).ToString(CultureInfo.InvariantCulture));
                else
                    sb.Append(dv.ToString("0.#####", CultureInfo.InvariantCulture));
                return;
            }
            Dictionary<string, object> dict = value as Dictionary<string, object>;
            if (dict != null) { WriteObject(sb, dict, indent); return; }
            System.Collections.IEnumerable en = value as System.Collections.IEnumerable;
            if (en != null) { WriteArray(sb, en, indent); return; }
            WriteString(sb, Convert.ToString(value, CultureInfo.InvariantCulture));
        }

        private static void WriteObject(StringBuilder sb, Dictionary<string, object> dict, int indent)
        {
            sb.Append('{');
            bool first = true;
            foreach (KeyValuePair<string, object> kv in dict)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append('\n');
                sb.Append(' ', (indent + 2) * 2);
                WriteString(sb, kv.Key);
                sb.Append(": ");
                WriteValue(sb, kv.Value, indent + 1);
            }
            if (!first)
            {
                sb.Append('\n');
                sb.Append(' ', indent * 2);
            }
            sb.Append('}');
        }

        private static void WriteArray(StringBuilder sb, System.Collections.IEnumerable items, int indent)
        {
            sb.Append('[');
            bool first = true;
            foreach (object item in items)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append('\n');
                sb.Append(' ', (indent + 2) * 2);
                WriteValue(sb, item, indent + 1);
            }
            if (!first)
            {
                sb.Append('\n');
                sb.Append(' ', indent * 2);
            }
            sb.Append(']');
        }

        private static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 32)
                            sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else
                            sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        private static void SkipWs(string s, ref int i)
        {
            while (i < s.Length)
            {
                char c = s[i];
                if (c == ' ' || c == '\t' || c == '\r' || c == '\n') i++;
                else break;
            }
        }

        private static object ParseValue(string s, ref int i)
        {
            SkipWs(s, ref i);
            if (i >= s.Length) throw new FormatException("JSON 意外结束");
            char c = s[i];
            if (c == '{') return ParseObject(s, ref i);
            if (c == '[') return ParseArray(s, ref i);
            if (c == '"') return ParseString(s, ref i);
            if (c == 't') { Expect(s, ref i, "true"); return true; }
            if (c == 'f') { Expect(s, ref i, "false"); return false; }
            if (c == 'n') { Expect(s, ref i, "null"); return null; }
            return ParseNumber(s, ref i);
        }

        private static object ParseObject(string s, ref int i)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            i++;
            SkipWs(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return dict; }
            while (true)
            {
                SkipWs(s, ref i);
                string key = ParseString(s, ref i);
                SkipWs(s, ref i);
                if (i >= s.Length || s[i] != ':') throw new FormatException("JSON 缺少冒号");
                i++;
                dict[key] = ParseValue(s, ref i);
                SkipWs(s, ref i);
                if (i >= s.Length) throw new FormatException("JSON 对象未闭合");
                char c = s[i++];
                if (c == '}') break;
                if (c != ',') throw new FormatException("JSON 对象格式错误");
            }
            return dict;
        }

        private static object ParseArray(string s, ref int i)
        {
            List<object> list = new List<object>();
            i++;
            SkipWs(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return list; }
            while (true)
            {
                list.Add(ParseValue(s, ref i));
                SkipWs(s, ref i);
                if (i >= s.Length) throw new FormatException("JSON 数组未闭合");
                char c = s[i++];
                if (c == ']') break;
                if (c != ',') throw new FormatException("JSON 数组格式错误");
            }
            return list;
        }

        private static string ParseString(string s, ref int i)
        {
            if (i >= s.Length || s[i] != '"') throw new FormatException("JSON 字符串格式错误");
            i++;
            StringBuilder sb = new StringBuilder();
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }
                if (i >= s.Length) break;
                char e = s[i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 > s.Length) throw new FormatException("JSON Unicode 转义错误");
                        sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16));
                        i += 4;
                        break;
                    default: throw new FormatException("JSON 非法转义");
                }
            }
            throw new FormatException("JSON 字符串未闭合");
        }

        private static object ParseNumber(string s, ref int i)
        {
            int start = i;
            if (i < s.Length && (s[i] == '-' || s[i] == '+')) i++;
            while (i < s.Length)
            {
                char c = s[i];
                if ((c >= '0' && c <= '9') || c == '.' || c == 'e' || c == 'E' || c == '-' || c == '+') i++;
                else break;
            }
            if (start == i) throw new FormatException("JSON 数字格式错误");
            return double.Parse(s.Substring(start, i - start), CultureInfo.InvariantCulture);
        }

        private static void Expect(string s, ref int i, string token)
        {
            if (i + token.Length > s.Length || s.Substring(i, token.Length) != token)
                throw new FormatException("JSON 令牌错误");
            i += token.Length;
        }
    }
}
