using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Voyager.UnitTests.Models
{
	public struct Id<TypeId> : IComparable<Id<TypeId>>, IEquatable<Id<TypeId>>
	{
		public static readonly Id<TypeId> Empty = new Id<TypeId>(Guid.Empty);

		public Id(Guid value)
		{
			Guid = value;
		}

		public Id(string id)
		{
			if (TryParse(id, out var typedId))
			{
				Guid = typedId.Guid;
			}
			else
			{
				throw new Exception($"Expected id for {GetPrefix()}, but got {id}.");
			}
		}

		public Guid Guid { get; }

		public static Id<TypeId>? From(Guid? guid)
		{
			if (guid.HasValue)
			{
				return new Id<TypeId>(guid.Value);
			}
			return null;
		}

		public static implicit operator Id<TypeId>(string id)
		{
			return Parse(id);
		}

		public static Id<TypeId> New() => new Id<TypeId>(Guid.NewGuid());

		public static bool operator !=(Id<TypeId> a, Id<TypeId> b) => !(a == b);

		public static bool operator ==(Id<TypeId> a, Id<TypeId> b) => a.CompareTo(b) == 0;

		public static Id<TypeId> Parse(string id)
		{
			return new Id<TypeId>(id);
		}

		public static Id<TypeId>? ParseMaybe(string id)
		{
			if (!string.IsNullOrWhiteSpace(id) && TryParse(id, out var parsedId))
			{
				return parsedId;
			}
			return null;
		}

		public static bool TryParse(string id, out Id<TypeId> typedId)
		{
			if (id != null && id.StartsWith($"{GetPrefix()}."))
			{
				if (Guid.TryParse(id.Substring(GetPrefix().Length + 1), out var guid))
				{
					typedId = new Id<TypeId>(guid);
					return true;
				}
			}
			typedId = Empty;
			return false;
		}

		public int CompareTo(Id<TypeId> other) => Guid.CompareTo(other.Guid);

		public bool Equals(Id<TypeId> other) => Guid.Equals(other.Guid);

		public override bool Equals(object obj)
		{
			if (obj is null)
			{
				return false;
			}
			return obj is Id<TypeId> other && Equals(other);
		}

		public override int GetHashCode() => (GetPrefix(), Guid).GetHashCode();

		public override string ToString() => $"{GetPrefix()}.{Guid}";

		private static string GetPrefix()
		{
			return typeof(TypeId).Name.ToLower();
		}
	}

	public class NullableStrongIdValueConverter<IdType> : JsonConverter<Id<IdType>?>
	{
		public override Id<IdType>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.String)
			{
				return Id<IdType>.ParseMaybe(reader.GetString());
			}
			return null;
		}

		public override void Write(Utf8JsonWriter writer, Id<IdType>? value, JsonSerializerOptions options)
		{
			if (value.HasValue)
			{
				writer.WriteStringValue(value.Value.ToString());
			}
			else
			{
				writer.WriteNullValue();
			}
		}
	}

	public class StrongIdValueConverter<IdType> : JsonConverter<Id<IdType>>
	{
		public override Id<IdType> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.String)
			{
				return new Id<IdType>(reader.GetString());
			}
			throw new JsonException();
		}

		public override void Write(Utf8JsonWriter writer, Id<IdType> value, JsonSerializerOptions options)
		{
			writer.WriteStringValue(value.ToString());
		}
	}

	public class StrongIdValueConverterFactory : JsonConverterFactory
	{
		public override bool CanConvert(Type typeToConvert)
		{
			var type = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;
			if (type.IsGenericType && typeof(Id<>) == type.GetGenericTypeDefinition())
			{
				return true;
			}
			return false;
		}

		public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
		{
			var type = Nullable.GetUnderlyingType(typeToConvert);
			if (type == null)
			{
				type = typeToConvert;
				return (JsonConverter)Activator.CreateInstance(typeof(StrongIdValueConverter<>).MakeGenericType(type.GetGenericArguments()[0]));
			}
			else
			{
				return (JsonConverter)Activator.CreateInstance(typeof(NullableStrongIdValueConverter<>).MakeGenericType(type.GetGenericArguments()[0]));
			}
		}
	}
}