using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using MSAPI = Microsoft.WindowsAPICodePack;


namespace MakeobjControllerLite
{
    /// <summary>
    /// Window1.xaml の相互作用ロジック
    /// </summary>
    public partial class Window1 : Window
    {
        public string tmp_pak = Properties.Settings.Default.defaultSavePath;
        public string tmp_file = Properties.Settings.Default.defaultSavePath_etc;
        public bool tmp_sve = Properties.Settings.Default.saveFilePath;
        public bool tmp_projectPak = Properties.Settings.Default.ahpAndRun;
        public bool tmp_useProjectDir = Properties.Settings.Default.useProjectDirectory;
        public bool tmp_projectPak_Arg = Properties.Settings.Default.ahpAndRunOnlyEnv;
        public bool tmp_useAbsolutePath = Properties.Settings.Default.ahpAbsolutePath;
        public Window1()
        {
            InitializeComponent();
            folderlabel.Text = tmp_file;
            switch (tmp_pak)
            {
                case "pak_dir_dat":
                    pak_dir_dat.IsChecked = true;
                    break;
                case "pak_dir_makeobj":
                    pak_dir_makeobj.IsChecked = true;
                    break;
                case "pak_dir_etc":
                    pak_dir_etc.IsChecked = true;
                    folderlabel.IsEnabled = true;
                    setdir.IsEnabled = true;
                    break;
            }
            if (tmp_sve)
            {
                sve_true.IsChecked = true;
            }
            else
            {
                sve_false.IsChecked = true;
            }
            if (tmp_projectPak_Arg) {
                project_load_pak_sys.IsChecked = true;
            }
            else if (tmp_projectPak)
            {
                project_load_pak_true.IsChecked = true;
            }
            else
            {
                project_load_pak_false.IsChecked = true;
            }
            project_absolute_path.IsChecked = tmp_useAbsolutePath;
            pak_useProjectDirectory.IsChecked = tmp_useProjectDir;
        }

        //保存
        private void OnSaveButtonClick(object sender, RoutedEventArgs e)
        {
            if (!tmp_file.EndsWith("\\"))
            {
                tmp_file = tmp_file + "\\";
            }
            Properties.Settings.Default.defaultSavePath = tmp_pak;
            Properties.Settings.Default.defaultSavePath_etc = tmp_file;
            Properties.Settings.Default.saveFilePath = tmp_sve;
            Properties.Settings.Default.ahpAndRun = tmp_projectPak;
            Properties.Settings.Default.ahpAndRunOnlyEnv = tmp_projectPak_Arg;
            Properties.Settings.Default.useProjectDirectory = tmp_useProjectDir;
            Properties.Settings.Default.ahpAbsolutePath = tmp_useAbsolutePath;
            Properties.Settings.Default.Save();
            this.Close();
        }

        //キャンセル
        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void pak_button_Checked(object sender, RoutedEventArgs e)
        {
            var radioButton = (RadioButton)sender;
            tmp_pak = radioButton.Name.ToString();
            var tmp_tf = (tmp_pak == "pak_dir_etc");
            folderlabel.IsEnabled = tmp_tf;
            setdir.IsEnabled = tmp_tf;
        }
        private void confirm_fileExists_button_Checked(object sender, RoutedEventArgs e)
        {
            var radioButton = (RadioButton)sender;
            switch (radioButton.Name.ToString())
            {
                case "sve_true":
                    tmp_sve = true;
                    break;
                case "sve_false":
                    tmp_sve = false;
                    break;
            }
        }
        private void ahp_and_pak_button_Checked(object sender, RoutedEventArgs e)
        {
            var radioButton = (RadioButton)sender;
            switch (radioButton.Name.ToString())
            {
                case "project_load_pak_true":
                    tmp_projectPak = true;
                    tmp_projectPak_Arg = false;
                    break;
                case "project_load_pak_false":
                    tmp_projectPak = false;
                    tmp_projectPak_Arg = false;
                    break;
                case "project_load_pak_sys":
                    tmp_projectPak = false;
                    tmp_projectPak_Arg = true;
                    break;
            }
        }

        private void OnSelectOutputFolderButtonClick(object sender, RoutedEventArgs e)
        {
            var dlg = new MSAPI::Dialogs.CommonOpenFileDialog();
            dlg.IsFolderPicker = true;
            dlg.Title = "フォルダを選択してください";
            dlg.InitialDirectory = tmp_file;
            if (dlg.ShowDialog() == MSAPI::Dialogs.CommonFileDialogResult.Ok)
            {
                tmp_file = dlg.FileName + "\\";
                folderlabel.Text = tmp_file;
            }
            this.Focus();
        }

        private void folderlabel_TextChanged(object sender, TextChangedEventArgs e)
        {
            tmp_file = folderlabel.Text;
        }

        private void pak_useProjectDirectory_Checked(object sender, RoutedEventArgs e)
        {
            tmp_useProjectDir = (bool)pak_useProjectDirectory.IsChecked;
        }
        private void project_useAbsolutePath_Checked(object sender, RoutedEventArgs e)
        {
            tmp_useAbsolutePath = (bool)project_absolute_path.IsChecked;
        }
    }
}
