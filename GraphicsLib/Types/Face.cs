using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GraphicsLib.Types
{
    public class Face
    {
        public int[] vIndices;
        public int[]? tIndices;
        public int[]? nIndices;
        public short MaterialIndex;

        public Face(int[] vIndices, int[]? tIndices, int[]? nIndices, short materialIndex)
        {
            this.vIndices = vIndices;
            this.tIndices = tIndices;
            this.nIndices = nIndices;
            MaterialIndex = materialIndex;
        }

        public override string? ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("f");
            for (int i = 0; i < vIndices.Length; i++)
            {
                stringBuilder.AppendFormat(" {0}",vIndices[i].ToString());
                if (tIndices != null)
                {
                    stringBuilder.AppendFormat("/{0}", tIndices[i]);
                    if(nIndices != null)
                    {
                        stringBuilder.AppendFormat("/{0}", nIndices[i]);
                    }
                } 
                else if (nIndices != null)
                {
                    stringBuilder.AppendFormat("//{0}", nIndices[i]);
                }                 
            }
            return stringBuilder.ToString();
        }
    }
}
