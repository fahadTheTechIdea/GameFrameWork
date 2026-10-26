using System;
using System.Collections.Generic;
using System.Text;

namespace Beep.SimEngine.Content
{
    public enum JsonKind { Null, Bool, Number, String, Array, Object }

    /// <summary>
    /// A JSON value with ORDERED object keys (content load order must be stable on every machine).
    /// Hand-written because netstandard2.1 has no built-in JSON and a dependency would leave the
    /// framework no longer plain C#.
    /// </summary>
    public sealed class JsonValue
    {
        public JsonKind Kind { get; private set; }
        public bool Bool { get; private set; }
        /// <summary>String value, or the raw text of a number.</summary>
        public string Text { get; private set; }
        private List<JsonValue> _items;
        private List<string> _keys;
        private List<JsonValue> _values;
        private bool _frozen;
        internal bool Frozen => _frozen;

        /// <summary>Seals an engine-owned authored value and every nested value before providers read it.</summary>
        internal void Freeze()
        {
            if (_frozen) return;
            if (_items != null) for (int i = 0; i < _items.Count; i++) _items[i].Freeze();
            if (_values != null) for (int i = 0; i < _values.Count; i++) _values[i].Freeze();
            _frozen = true;
        }

        private void RequireMutable()
        {
            if (_frozen) throw new InvalidOperationException("resolved authored JSON is immutable");
        }

        public static JsonValue Null() => new JsonValue { Kind = JsonKind.Null };
        public static JsonValue Of(bool b) => new JsonValue { Kind = JsonKind.Bool, Bool = b };
        public static JsonValue Of(string s) => new JsonValue { Kind = JsonKind.String, Text = s };
        public static JsonValue Number(string raw)
        {
            if (!Json.IsNumberLiteral(raw)) throw new FormatException("bad number '" + raw + "'");
            return new JsonValue { Kind = JsonKind.Number, Text = raw };
        }
        public static JsonValue NewObject() => new JsonValue { Kind = JsonKind.Object, _keys = new List<string>(), _values = new List<JsonValue>() };
        public static JsonValue NewArray() => new JsonValue { Kind = JsonKind.Array, _items = new List<JsonValue>() };

        public bool IsObject => Kind == JsonKind.Object;
        public bool IsArray => Kind == JsonKind.Array;
        public int Count => Kind == JsonKind.Object ? _keys.Count : Kind == JsonKind.Array ? _items.Count : 0;

        public JsonValue this[int i] => _items[i];
        public string KeyAt(int i) => _keys[i];
        public JsonValue ValueAt(int i) => _values[i];

        public JsonValue Get(string key)
        {
            if (Kind != JsonKind.Object) return null;
            int i = _keys.IndexOf(key);
            return i < 0 ? null : _values[i];
        }

        public bool Has(string key) => Get(key) != null;

        public void Set(string key, JsonValue v)
        {
            RequireMutable();
            int i = _keys.IndexOf(key);
            if (i < 0) { _keys.Add(key); _values.Add(v); }
            else _values[i] = v;
        }

        public bool Remove(string key)
        {
            RequireMutable();
            int i = _keys.IndexOf(key);
            if (i < 0) return false;
            _keys.RemoveAt(i);
            _values.RemoveAt(i);
            return true;
        }

        public void Add(JsonValue v) { RequireMutable(); _items.Add(v); }

        public JsonValue Clone()
        {
            var c = new JsonValue { Kind = Kind, Bool = Bool, Text = Text };
            if (_items != null)
            {
                c._items = new List<JsonValue>(_items.Count);
                for (int i = 0; i < _items.Count; i++) c._items.Add(_items[i].Clone());
            }
            if (_keys != null)
            {
                c._keys = new List<string>(_keys);
                c._values = new List<JsonValue>(_values.Count);
                for (int i = 0; i < _values.Count; i++) c._values.Add(_values[i].Clone());
            }
            return c;
        }

