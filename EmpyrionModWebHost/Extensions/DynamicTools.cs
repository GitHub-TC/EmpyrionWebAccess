namespace EmpyrionModWebHost.Extensions
{
    public static class DynamicTools
    {
        public static bool HasProperty(dynamic aObject, string aProperty)
        {
            return ((IDictionary<string, object>)aObject).ContainsKey(aProperty);
        }

        public static T GetProperty<T>(dynamic aObject, string aProperty, T aDefault = default(T))
        {
            return ((IDictionary<string, object>)aObject).TryGetValue(aProperty, out object Result)
                ? (Result is IConvertible ? (T)Convert.ChangeType(Result, typeof(T)) : (T)Result)
                : aDefault;
        }

    }
}
