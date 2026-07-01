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
            recNewCodes(root, codes, "");
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
        /// sum up each byte's length to get the total bit count so that its known
        /// write each code's bit to the array
        /// </algo>
        internal static byte[] encode_file(byte[] file, string[] codes)
        {
            long totalBits = 0;
            foreach (byte b in file) totalBits += codes[b].Length;

            int padding = (int)((8 - totalBits % 8) % 8);
            byte[] result = new byte[1 + (totalBits + padding) / 8];
            result[0] = (byte)padding;
            int bitPos = 0;
            foreach (byte b in file)
            {
                string code = codes[b];
                foreach (char c in code)
                {
                    if (c == '1')
                        result[1 + bitPos / 8] |= (byte)(1 << (7 - bitPos % 8));
                    bitPos++;
                }
            }
            return result;
        }

        /// <summary>
        /// saves the tree so that it can be used for unzipping
        /// </summary>
        /// <algo>
        /// count the leaves to get the total bit count so its known
        /// write each node's bit (and leaf's byte value) to the array
        /// </algo>
        internal static byte[] save(node top)
        {
            int leafCount = 0;
            void Count(node n)
            {
                if (n.L == null && n.R == null)
                    leafCount++;
                else
                {
                    Count(n.L);
                    Count(n.R);
                }
            }
            Count(top);

            long totalBits = 9L * leafCount + (leafCount - 1);
            int padding = (int)((8 - totalBits % 8) % 8);
            byte[] result = new byte[1 + (totalBits + padding) / 8];
            result[0] = (byte)padding;

            int bitPos = 0;
            void WriteBit(int bit)
            {
                if (bit == 1)
                    result[1 + bitPos / 8] |= (byte)(1 << (7 - bitPos % 8));
                bitPos++;
            }

            void Walk(node n)
            {
                if (n.L == null && n.R == null)
                {
                    WriteBit(1);
                    for (int i = 7; i >= 0; i--)
                        WriteBit((int)((n.B >> i) & 1));
                }
                else
                {
                    WriteBit(0);
                    Walk(n.L);
                    Walk(n.R);
                }
            }

            Walk(top);
            return result;
        }

        /// <summary>
        /// saves the tree with the data
        /// </summary>
        internal static void SaveFileWithTree(byte[] tree, byte[] data)
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                    return;

                using (var stream = System.IO.File.Create(dialog.FileName))
                {
                    stream.Write(BitConverter.GetBytes(tree.Length), 0, 4);
                    stream.Write(tree, 0, tree.Length);
                    stream.Write(data, 0, data.Length);
                }
            }
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