        /// <summary>Canonical compact text: used for fingerprints, so identical content hashes identically.</summary>
        public void WriteCanonical(StringBuilder sb)
        {
            switch (Kind)
            {
                case JsonKind.Null: sb.Append("null"); break;
                case JsonKind.Bool: sb.Append(Bool ? "true" : "false"); break;
                case JsonKind.Number: sb.Append(Text); break;
                case JsonKind.String: sb.Append('"').Append(Text.Replace("\\", "\\\\").Replace("\"", "\\\"")).Append('"'); break;
                case JsonKind.Array:
                    sb.Append('[');
                    for (int i = 0; i < _items.Count; i++) { if (i > 0) sb.Append(','); _items[i].WriteCanonical(sb); }
                    sb.Append(']');
                    break;
                case JsonKind.Object:
                    sb.Append('{');
                    for (int i = 0; i < _keys.Count; i++)
                    {
                        if (i > 0) sb.Append(',');
                        sb.Append('"').Append(_keys[i]).Append("\":");
                        _values[i].WriteCanonical(sb);
                    }
                    sb.Append('}');
                    break;
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            WriteCanonical(sb);
            return sb.ToString();
        }
    }

    public static class Json
    {
        /// <summary>Parses strict JSON, plus // line comments for hand-edited content.</summary>
        public static JsonValue Parse(string text, string source = null)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            int i = 0;
            try
            {
                JsonValue v = ParseValue(text, ref i);
                SkipWs(text, ref i);
                if (i != text.Length) throw new FormatException("unexpected trailing content");
                return v;
            }
            catch (Exception ex) when (ex is FormatException || ex is IndexOutOfRangeException || ex is ArgumentOutOfRangeException)
            {
                throw new ContentException((source ?? "json") + ": " + ex.Message + " at offset " + i);
            }
        }

        private static void SkipWs(string s, ref int i)
        {
            while (i < s.Length)
            {
                char c = s[i];
                if (c == ' ' || c == '\t' || c == '\r' || c == '\n' || c == '﻿') i++;
                else if (c == '/' && i + 1 < s.Length && s[i + 1] == '/') { while (i < s.Length && s[i] != '\n') i++; }
                else break;
            }
        }

        private static JsonValue ParseValue(string s, ref int i)
        {
            SkipWs(s, ref i);
            if (i >= s.Length) throw new FormatException("unexpected end");
            switch (s[i])
            {
                case '{': return ParseObject(s, ref i);
                case '[': return ParseArray(s, ref i);
                case '"': return JsonValue.Of(ParseString(s, ref i));
                case 't': Expect(s, ref i, "true"); return JsonValue.Of(true);
                case 'f': Expect(s, ref i, "false"); return JsonValue.Of(false);
                case 'n': Expect(s, ref i, "null"); return JsonValue.Null();
                default: return ParseNumber(s, ref i);
            }
        }

        private static void Expect(string s, ref int i, string lit)
        {
            if (i + lit.Length > s.Length || string.CompareOrdinal(s, i, lit, 0, lit.Length) != 0)
                throw new FormatException("expected '" + lit + "'");
            i += lit.Length;
        }

        private static JsonValue ParseObject(string s, ref int i)
        {
            JsonValue o = JsonValue.NewObject();
            i++;
            SkipWs(s, ref i);
            if (s[i] == '}') { i++; return o; }
            while (true)
            {
                SkipWs(s, ref i);
                string key = ParseString(s, ref i);
                SkipWs(s, ref i);
                if (s[i] != ':') throw new FormatException("expected ':'");
                i++;
                JsonValue val = ParseValue(s, ref i);
                if (o.Has(key)) throw new FormatException("duplicate key '" + key + "'");
                o.Set(key, val);
                SkipWs(s, ref i);
                if (s[i] == ',') { i++; continue; }
                if (s[i] == '}') { i++; return o; }
                throw new FormatException("expected ',' or '}'");
            }
        }

        private static JsonValue ParseArray(string s, ref int i)
        {
            JsonValue a = JsonValue.NewArray();
            i++;
            SkipWs(s, ref i);
            if (s[i] == ']') { i++; return a; }
            while (true)
            {
                a.Add(ParseValue(s, ref i));
                SkipWs(s, ref i);
                if (s[i] == ',') { i++; continue; }
                if (s[i] == ']') { i++; return a; }
                throw new FormatException("expected ',' or ']'");
            }
        }

