using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Configuration;
using System.ComponentModel;
using System.Windows.Threading;
using System.Xml;

namespace 自动备份系统
{
    class BackupCore
    {
        MainWindow winMain;
        public BackupCore(MainWindow _winMain)
        {
            winMain = _winMain;//获取MainWindow实例
 
            //if(!File.Exists("log.xml"))
            //{
            //    XmlDeclaration xdec = xml.CreateXmlDeclaration("1.0", "UTF-8", null);
            //    xml.AppendChild(xdec);
            //    xml.AppendChild(xml.CreateElement("文件自动备份系统日志"));
            //}
            //else
            //{
                xml.Load("log.xml");
           // }

        }
        XmlDocument xml = new XmlDocument();
        XmlNode currentLog;
        string taskName;
        //用于读取配置文件
        Configuration cfa = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        int logIndex = 0;
        private void appendLog(string value)
        {
            logIndex++;
            XmlElement xe = xml.CreateElement("log"+logIndex.ToString());
            xe.SetAttribute("Time", DateTime.Now.ToString() + "." + DateTime.Now.Millisecond);
            xe.SetAttribute("Event", value);
            currentLog.AppendChild(xe);
            winMain.log.Append("[" + taskName + "]"+DateTime.Now.ToString() + "." + DateTime.Now.Millisecond +"                 "+value + System.Environment.NewLine + System.Environment.NewLine);
        }

        public void Backup(object name)
        {
            taskName = (string)name;

            XmlElement root = xml.DocumentElement;
            currentLog = xml.CreateElement(taskName + "--" + DateTime.Now.ToString().Replace("/", "-").Replace(" ", "_").Replace(":", "-"));
            root.AppendChild(currentLog);
            xml.Save("log.xml");

            appendLog("开始备份");
            //获取黑白名单目录
            string tempWhiteListViewItems;
            tempWhiteListViewItems = cfa.AppSettings.Settings[name + "_White"] != null ? cfa.AppSettings.Settings[name + "_White"].Value : "";
            foreach (var i in tempWhiteListViewItems.Split(new string[] { "#Split#" }, StringSplitOptions.RemoveEmptyEntries))
            {
                whiteDirectories.Add(i);
            }
            string tempBlackListViewItems;
            tempBlackListViewItems = cfa.AppSettings.Settings[name + "_Black"] != null ? cfa.AppSettings.Settings[name + "_Black"].Value : "";
            foreach (var i in tempBlackListViewItems.Split(new string[] { "#Split#" }, StringSplitOptions.RemoveEmptyEntries))
            {
                blackDirectories.Add(i);
            }
            //挑出其中的文件，其余的列举文件
            foreach (var i in whiteDirectories)
            {
                if (new FileInfo(i).Attributes == FileAttributes.Directory)
                {
                    listFiles(i);
                }
                else
                {
                    fileName.Add(new FileInfo(i).Name);
                }
            }
            appendLog("共发现" + fileName.Count + "个需要检查的文件");
            targetDirectory = cfa.AppSettings.Settings[name + "_TargetDirectory"].Value;
            //列举目标目录文件
            listBackupedFiles(targetDirectory);
            //列举差异项
            listDiferrences();
            //列举并且重命名源文件夹消失的部分
            listOldBackupedFilesAndRename();
            //将不同的部分复制到目标文件夹
            moveDiferrences();
           //保存日志
            xml.Save("log.xml");
            //刷新日志
            winMain.refreshLog();
            //winMain.lbxLogList.Dispatcher.Invoke(new Action(() =>{winMain.lbxLogList.Items.Refresh(); }));

        }


