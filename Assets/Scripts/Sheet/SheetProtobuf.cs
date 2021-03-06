/**
 * Tool generation, do not modify!!!
 */

using ProtoBuf;
using System.IO;
using System.Collections.Generic;

namespace Sheet
{
    public class BaseList
    {
        public void Export(string outFile)
        {
            using (MemoryStream m = new MemoryStream())
            {
                Serializer.Serialize(m, this);
                m.Position = 0;
                int length = (int)m.Length;
                var buffer = new byte[length];
                m.Read(buffer, 0, length);
                File.WriteAllBytes(outFile, buffer);
            }
        }
    }
    
	[ProtoContract]
	public class Example
	{
		[ProtoMember(1)]
		public int exampleInt;
		[ProtoMember(2)]
		public string exampleString;
		[ProtoMember(3)]
		public float exampleFloat;
		[ProtoMember(4)]
		public bool exampleBool;
		[ProtoMember(5)]
		public List<int> exampleArray1 = new List<int>();
		[ProtoMember(6)]
		public List<float> exampleArray2 = new List<float>();
		[ProtoMember(7)]
		public List<string> exampleArray3 = new List<string>();
		[ProtoMember(8)]
		public string exampleClient;
	}

	[ProtoContract]
	public class ExampleList : BaseList
	{
		[ProtoMember(1)]
		public List<Example> Items = new List<Example>();
	}

}
