using System;
using System.Text.Json;

namespace Voyager.Generation;

public static class JsonDocumentExt
{
	public static T? MaybeGetDeserialize<T>(this JsonDocument document, string propertyName)
	{
		if (document.RootElement.TryGetProperty(propertyName, out var prop))
		{
			return prop.Deserialize<T>();
		}
		return default;
	}

	public static string? MaybeGetString(this JsonDocument document, string propertyName)
	{
		if (document.RootElement.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
		{
			return prop.GetString();
		}
		return default;
	}

	public static string GetString(this JsonDocument document, string propertyName)
	{
		return document.MaybeGetString(propertyName) ?? string.Empty;
	}

	public static bool? MaybeGetBool(this JsonDocument document, string propertyName)
	{
		if (document.RootElement.TryGetProperty(propertyName, out var prop))
		{
			return prop.ValueKind switch
			{
				JsonValueKind.True => true,
				JsonValueKind.False => false,
				_ => null
			};
		}
		return null;
	}

	public static bool GetBool(this JsonDocument document, string propertyName)
	{
		return document.MaybeGetBool(propertyName) ?? default;
	}

	public static byte? MaybeGetByte(this JsonDocument document, string propertyName)
	{
		if (document.RootElement.TryGetProperty(propertyName, out var prop) && prop.TryGetByte(out var @byte))
		{
			return @byte;
		}
		return default;
	}

	public static byte GetByte(this JsonDocument document, string propertyName)
	{
		return document.MaybeGetByte(propertyName) ?? default;
	}

	public static sbyte? MaybeGetSByte(this JsonDocument document, string propertyName)
	{
		if (document.RootElement.TryGetProperty(propertyName, out var prop) && prop.TryGetSByte(out var @byte))
		{
			return @byte;
		}
		return default;
	}

	public static sbyte GetSByte(this JsonDocument document, string propertyName)
	{
		return document.MaybeGetSByte(propertyName) ?? default;
	}

	public static decimal? MaybeGetDecimal(this JsonDocument document, string propertyName)
	{
		if (document.RootElement.TryGetProperty(propertyName, out var prop) && prop.TryGetDecimal(out var number))
		{
			return number;
		}
		return default;
	}

	public static decimal GetDecimal(this JsonDocument document, string propertyName)
	{
		return document.MaybeGetDecimal(propertyName) ?? default;
	}

	public static double? MaybeGetDouble(this JsonDocument document, string propertyName)
	{
		if (document.RootElement.TryGetProperty(propertyName, out var prop) && prop.TryGetDouble(out var number))
		{
			return number;
		}
		return default;
	}

	public static double GetDouble(this JsonDocument document, string propertyName)
	{
		return document.MaybeGetDouble(propertyName) ?? default;
	}

	public static short? MaybeGetInt16(this JsonDocument document, string propertyName)
	{
		if (document.RootElement.TryGetProperty(propertyName, out var prop) && prop.TryGetInt16(out var number))
		{
			return number;
		}
		return default;
	}

	public static short GetInt16(this JsonDocument document, string propertyName)
	{
		return document.MaybeGetInt16(propertyName) ?? default;
	}

	public static int? MaybeGetInt32(this JsonDocument document, string propertyName)
	{
		if (document.RootElement.TryGetProperty(propertyName, out var prop) && prop.TryGetInt32(out var number))
		{
			return number;
		}
		return default;
	}

	public static int GetInt32(this JsonDocument document, string propertyName)
	{
		return document.MaybeGetInt32(propertyName) ?? default;
	}

	public static long? MaybeGetInt64(this JsonDocument document, string propertyName)
	{
		if (document.RootElement.TryGetProperty(propertyName, out var prop) && prop.TryGetInt64(out var number))
		{
			return number;
		}
		return default;
	}

	public static long GetInt64(this JsonDocument document, string propertyName)
	{
		return document.MaybeGetInt64(propertyName) ?? default;
	}

	public static ushort? MaybeGetUInt16(this JsonDocument document, string propertyName)
	{
		if (document.RootElement.TryGetProperty(propertyName, out var prop) && prop.TryGetUInt16(out var number))
		{
			return number;
		}
		return default;
	}

	public static ushort GetUInt16(this JsonDocument document, string propertyName)
	{
		return document.MaybeGetUInt16(propertyName) ?? default;
	}

	public static uint? MaybeGetUInt32(this JsonDocument document, string propertyName)
	{
		if (document.RootElement.TryGetProperty(propertyName, out var prop) && prop.TryGetUInt32(out var number))
		{
			return number;
		}
		return default;
	}

	public static uint GetUInt32(this JsonDocument document, string propertyName)
	{
		return document.MaybeGetUInt32(propertyName) ?? default;
	}

	public static ulong? MaybeGetUInt64(this JsonDocument document, string propertyName)
	{
		if (document.RootElement.TryGetProperty(propertyName, out var prop) && prop.TryGetUInt64(out var number))
		{
			return number;
		}
		return default;
	}

	public static ulong GetUInt64(this JsonDocument document, string propertyName)
	{
		return document.MaybeGetUInt64(propertyName) ?? default;
	}

	public static DateTime? MaybeGetDateTime(this JsonDocument document, string propertyName)
	{
		if (document.RootElement.TryGetProperty(propertyName, out var prop) && prop.TryGetDateTime(out var dateTime))
		{
			return dateTime;
		}
		return default;
	}

	public static DateTime GetDateTime(this JsonDocument document, string propertyName)
	{
		return document.MaybeGetDateTime(propertyName) ?? default;
	}

	public static DateTimeOffset? MaybeGetDateTimeOffset(this JsonDocument document, string propertyName)
	{
		if (document.RootElement.TryGetProperty(propertyName, out var prop) && prop.TryGetDateTimeOffset(out var dateTime))
		{
			return dateTime;
		}
		return default;
	}

	public static DateTimeOffset GetDateTimeOffset(this JsonDocument document, string propertyName)
	{
		return document.MaybeGetDateTimeOffset(propertyName) ?? default;
	}

	public static Guid? MaybeGetGuid(this JsonDocument document, string propertyName)
	{
		if (document.RootElement.TryGetProperty(propertyName, out var prop) && prop.TryGetGuid(out var guid))
		{
			return guid;
		}
		return default;
	}

	public static Guid GetGuid(this JsonDocument document, string propertyName)
	{
		return document.MaybeGetGuid(propertyName) ?? default;
	}

	public static float? MaybeGetSingle(this JsonDocument document, string propertyName)
	{
		if (document.RootElement.TryGetProperty(propertyName, out var prop) && prop.TryGetSingle(out var number))
		{
			return number;
		}
		return default;
	}

	public static float GetSingle(this JsonDocument document, string propertyName)
	{
		return document.MaybeGetSingle(propertyName) ?? default;
	}
}