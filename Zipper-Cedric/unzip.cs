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
        /// return encoded data and tree top from the zipfile
        /// </summary> 
        /// <algo> 
        /// rebuild tree with empty string and when 
        /// string is to short while rebuilding tree 
        /// consume extra byte to lengthen string
        /// 
        /// 
        /// start with an empty string
        /// consume the first byte into bits to the string
        /// save the byte that you ate so you know where you are in the bytes[]
        /// when you ate bit from the string a
        /// 0 : go left then right
        /// 1 : make leaf and consume next 8 bits
        /// 
        /// when there arn't enought bits eat the next byte
        /// until you rebuild the tree
        /// 
        /// the final bytes in the array will be the encoded data
        /// return the encoded data and the top of the tree
        /// </algo>
        internal static (byte[] encode, node top) xrestoreTreeGetEncode(byte[] zipfile)
        {
            string t = "";
            int p = 0;
            foreach (byte b in zipfile)
            {
                t += s.convertByte2String(b);
                node top;
                try
                {
                    top = rebuildTree(t, p);
                    p += 8;
                }
                catch (Exception)
                {
                    continue;
                }
 
                int treeByte = (p+7) / 8;
                byte[] encode = zipfile.Skip(treeByte).ToArray();
                return (encode, top);
            }
            return (null, null);
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
        private static node rebuildTree(string t, int P)
        {
            char bit = t[P];
            P++;

            if (bit == '1')
            {
                byte b = Convert.ToByte(t.Substring(P, 8), 2);
                return new node(0, b);
            }
            else
            {
                node n = new node(0, 0);
                n.L = rebuildTree(t, P);
                n.R = rebuildTree(t, P);
                return n;
            }
        }
        /// <summary>
        /// Restore the file with the top of the tree
        /// and walk down it untill you have the tree
        /// </summary>
        /// <algo>
        /// make the byte[] encode into a bit string 
        /// minus the last place(This is padding)
        /// make a list to hold the bytes for now
        /// Start at the top of the tree
        /// Consume a bit untill you reach a leaf
        /// when in a leaf make the byte and put it in the byte[]
        /// confert the list<byte> to an byte[]
        /// return the byte[]
        /// </algo>
        internal static byte[] restoreFile(byte[] encode, node top)
        {
            byte padding = encode[encode.Length - 1];
            byte[] trimmed = encode.Take(encode.Length - 1).ToArray();
            string bits = string.Join("", trimmed.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
            string realBits = bits.Substring(0, bits.Length - padding);

            List<byte> data = new List<byte>();
            node cur = top;
            foreach (char b in realBits)
            {
                if (b == '1')
                {
                    cur = cur.L;
                }
                else
                {
                    cur = cur.R;
                }
                if (cur.L == null)
                {
                    data.Add(cur.B);
                    cur = top;
                }
            }
            return data.ToArray();
        }

        /// <summary>
        /// writes the data to a file
        /// </summary>
        internal static void saveFile(byte[] orgfile)
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                    return;

                using (var stream = System.IO.File.Create(dialog.FileName))
                {
                    foreach (byte b in orgfile)
                    {
                        stream.WriteByte(b);
                    }
                }
            }
        }
    }
}