using System;

namespace NestedMapper
{
    class PropertyBasicInfo
    {
        public string Name { get;}
        public Type Type { get; }

        public PropertyBasicInfo(string name, Type type)
        {
            Name = name;
            Type = type;
        }
    }
}