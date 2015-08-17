using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.IO;
using Path = System.IO.Path;
using System.Diagnostics;

namespace Nick
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string settingsPath = "settings.xml";

        private readonly List<String> _folders = new List<String> {"FuncTests", "Alyx3Tests", "HeimdallTests"};
        private List<TestFolder> _testFolders = new List<TestFolder>();
        private int selectedIndex = 0;

        //private String outputDir = String.Empty;

        List<String> selectedTests = new List<String>();

        List<TestRun> activeTests = new List<TestRun>();
        int activeTestIndex = -1;

        //private List<String> categoriesEnabled = new List<String>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            loadBasicSettings();
            if (nickSettings.skywalkerPath != null)
            {
                RefreshTestButton.IsEnabled = true;
                FolderDropDown.IsEnabled = true;
                setUpSkywalker();
            }
            else
            {
                FindSkywalker_Click(null, null);
            }

            loadSettings();
            TreeViewStateManager.readState(ref TestTreeView, _folders[selectedIndex] + "_TreeViewState.txt");
        }

        private Settings nickSettings = new Settings();


        private void loadBasicSettings()
        {
            if (File.Exists(settingsPath))
            {
                nickSettings = nickSettings.Read(settingsPath);
            }
            if (nickSettings.fileCaptureStatus == true)
            {
                cbWriteOutput.IsChecked = true;
            }
            if (nickSettings.screenshotCaptureStatus == true)
            {
                cbCaptureScreenshots.IsChecked = true;
            }
            if (nickSettings.capturePath != null)
            {
                tbCaptureDirectory.Text = nickSettings.capturePath;
            }
        }

        private void loadSettings()
        {
            if (nickSettings.funcTestsSite != null)
            {
                tbThorSite.Text = nickSettings.funcTestsSite;
            }
            if (nickSettings.alyx3TestsSite != null)
            {
                tbAlyx3Site.Text = nickSettings.alyx3TestsSite;
            }
            if (nickSettings.heimdallTestSite != null)
            {
                tbHeimdallSite.Text = nickSettings.heimdallTestSite;
            }
            if (!nickSettings.filters.Contains("PASS"))
            {
                cbPass.IsChecked = false;
            }
            if (!nickSettings.filters.Contains("FAIL"))
            {
                cbFail.IsChecked = false;
            }
            if (!nickSettings.filters.Contains("DEBUG"))
            {
                cbDebug.IsChecked = false;
            }
            if (!nickSettings.filters.Contains("WARNING"))
            {
                cbWarning.IsChecked = false;
            }
            if (!nickSettings.filters.Contains("INFO"))
            {
                cbInfo.IsChecked = false;
            }
            if (!nickSettings.filters.Contains("OTHER"))
            {
                cbOther.IsChecked = false;
            }

            // Prod/stag/thor selection
            if (nickSettings.funcTestsOption == "Prod")
            {
                rbProd.IsChecked = true;
            }
            else if (nickSettings.funcTestsOption == "Stag")
            {
                rbStag.IsChecked = true;
            }
            else if (nickSettings.funcTestsOption == "Thor")
            {
                rbThor.IsChecked = true;
                tbThorSite.IsEnabled = true;
            }
        }

        private void saveSettings()
        {
            nickSettings.Save(settingsPath);
        }

        private void defineTestFolders(String skywalkerPath)
        {
            if(_testFolders.Count == 0)
            {
                _testFolders.Add(new TestFolder(Path.Combine(skywalkerPath, "FuncTests", "tests"), "FuncTests", FuncTestsOptions));
                _testFolders.Add(new TestFolder(Path.Combine(skywalkerPath, "Alyx3Tests", "tests"), "Alyx3Tests", Alyx3Options));
                _testFolders.Add(new TestFolder(Path.Combine(skywalkerPath, "HeimdallTests", "tests"), "HeimdallTests", HeimdallOptions));
            }
        }

        private void ListDirectory(TreeView treeView, string path)
        {
            treeView.Items.Clear();
            var rootDirectoryInfo = new DirectoryInfo(path);

            // Don't add the root folder
            foreach (var directory in rootDirectoryInfo.GetDirectories()) {
                treeView.Items.Add(CreateDirectoryNode(directory));
            }
        }

        private TreeViewItem CreateDirectoryNode(DirectoryInfo directoryInfo)
        {
            var directoryNode = new TreeViewItem { Header = directoryInfo.Name };
            foreach (var directory in directoryInfo.GetDirectories())
                directoryNode.Items.Add(CreateDirectoryNode(directory));

            foreach (var file in directoryInfo.GetFiles())
            {
                if (file.Extension == ".js")
                {
                    CheckBox c = new CheckBox { Content = file.Name };
                    c.Margin = new Thickness(-16, 0, 0, 0);
                    ContextMenu cmenu = new ContextMenu();
                    
                    MenuItem editTest = new MenuItem()
                    {
                        Header = "Edit File"
                    };
                    editTest.Click += delegate
                    {
                        System.Diagnostics.Process.Start(file.FullName);
                    };
                    cmenu.Items.Add(editTest);

                    MenuItem openContaining = new MenuItem()
                    {
                        Header = "Open Containing Folder"
                    };
                    openContaining.Click += delegate
                    {
                        System.Diagnostics.Process.Start(file.Directory.FullName);
                    };
                    cmenu.Items.Add(openContaining);


                    c.ContextMenu = cmenu;
                    c.Checked += delegate
                    {
                        selectedTests.Add(file.FullName);
                    };
                    c.Unchecked += delegate
                    {
                        selectedTests.Remove(file.FullName);
                    };

                    c.Margin = new Thickness(0, 3, 0, 0);
                    directoryNode.Items.Add(c);
                }
            }
            return directoryNode;
        }

        private void FindSkywalker_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "Please select your \"skywalker\" directory:";
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if(result == System.Windows.Forms.DialogResult.OK)
            {
                nickSettings.skywalkerPath = dialog.SelectedPath;
                RefreshTestButton.IsEnabled = true;
                FolderDropDown.IsEnabled = true;
                setUpSkywalker();
            }
        }

        private void setUpSkywalker()
        {
            defineTestFolders(nickSettings.skywalkerPath);
            ListDirectory(TestTreeView, _testFolders[0].path);
            FolderDropDown.ItemsSource = _folders;
            FolderDropDown.SelectedIndex = 0;
        }

        public TestRun getActiveTest()
        {
            if (activeTestIndex == -1)
                return null;
            return activeTests[activeTestIndex];
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void FolderDropDown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int newSelectedIndex = _folders.IndexOf(e.AddedItems[0].ToString());
            if (newSelectedIndex == selectedIndex)
                return;

            TreeViewStateManager.writeState(TestTreeView, _folders[selectedIndex] + "_TreeViewState.txt");

            selectedIndex = newSelectedIndex;

            //Save TreeView state
            


            
            ListDirectory(TestTreeView, _testFolders[selectedIndex].path);
            TreeViewStateManager.readState(ref TestTreeView, _folders[selectedIndex] + "_TreeViewState.txt");
            for (int i = 0; i < _testFolders.Count; i++)
            {
                if (i == selectedIndex)
                {
                    _testFolders[i].options.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    _testFolders[i].options.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
            selectedTests = new List<String>();
        }

        private void Site_Selection_Changed(object sender, RoutedEventArgs e)
        {
            RadioButton selected = (sender as RadioButton);
            nickSettings.funcTestsOption = selected.Content.ToString();
            if (selected == rbProd)
            {
                var result = MessageBox.Show("Are you sure you want to test on production?", "Testing on Prod", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Yes)
                {
                    tbThorSite.IsEnabled = false;
                    _testFolders[0].selectedSite = "http://www.hudl.com";
                }
                else
                {
                    rbThor.IsChecked = true;
                    tbThorSite.IsEnabled = true;
                    _testFolders[0].selectedSite = "http://" + tbThorSite.Text + ".thorhudl.com";
                }
            }
            else if(selected == rbStag) {
                var result = MessageBox.Show("Are you sure you want to test on stage?", "Testing on Stag", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Yes)
                {
                    tbThorSite.IsEnabled = false;
                    _testFolders[0].selectedSite = "http://www.staghudl.com";
                }
                else
                {
                    rbThor.IsChecked = true;
                    tbThorSite.IsEnabled = true;
                    _testFolders[0].selectedSite = "http://" + tbThorSite.Text + ".thorhudl.com";
                }
            }
            else {
                tbThorSite.IsEnabled = true;
                _testFolders[0].selectedSite = "http://" + tbThorSite.Text + ".thorhudl.com";
            }
        }

        private void tbThorSite_TextChanged(object sender, TextChangedEventArgs e)
        {
            _testFolders[0].selectedSite = "http://" + tbThorSite.Text + ".thorhudl.com";
            tbThorSite.ToolTip = tbThorSite.Text;
            nickSettings.funcTestsSite = tbThorSite.Text;
        }

        private void tbAlyx3Site_TextChanged(object sender, TextChangedEventArgs e)
        {
            _testFolders[1].selectedSite = tbAlyx3Site.Text;
            tbAlyx3Site.ToolTip = tbAlyx3Site.Text;
            nickSettings.alyx3TestsSite = tbAlyx3Site.Text;
        }

        private void tbHeimdallSite_TextChanged(object sender, TextChangedEventArgs e)
        {
            _testFolders[2].selectedSite = tbHeimdallSite.Text;
            tbHeimdallSite.ToolTip = tbHeimdallSite.Text;
            nickSettings.heimdallTestSite = tbHeimdallSite.Text;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            nickSettings.Save(settingsPath);
            if (selectedTests.Count == 0)
            {
                MessageBox.Show("You need to select at least one test to run!");
                return;
            }
            activeTestIndex = activeTests.Count;
            int currentIndex = activeTestIndex;
            foreach (String test in selectedTests)
            {
                var testRun = new TestRun(test,_testFolders[selectedIndex].selectedSite, nickSettings.fileCaptureStatus, nickSettings.screenshotCaptureStatus, 
                    nickSettings.capturePath, this);
                testRun.run(Path.Combine(nickSettings.skywalkerPath, _folders[selectedIndex]));
                activeTests.Add(testRun);
                bDeleteActiveTests.IsEnabled = true;
                bDeleteAllTests.IsEnabled = true;


                ListViewItem l = createRunningTestItem(Path.GetFileName(test), _testFolders[selectedIndex].selectedSite);
                l.Selected += delegate
                {
                    activeTestIndex = TestRunList.Items.IndexOf(l);
                    // refresh?
                    refreshOutput();
                };
                TestRunList.Items.Add(l);
                currentIndex++;
            }

            ((ListViewItem)TestRunList.Items[activeTestIndex]).IsSelected = true;
        }

        private void Filter_Checked(object sender, RoutedEventArgs e)
        {
            String category = (sender as CheckBox).Content.ToString();
            nickSettings.filters.Add(category);
            refreshOutput();
        }

        private void Filter_Unchecked(object sender, RoutedEventArgs e)
        {
            String category = (sender as CheckBox).Content.ToString();
            nickSettings.filters.Remove(category);
            refreshOutput();
        }

        public void print(TestRun.OutputLine o)
        {
            TextRange rangeOfText1 = new TextRange(ConsoleOutput.Document.ContentEnd, ConsoleOutput.Document.ContentEnd);
            rangeOfText1.Text = o.text + "\r";
            if (o.category == "PASS")
            {
                rangeOfText1.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.LimeGreen);
                rangeOfText1.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
            }
            else if (o.category == "FAIL")
            {
                rangeOfText1.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Red);
                rangeOfText1.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
            }
            else if (o.category == "DEBUG")
            {
                rangeOfText1.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.LightBlue);
                rangeOfText1.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);
            }
            else if (o.category == "WARNING")
            {
                rangeOfText1.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Orange);
                rangeOfText1.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);
            }
            else if (o.category == "OTHER" || o.category == "INFO")
            {
                rangeOfText1.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.White);
                rangeOfText1.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);
            }
            ConsoleOutput.ScrollToEnd();
        }

        private void clearOutput()
        {
            ConsoleOutput.Document.Blocks.Clear();
        }

        public void addCategoryFilter(String filter)
        {
            nickSettings.filters.Add(filter);
        }

        public void removeCategoryFilter(String filter)
        {
            nickSettings.filters.Remove(filter);
        }

        public void refreshOutput()
        {
            if (activeTestIndex == -1)
                return;

            clearOutput();
            foreach (TestRun.OutputLine o in activeTests[activeTestIndex].getOutput())
            {
                if (nickSettings.filters.Contains(o.category))
                {
                    print(o);
                }
            }
        }

        public ListViewItem createRunningTestItem(String name, String site)
        {
            ListViewItem item = new ListViewItem()
            {
                Height = 44
            };

            Grid child = new Grid()
            {
                Height = 44,
                Width = 190,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
                ToolTip = name+" ("+site+")",
                Margin = new Thickness(-2,0,0,0)
            };

            Grid stripe = new Grid()
            {
                Width = 10,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
                Background = Brushes.Gray
            };

            Label testRunName = new Label()
            {
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                Content = name,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(10,0,0,0)
            };

            Label testRunEnv = new Label()
            {
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = System.Windows.VerticalAlignment.Bottom,
                Content = site,
                Margin = new Thickness(10,0,0,0)
            };

            //<Image Source="/Nick;component/Resources/delete.png" Height="20" Width="14" HorizontalAlignment="Right" Margin="0,0,2,2" Cursor="Hand"/>
            /*
            Image deleteButton = new Image()
            {
                Height = 20,
                Width = 14,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 2, 2),
                Cursor = Cursors.Hand
            };
            deleteButton.Source = new BitmapImage(new Uri("pack://application:,,/Nick;component/Resources/delete.png"));
            */
            child.Children.Add(stripe);
            child.Children.Add(testRunName);
            child.Children.Add(testRunEnv);
            //child.Children.Add(deleteButton);
            item.Content = child;

            return item;
        }

        private void deleteActiveTest(int testIndex)
        {
            activeTests.RemoveAt(testIndex);
            TestRunList.Items.RemoveAt(testIndex);
            clearOutput();
            foreach(ListViewItem l in TestRunList.Items)
            {
                l.IsSelected = false;
            }
            activeTestIndex = -1;

            if (testListHasRunningTests())
            {
                bDeleteActiveTests.IsEnabled = false;
            }
            if (isTestListEmpty())
            {
                bDeleteAllTests.IsEnabled = false;
            }
        }

        private Boolean isTestListEmpty()
        {
            return activeTests.Count == 0;
        }

        private Boolean testListHasRunningTests()
        {
            Boolean isRunningTests = false;
            foreach(var c in activeTests)
            {
                if (c.isRunning())
                    isRunningTests = true;
            }

            return isRunningTests;
        }

        private void TestRunList_KeyUp(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Delete && activeTestIndex != -1)
            {
                deleteActiveTest(activeTestIndex);
            }
        }

        private void bDeleteActiveTests_Click(object sender, RoutedEventArgs e)
        {
            for(int i = activeTests.Count-1;i > -1;i--)
            {
                if(!activeTests[i].isRunning())
                {
                    deleteActiveTest(i);
                }
            }
        }

        private void RefreshTestButton_Click(object sender, RoutedEventArgs e)
        {
            if (nickSettings.skywalkerPath != null)
            {
                TreeViewStateManager.writeState(TestTreeView, _folders[selectedIndex] + "_TreeViewState.txt");
                setUpSkywalker();
                selectedTests.Clear();
                TreeViewStateManager.readState(ref TestTreeView, _folders[selectedIndex] + "_TreeViewState.txt");
            }
        }

        // Toggle capture directory ON
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            gridOutputDirectory.Visibility = System.Windows.Visibility.Visible;
            nickSettings.screenshotCaptureStatus = true;
        }

        // Toggle capture directory OFF
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            gridOutputDirectory.Visibility = (bool)cbWriteOutput.IsChecked ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
            nickSettings.screenshotCaptureStatus = false;
        }

        // Toggle saving to file ON
        private void CheckBox_Checked_1(object sender, RoutedEventArgs e)
        {
            gridOutputDirectory.Visibility = System.Windows.Visibility.Visible;
            nickSettings.fileCaptureStatus = true;
        }

        // Toggle saving to file OFF
        private void CheckBox_Unchecked_1(object sender, RoutedEventArgs e)
        {
            gridOutputDirectory.Visibility = (bool)cbCaptureScreenshots.IsChecked ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
            nickSettings.fileCaptureStatus = false;
        }

        // Get capture directory w/ folder browser
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            nickSettings.capturePath = dialog.SelectedPath;
            tbCaptureDirectory.Text = nickSettings.capturePath;
            tbCaptureDirectory.ToolTip = nickSettings.capturePath;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            saveSettings();
            TreeViewStateManager.writeState(TestTreeView, _folders[selectedIndex] + "_TreeViewState.txt");
        }

        public void markPass(TestRun test)
        {
            
            int index = activeTests.IndexOf(test);
            this.Dispatcher.Invoke((Action)(() =>
            {
                ListViewItem lvi = (ListViewItem)TestRunList.Items[index];
                ((Grid)((Grid)lvi.Content).Children[0]).Background = Brushes.LimeGreen;
            })); 
        }

        public void markFail(TestRun test)
        {

            int index = activeTests.IndexOf(test);
            this.Dispatcher.Invoke((Action)(() =>
            {
                ListViewItem lvi = (ListViewItem)TestRunList.Items[index];
                ((Grid)((Grid)lvi.Content).Children[0]).Background = Brushes.Red;
            }));
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            for (int i = activeTests.Count - 1; i > -1; i--)
            {
                if(activeTests[i].isRunning())
                {
                    activeTests[i].Kill();
                }

                deleteActiveTest(i);
                
            }
        }

        private void bOpenOutputDir_Click(object sender, RoutedEventArgs e)
        {
            if(Directory.Exists(nickSettings.capturePath))
            {
                Process.Start(nickSettings.capturePath);
            }
            else
            {
                MessageBox.Show("The directory \"" + nickSettings.capturePath + "\" doesn't exist!");
            }
        }
    }
}
