using System;
using System.Windows;
using System.IO;
using System.Configuration;
using System.Windows.Media.Imaging;

namespace 自动备份系统
{
    /// <summary>
    /// TaskSettings.xaml 的交互逻辑
    /// </summary>
    public partial class TaskSettings : Window
    {

        string name;
        public TaskSettings(string taskName)
        {
            InitializeComponent();
            name = taskName;
        }
        #region 白名单
        private void NewWhiteDirectoryButtonClickEventHandler(object sender, RoutedEventArgs e)
        {
            if(Directory.Exists(txtNewWhiteDirectory.Text))
            {
                lvwWhite.Items.Add(txtNewWhiteDirectory.Text);
                txtNewWhiteDirectory.Text = "输入地址或者拖放目录到列表框";
            }
            else
            {
                MessageBox.Show("请检查输入的地址是否正确！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void DeleteWhiteDirectoryButtonClickEventHandler(object sender, RoutedEventArgs e)
        {
            int index = lvwWhite.SelectedIndex;
            if(index!=-1)
            {
                lvwWhite.Items.RemoveAt(index);
                if(lvwWhite.Items.Count!=0)
                {
                    lvwWhite.SelectedIndex = (index == lvwWhite.Items.Count ? index - 1 : index);
                    //如果刚刚选择的是最后一个了，那么选择前面一个，否则选择后面一个
                }
            }
        }

        private void LvwWhiteDragEnterEventHandler(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Link;
                txtNewWhiteDirectory.Text = "拖放到列表框上方加入白名单";
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void LvwWhiteDropEventHandler(object sender, DragEventArgs e)
        {
            txtNewWhiteDirectory.Text = "输入地址或者拖放目录到列表框";
            try
            {
                foreach (var item in (string[])e.Data.GetData(DataFormats.FileDrop, false))
                {
                    lvwWhite.Items.Add(item);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("请检查是否拖入了不可引用的目录！","错误",MessageBoxButton.OK,MessageBoxImage.Error);
            }
        }

        private void LvwWhiteDragLeaveEventHandler(object sender, DragEventArgs e)
        {
            txtNewWhiteDirectory.Text = "输入地址或者拖放目录到列表框";
        }

        private void SelectWhiteDirectoryButtonClickEventHandler(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog() { Description = "请选择需要备份的目录", ShowNewFolderButton = true };

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                lvwWhite.Items.Add(fbd.SelectedPath);
            }
        }
        #endregion

        #region 黑名单
        private void NewBlackDirectoryButtonClickEventHandler(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(txtNewBlackDirectory.Text))
            {
                lvwBlack.Items.Add(txtNewBlackDirectory.Text);
                txtNewBlackDirectory.Text = "输入地址或者拖放目录到列表框";
            }
            else
            {
                MessageBox.Show("请检查输入的地址是否正确！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void DeleteBlackDirectoryButtonClickEventHandler(object sender, RoutedEventArgs e)
        {
            int index = lvwBlack.SelectedIndex;
            if (index != -1)
            {
                lvwBlack.Items.RemoveAt(index);
                if (lvwBlack.Items.Count != 0)
                {
                    lvwBlack.SelectedIndex = (index == lvwBlack.Items.Count ? index - 1 : index);
                    //如果刚刚选择的是最后一个了，那么选择前面一个，否则选择后面一个
                }
            }
        }

        private void LvwBlackDragEnterEventHandler(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Link;
                txtNewBlackDirectory.Text = "拖放到列表框上方加入白名单";
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void LvwBlackDropEventHandler(object sender, DragEventArgs e)
        {
            txtNewBlackDirectory.Text = "输入地址或者拖放目录到列表框";
            try
            {
                foreach (var item in (string[])e.Data.GetData(DataFormats.FileDrop, false))
                {
                    lvwBlack.Items.Add(item);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("请检查是否拖入了不可引用的目录！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LvwBlackDragLeaveEventHandler(object sender, DragEventArgs e)
        {
            txtNewBlackDirectory.Text = "输入地址或者拖放目录到列表框";
        }

        private void SelectBlackDirectoryButtonClickEventHandler(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog() { Description = "请选择需要排除的目录", ShowNewFolderButton = true };

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                lvwBlack.Items.Add(fbd.SelectedPath);
            }
        }
        #endregion

        private void SelectTargetDirectoryButtonClickEventHandler(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog() { Description="请选择希望把文件备份到的目标目录",ShowNewFolderButton=true};
            
            if (fbd.ShowDialog()== System.Windows.Forms.DialogResult.OK)
            {
                txtTargetDirectory.Text = fbd.SelectedPath;
            }
        }
        Configuration cfa = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        private void OKButtonClickEventHandler(object sender, RoutedEventArgs e)
        {
            string white = "";
            foreach (var i in lvwWhite.Items)
            {
                white += i.ToString()+"#Split#";
            }

            ChangeAppSettings("White", white);

            string black = "";
            foreach (var i in lvwBlack.Items)
            {
                black += i.ToString() + "#Split#";
            }

            ChangeAppSettings("Black", black);

            ChangeAppSettings("TargetDirectory", txtTargetDirectory.Text);

            int multiplier = 1;
           switch(cboTimeUnit.SelectedIndex)
            {
                case 1: multiplier = 60;
                    break;
                case 2:multiplier = 3600;
                    break;
                case 3:multiplier = 86400;
                    break;
                case 4:multiplier = 604800;
                    break;
            }
            try { ChangeAppSettings("Interval", (int.Parse(txtInterval.Text) * multiplier).ToString()); }
            catch(Exception)
            {
                MessageBox.Show("输入的时间有误！");
            }

            cfa.AppSettings.Settings["Items"].Value += txtName.Text + "#Split#";
            cfa.Save();
            this.Close();
        }

        private void ChangeAppSettings(string key,string targetValue)
        {
            key = txtName.Text+"_"+key;
            if (cfa.AppSettings.Settings[key] == null)
            {
                cfa.AppSettings.Settings.Add(new KeyValueConfigurationElement(key, targetValue));
            }
            else
            {
                cfa.AppSettings.Settings[key].Value = targetValue;
            }
        }

        private void WindowLoadedEventHandler(object sender, RoutedEventArgs e)
        {
            string tempFileName = System.IO.Path.GetTempFileName();
            FileStream fs = new FileStream(tempFileName, FileMode.Create);
            Properties.Resources.settings.Save(fs);
            fs.Close();
            this.Icon = new BitmapImage(new Uri(tempFileName));


            string tempWhiteListViewItems;
            tempWhiteListViewItems = cfa.AppSettings.Settings[name + "_White"] != null ? cfa.AppSettings.Settings[name + "_White"].Value : "";
            foreach (var i in tempWhiteListViewItems.Split(new string[] { "#Split#" }, StringSplitOptions.RemoveEmptyEntries))
            {
                lvwWhite.Items.Add(i);
            }
            string tempBlackListViewItems;
            tempBlackListViewItems = cfa.AppSettings.Settings[name + "_Black"] != null ? cfa.AppSettings.Settings[name + "_Black"].Value : "";
            foreach (var i in tempBlackListViewItems.Split(new string[] { "#Split#" }, StringSplitOptions.RemoveEmptyEntries))
            {
                lvwBlack.Items.Add(i);
            }

            txtTargetDirectory.Text = cfa.AppSettings.Settings[name + "_TargetDirectory"] != null ? cfa.AppSettings.Settings[name + "_TargetDirectory"].Value : "";
            txtInterval.Text = cfa.AppSettings.Settings[name + "_Interval"] != null ? cfa.AppSettings.Settings[name + "_Interval"].Value : "60";
            txtName.Text = name;

        }
    }
}
    