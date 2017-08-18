using System;
using System.Windows;
using System.IO;
using System.Configuration;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;

namespace 自动备份系统
{
    /// <summary>
    /// TaskSettings.xaml 的交互逻辑
    /// </summary>
    public partial class TaskSettings : Window
    {

        string name;
        public TaskSettings(string taskName,bool lockName)
        {
            InitializeComponent();
            name = taskName;
            if(lockName)
            {
                txtName.IsEnabled = false;
            }
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
            //检查目录
            if (!Directory.Exists(txtTargetDirectory.Text))
            {
                try
                {
                    Directory.CreateDirectory(txtTargetDirectory.Text);
                }
                catch
                {
                    MessageBox.Show("目标目录输入不存在且无法创建！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            try
            {
                File.Create(new DirectoryInfo(txtTargetDirectory.Text).FullName + "\\" + "FileBackuper_" + txtName.Text);
            }
            catch
            {
                MessageBox.Show("无法创建校验文件！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;

            }
            //检查名称
            string tempFileName = System.IO.Path.GetTempFileName();
            using (FileStream fs = new FileStream(tempFileName, FileMode.Create))
            { fs.Write(System.Text.Encoding.UTF8.GetBytes(Properties.Resources.XmlTest), 0, System.Text.Encoding.UTF8.GetBytes(Properties.Resources.XmlTest).Length); }

            try
            {
                XmlDocument XmlTest = new XmlDocument();
                XmlTest.Load(tempFileName);
                XmlTest.DocumentElement.AppendChild(XmlTest.CreateElement(txtName.Text));
                XmlTest.Save(tempFileName);
                XmlTest.Load(tempFileName);
            }
            catch
            {
                MessageBox.Show("名称格式不正确！需要以文字开头，不能使用某些符号", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (cfa.AppSettings.Settings[txtName.Text + "_State"] == null)
            {
                cfa.AppSettings.Settings.Add(new KeyValueConfigurationElement(txtName.Text + "_State","true"));
            }

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

if(txtName.IsEnabled)
            {
                cfa.AppSettings.Settings["Items"].Value += txtName.Text + "#Split#";

            }
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
  
        Dictionary<TextBox, string> lastString = new Dictionary<TextBox, string>();
        Dictionary<TextBox, int> lastSelectPosition = new Dictionary<TextBox, int>();
        bool isChanging = false;
        private void TxtEnterOnlyPlusIntergerNumberTextChangedEventHandler(object sender, TextChangedEventArgs e)
        {if(isChanging)
            {
                return;
            }
            isChanging = true;
            if (((TextBox)sender).Text != "")
            {
                double tryNum;
                try
                {
                    tryNum = double.Parse(((TextBox)sender).Text);
                    if (tryNum != Math.Round(tryNum) || tryNum <= 0 || ((TextBox)sender).Text.Contains("."))
                    {
                        ((TextBox)sender).Text = lastString[(TextBox)sender];
                        ((TextBox)sender).Select(lastSelectPosition[(TextBox)sender], 0);
                        isChanging = false;
                        return;
                    }
                    lastString[(TextBox)sender] = ((TextBox)sender).Text;
                    lastSelectPosition[(TextBox)sender] = ((TextBox)sender).SelectionStart;
                }
                catch (Exception)
                {
                    try
                    {
                        ((TextBox)sender).Text = lastString[(TextBox)sender];
                    }
                    catch
                    {
                        ((TextBox)sender).Text = "";
                    }
                   ((TextBox)sender).Select(lastSelectPosition[(TextBox)sender], 0);
                }
            }
            else
            {
                lastString[(TextBox)sender] = "";
                lastSelectPosition[(TextBox)sender]= 0;
            }
            isChanging = false;
        }

        private void TxtEnterOnlyPlusIntergerNumberPreviewMouseUpEventHandler(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            lastSelectPosition[(TextBox)sender] = ((TextBox)sender).SelectionStart;
        }
    }
    }
    