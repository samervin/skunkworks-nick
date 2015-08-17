using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Threading;
using System.Diagnostics;
using System.Windows.Threading;
using System.IO;

namespace Nick
{
    public class TestRun
    {
        public struct OutputLine
        {
            public String text;
            public String category;
            public OutputLine(String _text)
            {
                this.text = _text;
                this.category = "";
            }
        }
        private MainWindow context;
        private String testPath;
        private String site;
        private bool writeOutput;
        private bool captureDir;
        private String outputDir;

        
        
        

        private List<OutputLine> testOutput;

        private bool running;

        public TestRun(String _testPath, String _site,bool _writeOutput, bool _captureDir, String _outputDir, MainWindow _context)
        {
            this.testPath = _testPath;
            this.site = _site;
            this.writeOutput = _writeOutput;
            this.captureDir = _captureDir;
            this.outputDir = _outputDir;
            this.context = _context;
            testOutput = new List<OutputLine>();
        }

        public List<OutputLine> getOutput()
        {
            return testOutput;
        }

        public bool isRunning()
        {
            return running;
        }

        volatile Process process;

        public void run(String baseFolder)
        {
            Thread runTestThread = new Thread(new ThreadStart(delegate
            {
                String command = "/C casperjs test " + testPath + " --verbose --ignore-ssl-errors=true --web-security=false --ssl-protocol=any --log-level=debug --includes=include.js --pre=pre.js --site="+site;
                if (captureDir)
                {
                    command += " --screenCaptureDir=" + outputDir;
                }
                ProcessStartInfo startInfo = new ProcessStartInfo("cmd.exe")
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WorkingDirectory = baseFolder,
                    Arguments = command
                };


                process = Process.Start(startInfo);
                process.OutputDataReceived += (_sender, _e) => appendTestOutput(_e.Data);
                process.BeginOutputReadLine();

                process.Start();
                process.WaitForExit();
                running = false;

                // Test has completed, time to write to file
                if(writeOutput)
                {
                    DateTime now = DateTime.Now;
                    String filename = Path.GetFileNameWithoutExtension(testPath)
                                      + now.ToString("yyMMdd_HHmmss") + ".txt";
                    String textOutputDir = Path.Combine(outputDir, "Test Output");
                    if (!Directory.Exists(textOutputDir))
                    {
                        Directory.CreateDirectory(textOutputDir);
                    }
                    TextWriter output = new StreamWriter(Path.Combine(textOutputDir, filename));
                    foreach (OutputLine line in testOutput)
                    {
                        output.WriteLine(line.text);
                    }
                    output.Close();
                }

                // TODO: Take this out. Sometimes the process "exits" before output is finished
                Thread.Sleep(500);

                List<OutputLine> copiedOutput = testOutput.ToList();

                foreach (OutputLine line in copiedOutput)
                {
                    if(line.text.Contains(" executed in "))
                    {
                        if(line.text.Contains("PASS"))
                        {
                            context.markPass(this);
                        }
                        else if(line.text.Contains("FAIL"))
                        {
                            context.markFail(this);
                        }
                    }
                }
            }));
            runTestThread.Start();
            running = true;
        }

        public void Kill()
        {
            this.process.Kill();
            this.process.WaitForExit();
        }

        private void appendTestOutput(String text) {
            if (text == null)
                return;
            // Is this the final line? oh my god please change this conditional
            else if (text.Contains("executed in"))
            {
                if(running)
                    process.Kill();
            }

            // Thread-safe
            context.Dispatcher.Invoke((Action)(() =>
            {
                
                OutputLine output = new OutputLine(text);
                if (text.Contains("PASS"))
                {
                    output.category = "PASS";
                }
                else if (text.Contains("FAIL"))
                {
                    output.category = "FAIL";
                }
                else if (text.Contains("[debug]"))
                {
                    output.category = "DEBUG";
                }
                else if (text.Contains("[warning]"))
                {
                    output.category = "WARNING";
                }
                else if (text.Contains("[info]"))
                {
                    output.category = "INFO";
                }
                else
                {
                    output.category = "OTHER";
                }
                testOutput.Add(output);
                if(context.getActiveTest() == this)
                {
                    context.print(output);
                }
            }));
        }
        
    }
}
