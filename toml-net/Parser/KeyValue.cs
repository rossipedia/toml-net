namespace Toml.Parser
{
    internal class KeyValue
    {
        private readonly string key;

        private readonly object value;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public KeyValue(string key, object value)
        {
            this.key = key;
            this.value = value;
        }

        public string Key
        {
            get { return this.key; }
        }

        public object Value
        {
            get { return this.value; }
        }
    }
}