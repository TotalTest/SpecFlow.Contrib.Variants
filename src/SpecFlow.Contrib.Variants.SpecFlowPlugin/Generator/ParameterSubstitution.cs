using System.Collections.Generic;

namespace SpecFlow.Contrib.Variants.SpecFlowPlugin.Generator
{
    internal class ParameterSubstitution : List<KeyValuePair<string, string>>
    {
        public void Add(string parameter, string identifier)
        {
            Add(new KeyValuePair<string, string>(parameter.Trim(), identifier));
        }

        public bool TryGetIdentifier(string param, out string id)
        {
            param = param.Trim();
            foreach (KeyValuePair<string, string> keyValuePair in this)
            {
                if (keyValuePair.Key.Equals(param))
                {
                    id = keyValuePair.Value;
                    return true;
                }
            }
            id = null;
            return false;
        }
    }
}