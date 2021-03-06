﻿
namespace Halforbit.Facets.Attributes
{
    public abstract class FacetParameterAttribute : FacetAttribute
    {
        public FacetParameterAttribute(
            string value = null, 
            string configKey = null)
        {
            Value = value;

            ConfigKey = configKey;
        }

        public abstract string ParameterName { get; }

        public virtual string Value { get; }

        public virtual string ConfigKey { get; }

        public override string ToString()
        {
            return $"{GetType().Namespace}." +
                $"{GetType().Name.Replace("Attribute", "")}" +
                $"({(string.IsNullOrWhiteSpace(Value) ? ConfigKey : Value)})";
        }
    }
}
