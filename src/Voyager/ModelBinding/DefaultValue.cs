namespace Voyager.ModelBinding;

public record DefaultValue<TValue>(TValue Value)
{
	public static implicit operator DefaultValue<TValue>(TValue value) => new DefaultValue<TValue>(value);
	public static implicit operator TValue(DefaultValue<TValue> value) => value.Value;
}
