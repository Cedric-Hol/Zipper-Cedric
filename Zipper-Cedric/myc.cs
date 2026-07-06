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
        /// returns byte array with extra padding in order
        /// to have a multiple of 8 
        /// </summary>
        /// <algo>
        /// make an empty string
        /// read each byte of file and add corresponding code to string
        /// add padding to get a length multiple of 8
        /// convert bits to byte[]
        /// </algo>
        internal static byte[] encode_file(byte[] file, string[] codes)
        {
            string d = "";
            for (int i = 0; i < file.Length; i++)
            {
                d += codes[file[i]];
            }
            int remain = d.Length % 8;
            int padding = 0;
            if (remain != 0)
            {
                padding = 8 - remain;
                for (int i = 0; i < padding; i++)
                {
                    d += "0";
                }
            }
            byte[] result = new byte[d.Length / 8 + 1];
            for (int i = 0; i < d.Length / 8; i++)
            {
                result[i] = Convert.ToByte(d.Substring(i * 8, 8), 2);
            }
            result[result.Length - 1] = (byte)padding;
            return result;
        }

        /// <summary>
        /// with the top of the tree walk down the tree
        /// save what you find to a byte array
        /// </summary>
        /// <algo>
        /// make an empty string
        /// walk down the tree.
        /// 
        /// check if there is any padding needed
        /// convert string to byte[] and return that byte[]
        /// </algo>
        internal static byte[] savetree(node top)
        {
            string t = "";
            string finaleT = walksavetree(top, t);
            int remain = finaleT.Length % 8;
            int padding = 0;
            if (remain != 0)
            {
                padding = 8 - remain;
                for (int i = 0; i < padding; i++)
                {
                    finaleT += "0";
                }
            }
            byte[] result = new byte[finaleT.Length / 8];
            for (int i = 0; i < finaleT.Length / 8; i++)
            {
                result[i] = Convert.ToByte(finaleT.Substring(i * 8, 8), 2);
            }
            return result;
        }
        /// <summary>
        /// The walk function for the savetree
        /// returns the finale string t with all the right bits
        /// </summary>
        /// <algo>
        /// check if the leaf L and R are empty for the recursion.
        /// 
        /// NL(non-leaf): encode as 0 and go Left and thereafter Right
        /// L (leaf)    : encode as 1 and save byte as 8 bits
        /// 
        /// return t as the final string
        /// </algo>
        internal static string walksavetree(node n, string t)
        {
            if (n.L != null)
            {
                t += '0';
                t = walksavetree(n.L, t);
                t = walksavetree(n.R, t);
            }
            else 
            {
                t += '1';
                t += convertByte2String(n.B);
            }
            return t;
        }

        /// <summary>
        /// converts the given byte B to a string
        /// </summary>
        /// <algo>
        /// make temp sting
        /// walk truh the byte and convert them to string
        /// return the temp string
        /// </algo>
        public static string convertByte2String(byte B)
        {
            string temp = "";
            for (int i = 7; i >= 0; i--)
            {
                temp += (char)('0' + ((B >> i) & 1));
            }
            return temp;
        }

        /// <summary>
        /// saves to a file with the tree and the data
        /// </summary>
        /// <algo>
        /// make the two arrays into 1
        /// write each byte to a file
        /// </algo>
        internal static void SaveFileWithTree(byte[] tree, byte[] data)
        {
            byte[] joinedArray = tree.Concat(data).ToArray();
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                    return;

                using (var stream = System.IO.File.Create(dialog.FileName))
                {
                    foreach (byte b in joinedArray)
                    {
                        stream.WriteByte(b);
                    }
                }
            }
        }
    }

    public class node
    {
        public uint F;
        public byte B;
        public node P, N, L, R;

        public node(uint f, byte b)
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