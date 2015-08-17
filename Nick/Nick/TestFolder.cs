using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Controls;

namespace Nick
{
    // Used to differentiate between FuncTests, Alyx3Tests, HeimdallTests
    class TestFolder
    {
        public readonly String path;
        public readonly String name;
        public Grid options;

        public String selectedSite;

        public TestFolder(String _path, String _name, Grid _options)
        {
            this.path = _path;
            this.name = _name;
            this.options = _options;
        }
    }
}
