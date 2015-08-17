using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Controls;
using System.Windows;

using System.IO;

namespace Nick
{
    static class TreeViewStateManager
    {

        private static TextWriter output;
        private static List<String> expanded = new List<String>();

        public static void writeState(TreeView t,String filename)
        {
            using (output = new StreamWriter(filename))
            {
                foreach (var i in t.Items)
                {
                    iterate((TreeViewItem)i);
                }

                output.Close();
            }
        }

        public static void readState(ref TreeView t, String filename)
        {
            if(!File.Exists(filename))
            {
                return;
            }
            expanded = File.ReadAllLines(filename).ToList();
            foreach (var i in t.Items)
            {
                setState((TreeViewItem)i);
            }
        }

        private static void setState(TreeViewItem node)
        {
            node.IsExpanded = expanded.Contains(node.Header.ToString());

            var children = node.Items;
            //rec call
            foreach (var child in children)
            {
                if (child is TreeViewItem)
                {
                    setState((TreeViewItem)child);
                }
            }
        }


        private static void iterate(TreeViewItem node)
        {
            if(node.IsExpanded)
            {
                output.WriteLine(node.Header.ToString());
            }
            var children = node.Items;
            //rec call
            foreach(var child in children)
            {
                if (child is TreeViewItem)
                {
                    iterate((TreeViewItem)child);
                }
            }
        }
    }
}
