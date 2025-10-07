/*
 * File: FlexibleStringSerializer.cs
 * Description: Custom BSON serializer that handles both ObjectId and String values
 * Author: System
 * Date: 2025-10-06
 */

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace WebApplication1.Serializers
{
    /// <summary>
    /// Custom serializer that can deserialize both ObjectId and String values as strings
    /// This handles legacy data where userId was stored as ObjectId
    /// </summary>
    public class FlexibleStringSerializer : SerializerBase<string>
    {
        public override string Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonType = context.Reader.GetCurrentBsonType();
            
            switch (bsonType)
            {
                case BsonType.String:
                    return context.Reader.ReadString();
                
                case BsonType.ObjectId:
                    // Convert ObjectId to string
                    var objectId = context.Reader.ReadObjectId();
                    return objectId.ToString();
                
                case BsonType.Null:
                    context.Reader.ReadNull();
                    return string.Empty;
                
                default:
                    throw new NotSupportedException($"Cannot deserialize BsonType {bsonType} to String");
            }
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                context.Writer.WriteNull();
            }
            else
            {
                // Always serialize as string
                context.Writer.WriteString(value);
            }
        }
    }
}
