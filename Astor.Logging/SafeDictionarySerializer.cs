using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Astor.Logging;

public class SafeDictionaryJson(SafeDictionaryJson.Options options)
{
    public class Options
    {
        public JsonNamingPolicy NamingPolicy { get; init; } = JsonNamingPolicy.CamelCase;
        public bool Indented { get; init; }

        public Dictionary<Type, Action<Utf8JsonWriter, KeyValuePair<string, object>>> CustomTypeWriters { get; } = new();
    }
    
    readonly JsonWriterOptions _writerOptions = new()
    {
        Indented = options.Indented,
        
    };
    
    public string Serialize(Dictionary<string, object> dict)
    {
        using var memoryStream = new MemoryStream();
        using (var jsonWriter = new Utf8JsonWriter(memoryStream, _writerOptions))
        {
            jsonWriter.WriteStartObject();

            foreach (var item in dict)
            {
                var written = new KeyValuePair<string, object>(options.NamingPolicy.ConvertName(item.Key), item.Value);
                WriteItem(jsonWriter, written);
            }

            jsonWriter.WriteEndObject();
            jsonWriter.Flush();
        }

        memoryStream.Seek(0, SeekOrigin.Begin);
            
        using var reader = new StreamReader(memoryStream);
        return reader.ReadToEnd();
    }

    void WriteItem(Utf8JsonWriter writer, KeyValuePair<string, object> item)
    {
        var key = item.Key;
        if (options.CustomTypeWriters.TryGetValue(item.Value.GetType(), out var customWriter))
        {
            customWriter(writer, item);
            return;
        }
        
        switch (item.Value)
        {
            case bool boolValue:
                writer.WriteBoolean(key, boolValue);
                break;
            case byte byteValue:
                writer.WriteNumber(key, byteValue);
                break;
            case sbyte sbyteValue:
                writer.WriteNumber(key, sbyteValue);
                break;
            case char charValue:
                writer.WriteString(key, charValue.ToString());
                break;
            case decimal decimalValue:
                writer.WriteNumber(key, decimalValue);
                break;
            case double doubleValue:
                writer.WriteNumber(key, doubleValue);
                break;
            case float floatValue:
                writer.WriteNumber(key, floatValue);
                break;
            case int intValue:
                writer.WriteNumber(key, intValue);
                break;
            case uint uintValue:
                writer.WriteNumber(key, uintValue);
                break;
            case long longValue:
                writer.WriteNumber(key, longValue);
                break;
            case ulong ulongValue:
                writer.WriteNumber(key, ulongValue);
                break;
            case short shortValue:
                writer.WriteNumber(key, shortValue);
                break;
            case ushort ushortValue:
                writer.WriteNumber(key, ushortValue);
                break;
            case null:
                writer.WriteNull(key);
                break;
            case string strValue:
                writer.WriteString(key, strValue);
                break;
            default:
                this.WriteUnrecognized(writer, item);
                break;
        }
    }

    public void WriteUnrecognized(Utf8JsonWriter writer, KeyValuePair<string, object> item)
    {
        string? json;
        
        try
        {
           var serializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = options.NamingPolicy,
                DictionaryKeyPolicy = options.NamingPolicy,
                Converters = { new ExceptionConverter() },

            };
            if (item.Value is Exception)
            {
                json = JsonSerializer.Serialize<Exception>(item.Value as Exception, serializerOptions);
            }
            else
            {
                json = JsonSerializer.Serialize(item.Value,serializerOptions);
            }      
        }
        catch
        {
            writer.WriteString(item.Key, ToInvariantString(item.Value));
            return;
        }
        
        writer.WritePropertyName(item.Key);
        writer.WriteRawValue(json);
    }
    
    
    static string? ToInvariantString(object? obj) => Convert.ToString(obj, CultureInfo.InvariantCulture);


    internal class ExceptionConverter : JsonConverter<Exception>
    {
       public override Exception Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
       {
           throw new NotImplementedException("Not implemented for reading");
       }  

       public override void Write(Utf8JsonWriter writer, Exception value, JsonSerializerOptions options)
       {
           writer.WriteStartObject();
           writer.WriteString("Message", value.Message);
           writer.WriteString("StackTrace", value.StackTrace);
           writer.WriteString("Source", value.Source);
           writer.WriteEndObject();
       }
   }
    
}
