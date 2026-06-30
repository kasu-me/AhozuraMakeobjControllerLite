using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace MakeobjControllerLite
{
    public class MyCommands
    {
        public static RoutedUICommand openProject = new RoutedUICommand("openProject", "openProject", typeof(MyCommands), new InputGestureCollection { new KeyGesture(Key.M, ModifierKeys.Control) });
        public static RoutedUICommand saveProject = new RoutedUICommand("openProject", "openProject", typeof(MyCommands), new InputGestureCollection { new KeyGesture(Key.M, ModifierKeys.Control) });
    }
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public Dictionary<string, string> PakTypes { get; set; }
        public string currentPakType = "";
        public string currentFileName = "";
        public string AhozuraVer = "";
        public string makeobjdir = "";
        public bool isUsingSaveDatAndPakPath = Properties.Settings.Default.saveFilePath;
        public string openingProject="";



        //リストに追加のやつ
        class CmbObject
        {
            public string Value { get; set; }
            public string FullPath { get; set; }

            public CmbObject(string Value, string FullPath)
            {
                this.Value = Value;
                this.FullPath = FullPath;
            }

            public override string ToString()
            {
                return Value;
            }
        }


        public MainWindow()
        {
            makeobjdir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\";
            //makeobj存在確認
            if (!File.Exists(makeobjdir + "makeobj.exe"))
            {
                MessageBox.Show("makeobj.exeが存在していません。このソフトと同じディレクトリに配置してください。", "Ahozura Makeobj Controller", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }

            InitializeComponent();

            //ショートカットキー関連
            this.CommandBindings.Add(new CommandBinding(MyCommands.openProject, delegate { openProject(null, null); }));
            this.CommandBindings.Add(new CommandBinding(MyCommands.saveProject, delegate { saveProject(null, null); }));

            //pak定義
            PakTypes = new Dictionary<string, string>()
            {
                { "32", "32(メニューボタンなど)" },
                { "64", "64系" },
                { "128", "128系" },
                { "else", "その他" },
            };

            //既定値フィル
            DataContext = this;
            paktype.SelectedIndex = Properties.Settings.Default.selectedPakTypeIndex;
            paktype_else.Text = Properties.Settings.Default.customizedPakType;

            //dat･pak名
            if (isUsingSaveDatAndPakPath)
            {
                filepath.Text = Properties.Settings.Default.currentDatPath;
                pakFileName.Text = Properties.Settings.Default.currentPakName;
            }


            //バージョン情報取得
            AhozuraVer = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            //引数
            string[] ArgFiles = System.Environment.GetCommandLineArgs();
            if (ArgFiles.Length > 1)
            {
                if (System.IO.Path.GetExtension(ArgFiles[1]) == ".dat")
                {
                    filepath.Text = ArgFiles[1];
                }else if (System.IO.Path.GetExtension(ArgFiles[1]) == ".ahp")
                {
                    openProjectFromFileName(ArgFiles[1],true);
                }
            }

        }



        //makeobjを実行
        private string runMakeobj(string makeobjArg,string datDir)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = makeobjdir + "makeobj.exe";
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.WorkingDirectory = datDir;
            startInfo.Arguments = makeobjArg;
            startInfo.RedirectStandardError = true;
            var process = Process.Start(startInfo);
            process.WaitForExit();
            if (process.ExitCode.ToString() == "0")
            {
                return "0";
            }
            else
            {
                return process.StandardError.ReadToEnd();
            }
            
        }

        //pak

        //プルダウンpakタイプ指定時
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            currentPakType =paktype.SelectedValue.ToString();
            Properties.Settings.Default.selectedPakTypeIndex = paktype.SelectedIndex;
            Properties.Settings.Default.Save();

            if (currentPakType == "else")
            {
                paktype_else.IsEnabled = true;
               currentPakType = paktype_else.Text;
            }
            else
            {
                paktype_else.IsEnabled = false;
            }
        }

        //その他のpakサイズ指定時
        private void paktype_else_TextChanged(object sender, TextChangedEventArgs e)
        {
            currentPakType = paktype_else.Text;
            Properties.Settings.Default.customizedPakType= paktype_else.Text;
            Properties.Settings.Default.Save();
        }

        private void OnDatFileSelectButtonClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Simutrans datファイル (*.dat)|*.dat|全てのファイル (*.*)|*.*";
            dialog.InitialDirectory = Properties.Settings.Default.initialdirectory_datfile;
            if (dialog.ShowDialog() == true)
            {
                filepath.Text = dialog.FileName;
                Properties.Settings.Default.initialdirectory_datfile = System.IO.Path.GetDirectoryName(dialog.FileName);
                Properties.Settings.Default.Save();
            }
        }

        private void filepath_TextChanged(object sender, TextChangedEventArgs e)
        {
            currentFileName = filepath.Text;
            Properties.Settings.Default.currentDatPath = filepath.Text;
            Properties.Settings.Default.Save();
        }

        private void pakname_TextChanged(object sender, TextChangedEventArgs e)
        {
            Properties.Settings.Default.currentPakName = pakFileName.Text;
            Properties.Settings.Default.Save();
        }

        //ドラッグアンドドロップ
        private void filepath_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop, true))
            {
                e.Effects = System.Windows.DragDropEffects.Copy;
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
            }
            e.Handled = true;
        }

        //ドラッグアンドドロップ
        private void filepath_Drop(object sender, DragEventArgs e)
        {
            var dropFiles = e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[];
            if (dropFiles == null) return;
            if (System.IO.Path.GetExtension(dropFiles[0]) == ".ahp"){
                if (Properties.Settings.Default.ahpAndRun)
                {
                    foreach (var i in dropFiles)
                    {
                        if (System.IO.Path.GetExtension(i) == ".ahp")
                        {
                            openProjectFromFileName(i, false);
                        }
                    }
                }
                else
                {
                    openProjectFromFileName(dropFiles[0], false);
                }
            }
            else { 
                filepath.Text = dropFiles[0];
                Properties.Settings.Default.initialdirectory_datfile = System.IO.Path.GetDirectoryName(dropFiles[0]);
                Properties.Settings.Default.Save();
            }
        }

        //pak作成
        private void OnExecutePakButtonClick(object sender, RoutedEventArgs e)
        {
            status.Content = "pakを実行します";
            Encoding enc = Encoding.GetEncoding("UTF-8");

            if (currentFileName == "") {
                status.Content = "datファイルが指定されていません";
            }
            else
            {
                //作業ディレクトリ名を取得
                var dat_dir = System.IO.Path.GetDirectoryName(currentFileName) + "\\";

                //出力先ディレクトリ名を設定
                var tmp_pak = Properties.Settings.Default.defaultSavePath;
                var pak_dir = "";
                if (Properties.Settings.Default.useProjectDirectory && openingProject!="")
                {
                    pak_dir = System.IO.Path.GetDirectoryName(openingProject)+"\\";
                }
                else
                {
                    switch (tmp_pak)
                    {
                        case "pak_dir_dat":
                            pak_dir = dat_dir;
                            break;
                        case "pak_dir_makeobj":
                            pak_dir = makeobjdir;
                            break;
                        case "pak_dir_etc":
                            pak_dir = Properties.Settings.Default.defaultSavePath_etc;
                            break;
                    }
                }


                //pakタイプ
                var makeobjArg = "pak"+currentPakType;

                //出力先
                if (pakFileName.Text == "")
                {
                    makeobjArg += " "+pak_dir;
                }
                else
                {
                    if (System.IO.Path.GetExtension(pakFileName.Text) == ".pak")
                    {
                        makeobjArg += " " + pak_dir + pakFileName.Text;
                    }
                    else
                    {
                        makeobjArg += " " + pak_dir + pakFileName.Text + ".pak";
                    }
                }

                //ソース
                makeobjArg += " "+currentFileName;

                var r=runMakeobj(makeobjArg, dat_dir);

                if (r == "0")
                {
                    status.Content = "pakを実行しました";
                }
                else
                {
                    status.Content = "エラーが発生しました";
                    using (StreamWriter writer = new StreamWriter(makeobjdir + "err.log", false))
                    {
                        writer.WriteLine(r);
                    }
                    System.Diagnostics.Process.Start(makeobjdir + "err.log");
                }

            }

        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }


        //merge

        private void ListBox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.All;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }
        private void ListBox_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            var n = 0;
            for (int i = 0; i < files.Length; i++)
            {
                if (System.IO.Path.GetExtension(files[i]) == ".pak")
                {
                    CmbObject fil = new CmbObject(System.IO.Path.GetFileName(files[i]), files[i]);
                    ListBox1.Items.Add(fil);
                    n++;
                }
            }
            status.Content = n + "件のpakファイルを追加しました";
        }

        private void ListBox_File_Clear(object sender, RoutedEventArgs e)
        {
            var n=ListBox1.SelectedItems.Count;
            if (n>0)
            {
                while (ListBox1.SelectedIndex > -1)
                {
                    ListBox1.Items.RemoveAt(ListBox1.SelectedIndex);
                }
                status.Content = n + "件のファイルをリストから削除しました";
            }
            else
            {
                status.Content = "ファイルが選択されていません";
            }
        }
        private void ListBox_Clear(object sender, RoutedEventArgs e)
        {
            ListBox1.Items.Clear();
            status.Content = "リストを初期化しました";
        }

        //pakを結合
        private void OnMergePakButtonClick(object sender, RoutedEventArgs e)
        {
            string[] pakListToMerge = new string[] { };
            var n = 0;
            var mergeList = "";
            for (int i = 0; i < ListBox1.Items.Count; i++)
            {
                CmbObject obj = (CmbObject)ListBox1.Items[i];
                Array.Resize(ref pakListToMerge, pakListToMerge.Length + 1);
                pakListToMerge[pakListToMerge.Length - 1] = obj.FullPath.ToString();
                mergeList += " " + obj.FullPath.ToString();
                n++;
            }
            status.Content = n + "件のpakファイルを統合します";

            Encoding enc = Encoding.GetEncoding("UTF-8");

            if (n == 0)
            {
                status.Content = "pakファイルが指定されていません";
            }
            else if (pakFileName_merge.Text == "")
            {
                status.Content = "pak名が指定されていません";
            }
            else
            {

                //出力先ディレクトリ名を設定
                var tmp_pak = Properties.Settings.Default.defaultSavePath;
                var pak_dir = "";
                switch (tmp_pak)
                {
                    case "pak_dir_dat":
                        pak_dir = System.IO.Path.GetDirectoryName(pakListToMerge[0])+"\\";
                        break;
                    case "pak_dir_makeobj":
                        pak_dir = makeobjdir;
                        break;
                    case "pak_dir_etc":
                        pak_dir = Properties.Settings.Default.defaultSavePath_etc;
                        break;
                }

                //makeobj
                var makeobjArg = "merge ";

                //merge後ファイル名
                if (System.IO.Path.GetExtension(pakFileName_merge.Text) == ".pak")
                {
                    makeobjArg += pak_dir + pakFileName_merge.Text;
                }
                else
                {
                    makeobjArg += pak_dir + pakFileName_merge.Text + ".pak";
                }

                //マージするpakたち
                makeobjArg += mergeList;

                var r = runMakeobj(makeobjArg, pak_dir);

                if (r == "0")
                {
                    status.Content = "mergeを実行しました";
                }
                else
                {
                    status.Content = "エラーが発生しました";
                    using (StreamWriter writer = new StreamWriter(makeobjdir + "err.log", false))
                    {
                        writer.WriteLine(r);
                    }
                    System.Diagnostics.Process.Start(makeobjdir + "err.log");
                }


            }
        }

        //extract

        private void pakFileToExtract_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        //ドラッグアンドドロップ
        private void pakFileToExtract_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop, true))
            {
                e.Effects = System.Windows.DragDropEffects.Copy;
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
            }
            e.Handled = true;
        }

        //ドラッグアンドドロップ
        private void pakFileToExtract_Drop(object sender, DragEventArgs e)
        {
            var dropFiles = e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[];
            if (dropFiles == null) return;
           pakFileToExtract.Text = dropFiles[0];
        }

        //ファイル参照ダイアログ
        private void OnExtractPakFileSelectButtonClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Simutrans pakファイル (*.pak)|*.pak|全てのファイル (*.*)|*.*";
            if (dialog.ShowDialog() == true)
            {
                pakFileToExtract.Text = dialog.FileName;
            }
        }


        //pakを分離
        private void OnExtractPakButtonClick(object sender, RoutedEventArgs e)
        {
            Encoding enc = Encoding.GetEncoding("UTF-8");

            if (pakFileToExtract.Text == "")
            {
                status.Content = "pakファイルが指定されていません";
            }
            else
            {
                //作業ディレクトリ名を取得
                var dat_dir = System.IO.Path.GetDirectoryName(pakFileToExtract.Text) + "\\";

                //出力先ディレクトリ名を設定
                var tmp_pak = Properties.Settings.Default.defaultSavePath;
                var pak_dir = "";
                switch (tmp_pak)
                {
                    case "pak_dir_dat":
                        pak_dir = dat_dir;
                        break;
                    case "pak_dir_makeobj":
                        pak_dir = makeobjdir;
                        break;
                    case "pak_dir_etc":
                        pak_dir = Properties.Settings.Default.defaultSavePath_etc;
                        break;
                }


                //引数
                var makeobjArg = "extract " + pakFileToExtract.Text;

                var r = runMakeobj(makeobjArg, dat_dir);

                if (r == "0")
                {
                    status.Content = "extractを実行しました";
                }
                else
                {
                    status.Content = "エラーが発生しました";
                    using (StreamWriter writer = new StreamWriter(makeobjdir + "err.log", false))
                    {
                        writer.WriteLine(r);
                    }
                    System.Diagnostics.Process.Start(makeobjdir + "err.log");
                }

            }
        }


        //その他

        //説明書を表示
        private void OnReadMeMenuItemClick(object sender, RoutedEventArgs e)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = System.IO.Path.GetDirectoryName(System.Environment.GetCommandLineArgs()[0])+"\\ReadMe.txt";
            var process = Process.Start(startInfo);
        }

        //Twitterを表示
        private void OnTwitterMenuItemClick(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://twitter.com/KasumiTrans");
        }
        //公式サイトを表示
        private void OnOfficialSiteMenuItemClick(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://ahozura.kasu.me/portal/?p=1045");
        }
        //バージョン情報を表示
        private void OnVersionInfoMenuItemClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Ahozura Makeobj Controller Lite\nVer. "+AhozuraVer,"バージョン情報");
        }

        //設定ダイアログ
        private void OnSettingsMenuItemClick(object sender, RoutedEventArgs e)
        {
            Window1 setw = new Window1();
            setw.ShowDialog();
        }

        //プロジェクト関連

        //上書き
        private void saveProject(object sender, RoutedEventArgs e)
        {
            if (openingProject == "")
            {
                createProject(null, null);
            }
            else
            {
                Encoding enc = Encoding.GetEncoding("UTF-8");
                using (StreamWriter writer = new StreamWriter(openingProject, false, enc))
                {
                    writer.WriteLine(BuildProjectFileContent(openingProject));
                }
                status.Content = "プロジェクトファイルを保存しました";
            }
        }

        //プロジェクトの作成
        private void createProject(object sender, RoutedEventArgs e)
        {
            Encoding enc = Encoding.GetEncoding("UTF-8");

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = "NewProject.ahp";
            sfd.InitialDirectory = Properties.Settings.Default.defaultProjectDirectory;
            sfd.Filter = "Ahozura Makeobj Controller プロジェクト(*.ahp)|*.ahp|すべてのファイル(*.*)|*.*";
            sfd.FilterIndex = 1;
            sfd.RestoreDirectory = true;
            sfd.OverwritePrompt = true;
            sfd.CheckPathExists = true;

            //ダイアログを表示する
            Nullable<bool> result = sfd.ShowDialog();
            if (result==true)
            {
                var jsonData = BuildProjectFileContent(sfd.FileName);
                using (StreamWriter writer = new StreamWriter(sfd.FileName, false, enc))
                {
                    writer.WriteLine(jsonData);
                }
                Properties.Settings.Default.defaultProjectDirectory = System.IO.Path.GetDirectoryName(sfd.FileName);
                Properties.Settings.Default.Save();
                openingProject = sfd.FileName;
                this.Title = System.IO.Path.GetFileName(sfd.FileName) + " - Ahozura Makeobj Controller Lite";
                status.Content = "プロジェクトファイルを保存しました";
            }
        }

        //プロジェクトを開く
        private void openProject(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = Properties.Settings.Default.defaultProjectDirectory;
            ofd.Filter = "Ahozura Makeobj Controller プロジェクト(*.ahp)|*.ahp|すべてのファイル(*.*)|*.*";
            ofd.FilterIndex = 1;
            ofd.RestoreDirectory = true;
            ofd.CheckPathExists = true;

            //ダイアログを表示する
            Nullable<bool> result = ofd.ShowDialog();
            if (result == true)
            {
                openProjectFromFileName(ofd.FileName,false);
            }
        }

        private void clearProject(object sender, RoutedEventArgs e)
        {
            openingProject = "";
            filepath.Text = "";
            pakFileName.Text = "";
            status.Content = "プロジェクをクリアしました";
            this.Title = "Ahozura Makeobj Controller Lite";
        }
        //終了
        private void closeWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        //ディレクトリを開く
        private void openDatDirectory(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Directory.Exists(System.IO.Path.GetDirectoryName(filepath.Text)))
                {
                    System.Diagnostics.Process.Start(System.IO.Path.GetDirectoryName(filepath.Text));
                    status.Content = "datファイルの場所を開きました";
                }
                else
                {
                    status.Content = "存在しない場所が指定されているようです";
                }
            }
            catch 
            {
                status.Content = "datファイルの場所を開けませんでした";
            }
        }
        private void openProjectDirectory(object sender, RoutedEventArgs e)
        {
            if (openingProject == "")
            {
                status.Content = "プロジェクトを開いていません";
            }
            else
            {
                System.Diagnostics.Process.Start(System.IO.Path.GetDirectoryName(openingProject));
                status.Content = "プロジェクトファイルの場所を開きました";
            }
        }

        //汎用関数等

        private string getProjectData(string data, string index)
        {
            if (data.IndexOf(index) == -1)
            {
                return "";

            }
            else
            {
                return (data.Substring(data.IndexOf(index) + index.Length + 1).Split("\n".ToCharArray())[0]).Replace("\n", "").Replace("\r", ""); 
            }
        }

        //プロジェクトファイル用の文字列に変換する
        private string BuildProjectFileContent(string ahpPlace)
        {
            var relativeUri = filepath.Text;
            //相対パス利用なら相対パスに変換
            if (!Properties.Settings.Default.ahpAbsolutePath)
            {
                Uri u1 = new Uri(ahpPlace);
                Uri u2 = new Uri(relativeUri);

                //絶対Uriから相対Uriを取得する
                relativeUri = u1.MakeRelativeUri(u2).ToString();
            }

            return "Version=" + AhozuraVer + "\nAbsolutePath=" + Properties.Settings.Default.ahpAbsolutePath.ToString() + "\nPakType=" + currentPakType + "\nDatFilePath=" + relativeUri + "\nPakName=" + pakFileName.Text;

        }
        //ファイルパスからプロジェクトを開く
        private void openProjectFromFileName(string projectFilePath , bool envp)
        {
            using (StreamReader reader = new StreamReader(projectFilePath))
            {
                var jsonData = reader.ReadToEnd();
                try
                {
                    var projectVersion = getProjectData(jsonData, "Version");
                    var projectPakType = getProjectData(jsonData, "PakType");
                    var projectDatPath = getProjectData(jsonData, "DatFilePath");
                    var projectPakName = getProjectData(jsonData, "PakName");
                    //ver1.1以外は絶対パスかどうかのパラメータがある
                    if (projectVersion !="1.1.0.0")
                    {
                        switch (getProjectData(jsonData, "AbsolutePath")){
                            case "True":
                                break;
                            case "False":
                                Uri u1 = new Uri(projectFilePath);
                                Uri u2 = new Uri(u1, projectDatPath);
                                projectDatPath = u2.LocalPath;
                                break;
                        }
                        
                    }
                    Console.WriteLine(projectPakType);
                    filepath.Text = projectDatPath;
                    pakFileName.Text = projectPakName;
                    switch (projectPakType)
                    {
                        case "128":
                        case "64":
                        case "32":
                            paktype.SelectedValue = projectPakType;
                            break;
                        case "":
                            break;
                        default:
                            paktype.SelectedValue = "else";
                            paktype_else.Text = projectPakType;
                            break;
                    }
                    currentPakType =projectPakType;
                    openingProject = projectFilePath;
                    this.Title = System.IO.Path.GetFileName(projectFilePath) + " - Ahozura Makeobj Controller Lite";
                    status.Content = "プロジェクトファイルを開きました";
                    if (Properties.Settings.Default.ahpAndRun || (Properties.Settings.Default.ahpAndRunOnlyEnv && envp))
                    {
                        OnExecutePakButtonClick(null, null);
                    }
                }
                catch
                {
                    MessageBox.Show("プロジェクトファイルの読み込みに失敗しました。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            Properties.Settings.Default.defaultProjectDirectory = System.IO.Path.GetDirectoryName(projectFilePath);
            Properties.Settings.Default.Save();
        }

        private void RegisterAhpFileExtension(object sender, RoutedEventArgs e)
        {
            string extension = ".ahp";
            //実行するコマンドライン
            string commandline = "\"" +  Environment.GetCommandLineArgs()[0] + "\" \"%1\"";
            //ファイルタイプ名
            string fileType = "aaaaaaa" + ".0";
            //説明（「ファイルの種類」として表示される）
            string description = "MyApplication File";
            //動詞
            string verb = "open";
            //動詞の説明（エクスプローラのコンテキストメニューに表示される。
            //　省略すると、「開く(&O)」となる。）
            string verbDescription = "MyApplicationで開く(&O)";
            //アイコンのパスとインデックス
            string iconPath = Environment.GetCommandLineArgs()[0];
            int iconIndex = 0;

            Microsoft.Win32.RegistryKey rootkey =
                Microsoft.Win32.Registry.ClassesRoot;

            //拡張子のキーを作成し、そのファイルタイプを登録
            Microsoft.Win32.RegistryKey regkey =
                rootkey.CreateSubKey(extension);
            regkey.SetValue("", fileType);
            regkey.Close();

            //ファイルタイプのキーを作成し、その説明を登録
            Microsoft.Win32.RegistryKey typekey =
                rootkey.CreateSubKey(fileType);
            typekey.SetValue("", description);
            typekey.Close();

            //動詞のキーを作成し、その説明を登録
            Microsoft.Win32.RegistryKey verblkey =
                rootkey.CreateSubKey(fileType + "\\shell\\" + verb);
            verblkey.SetValue("", verbDescription);
            verblkey.Close();

            //コマンドのキーを作成し、実行するコマンドラインを登録
            Microsoft.Win32.RegistryKey cmdkey =
                rootkey.CreateSubKey(fileType + "\\shell\\" + verb + "\\command");
            cmdkey.SetValue("", commandline);
            cmdkey.Close();

            //アイコンのキーを作成し、アイコンのパスと位置を登録
            Microsoft.Win32.RegistryKey iconkey =
                rootkey.CreateSubKey(fileType + "\\DefaultIcon");
            iconkey.SetValue("", iconPath + "," + iconIndex.ToString());
            iconkey.Close();

        }
    }
}