        private static string ParseString(string s, ref int i)
        {
            if (s[i] != '"') throw new FormatException("expected a string");
            i++;
            var sb = new StringBuilder();
            while (s[i] != '"')
            {
                if (s[i] == '\\')
                {
                    i++;
                    char e = s[i++];
                    switch (e)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 't': sb.Append('\t'); break;
                        case 'r': sb.Append('\r'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'u': sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16)); i += 4; break;
                        default: throw new FormatException("bad escape '\\" + e + "'");
                    }
                }
                else sb.Append(s[i++]);
            }
            i++;
            return sb.ToString();
        }

        private static JsonValue ParseNumber(string s, ref int i)
        {
            int start = i;
            while (i < s.Length)
            {
                char c = s[i];
                if ((c >= '0' && c <= '9') || c == '-' || c == '+' || c == '.' || c == 'e' || c == 'E') i++;
                else break;
            }
            string raw = s.Substring(start, i - start);
            return JsonValue.Number(raw);
        }

        internal static bool IsNumberLiteral(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return false;
            int p = 0;
            if (p < raw.Length && raw[p] == '-') p++;
            if (p == raw.Length) return false;
            if (raw[p] == '0') p++;
            else
            {
                if (raw[p] < '1' || raw[p] > '9') return false;
                while (p < raw.Length && raw[p] >= '0' && raw[p] <= '9') p++;
            }
            if (p < raw.Length && raw[p] == '.')
            {
                int first = ++p;
                while (p < raw.Length && raw[p] >= '0' && raw[p] <= '9') p++;
                if (p == first) return false;
            }
            if (p < raw.Length && (raw[p] == 'e' || raw[p] == 'E'))
            {
                p++;
                if (p < raw.Length && (raw[p] == '+' || raw[p] == '-')) p++;
                int first = p;
                while (p < raw.Length && raw[p] >= '0' && raw[p] <= '9') p++;
                if (p == first) return false;
            }
            return p == raw.Length;
        }
    }

    /// <summary>Content that cannot be loaded. Always names where the problem is.</summary>
    public sealed class ContentException : Exception
    {
        public ContentException(string message) : base(message) { }
    }

    /// <summary>
    /// Strict typed reader over one JSON object. Every key read is recorded; <see cref="End"/> fails
    /// if the object holds a key nobody read, so a misspelt key is a load error, never a silent default.
    /// Wrong types are errors too.
    /// </summary>
    public sealed class JsonReader
    {
        private readonly JsonValue _obj;
        private readonly string _where;
        private readonly bool[] _read;

        public JsonReader(JsonValue obj, string where)
        {
            if (obj == null || !obj.IsObject) throw new ContentException(where + ": expected an object");
            _obj = obj;
            _where = where;
            _read = new bool[obj.Count];
        }

        public string Where => _where;
        public JsonValue Raw => _obj;

        private JsonValue Take(string key)
        {
            for (int i = 0; i < _obj.Count; i++)
                if (string.Equals(_obj.KeyAt(i), key, StringComparison.Ordinal)) { _read[i] = true; return _obj.ValueAt(i); }
            return null;
        }

        public bool Has(string key)
        {
            for (int i = 0; i < _obj.Count; i++)
                if (string.Equals(_obj.KeyAt(i), key, StringComparison.Ordinal)) return true;
            return false;
        }

        public ContentException Error(string message) => new ContentException(_where + ": " + message);

        private JsonValue Required(string key)
        {
            JsonValue v = Take(key);
            if (v == null) throw Error("missing '" + key + "'");
            return v;
        }

        public string String(string key) => AsString(Required(key), key);
        public string String(string key, string fallback) { JsonValue v = Take(key); return v == null ? fallback : AsString(v, key); }
        /// <summary>An optional reference/text value. Explicit null is absence, never a substituted identity.</summary>
        public string NullableString(string key)
        {
            JsonValue value = Take(key);
            return value == null || value.Kind == JsonKind.Null ? null : AsString(value, key);
        }
        public Fix Fix(string key) => AsFix(Required(key), key);
        public Fix Fix(string key, Fix fallback) { JsonValue v = Take(key); return v == null ? fallback : AsFix(v, key); }
        public int Int(string key) => AsInt(Required(key), key);
        public int Int(string key, int fallback) { JsonValue v = Take(key); return v == null ? fallback : AsInt(v, key); }
        public long Long(string key) => AsLong(Required(key), key);
        /// <summary>Required authored positive 64-bit limit; explicit null selects unlimited.</summary>
        public long? PositiveLimit(string key)
        {
            JsonValue value = Required(key);
            if (value.Kind == JsonKind.Null) return null;
            long result = AsLong(value, key);
            if (result <= 0) throw Error("'" + key + "' must be positive or explicit null");
            return result;
        }
        private long AsLong(JsonValue v, string key)
        {
            if (v.Kind != JsonKind.Number || !long.TryParse(v.Text, System.Globalization.NumberStyles.AllowLeadingSign,
                System.Globalization.CultureInfo.InvariantCulture, out long result)) throw Error("'" + key + "' must be a 64-bit whole number");
            return result;
        }
        public bool Bool(string key) => AsBool(Required(key), key);
        public bool Bool(string key, bool fallback) { JsonValue v = Take(key); return v == null ? fallback : AsBool(v, key); }

        /// <summary>Exact authored duration retained independently of the selected session quantum.</summary>
        public GameDuration Duration(string key, DurationUnit unit, long fallback)
        {
            JsonValue v = Take(key);
            if (v != null && v.Kind != JsonKind.Number) throw Error("'" + key + "' must be a number");
            try
            {
                return v == null ? new GameDuration(fallback, 1, unit) : GameDuration.Decimal(v.Text, unit);
            }
            catch (OverflowException) { throw Error("'" + key + "' exceeds the current duration representation"); }
            catch (ArgumentOutOfRangeException) { throw Error("'" + key + "' cannot be negative"); }
        }

        public GameDuration RationalDuration()
        {
            long numerator = Long("numerator"), denominator = Long("denominator");
            DurationUnit unit = TimeUnit(String("unit"), this); End();
            if (numerator < 0 || denominator <= 0) throw Error("duration requires nonnegative numerator and positive denominator");
            return new GameDuration(numerator, denominator, unit);
        }

        internal static DurationUnit TimeUnit(string unit, JsonReader reader)
        {
            switch (unit)
            {
                case "second": return DurationUnit.Second;
                case "minute": return DurationUnit.Minute;
                case "hour": return DurationUnit.Hour;
                case "day": return DurationUnit.Day;
                default: throw reader.Error("unknown elapsed-time unit '" + unit + "'");
            }
        }

        public JsonValue Value(string key) => Take(key);
        public JsonValue Object(string key) { JsonValue v = Take(key); if (v != null && !v.IsObject) throw Error("'" + key + "' must be an object"); return v; }
        public JsonValue Array(string key) { JsonValue v = Take(key); if (v != null && !v.IsArray) throw Error("'" + key + "' must be an array"); return v; }

        public string[] Strings(string key)
        {
            JsonValue a = Array(key);
            if (a == null) return new string[0];
            var r = new string[a.Count];
            for (int i = 0; i < a.Count; i++) r[i] = AsString(a[i], key);
            return r;
        }

        public JsonReader Child(string key)
        {
            JsonValue v = Object(key);
            return v == null ? null : new JsonReader(v, _where + "." + key);
        }

        /// <summary>Marks every key as read: for sections a different reader consumes.</summary>
        public void Skip(string key) => Take(key);

        /// <summary>Fails on any key nobody read.</summary>
        public void End()
        {
            for (int i = 0; i < _read.Length; i++)
                if (!_read[i]) throw Error("unknown key '" + _obj.KeyAt(i) + "'");
        }

        private string AsString(JsonValue v, string key)
        {
            if (v.Kind != JsonKind.String) throw Error("'" + key + "' must be a string");
            return v.Text;
        }

        private Fix AsFix(JsonValue v, string key)
        {
            if (v.Kind != JsonKind.Number) throw Error("'" + key + "' must be a number");
            if (!Beep.SimEngine.Fix.TryParse(v.Text, out Fix result))
                throw Error("'" + key + "' exceeds the fixed-point quantity representation");
            return result;
        }

        private int AsInt(JsonValue v, string key)
        {
            if (v.Kind != JsonKind.Number || !int.TryParse(v.Text, System.Globalization.NumberStyles.AllowLeadingSign,
                    System.Globalization.CultureInfo.InvariantCulture, out int r))
                throw Error("'" + key + "' must be a whole number");
            return r;
        }

        private bool AsBool(JsonValue v, string key)
        {
            if (v.Kind != JsonKind.Bool) throw Error("'" + key + "' must be true or false");
            return v.Bool;
        }
    }
}
