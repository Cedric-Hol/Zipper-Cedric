using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Zipper_Cedric
{
    public partial class Form1 : Form
    {
        public listStructure list = new listStructure();
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            byte[] file = s.OpenFile();
            uint[] freq = s.CountBytes(file); 
            listStructure L = s.makeList(freq); 
            node top = L.MakeTree(L); 
            string[] codes = s.NewCodes(top);
            byte[] encode = s.encode_file(file, codes);
            byte[] savetree = s.save(top);  
            s.SaveFileWithTree(savetree, encode); 
        }

        private void button2_Click(object sender, EventArgs e)
        {
            s.OpenCompressedFile(out byte[] tree, out byte[] data); //picks a compressed file and splits it into tree bytes + encoded data
            node top = s.loadTree(tree); //rebuilds the Huffman tree
            byte[] decoded = s.decode_file(data, top); //walks the tree bit by bit to recover the original bytes
            s.SaveFile(decoded); //lets the user pick where to save the decompressed file
        }
    }
}
