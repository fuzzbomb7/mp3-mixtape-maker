using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Mixtape_Maker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            dtaCompilation.DataContext = Compilation;
        }

        // The collection containing the playlist
        ObservableCollection<CompilationFile> Compilation = new ObservableCollection<CompilationFile>();
        
        // The class that represents each file in the playlist
        public class CompilationFile : INotifyPropertyChanged
        {
            public CompilationFile(string setPath, string setFile, string setExt)
            {
                this.Path = setPath;
                this.FileName = setFile;
                this.Extension = setExt;
            }

            // Properties
            private string _fileName;
            private string _oldFileName;

            public string FileName 
            {
                get { return _fileName; }
                set
                {
                    _oldFileName = _fileName;
                    _fileName = value;
                    OnPropertyChanged("FileName");
                    RenameFile();
                }
            }

            private string Path { get;  set; }
            private string Extension { get;  set; }

            // Methods
            private void RenameFile()
            {
                if (_fileName == null || _oldFileName == null || _fileName == string.Empty || _oldFileName == string.Empty) return;
                
                string currentPath = System.IO.Path.Combine(Path, _oldFileName + Extension);
                if (File.Exists(currentPath))
                {
                    string newPath = System.IO.Path.Combine(Path, _fileName + Extension);
                    try
                    {
                        File.Move(currentPath, newPath);
                    }
                    catch(Exception ex)
                    {
                        string msg = "Could not rename file." + "\n" + "Current file: " + currentPath + "\n" + "New file: " + newPath + "\n" + "Exception: " + ex.Message;
                        MessageBox.Show(msg, "File rename error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            public override string ToString()
            {
                string fullPath = System.IO.Path.Combine(Path, FileName) + Extension;
                return (fullPath);
            }

            // Events
            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(string name)
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs(name));
                }
            }
        }

        private void btnAddFiles_Click(object sender, RoutedEventArgs e)
        {
            string[] files = SelectFiles("MP3 Files|*.mp3");
            if (files != null) AddFiles(files);
        }

        private void btnAddPlaylist_Click(object sender, RoutedEventArgs e)
        {
            string[] files = SelectFiles("M3U Playlist|*.m3u");
            if (files != null) AddPlaylist(files);
        }

        public string[] SelectFiles(string fileFilter)
        {
            OpenFileDialog select = new OpenFileDialog();
            select.Filter = fileFilter;
            select.Multiselect = true;
            select.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            select.Title = "Select Files to Add -- Ctrl + Click to Select Multiple Files";
            bool? result = select.ShowDialog();
            if (result == true)
            {
                string[] files = select.FileNames;
                select = null;
                return (files);
            }
            else return null;
        }

        public void AddFiles(string[] files)
        {
            foreach (string file in files)
            {
                if(lstAddList.Items.Contains(file) == false) lstAddList.Items.Add(file);
            }
        }

        public void AddPlaylist(string[] files)
        {
            foreach (string file in files)
            {
                string[] playList = File.ReadAllLines(file);
                string filePath = System.IO.Path.GetFullPath(file);
                string fileDir = filePath.Replace(System.IO.Path.GetFileName(file), "");

                foreach (string song in playList)
                {
                    Regex drive = new Regex(@"([A-Za-z]):\\");
                    string driveMatch = song.Substring(0, 3);
                    string addFile = song;

                    if (drive.IsMatch(driveMatch) == false)
                    {
                        addFile = System.IO.Path.Combine(fileDir, song);
                    }

                    if (File.Exists(addFile) == false)
                    {
                        MessageBox.Show("File " + addFile + "does not exist!", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else lstAddList.Items.Add(addFile);
                }
            }
        }

        private void btnMoveFolder_Click(object sender, RoutedEventArgs e)
        {
            WPFFolderBrowser.WPFFolderBrowserDialog folders = new WPFFolderBrowser.WPFFolderBrowserDialog();
            folders.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            folders.Title = "Select or Create a Folder to Copy Files To";
            
            bool? result = folders.ShowDialog();
            if (result == true)
            {
                string copyFolder = folders.FileName;
                txtCompFolder.Text = copyFolder;

                foreach (object song in lstAddList.Items)
                {
                    string songFileName = System.IO.Path.GetFileName(song.ToString());
                    string newSong = System.IO.Path.Combine(copyFolder, songFileName);
                    if (File.Exists(newSong) == false) File.Copy(song.ToString(), newSong, true);

                    string newSongTitle = System.IO.Path.GetFileNameWithoutExtension(newSong);
                    string extension = System.IO.Path.GetExtension(newSong);

                    CompilationFile addFile = new CompilationFile(copyFolder, newSongTitle, extension);
                    if(Compilation.Contains(addFile) == false) Compilation.Add(addFile);
                }

                lstAddList.Items.Clear();
                tabEditComp.Focus();
            }
        }

        private void lstAddList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                for (int i = lstAddList.Items.Count - 1; i >= 0; i--)
                {
                    if (lstAddList.SelectedItems.Contains(lstAddList.Items[i])) lstAddList.Items.Remove(lstAddList.Items[i]);
                }
            }
        }

        private void btnMoveUp_Click(object sender, RoutedEventArgs e)
        {
            int curIdx = dtaCompilation.SelectedIndex;
            if (curIdx > 0)
            {
                CompilationFile item = Compilation[curIdx];
                Compilation.RemoveAt(curIdx);
                int newIdx = curIdx - 1;
                Compilation.Insert(newIdx, item);
                dtaCompilation.SelectedIndex = newIdx;
            }
        }

        private void btnMoveDown_Click(object sender, RoutedEventArgs e)
        {
            int curIdx = dtaCompilation.SelectedIndex;
            if (curIdx < Compilation.Count - 1)
            {
                CompilationFile item = Compilation[curIdx];
                Compilation.RemoveAt(curIdx);
                int newIdx = curIdx + 1;
                Compilation.Insert(newIdx, item);
                dtaCompilation.SelectedIndex = newIdx;
            }
        }

        private void btnAutoNum_Click(object sender, RoutedEventArgs e)
        {
            for(int i = 0; i < Compilation.Count; i++)
            {
                string fileName;
                Match numFound = Regex.Match(Compilation[i].FileName, @"^\d{1,}");
                if (numFound.Success == true)
                {
                    fileName = Compilation[i].FileName.Remove(numFound.Index, numFound.Length);
                }
                else fileName = Compilation[i].FileName;

                int trackNum = i + 1;
                Compilation[i].FileName = trackNum.ToString("00") + fileName;
            }
        }

        private void btnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            WPFFolderBrowser.WPFFolderBrowserDialog folders = new WPFFolderBrowser.WPFFolderBrowserDialog();
            folders.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            folders.Title = "Select a Folder to Open";

            bool? result = folders.ShowDialog();

            if (result == true)
            {
                string openFolder = folders.FileName;

                string[] files = Directory.GetFiles(openFolder, "*.mp3");
                if (files.Length == 0)
                {
                    MessageBox.Show("No .mp3 files in this folder!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                txtCompFolder.Text = openFolder;
                Compilation.Clear();

                foreach (string file in files)
                {
                    string songTitle = System.IO.Path.GetFileNameWithoutExtension(file);
                    string extension = System.IO.Path.GetExtension(file);

                    CompilationFile addFile = new CompilationFile(openFolder, songTitle, extension);
                    if (Compilation.Contains(addFile) == false) Compilation.Add(addFile);
                }

            }
        }

        private void btnCreatePlaylist_Click(object sender, RoutedEventArgs e)
        {
            string playList = string.Empty;

            foreach (object song in Compilation)
            {
                string file = song.ToString();
                playList += file + "\r\n";
            }

            SaveFileDialog savePlaylist = new SaveFileDialog();
            savePlaylist.DefaultExt = "*.m3u";
            savePlaylist.Filter = "M3U Files|*.m3u";
            bool? save = savePlaylist.ShowDialog();

            if(save == true)
            {
                try
                {
                    File.WriteAllText(savePlaylist.FileName, playList);
                    MessageBox.Show("Playlist saved to " + savePlaylist.FileName, "File Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Could not save playlist file." + "\n" + "Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

        }


    }

    

}
