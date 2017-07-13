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

namespace 自动备份系统
{
    class BackupCore
    {
        MainWindow winMain;
        public BackupCore(MainWindow _winMain)
        {
            winMain = _winMain;//获取MainWindow实例
        }


        //用于读取配置文件
        Configuration cfa = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

        StringBuilder log = new StringBuilder();
        public void Backup(object name)
        {
            //获取黑白名单目录
            string tempWhiteListViewItems;
            tempWhiteListViewItems = cfa.AppSettings.Settings[name + "_White"] != null ? cfa.AppSettings.Settings[name + "_White"].Value : "";
            foreach (var i in tempWhiteListViewItems.Split(new string[] { "??" }, StringSplitOptions.RemoveEmptyEntries))
            {
                whiteDirectories.Add(i);
            }
            string tempBlackListViewItems;
            tempBlackListViewItems = cfa.AppSettings.Settings[name + "_Black"] != null ? cfa.AppSettings.Settings[name + "_Black"].Value : "";
            foreach (var i in tempBlackListViewItems.Split(new string[] { "??" }, StringSplitOptions.RemoveEmptyEntries))
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
            log.Append(DateTime.Now.ToString()+DateTime.Now.Millisecond + "                共发现" + fileName.Count + "个需要检查的文件" + Environment.NewLine + Environment.NewLine);
            refreshLog();

            targetDirectory = cfa.AppSettings.Settings[name + "_TargetDirectory"].Value;
            //列举目标目录文件
            listBackupedFiles(targetDirectory);
            //列举差异项
            listDiferrences();
            //将不同的部分复制到目标文件夹
            moveDiferrences();
            //foreach (var i in fileName)
            //{
            //    Debug.WriteLine(i);
            //}
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
                    fileName.Add(i.Replace(path,""));//去除相同目录的文件名
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

            //int n = 0;
            foreach (var i in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                //n++;
                backupedFileName.Add(i.Replace(path, ""));
                FileInfo fif = new FileInfo(i);
                backupedFileLastWriteTime.Add(fif.LastWriteTimeUtc);
                backupedFileLength.Add(fif.Length);
            }
        }

        //List<int> differentFilesIndex = new List<int>();
        List<int> sameFilesIndex = new List<int>();
        /// <summary>
        /// 列举源目录和目标目录不同的文件，并且把相同的但是修改时间不同的文件改名用于备份
        /// </summary>
        private void listDiferrences()
        {//passLogDelegate passLog=new passLogDelegate(MainWindow.re)
            //列举每一个源目录里的文件，寻找目标目录是否有相同的文件
           for(int i=0;i<fileName.Count;i++)
            {
                for(int j=0;j<backupedFileName.Count;j++)//列举目标目录文件
                {
                    FileInfo targetFile = new FileInfo(targetDirectory + "\\" + fullFileName[i].Replace(fileName[i], "").Replace(":", "#C#").Replace("\\", "#S#") + fileName[i]);
                    //                                               目标目录                             源目录                替换掉不同的部分              把冒号替换             把斜杠替换    加上名字
                    if (backupedFileName[j].EndsWith(fileName[i]))//如果找到相同文件名的文件
                    {
                        if (fileLastWriteTime[i] == backupedFileLastWriteTime[j])//如果修改时间相同
                        {
                            if(fileLength[i]==backupedFileLength[j])//如果文件大小相同
                            {
                                sameFilesIndex.Add(i);
                                //文件名、文件大小和文件修改时间全部相同，几乎可以证明两个文件相同
                            }
                            else
                            {
                                //修改时间相同但是大小不同，应该说是一件比较蹊跷的事，但是还是考虑一下
                                targetFile.MoveTo(targetFile.FullName.Replace(targetFile.Extension,"")+"??"+ targetFile.Extension);
                                log.Append(DateTime.Now.ToString()+DateTime.Now.Millisecond + "                已重命名" + targetFile.FullName +"为" + targetFile.FullName.Replace(targetFile.Extension, "") + "??" + targetFile.Extension + System.Environment.NewLine + System.Environment.NewLine);

                                //differentFilesIndex.Add(i);
                            }
                        }
                        else
                        {
                            //如果文件名一样但是修改时间变新了，说明后来修改过文件
                            //此时要把原来的文件加上时间标签重命名
                           // FileInfo targetFile = new FileInfo(targetDirectory + "\\" + fullFileName[i].Replace(fileName[i], "").Replace(":", "#C#").Replace("\\", "#S#") + fileName[i]);
                            targetFile.MoveTo(targetFile.FullName.Replace(targetFile.Extension, "") + targetFile.LastWriteTimeUtc.ToFileTimeUtc() + targetFile.Extension);
                            log.Append(DateTime.Now.ToString()+DateTime.Now.Millisecond + "                已重命名" + targetFile.FullName+ "为"  + targetFile.FullName.Replace(targetFile.Extension, "") + targetFile.LastWriteTimeUtc.ToFileTimeUtc() + targetFile.Extension + System.Environment.NewLine + System.Environment.NewLine);

                            //differentFilesIndex.Add(i);
                        }
                        //backupedFileName.RemoveAt(j);
                        refreshLog();
                        continue;
                    }
                    
                }
                
            }
            log.Append(DateTime.Now.ToString()+DateTime.Now.Millisecond + "                共发现" + sameFilesIndex.Count + "个文件没有更新"+Environment.NewLine+Environment.NewLine);
            refreshLog();
        }

        public delegate void passLogDelegate();




        /// <summary>
        /// 备份差异项
        /// </summary>
        private void moveDiferrences()
        {
            int skipFile = 0;
           for(int i=0;i<fileName.Count;i++)//循环每一个源文件
            {
                if(sameFilesIndex.Contains(i))//如果文件索引出现在了相同文件的List上
                {
                    skipFile++;
                    continue;//跳过备份
                }
                FileInfo targetFile = new FileInfo(targetDirectory+"\\" +fullFileName[i].Replace(fileName[i],"").Replace(":","#C#").Replace("\\","#S#")+ fileName[i]);
                if(!targetFile.Directory.Exists)
                {
                    //如果目标文件的目录不存在的话就创建一个，否则会报异常
                    targetFile.Directory.Create();
                }
                //复制文件
                File.Copy(fullFileName[i], targetFile.FullName);
                log.Append(DateTime.Now.ToString()+DateTime.Now.Millisecond+"                已复制" + fullFileName[i] + "到" + targetFile.FullName + System.Environment.NewLine + System.Environment.NewLine);
                // winMain.txtLogPanel.Dispatcher.c
                refreshLog();
                
                //winMain.refreshLog(log);
            }

        }
        
        private void refreshLog()
        {
            //winMain.Dispatcher.Invoke(new dLog(winMain.refreshLog), log);
            //throw new NotImplementedException();
            winMain.txtLogPanel.Dispatcher.Invoke(new Action(() =>
            {
                winMain.txtLogPanel.Text = log.ToString();
            }));
            
        }

        //private delegate void dLog(StringBuilder log);


    }
}
