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
        /// opens the file sends the tree and data out
        /// </summary>
        internal static void OpenCompressedFile(out byte[] tree, out byte[] data)
        {
            tree = null;
            data = null;

            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                    return;

                byte[] all = System.IO.File.ReadAllBytes(dialog.FileName);
                int treeLength = BitConverter.ToInt32(all, 0);

                tree = new byte[treeLength];
                Array.Copy(all, 4, tree, 0, treeLength);

                data = new byte[all.Length - 4 - treeLength];
                Array.Copy(all, 4 + treeLength, data, 0, data.Length);
            }
        }

        /// <summary>
        /// Rebuilds the Huffman tree from the format written by save().
        /// </summary>
        /// <algo>
        /// where we are in the bit string while reading
        /// mirror of save()'s Walk: read one marker bit.
        /// "1" -> read the next 8 bits as a leaf's byte value.
        /// "0" -> it's an internal node, so read its left child then its right child.
        /// </algo>
        internal static node loadTree(byte[] data)
        {
            int padding = data[0];
            int bitPos = 0;

            int ReadBit()
            {
                int byteIndex = 1 + bitPos / 8;
                int bit = (data[byteIndex] >> (7 - bitPos % 8)) & 1;
                bitPos++;
                return bit;
            }

            node Walk()
            {
                if (ReadBit() == 1)
                {
                    byte b = 0;
                    for (int i = 0; i < 8; i++)
                        b = (byte)((b << 1) | ReadBit());
                    return new node(0, b);
                }

                node n = new node(0, 0);
                n.L = Walk();
                n.R = Walk();
                return n;
            }

            return Walk();
        }

        /// <summary>
        /// decode the file with data and the root of the tree
        /// </summary>
        /// <algo>
        /// step 1: turn the bytes back into a string of 0's and 1's,
        /// and cut off the padding bits from the end
        /// step 2: single-node tree = only one symbol in the whole file,
        /// so every bit just means "one more of that symbol"
        /// step 3: walk the tree bit by bit, 0 = go left, 1 = go right.
        /// whenever we land on a leaf, that's a decoded byte, so we
        /// remember it and jump back to the root to start the next one
        /// </algo>
        internal static byte[] decode_file(byte[] data, node root)
        {
            int padding = data[0];
            int totalBits = (data.Length - 1) * 8 - padding;

            int ReadBit(int i)
            {
                int byteIndex = 1 + i / 8;
                return (data[byteIndex] >> (7 - i % 8)) & 1;
            }

            List<byte> result = new List<byte>();

            if (root.L == null && root.R == null)
            {
                for (int i = 0; i < totalBits; i++)
                    result.Add((byte)root.B);
                return result.ToArray();
            }

            node cur = root;
            for (int i = 0; i < totalBits; i++)
            {
                cur = ReadBit(i) == 1 ? cur.L : cur.R;
                if (cur.L == null && cur.R == null)
                {
                    result.Add((byte)cur.B);
                    cur = root;
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Writes a plain byte array to a file the user picks.
        /// </summary>
        internal static void SaveFile(byte[] data)
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
                if (dialog.ShowDialog() == DialogResult.OK)
                    System.IO.File.WriteAllBytes(dialog.FileName, data);
        }

    }
}
