using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Serialization;

namespace Nick
{
    public class Settings
    {
        public String skywalkerPath;
        public String funcTestsOption;
        public String funcTestsSite;
        public String alyx3TestsSite;
        public String heimdallTestSite;
        public List<String> filters = new List<String>();
        public Boolean screenshotCaptureStatus;
        public Boolean fileCaptureStatus;
        public String capturePath;

        public void Save(string filename)
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                XmlSerializer xmls = new XmlSerializer(typeof(Settings));
                xmls.Serialize(sw, this);
            }
        }

        public Settings Read(string filename)
        {
            using (StreamReader sw = new StreamReader(filename))
            {
                XmlSerializer xmls = new XmlSerializer(typeof(Settings));
                return xmls.Deserialize(sw) as Settings;
            }
        }
    }
}
