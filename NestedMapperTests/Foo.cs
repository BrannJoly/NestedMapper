using System;

namespace NestedMapperTests
{
    public class NestedType
    {
        public DateTime A { get; set; }
        public string B { get; set; }
    }

    public class Foo
    {
        public int I { get; set; }

        public NestedType N { get; set; }

        public Foo()
        {
            N = new NestedType();
        }
    }
}
