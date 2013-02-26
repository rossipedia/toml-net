namespace Toml.Parser
{
    using System.Collections.Generic;
    using System.Linq;

    internal class Section
    {
        private readonly string[] path;

        private readonly KeyValue[] values;

        public Section(string[] path, IEnumerable<KeyValue> values)
        {
            this.path = path;
            this.values = (values ?? Enumerable.Empty<KeyValue>()).ToArray();
        }

        public string[] Path
        {
            get { return this.path; }
        }

        public KeyValue[] Values
        {
            get { return this.values; }
        }
    }
}