        string targetDirectory;
        //黑白名单的目录
        List<string> whiteDirectories = new List<string>();
        List<string> blackDirectories = new List<string>();
        //源目录文件去掉相同地址后的文件名、文件全名、修改时间和大小
        List<string> fileName = new List<string>();
        List<string> fullFileName = new List<string>();
        List<DateTime> fileLastWriteTime = new List<DateTime>();
        List<long> fileLength = new List<long>();
        //备份过的文件的文件名、修改时间和大小
        List<string> backupedFileName = new List<string>();
        List<DateTime> backupedFileLastWriteTime = new List<DateTime>();
        List<long> backupedFileLength = new List<long>();
        /// <summary>
        /// 列举源目录文件
        /// </summary>
        /// <param name="path"></param>
        private void listFiles(string path)
        {

            // int n = 0;
            foreach (var i in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                //用于跳出两层循环，代替goto
                bool needBackup = true;
                foreach (var j in blackDirectories)
                {
                    if (i.StartsWith(j))//如果是黑名单的文件
                    {
                        needBackup = false;
                    }
                }
                if (needBackup)
                {
                    //  n++;
                    fullFileName.Add(i);//文件全名
                    fileName.Add(i.Replace(path, ""));//去除相同目录的文件名
                    FileInfo fif = new FileInfo(i);
                    fileLastWriteTime.Add(fif.LastWriteTimeUtc);//文件修改时间
                    fileLength.Add(fif.Length);//文件大小
                }
            }

        }
        /// <summary>
        /// 列举目标目录里面已经备份过的文件
        /// </summary>
        /// <param name="path"></param>
        private void listBackupedFiles(string path)
        {
            if (Directory.Exists(path))
            {
                foreach (var i in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                {
                    //n++;
                    backupedFileName.Add(i.Replace(path, ""));
                    FileInfo fif = new FileInfo(i);
                    backupedFileLastWriteTime.Add(fif.LastWriteTimeUtc);
                    backupedFileLength.Add(fif.Length);
                }
            }
            //int n = 0;

        }

        //List<int> differentFilesIndex = new List<int>();
        List<int> sameFilesIndex = new List<int>();
        List<int> sameBackupedFilesIndex = new List<int>();
        List<string> fileDirectories = new List<string>();
        /// <summary>
        /// 列举源目录和目标目录不同的文件，并且把相同的但是修改时间不同的文件改名用于备份
        /// </summary>
        private void listDiferrences()
        {
            int n = backupedFileName.Count;
            //列举每一个源目录里的文件，寻找目标目录是否有相同的文件
            for (int i = 0; i < fileName.Count; i++)
            {
                winMain.CurrentFileCount = "正在查找：" + i.ToString() + "/" + fileName.Count.ToString();

                for (int j = 0; j < backupedFileName.Count; j++)//列举目标目录文件
                {
                    FileInfo targetFile = new FileInfo(targetDirectory + "\\" + fullFileName[i].Replace(fileName[i], "").Replace(":", "#C#").Replace("\\", "#S#") + fileName[i]);
                    //                                               目标目录                             源目录                替换掉不同的部分              把冒号替换             把斜杠替换    加上名字
                    if (!fileDirectories.Contains(fullFileName[i].Replace(fileName[i], "").Replace(":", "#C#").Replace("\\", "#S#"))) { fileDirectories.Add(fullFileName[i].Replace(fileName[i], "").Replace(":", "#C#").Replace("\\", "#S#")); }
                    if (backupedFileName[j].EndsWith(fileName[i]))//如果找到相同文件名的文件
                    {
                        if (fileLastWriteTime[i] == backupedFileLastWriteTime[j])//如果修改时间相同
                        {
                            if (fileLength[i] == backupedFileLength[j])//如果文件大小相同
                            {
                                sameFilesIndex.Add(i);
                                //文件名、文件大小和文件修改时间全部相同，几乎可以证明两个文件相同
                                sameBackupedFilesIndex.Add(j);
                            }
                            else
                            {
                                //修改时间相同但是大小不同，应该说是一件比较蹊跷的事，但是还是考虑一下
                                targetFile.MoveTo(targetFile.FullName.Replace(targetFile.Extension, "") + "#OldBackupedFile#" + targetFile.Extension);
                                appendLog("已重命名" + targetFile.FullName + "为" + targetFile.FullName.Replace(targetFile.Extension, "") + "#OldBackupedFile#" + targetFile.Extension);
                                //winMain.log.Append(DateTime.Now.ToString() + "." + DateTime.Now.Millisecond + "[" + taskName + "]                已重命名" + targetFile.FullName + "为" + targetFile.FullName.Replace(targetFile.Extension, "") + "#OldBackupedFile#" + targetFile.Extension + System.Environment.NewLine + System.Environment.NewLine);
                            }
                        }
                        else
                        {
                            //如果文件名一样但是修改时间变新了，说明后来修改过文件
                            //此时要把原来的文件加上时间标签重命名
                            targetFile.MoveTo(targetFile.FullName.Replace(targetFile.Extension, "") + "#OldBackupedFile#" + targetFile.LastWriteTimeUtc.ToFileTimeUtc() + targetFile.Extension);
                            appendLog("已重命名" + targetFile.FullName + "为" + targetFile.FullName.Replace(targetFile.Extension, "") + "#OldBackupedFile#" + targetFile.LastWriteTimeUtc.ToFileTimeUtc() + targetFile.Extension);
                            //winMain.log.Append(DateTime.Now.ToString() + "." + DateTime.Now.Millisecond + "[" + taskName + "]                已重命名" + targetFile.FullName + "为" + targetFile.FullName.Replace(targetFile.Extension, "") + "#OldBackupedFile#" + targetFile.LastWriteTimeUtc.ToFileTimeUtc() + targetFile.Extension + System.Environment.NewLine + System.Environment.NewLine);
                        }
                        continue;
                    }

                }

            }
            appendLog("共发现" + sameFilesIndex.Count + "个文件没有更新");
           // winMain.log.Append(DateTime.Now.ToString() + "." + DateTime.Now.Millisecond + "[" + taskName + "]                共发现" + sameFilesIndex.Count + "个文件没有更新" + Environment.NewLine + Environment.NewLine);
            //refreshLog();
        }

        //public delegate void passLogDelegate();


            /// <summary>
            /// 列举并且重命名那些旧的备份
            /// </summary>
        private void listOldBackupedFilesAndRename()
        {

            for (int i = 0; i < backupedFileName.Count; i++)//循环每一个备份文件
            {
                if (!sameBackupedFilesIndex.Contains(i))//如果文件发生了改变
                {
                    foreach (var j in fileDirectories)//循环每一个备份目录
                    {
                        if (backupedFileName[i].Contains(j) && !backupedFileName[i].Contains("OldBackupedFile#"))//如果确实是在备份目录下的文件（为了防止把其他文件改掉）而且是没有改过名的文件
                        {
                            FileInfo targetFile = new FileInfo(targetDirectory + backupedFileName[i]);
                            targetFile.MoveTo(targetFile.FullName.Replace(targetFile.Extension, "") + "#OldBackupedFile#" + targetFile.LastWriteTimeUtc.ToFileTimeUtc() + targetFile.Extension);
                            //winMain.log.Append(DateTime.Now.ToString() + "." + DateTime.Now.Millisecond + "[" + taskName + "]                已重命名" + targetFile.FullName + "为" + targetFile.FullName.Replace(targetFile.Extension, "") + "#OldBackupedFile#" + targetFile.LastWriteTimeUtc.ToFileTimeUtc() + targetFile.Extension + System.Environment.NewLine + System.Environment.NewLine);
                            appendLog("已重命名" + targetFile.FullName + "为" + targetFile.FullName.Replace(targetFile.Extension, "") + "#OldBackupedFile#" + targetFile.LastWriteTimeUtc.ToFileTimeUtc() + targetFile.Extension);
                            continue;
                        }
                    }

                }
            }
        }




        /// <summary>
        /// 备份差异项
        /// </summary>
        private void moveDiferrences()
        {
            int skipFile = 0;
            int fileCount = 0;
            for (int i = 0; i < fileName.Count; i++)//循环每一个源文件
            {
                if (sameFilesIndex.Contains(i))//如果文件索引出现在了相同文件的List上
                {
                    skipFile++;
                    continue;//跳过备份
                }
                fileCount++;
                FileInfo targetFile = new FileInfo(targetDirectory + "\\" + fullFileName[i].Replace(fileName[i], "").Replace(":", "#C#").Replace("\\", "#S#") + fileName[i]);
                if (!targetFile.Directory.Exists)
                {
                    //如果目标文件的目录不存在的话就创建一个，否则会报异常
                    targetFile.Directory.Create();
                }
                //复制文件
                File.Copy(fullFileName[i], targetFile.FullName);
                //winMain.log.Append(DateTime.Now.ToString() + "." + DateTime.Now.Millisecond + "[" + taskName + "]                已复制" + fullFileName[i] + "到" + targetFile.FullName + System.Environment.NewLine + System.Environment.NewLine);
                appendLog("已复制" + fullFileName[i] + "到" + targetFile.FullName);
                // winMain.txtLogPanel.Dispatcher.c
                //refreshLog();
                winMain.CurrentFileCount = "正在复制：" + fileCount.ToString() + "/" + (fileName.Count - sameFilesIndex.Count).ToString();
            }
            winMain.CurrentBackupThreads = 0;
            //winMain.refreshLog(log);
            winMain.itemsLastTime[winMain.itemsName.IndexOf(taskName)] = int.Parse(cfa.AppSettings.Settings[taskName + "_Interval"].Value);
            // winMain.log.Append(DateTime.Now.ToString() + "." + DateTime.Now.Millisecond + "[" + taskName + "]                备份完成，复制了" + (fileName.Count - sameFilesIndex.Count).ToString() + "个文件" + System.Environment.NewLine + System.Environment.NewLine);
            appendLog("备份完成，复制了" + (fileName.Count - sameFilesIndex.Count).ToString() + "个文件");
        }

    }
}
