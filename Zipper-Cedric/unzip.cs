using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Zipper_Cedric
{
    internal class unzip
    {


        /// <summary>
        /// return encoded data and tree top from the zipfile
        /// </summary>
        /// <algo>
        /// Read the first min(300,zipfile.Length) bytes and convert to bits
        /// 
        /// restore tree and register to what position in the string (or byte[]) you got.
        /// </algo> 
        internal static (byte[] encode, node top) restoreTreeGetEncode(byte[] zipfile)
        {
            int length = Math.Min(300, zipfile.Length);
            string t = "";
            for (int i = 0; i < length; i++)
            {
                t += s.convertByte2String(zipfile[i]);
            }

            int p = 0;
            node top = rebuildTree(t, ref p);
            int f = (p + 7) / 8;
            byte[] encode = new byte[zipfile.Length - f];
            Array.Copy(zipfile, f, encode, 0, encode.Length);
            return (encode, top);
        }


        /// <summary>
        /// rebuild the tree with the string
        /// </summary>
        /// <algo>
        /// eat a bit and make coresponding:
        /// 1 make leaf (consume 8 bits and fill leaf with byte)
        /// 0 make non leaf (Go left then go right)
        /// and go to the right for the next leaf
        /// </algo>
        private static node rebuildTree(string t, ref int P)
        {
            char bit = t[P];
            P++;

            if (bit == '1')
            {
                byte b = Convert.ToByte(t.Substring(P, 8), 2);
                P += 8;
                return new node(0, b);
            }
            else
            {
                node n = new node(0, 0);
                n.L = rebuildTree(t, ref P);
                n.R = rebuildTree(t, ref P);
                return n;
            }
        }

        internal static byte[] restoreFile(byte[] encode, node top)
        {
            throw new NotImplementedException();

        }


        internal static void saveFile(byte[] orgfile)
        {
            throw new NotImplementedException();
        }
    }
}
