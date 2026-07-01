using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Zipper_Cedric
{
    //all statics
    public class s
    {
        /// <summary>
        /// Opens the file explorer to select a file
        /// </summary>
        internal static byte[] OpenFile()
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
                if (dialog.ShowDialog() == DialogResult.OK)
                    return System.IO.File.ReadAllBytes(dialog.FileName);
            return null;
        }

        /// <summary>
        /// saves the tree with the data
        /// </summary>
        internal static void SaveFileWithTree(byte[] tree, byte[] data)
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    byte[] treeLength = BitConverter.GetBytes(tree.Length);
                    byte[] output = new byte[treeLength.Length + tree.Length + data.Length];

                    treeLength.CopyTo(output, 0);
                    tree.CopyTo(output, treeLength.Length);
                    data.CopyTo(output, treeLength.Length + tree.Length);

                    System.IO.File.WriteAllBytes(dialog.FileName, output);
                }
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

        /// <summary>
        /// opens the file sends the tree and data out
        /// </summary>
        internal static void OpenCompressedFile(out byte[] tree, out byte[] data)
        {
            tree = null;
            data = null;

            using (OpenFileDialog dialog = new OpenFileDialog())
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    byte[] all = System.IO.File.ReadAllBytes(dialog.FileName);
                    int treeLength = BitConverter.ToInt32(all, 0);

                    tree = new byte[treeLength];
                    Array.Copy(all, 4, tree, 0, treeLength);

                    data = new byte[all.Length - 4 - treeLength];
                    Array.Copy(all, 4 + treeLength, data, 0, data.Length);
                }
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
            StringBuilder bits = new StringBuilder();
            for (int i = 1; i < data.Length; i++)
                bits.Append(Convert.ToString(data[i], 2).PadLeft(8, '0'));
            bits.Length -= padding;

            List<byte> result = new List<byte>();

            if (root.L == null && root.R == null)
            {
                for (int i = 0; i < bits.Length; i++)
                    result.Add((byte)root.B);
                return result.ToArray();
            }
            node cur = root;
            for (int i = 0; i < bits.Length; i++)
            {
                cur = bits[i] == '0' ? cur.L : cur.R;
                if (cur.L == null && cur.R == null)
                {
                    result.Add((byte)cur.B);
                    cur = root;
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// walk from top of tree to each leaf
        /// collect 1 for left and 0 for right while descending
        /// </summary>
        /// <algo>
        /// NL : L,R
        /// L  : path found==> fill byte
        /// 
        /// walk every branch: going left adds a "0", going right adds a "1".
        /// once we hit a leaf, that built-up string is the leaf's code
        /// </algo>
        internal static string[] NewCodes(node root)
        {
            string[] codes = new string[256];
            recNewCodes(root, codes,"");
            return codes;
        }

        /// <summary>
        /// fill codes if Leaf reached
        /// add 1 for l and 0 for r
        /// </summary>
        private static void recNewCodes(node n, string[] codes, string s)
        {
            if (n.L == null)
            {
                codes[n.B] = s;  
            }
            else
            {
                recNewCodes(n.L, codes, s + '1');
                recNewCodes(n.R, codes, s + '0');
            }
        }

        /// <summary>
        /// encodes the file with the original file and the codes
        /// </summary>
        /// <algo>
        /// step 1: replace every byte in the file with its Huffman code,
        /// and glue all those little bit-strings into one long string
        /// step 2: pack that long bit-string into real bytes
        /// </algo>
        internal static byte[] encode_file(byte[] file, string[] codes)
        {
            StringBuilder bits = new StringBuilder();
            foreach (byte b in file)
                bits.Append(codes[b]);
            return PackBits(bits);
        }

        /// <summary>
        /// saves the tree so that it can be used for unzipping
        /// </summary>
        /// <algo>
        /// for every node: leaf -> write "1" + its byte value (8 bits).
        /// internal node -> write "0", then do the same for left, then right.
        /// reading it back in that same order is enough to rebuild the tree.
        /// </algo>
        internal static byte[] save(node top)
        {
            StringBuilder bits = new StringBuilder();

            void Walk(node n)
            {
                if (n.L == null && n.R == null)
                {
                    bits.Append('1');
                    bits.Append(Convert.ToString(n.B, 2).PadLeft(8, '0'));
                }
                else
                {
                    bits.Append('0');
                    Walk(n.L);
                    Walk(n.R);
                }
            }

            Walk(top);
            return PackBits(bits);
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
            StringBuilder bits = new StringBuilder();
            for (int i = 1; i < data.Length; i++)
                bits.Append(Convert.ToString(data[i], 2).PadLeft(8, '0'));
            bits.Length -= padding;

            int pos = 0;
            node Walk()
            {
                char marker = bits[pos++];
                if (marker == '1')
                {
                    byte b = Convert.ToByte(bits.ToString(pos, 8), 2);
                    pos += 8;
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
        /// Packs a bit string into bytes, storing the padding-bit count in the first byte.
        /// </summary>
        /// <algo>
        /// step 1: bits only come in groups of 8, so pad the end with
        /// zeros until the length is a multiple of 8, and remember how
        /// many zeros we added (so decode can throw them away later)
        /// step 2: turn every group of 8 characters ("1"/"0") into one real byte
        /// </algo>
        private static byte[] PackBits(StringBuilder bits)
        {
            int padding = (8 - bits.Length % 8) % 8;
            bits.Append('0', padding);

            byte[] result = new byte[1 + bits.Length / 8];
            result[0] = (byte)padding;

            
            for (int i = 0; i < bits.Length; i += 8)
            {
                byte b = 0;
                for (int j = 0; j < 8; j++)
                    if (bits[i + j] == '1')
                        b |= (byte)(1 << (7 - j));
                result[1 + i / 8] = b;
            }

            return result;
        }

        /// <summary>
        /// Counts the how many freqenties there are in file
        /// </summary>
        internal static uint[] CountBytes(byte[] file)
        {
            uint[] freq = new uint[256];
            foreach (byte b in file)
                freq[b]++;
            return freq;
        }

        internal static listStructure makeList(uint[] freq)
        {
            listStructure L = new listStructure();
            for (int i = 0; i < freq.Length; i++)
            {
                if (freq[i] != 0)
                {
                    node n = new node(freq[i], (byte)i);
                    L.addAsc(n);
                }
            }
            return L;
        }
    }

    public class node
    {
        public uint F, B;
        public node P, N, L, R;

        public node(uint f, uint b)
        {
            this.F = f;
            this.B = b;
        }
    }

    public class listStructure
    {
        public node Head,Tail;
        /// <summary>
        /// Doubly-linked list of nodes kept sorted ascending by frequency.
        /// </summary>
        /// <algo>
        /// case 1: list is empty, or n is smaller than everything
        /// in it -> n simply becomes the new first node (Head)
        /// case 2: walk forward from Head until we find the spot
        /// where n still fits in ascending order, then slot it in
        /// between "cur" and whatever came after "cur"
        /// </algo>
        internal void addAsc(node n)
        {
            if (Head == null)
            {
                Head = n;
                Tail = n;
            }
            else
            {
                if (n.F <= Head.F)
                {
                    n.N = Head;
                    Head.P = n;
                    Head = n;
                }
                else
                {
                    if (n.F > Tail.F)
                    {
                        Tail.N = n;
                        n.P = Tail;
                        Tail = n;
                    }
                    else
                    {
                        node c = Head;
                        while (n.F > c.F) c = c.N;
                        n.P = c.P;
                        n.N = c;
                        c.P.N = n;
                        c.P = n;
                    }
                }
            }
        }

        /// <summary>
        /// makes a huffman tree for beter compression
        /// </summary>
        /// <algo>
        /// keep combining the two smallest nodes into one bigger node,
        /// and put that combined node back in the right sorted spot,
        /// until only one node (the root) is left
        /// step 1: take the two smallest nodes off the front
        /// step 2: merge them into one parent node (frequency = sum of both)
        /// step 3: reinsert the parent, same ascending-order logic as addAsc
        /// </algo>
        public node MakeTree(listStructure L)
        {
            node c = Head;

            while (c != null && c.N != null)
            {
                node n = new node(0, 0);
                n.L = c; n.R = c.N;
                n.F = c.F + c.N.F;
                L.addAsc(n);
                c = c.N.N;
            }
            return c;
        }



           
    }
}