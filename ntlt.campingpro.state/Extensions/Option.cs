namespace ntlt.campingpro.state.Extensions
{
    public struct Option<T>
    {
        public static Option<T> None => default;
        public static Option<T> Some(T value) => new Option<T>(value);

        private Option(T value)
        {
            Value = value;
            IsSome = true;
        }

        public bool IsSome { get; set; }
        public T Value { get; set; }

        public T ReplaceIfSome(T defaultValue) => IsSome ? Value : defaultValue;
    }
}