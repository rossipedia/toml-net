namespace Toml.Parser
{
    using System.Collections.Generic;
    using System.Linq;

    internal class Document
    {
        private readonly KeyValue[] rootValues;

        private readonly Section[] sections;

        public Document(IEnumerable<KeyValue> rootValues, IEnumerable<Section> sections)
        {
            this.rootValues = (rootValues ?? Enumerable.Empty<KeyValue>()).ToArray();
            this.sections = (sections ?? Enumerable.Empty<Section>()).ToArray();
        }

        public KeyValue[] RootValues
        {
            get { return this.rootValues; }
        }

        public Section[] Sections
        {
            get { return this.sections; }
        }
    }
}
