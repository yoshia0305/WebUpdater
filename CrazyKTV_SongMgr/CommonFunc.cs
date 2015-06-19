﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace CrazyKTV_SongMgr
{
    public static class ControlExtentions
    {
        public static void MakeDoubleBuffered(this Control control, bool setting)
        {
            Type controlType = control.GetType();
            PropertyInfo pi = controlType.GetProperty("DoubleBuffered",
            BindingFlags.Instance | BindingFlags.NonPublic);
            pi.SetValue(control, setting, null);
        }
    }

    public partial class MainFrom : Form
    {
        private static object LockThis = new object();

        private void Common_NumericOnly_TextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (((int)e.KeyChar < 48 | (int)e.KeyChar > 57) & (int)e.KeyChar != 8 & (int)e.KeyChar != 13 & (int)e.KeyChar != 27)
            {
                e.Handled = true;
                switch (((TextBox)sender).Name)
                {
                    case "SongAdd_DefaultSongVolume_TextBox":
                        SongAdd_Tooltip_Label.Text = "此項目只能輸入數字!";
                        break;
                    case "SongMaintenance_VolumeChange_TextBox":
                        SongMaintenance_Tooltip_Label.Text = "此項目只能輸入數字!";
                        break;
                    default:
                        SongMgrCfg_Tooltip_Label.Text = "此項目只能輸入數字!";
                        break;
                }
            }
            else
            {
                switch (((TextBox)sender).Name)
                {
                    case "SongAdd_DefaultSongVolume_TextBox":
                        if (SongAdd_Tooltip_Label.Text == "此項目只能輸入數字!") SongAdd_Tooltip_Label.Text = "";
                        break;
                    case "SongMaintenance_VolumeChange_TextBox":
                        if (SongMaintenance_Tooltip_Label.Text == "此項目只能輸入數字!") SongMaintenance_Tooltip_Label.Text = "";
                        break;
                    default:
                        if (SongMgrCfg_Tooltip_Label.Text == "此項目只能輸入數字!") SongMgrCfg_Tooltip_Label.Text = "";
                        break;
                }
            }
            if ((int)e.KeyChar == 13)
            {
                SendKeys.Send("{tab}");
            }
        }

        private void Common_IsNullOrEmpty_TextBox_Validating(object sender, CancelEventArgs e)
        {
            if (string.IsNullOrEmpty(((TextBox)sender).Text))
            {
                SongMaintenance_Tooltip_Label.Text = "此項目的值不能為空白!";
                e.Cancel = true;
            }
        }

        private void Common_ListBox_DoubleClick(object sender, EventArgs e)
        {
            string str = "";
            ListBox lbox = ((ListBox)sender);

            if (lbox.Items.Count > 0)
            {
                DataTable dt = (DataTable)lbox.DataSource;
                foreach (DataRow row in dt.Rows)
                {
                    str += row["Display"] + Environment.NewLine;
                }
                
                try
                {
                    Clipboard.SetData(DataFormats.UnicodeText, str);
                }
                catch
                {
                    // 剪貼簿被別的程式占用
                    Global.SongLogDT.Rows.Add(Global.SongLogDT.NewRow());
                    Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][0] = "【複製到剪貼簿】無法完成複製到剪貼簿,因剪貼簿已被占用。";
                    Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][1] = Global.SongLogDT.Rows.Count;
                }
            }
        }

        private void Common_CheckDBVer()
        {
            if (File.Exists(Global.CrazyktvDatabaseFile))
            {
                string SongAllSingerQuerySqlStr = "select Singer_Name, Singer_Type from ktv_AllSinger";
                Global.CrazyktvDatabaseVer = CommonFunc.OleDbCheckDB(Global.CrazyktvDatabaseFile, SongAllSingerQuerySqlStr, "");
            }

            if (Global.CrazyktvDatabaseVer == "Error" | !File.Exists(Global.CrazyktvDatabaseFile) | !Directory.Exists(Global.SongMgrDestFolder))
            {
                Common_SwitchDBVerErrorUI(false);
            }

            if (Global.CrazyktvDatabaseVer == "Error")
            {
                if (File.Exists(Global.CrazyktvDatabaseFile) & File.Exists(Application.StartupPath + @"\SongMgr\Update\UpdateSingerDB.txt") & File.Exists(Application.StartupPath + @"\SongMgr\Update\UpdatePhoneticsDB.txt"))
                {
                    MainTabControl.SelectedIndex = MainTabControl.TabPages.IndexOf(SongMaintenance_TabPage);
                    SongMaintenance_TabControl.SelectedIndex = SongMaintenance_TabControl.TabPages.IndexOf(SongMaintenance_DBVer_TabPage);
                    SongMaintenance_DBVerTooltip_Label.Text = "偵測到使用舊版歌庫,開始進行更新...";
                    Common_UpdateDB("OldDB");
                }
            }

            if (Global.CrazyktvDatabaseVer != "Error" & File.Exists(Global.CrazyktvDatabaseFile))
            {
                DataTable dt = new DataTable();
                string SongQuerySqlStr = "select Song_Id from ktv_Song";
                dt = CommonFunc.GetOleDbDataTable(Global.CrazyktvDatabaseFile, SongQuerySqlStr, "");
                if (dt.Rows.Count > 0)
                {
                    var d5code = from row in dt.AsEnumerable()
                                 where row.Field<string>("Song_Id").Length == 5
                                 select row;

                    var d6code = from row in dt.AsEnumerable()
                                 where row.Field<string>("Song_Id").Length == 6
                                 select row;
                    
                    int MaxDigitCode;
                    if (d5code.Count<DataRow>() > d6code.Count<DataRow>()) { MaxDigitCode = 5; } else { MaxDigitCode = 6; }

                    switch (MaxDigitCode)
                    {
                        case 5:
                            SongMgrCfg_MaxDigitCode_ComboBox.Enabled = false;
                            if (Global.SongMgrMaxDigitCode != "1")
                            {
                                SongMgrCfg_MaxDigitCode_ComboBox.SelectedValue = 1;
                                CommonFunc.SaveConfigXmlFile(Global.SongMgrCfgFile, "SongMgrMaxDigitCode", Global.SongMgrMaxDigitCode);
                                CommonFunc.SaveConfigXmlFile(Global.SongMgrCfgFile, "SongMgrLangCode", Global.SongMgrLangCode);
                            }
                            break;
                        case 6:
                            SongMgrCfg_MaxDigitCode_ComboBox.Enabled = false;
                            if (Global.SongMgrMaxDigitCode != "2")
                            {
                                SongMgrCfg_MaxDigitCode_ComboBox.SelectedValue = 2;
                                CommonFunc.SaveConfigXmlFile(Global.SongMgrCfgFile, "SongMgrMaxDigitCode", Global.SongMgrMaxDigitCode);
                                CommonFunc.SaveConfigXmlFile(Global.SongMgrCfgFile, "SongMgrLangCode", Global.SongMgrLangCode);
                            }
                            break;
                    }
                    
                    var query = from row in dt.AsEnumerable()
                                where row.Field<string>("Song_Id").Length != MaxDigitCode
                                select row;
                    if (query.Count<DataRow>() > 0)
                    {
                        Common_SwitchDBVerErrorUI(false);
                        SongMaintenance_CodeConvTo5_Button.Enabled = false;
                        SongMaintenance_CodeConvTo6_Button.Enabled = false;
                        SongMaintenance_CodeCorrect_Button.Enabled = true;
                        Global.CrazyktvDatabaseMaxDigitCode = "Error";
                    }
                    else
                    {
                        if (Directory.Exists(Global.SongMgrDestFolder)) { Common_SwitchDBVerErrorUI(true); } else { Common_SwitchDBVerErrorUI(false); }
                        
                        switch (Global.SongMgrMaxDigitCode)
                        {
                            case "1":
                                SongMaintenance_CodeConvTo5_Button.Enabled = false;
                                SongMaintenance_CodeConvTo6_Button.Enabled = true;
                                break;
                            case "2":
                                SongMaintenance_CodeConvTo5_Button.Enabled = true;
                                SongMaintenance_CodeConvTo6_Button.Enabled = false;
                                break;
                        }
                        SongMaintenance_CodeCorrect_Button.Enabled = false;
                        Global.CrazyktvDatabaseMaxDigitCode = "Pass";
                    }
                }
                dt.Dispose();
                Common_CheckDBUpdate();
            }
        }

        private void Common_CheckDBUpdate()
        {
            string VersionQuerySqlStr = "select * from ktv_Version";
            string VersionQueryStatus = "";
            if (File.Exists(Global.CrazyktvDatabaseFile))
            {
                VersionQueryStatus = CommonFunc.OleDbCheckDB(Global.CrazyktvDatabaseFile, VersionQuerySqlStr, "");
            }

            if (Global.CrazyktvDatabaseVer != "Error" & VersionQueryStatus == "Error")
            {
                if (File.Exists(Global.CrazyktvDatabaseFile) & File.Exists(Application.StartupPath + @"\SongMgr\Update\UpdateSingerDB.txt") & File.Exists(Application.StartupPath + @"\SongMgr\Update\UpdatePhoneticsDB.txt"))
                {
                    Common_SwitchDBVerErrorUI(false);
                    MainTabControl.SelectedIndex = MainTabControl.TabPages.IndexOf(SongMaintenance_TabPage);
                    SongMaintenance_TabControl.SelectedIndex = SongMaintenance_TabControl.TabPages.IndexOf(SongMaintenance_DBVer_TabPage);
                    SongMaintenance_DBVerTooltip_Label.Text = "偵測到歌庫版本更新,開始進行更新...";
                    Common_UpdateDB("AddktvVersion");
                }
            }
            else
            {
                DataTable dt = CommonFunc.GetOleDbDataTable(Global.CrazyktvDatabaseFile, VersionQuerySqlStr, "");
                double SongDBVer = 0.00;
                string SingerDBVer = "0";
                string PhoneticsDBVer = "0";

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        SongDBVer = Convert.ToDouble(row["SongDB"]);
                        SingerDBVer = row["SingerDB"].ToString();
                        PhoneticsDBVer = row["PhoneticsDB"].ToString();
                    }

                    SongMaintenance_DBVer1Value_Label.Text = SongDBVer.ToString("F2") + " 版";
                    SongMaintenance_DBVer2Value_Label.Text = SingerDBVer.ToString() + " 版";
                    SongMaintenance_DBVer3Value_Label.Text = PhoneticsDBVer.ToString() + " 版";

                    if (Convert.ToDouble(Global.CrazyktvSongDBVer) > SongDBVer)
                    {
                        if (File.Exists(Global.CrazyktvDatabaseFile) & File.Exists(Application.StartupPath + @"\SongMgr\Update\UpdateSingerDB.txt") & File.Exists(Application.StartupPath + @"\SongMgr\Update\UpdatePhoneticsDB.txt"))
                        {
                            if (Global.DBVerEnableDBVerUpdate == "True")
                            {
                                if (MessageBox.Show("你確定要更新歌庫版本嗎?", "偵測到歌庫版本更新", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                                {
                                    Common_SwitchDBVerErrorUI(false);
                                    MainTabControl.SelectedIndex = MainTabControl.TabPages.IndexOf(SongMaintenance_TabPage);
                                    SongMaintenance_TabControl.SelectedIndex = SongMaintenance_TabControl.TabPages.IndexOf(SongMaintenance_DBVer_TabPage);
                                    SongMaintenance_DBVerTooltip_Label.Text = "開始進行歌庫版本更新...";
                                    Common_UpdateDB("UpdateVersion");
                                }
                            }
                        }
                    }
                }
            }
        }
        
        private void Common_UpdateDB(string UpdateType)
        {
            Global.TimerStartTime = DateTime.Now;
            DataTable dt = new DataTable();
            OleDbConnection conn = new OleDbConnection();
            conn = CommonFunc.OleDbOpenConn(Global.CrazyktvDatabaseFile, "");
            OleDbCommand SongDBVerUpdatecmd = new OleDbCommand();
            OleDbCommand GodLiuColumnDropcmd = new OleDbCommand();
            string SongDBVerUpdatecmdSqlStr = "";
            string GodLiuColumnDropSqlStr = "";
            bool RebuildSingerData = true;

            List<string> GodLiuColumnlist = new List<string>() { "Song_SongNameFuzzy", "Song_SingerFuzzy", "Song_FuzzyVer", "DLspace", "Epasswd", "imgpath", "cashboxsongid", "cashboxdat", "holidaysongid" };

            switch (UpdateType)
            {
                case "OldDB": // 轉換舊版資料庫
                    if (!Directory.Exists(Application.StartupPath + @"\SongMgr\Backup")) Directory.CreateDirectory(Application.StartupPath + @"\SongMgr\Backup");
                    File.Copy(Global.CrazyktvDatabaseFile, Application.StartupPath + @"\SongMgr\Backup\CrazySongOld.mdb", true);
                    
                    OleDbCommand[] cmds = 
                    {
                        new OleDbCommand("drop table ktv_Singer", conn),
                        new OleDbCommand("create table ktv_AllSinger (Singer_Id INTEGER NOT NULL PRIMARY KEY, Singer_Name TEXT(40) WITH COMPRESSION, Singer_Type TEXT(20) WITH COMPRESSION, Singer_Spell TEXT(40) WITH COMPRESSION, Singer_Strokes BYTE, Singer_SpellNum TEXT(10) WITH COMPRESSION, Singer_PenStyle TEXT(80) WITH COMPRESSION)", conn),
                        new OleDbCommand("create table ktv_Singer (Singer_Id INTEGER NOT NULL PRIMARY KEY, Singer_Name TEXT(40) WITH COMPRESSION, Singer_Type TEXT(20) WITH COMPRESSION, Singer_Spell TEXT(40) WITH COMPRESSION, Singer_Strokes BYTE, Singer_SpellNum TEXT(10) WITH COMPRESSION, Singer_PenStyle TEXT(80) WITH COMPRESSION)", conn),
                        new OleDbCommand("create table ktv_Version (Id INTEGER NOT NULL PRIMARY KEY, SongDB TEXT(10), SingerDB INTEGER, PhoneticsDB INTEGER)", conn),
                        new OleDbCommand("insert into ktv_Version ( Id, SongDB, SingerDB, PhoneticsDB) values ( 1, '1.00', 0, 0 )", conn),
                        new OleDbCommand("alter table ktv_Phonetics alter column PenStyle TEXT(40) WITH COMPRESSION", conn),
                        new OleDbCommand("alter table ktv_Song alter column Song_PenStyle TEXT(60) WITH COMPRESSION", conn)
                    };

                    foreach (OleDbCommand cmd in cmds)
                    {
                        cmd.ExecuteNonQuery();
                    }
                    break;
                case "AddktvVersion": // 加入 ktv_Version 資料表
                    if (!Directory.Exists(Application.StartupPath + @"\SongMgr\Backup")) Directory.CreateDirectory(Application.StartupPath + @"\SongMgr\Backup");
                    File.Copy(Global.CrazyktvDatabaseFile, Application.StartupPath + @"\SongMgr\Backup\" + DateTime.Now.ToLongDateString() + "_CrazySong.mdb", true);
                    RebuildSingerData = bool.Parse(Global.DBVerRebuildSingerData);
                    
                    OleDbCommand[] Acmds = 
                    {
                        new OleDbCommand("create table ktv_Version (Id INTEGER NOT NULL PRIMARY KEY, SongDB TEXT(10), SingerDB INTEGER, PhoneticsDB INTEGER)", conn),
                        new OleDbCommand("insert into ktv_Version ( Id, SongDB, SingerDB, PhoneticsDB) values ( 1, '1.00', 0, 0 )", conn),
                        new OleDbCommand("alter table ktv_Phonetics alter column PenStyle TEXT(40) WITH COMPRESSION", conn),
                        new OleDbCommand("alter table ktv_Song alter column Song_PenStyle TEXT(60) WITH COMPRESSION", conn),
                        new OleDbCommand("alter table ktv_AllSinger alter column Singer_PenStyle TEXT(80) WITH COMPRESSION", conn),
                        new OleDbCommand("alter table ktv_Singer alter column Singer_PenStyle TEXT(80) WITH COMPRESSION", conn)
                    };

                    foreach (OleDbCommand cmd in Acmds)
                    {
                        cmd.ExecuteNonQuery();
                    }
                    break;
                case "UpdateVersion": // 更新資料庫版本
                    if (!Directory.Exists(Application.StartupPath + @"\SongMgr\Backup")) Directory.CreateDirectory(Application.StartupPath + @"\SongMgr\Backup");
                    File.Copy(Global.CrazyktvDatabaseFile, Application.StartupPath + @"\SongMgr\Backup\" + DateTime.Now.ToLongDateString() + "_CrazySong.mdb", true);
                    RebuildSingerData = bool.Parse(Global.DBVerRebuildSingerData);

                    OleDbCommand[] Ucmds = 
                    {
                        new OleDbCommand("alter table ktv_Phonetics alter column PenStyle TEXT(40) WITH COMPRESSION", conn),
                        new OleDbCommand("alter table ktv_Song alter column Song_PenStyle TEXT(60) WITH COMPRESSION", conn),
                        new OleDbCommand("alter table ktv_AllSinger alter column Singer_PenStyle TEXT(80) WITH COMPRESSION", conn),
                        new OleDbCommand("alter table ktv_Singer alter column Singer_PenStyle TEXT(80) WITH COMPRESSION", conn)
                    };

                    foreach (OleDbCommand cmd in Ucmds)
                    {
                        cmd.ExecuteNonQuery();
                    }
                    break;
            }

            SongDBVerUpdatecmdSqlStr = "update ktv_Version set SongDB = @SongDB where Id=@Id";
            SongDBVerUpdatecmd = new OleDbCommand(SongDBVerUpdatecmdSqlStr, conn);
            SongDBVerUpdatecmd.Parameters.AddWithValue("@SongDB", Global.CrazyktvSongDBVer);
            SongDBVerUpdatecmd.Parameters.AddWithValue("@Id", "1");
            SongDBVerUpdatecmd.ExecuteNonQuery();
            SongDBVerUpdatecmd.Parameters.Clear();

            string ColumnQuerySqlStr = "select top 1 * from ktv_Song";
            dt = CommonFunc.GetOleDbDataTable(Global.CrazyktvDatabaseFile, ColumnQuerySqlStr, "");

            if (dt.Rows.Count > 0)
            {
                foreach (DataColumn column in dt.Columns)
                {
                    if (GodLiuColumnlist.IndexOf(column.ColumnName) != -1)
                    {
                        switch (column.ColumnName)
                        {
                            case "Song_SongNameFuzzy":
                                GodLiuColumnDropSqlStr = "alter table ktv_Song drop column Song_SongNameFuzzy";
                                break;
                            case "Song_SingerFuzzy":
                                GodLiuColumnDropSqlStr = "alter table ktv_Song drop column Song_SingerFuzzy";
                                break;
                            case "Song_FuzzyVer":
                                GodLiuColumnDropSqlStr = "alter table ktv_Song drop column Song_FuzzyVer";
                                break;
                            case "DLspace":
                                GodLiuColumnDropSqlStr = "alter table ktv_Song drop column DLspace";
                                break;
                            case "Epasswd":
                                GodLiuColumnDropSqlStr = "alter table ktv_Song drop column Epasswd";
                                break;
                            case "imgpath":
                                GodLiuColumnDropSqlStr = "alter table ktv_Song drop column imgpath";
                                break;
                            case "cashboxsongid":
                                GodLiuColumnDropSqlStr = "alter table ktv_Song drop column cashboxsongid";
                                break;
                            case "cashboxdat":
                                GodLiuColumnDropSqlStr = "alter table ktv_Song drop column cashboxdat";
                                break;
                            case "holidaysongid":
                                GodLiuColumnDropSqlStr = "alter table ktv_Song drop column holidaysongid";
                                break;
                        }
                        GodLiuColumnDropcmd = new OleDbCommand(GodLiuColumnDropSqlStr, conn);
                        GodLiuColumnDropcmd.ExecuteNonQuery();
                    }
                }
            }
            conn.Close();
            dt.Dispose();

            var tasks = new List<Task>();
            tasks.Add(Task.Factory.StartNew(() => Common_UpdateDBTask(RebuildSingerData)));
        }

        private void Common_UpdateDBTask(bool RebuildSingerData)
        {
            Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
            Global.TotalList = new List<int>() { 0, 0, 0, 0 };
            var PhoneticsImportTask = Task.Factory.StartNew(() => SongMaintenance_PhoneticsImportTask(true));
            PhoneticsImportTask.Wait();

            Global.TotalList = new List<int>() { 0, 0, 0, 0 };
            var SingerImportTask = Task.Factory.StartNew(() => SongMaintenance_SingerImportTask());
            SingerImportTask.Wait();

            SongMaintenance.CreateSongDataTable();
            Global.TotalList = new List<int>() { 0, 0, 0, 0 };
            var SpellCorrectTask = Task.Factory.StartNew(() => SongMaintenance_SpellCorrectTask("ktv_Song"));
            SpellCorrectTask.Wait();

            if (RebuildSingerData)
            {
                Global.TotalList = new List<int>() { 0, 0, 0, 0 };
                var RebuildSingerDataTask = Task.Factory.StartNew(() => Common_RebuildSingerDataTask("SongMaintenance"));
                RebuildSingerDataTask.Wait();
            }

            CommonFunc.CompactAccessDB("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + Global.CrazyktvDatabaseFile + ";", Global.CrazyktvDatabaseFile);
            Global.TimerEndTime = DateTime.Now;
            this.BeginInvoke((Action)delegate()
            {
                if (Global.SongLogDT.Rows.Count > 0)
                {
                    SongLog_ListBox.DataSource = Global.SongLogDT;
                    SongLog_ListBox.DisplayMember = "Display";
                    SongLog_ListBox.ValueMember = "Value";

                    SongLog_TabPage.Text = "操作記錄 (" + Global.SongLogDT.Rows.Count + ")";
                }
                else
                {
                    SongLog_TabPage.Text = "操作記錄";
                }

                SongMaintenance.DisposeSongDataTable();

                Common_SwitchDBVerErrorUI(true);
                // 檢查資料庫檔案是否為舊版資料庫 (可能會照成無限迴圈,不過機率小)
                Common_CheckDBVer();

                // 檢查是否有自訂語系
                Common_CheckSongLang();

                // 統計歌曲數量
                Task.Factory.StartNew(() => Common_GetSongStatisticsTask());

                // 統計歌手數量
                Task.Factory.StartNew(() => Common_GetSingerStatisticsTask());

                // 載入我的最愛清單
                Global.SongQueryFavoriteQuery = "False";
                SongQuery_GetFavoriteUserList();
                SongMaintenance_GetFavoriteUserList();

                SongMaintenance_DBVerTooltip_Label.Text = "";
                SongMaintenance_Tooltip_Label.Text = "已完成歌庫版本更新,共花費 " + (long)(Global.TimerEndTime - Global.TimerStartTime).TotalSeconds + " 秒完成。";
            });
        }

        private void Common_SwitchDBVerErrorUI(bool status)
        {
            SongQuery_Query_GroupBox.Enabled = status;
            SongQuery_OtherQuery_GroupBox.Enabled = status;
            SongQuery_Statistics_GroupBox.Enabled = status;
            SongQuery_EditMode_CheckBox.Enabled = status;
            SongAdd_DefaultSongInfo_GroupBox.Enabled = status;
            SongAdd_SpecialStr_GroupBox.Enabled = status;
            SongAdd_SongAddCfg_GroupBox.Enabled = status;
            SongAdd_Save_Button.Enabled = status;
            SongAdd_DataGridView.Enabled = status;
            SongMaintenance_SpellCorrect_GroupBox.Enabled = status;
            SongMaintenance_CodeConv_GroupBox.Enabled = status;
            SongMaintenance_TrackExchange_GroupBox.Enabled = status;
            SongMaintenance_VolumeChange_GroupBox.Enabled = status;
            SongMaintenance_TabControl.Enabled = status;
            SongMaintenance_PlayCount_GroupBox.Enabled = status;
            SongMaintenance_SongPathChange_GroupBox.Enabled = status;
            SongMaintenance_Save_Button.Enabled = status;
            SingerMgr_Query_GroupBox.Enabled = status;
            SingerMgr_Statistics_GroupBox.Enabled = status;
            SingerMgr_SingerAdd_GroupBox.Enabled = status;
            SingerMgr_Manager_GroupBox.Enabled = status;
            SingerMgr_DataGridView.Enabled = status;
        }

        private void Common_SwitchSetUI(bool status)
        {
            if (!status)
            {
                SongLog_ListBox.DataSource = null;
                SongLog_ListBox.Items.Clear();

                SongAddResult_DuplicateSong_ListBox.DataSource = null;
                SongAddResult_DuplicateSong_ListBox.Items.Clear();

                SongAddResult_FailureSong_ListBox.DataSource = null;
                SongAddResult_FailureSong_ListBox.Items.Clear();
            }

            SongAdd_DefaultSongInfo_GroupBox.Enabled = status;
            SongAdd_SpecialStr_GroupBox.Enabled = status;
            SongAdd_SongAddCfg_GroupBox.Enabled = status;
            SongAdd_Save_Button.Enabled = status;
            SongMgrCfg_General_GroupBox.Enabled = status;
            SongMgrCfg_SongID_GroupBox.Enabled = status;
            SongMgrCfg_SongType_GroupBox.Enabled = status;
            SongMgrCfg_SongStructure_GroupBox.Enabled = status;
            SongMgrCfg_Save_Button.Enabled = status;
            SongMaintenance_SpellCorrect_GroupBox.Enabled = status;
            SongMaintenance_CodeConv_GroupBox.Enabled = status;
            SongMaintenance_TrackExchange_GroupBox.Enabled = status;
            SongMaintenance_VolumeChange_GroupBox.Enabled = status;
            SongMaintenance_TabControl.Enabled = status;
            SongMaintenance_PlayCount_GroupBox.Enabled = status;
            SongMaintenance_SongPathChange_GroupBox.Enabled = status;
            SongMaintenance_Save_Button.Enabled = status;
            SingerMgr_Query_GroupBox.Enabled = status;
            SingerMgr_Statistics_GroupBox.Enabled = status;
            SingerMgr_SingerAdd_GroupBox.Enabled = status;
            SingerMgr_Manager_GroupBox.Enabled = status;
            SingerMgr_DataGridView.Enabled = status;

            if (Global.SongLogDT.Rows.Count > 0)
            {
                if (status)
                {
                    SongLog_ListBox.DataSource = Global.SongLogDT;
                    SongLog_ListBox.DisplayMember = "Display";
                    SongLog_ListBox.ValueMember = "Value";

                    if (MainTabControl.TabPages.IndexOf(SongLog_TabPage) < 0)
                    {
                        MainCfg_HideSongLogTab_CheckBox.Checked = false;
                    }
                }
                SongLog_TabPage.Text = "操作記錄 (" + Global.SongLogDT.Rows.Count + ")";
            }
            else
            {
                SongLog_TabPage.Text = "操作記錄";
            }

            if (Global.DuplicateSongDT.Rows.Count > 0)
            {
                if (status)
                {
                    SongAddResult_DuplicateSong_ListBox.DataSource = Global.DuplicateSongDT;
                    SongAddResult_DuplicateSong_ListBox.DisplayMember = "Display";
                    SongAddResult_DuplicateSong_ListBox.ValueMember = "Value";
                }
            }

            if (Global.FailureSongDT.Rows.Count > 0)
            {
                if (status)
                {
                    SongAddResult_FailureSong_ListBox.DataSource = Global.FailureSongDT;
                    SongAddResult_FailureSong_ListBox.DisplayMember = "Display";
                    SongAddResult_FailureSong_ListBox.ValueMember = "Value";
                }
            }

            if (Global.DuplicateSongDT.Rows.Count > 0 | Global.FailureSongDT.Rows.Count > 0)
            {
                if (status)
                {
                    if (MainTabControl.TabPages.IndexOf(SongAddResult_TabPage) < 0)
                    {
                        MainCfg_HideSongAddResultTab_CheckBox.Checked = false;
                    }
                }
            }
        }

        private void Common_GetSongStatisticsTask()
        {
            if (File.Exists(Global.CrazyktvDatabaseFile) & Global.CrazyktvDatabaseVer != "Error")
            {
                string SongQuerySqlStr = "select Song_Id, Song_Lang, Song_Singer, Song_SongName, Song_SongType, Song_FileName, Song_Path from ktv_Song";
                Global.SongStatisticsDT = CommonFunc.GetOleDbDataTable(Global.CrazyktvDatabaseFile, SongQuerySqlStr, "");

                List<int> SongLangCount = new List<int>();

                Label[] SongQuery_Statistics_Label = 
                    {
                        SongQuery_Statistics2_Label,
                        SongQuery_Statistics3_Label,
                        SongQuery_Statistics4_Label,
                        SongQuery_Statistics5_Label,
                        SongQuery_Statistics6_Label,
                        SongQuery_Statistics7_Label,
                        SongQuery_Statistics8_Label,
                        SongQuery_Statistics9_Label,
                        SongQuery_Statistics10_Label,
                        SongQuery_Statistics11_Label
                    };

                this.BeginInvoke((Action)delegate()
                {
                    for (int i = 0; i < SongQuery_Statistics_Label.Count<Label>(); i++)
                    {
                        SongQuery_Statistics_Label[i].Text = Global.CrazyktvSongLangList[i] + ":";
                    }
                });

                Label[] SongQuery_StatisticsValue_Label = 
                    {
                        SongQuery_Statistics2Value_Label,
                        SongQuery_Statistics3Value_Label,
                        SongQuery_Statistics4Value_Label,
                        SongQuery_Statistics5Value_Label,
                        SongQuery_Statistics6Value_Label,
                        SongQuery_Statistics7Value_Label,
                        SongQuery_Statistics8Value_Label,
                        SongQuery_Statistics9Value_Label,
                        SongQuery_Statistics10Value_Label,
                        SongQuery_Statistics11Value_Label,
                        SongQuery_Statistics1Value_Label,
                        SongQuery_Statistics12Value_Label
                    };

                var task = Task<List<int>>.Factory.StartNew(CommonFunc.GetSongLangCount);

                SongLangCount = task.Result;

                TextBox[] SongMaintenance_Lang_TextBox =
                {
                    SongMaintenance_Lang1_TextBox,
                    SongMaintenance_Lang2_TextBox,
                    SongMaintenance_Lang3_TextBox,
                    SongMaintenance_Lang4_TextBox,
                    SongMaintenance_Lang5_TextBox,
                    SongMaintenance_Lang6_TextBox,
                    SongMaintenance_Lang7_TextBox,
                    SongMaintenance_Lang8_TextBox,
                    SongMaintenance_Lang9_TextBox,
                    SongMaintenance_Lang10_TextBox
                };
                this.BeginInvoke((Action)delegate()
                {
                    for (int i = 0; i < SongLangCount.Count; i++)
                    {
                        if (i != 11)
                        {
                            SongQuery_StatisticsValue_Label[i].Text = SongLangCount[i].ToString() + " 首";
                            if (i < 10)
                            {
                                if (SongLangCount[i] > 0) SongMaintenance_Lang_TextBox[i].Enabled = false;
                            }
                        }
                        else
                        {
                            SongQuery_StatisticsValue_Label[i].Text = SongLangCount[i].ToString() + " 個";
                        }
                    }
                });

                Global.SongStatisticsDT.Dispose();
            }
        }

        private void Common_CheckSongLang()
        {
            if (File.Exists(Global.CrazyktvDatabaseFile) & Global.CrazyktvDatabaseVer != "Error")
            {
                bool UpdateLang = false;
                List<string> list = new List<string>();

                DataTable dt = new DataTable();
                string SongQuerySqlStr = "select Langauage_Id, Langauage_Name from ktv_Langauage";
                dt = CommonFunc.GetOleDbDataTable(Global.CrazyktvDatabaseFile, SongQuerySqlStr, "");

                if (dt.Rows.Count != 0)
                {
                    foreach (DataRow row in dt.AsEnumerable())
                    {
                        int i = Convert.ToInt32(row["Langauage_Id"]);
                        if (i < 10)
                        {
                            if (row["Langauage_Name"].ToString() != Global.CrazyktvSongLangList[i])
                            {
                                UpdateLang = true;
                            }
                            list.Add(row["Langauage_Name"].ToString());
                        }
                    }

                    if (UpdateLang)
                    {
                        Global.CrazyktvSongLangList = list;
                        CommonFunc.SaveConfigXmlFile(Global.SongMgrCfgFile, "CrazyktvSongLangStr", string.Join(",", Global.CrazyktvSongLangList));
                        Common_RefreshSongLang();
                    }
                }
                dt.Dispose();
            }
        }

        private void Common_RefreshSongLang()
        {
            SongQuery_QueryFilter_ComboBox.DataSource = SongQuery.GetSongQueryFilterList();
            SongQuery_QueryFilter_ComboBox.DisplayMember = "Display";
            SongQuery_QueryFilter_ComboBox.ValueMember = "Value";
            SongQuery_QueryFilter_ComboBox.SelectedValue = 1;

            SongAdd_DefaultSongLang_ComboBox.DataSource = SongAdd.GetDefaultSongInfo("DefaultSongLang");
            SongAdd_DefaultSongLang_ComboBox.DisplayMember = "Display";
            SongAdd_DefaultSongLang_ComboBox.ValueMember = "Value";
            SongAdd_DefaultSongLang_ComboBox.SelectedValue = int.Parse(Global.SongAddDefaultSongLang);

            Task.Factory.StartNew(() => Common_GetSongStatisticsTask());
            SongMgrCfg_SetLangLB();
        }

        private void Common_GetSingerStatisticsTask()
        {
            if (File.Exists(Global.CrazyktvDatabaseFile) & Global.CrazyktvDatabaseVer != "Error")
            {
                SingerMgr.CreateSongDataTable();
                List<int> SingerTypeCount = new List<int>();
                List<string> SingerTypeList = new List<string>();

                foreach (string str in Global.CrazyktvSingerTypeList)
                {
                    if (str != "未使用")
                    {
                        SingerTypeList.Add(str);
                    }
                }

                Label[] SingerMgr_Statistics_Label = 
                    {
                        SingerMgr_Statistics2_Label,
                        SingerMgr_Statistics3_Label,
                        SingerMgr_Statistics4_Label,
                        SingerMgr_Statistics5_Label,
                        SingerMgr_Statistics6_Label,
                        SingerMgr_Statistics7_Label,
                        SingerMgr_Statistics8_Label,
                        SingerMgr_Statistics9_Label,
                        SingerMgr_Statistics10_Label
                    };

                this.BeginInvoke((Action)delegate()
                {
                    for (int i = 0; i < SingerMgr_Statistics_Label.Count<Label>(); i++)
                    {
                        SingerMgr_Statistics_Label[i].Text = SingerTypeList[i] + ":";
                    }
                });

                Label[] SingerMgr_StatisticsValue_Label = 
                    {
                        SingerMgr_Statistics2Value_Label,
                        SingerMgr_Statistics3Value_Label,
                        SingerMgr_Statistics4Value_Label,
                        SingerMgr_Statistics5Value_Label,
                        SingerMgr_Statistics6Value_Label,
                        SingerMgr_Statistics7Value_Label,
                        SingerMgr_Statistics8Value_Label,
                        SingerMgr_Statistics9Value_Label,
                        SingerMgr_Statistics10Value_Label,
                        SingerMgr_Statistics1Value_Label
                    };

                var task = Task<List<int>>.Factory.StartNew(CommonFunc.GetSingerTypeCount);

                SingerTypeCount = task.Result;
                this.BeginInvoke((Action)delegate()
                {
                    for (int i = 0; i < SingerTypeCount.Count; i++)
                    {
                        SingerMgr_StatisticsValue_Label[i].Text = SingerTypeCount[i].ToString() + " 位";
                    }
                });
                SingerMgr.DisposeSongDataTable();
            }
        }

        private void Common_RebuildSingerDataTask(string TooltipName)
        {
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;
            OleDbConnection conn = new OleDbConnection();
            OleDbCommand cmd = new OleDbCommand();

            conn = CommonFunc.OleDbOpenConn(Global.CrazyktvDatabaseFile, "");
            string TruncateSqlStr = "delete * from ktv_Singer";
            cmd = new OleDbCommand(TruncateSqlStr, conn);
            cmd.ExecuteNonQuery();
            conn.Close();

            int MaxSingerId = CommonFunc.GetMaxSingerId("ktv_Singer", Global.CrazyktvDatabaseFile) + 1;
            List<string> NotExistsSingerId = new List<string>();
            NotExistsSingerId = CommonFunc.GetNotExistsSingerId("ktv_Singer", Global.CrazyktvDatabaseFile);

            DataTable dt = new DataTable();
            string SingerQuerySqlStr = "SELECT First(Song_Singer) AS Song_Singer, First(Song_SingerType) AS Song_SingerType, Count(Song_Singer) AS Song_SingerCount FROM ktv_Song GROUP BY Song_Singer HAVING (((First(Song_SingerType))<>10) AND ((Count(Song_Singer))>0)) ORDER BY First(Song_SingerType), First(Song_Singer)";
            dt = CommonFunc.GetOleDbDataTable(Global.CrazyktvDatabaseFile, SingerQuerySqlStr, "");

            if (dt.Rows.Count > 0)
            {
                string SingerId = "";
                string SingerName = "";
                string SingerType = "";

                List<string> list = new List<string>();
                List<string> Addlist = new List<string>();
                List<string> spelllist = new List<string>();
                List<string> ChorusSingerList = new List<string>();
                List<string> SpecialStrlist = new List<string>(Regex.Split(Global.SongAddSpecialStr, ",", RegexOptions.IgnoreCase));

                foreach (DataRow row in dt.AsEnumerable())
                {
                    SingerName = row["Song_Singer"].ToString();
                    SingerType = row["Song_SingerType"].ToString();

                    if (SingerType == "3")
                    {
                        // 處理合唱歌曲中的特殊歌手名稱
                        foreach (string SpecialSingerName in SpecialStrlist)
                        {
                            Regex SpecialStrRegex = new Regex(SpecialSingerName, RegexOptions.IgnoreCase);
                            if (SpecialStrRegex.IsMatch(SingerName))
                            {
                                if (ChorusSingerList.IndexOf(SpecialSingerName) < 0)
                                {
                                    ChorusSingerList.Add(SpecialSingerName);
                                }
                                SingerName = Regex.Replace(SingerName, "&" + SpecialSingerName + "|" + SpecialSingerName + "&", "");
                            }
                        }

                        Regex r = new Regex("[&+](?=(?:[^%]*%%[^%]*%%)*(?![^%]*%%))");
                        if (r.IsMatch(SingerName))
                        {
                            string[] singers = Regex.Split(SingerName, "&", RegexOptions.None);
                            foreach (string str in singers)
                            {
                                if (ChorusSingerList.IndexOf(str) < 0)
                                {
                                    ChorusSingerList.Add(str);
                                }
                            }
                        }
                        else
                        {
                            if (ChorusSingerList.IndexOf(SingerName) < 0)
                            {
                                ChorusSingerList.Add(SingerName);
                            }
                        }
                    }
                    else
                    {
                        if (NotExistsSingerId.Count > 0)
                        {
                            SingerId = NotExistsSingerId[0];
                            NotExistsSingerId.RemoveAt(0);
                        }
                        else
                        {
                            SingerId = MaxSingerId.ToString();
                            MaxSingerId++;
                        }

                        spelllist = new List<string>();
                        spelllist = CommonFunc.GetSongNameSpell(SingerName);
                        Addlist.Add("ktv_Singer" + "*" + SingerId + "*" + SingerName + "*" + SingerType + "*" + spelllist[0] + "*" + spelllist[2] + "*" + spelllist[1] + "*" + spelllist[3]);
                    }

                    this.BeginInvoke((Action)delegate()
                    {
                        switch (TooltipName)
                        {
                            case "SongMaintenance":
                                SongMaintenance_Tooltip_Label.Text = "正在解析第 " + SingerId + " 位歌手資料,請稍待...";
                                break;
                            case "SingerMgr":
                                SingerMgr_Tooltip_Label.Text = "正在解析第 " + SingerId + " 位歌手資料,請稍待...";
                                break;
                        }
                    });
                }

                string sqlColumnStr = "Singer_Id, Singer_Name, Singer_Type, Singer_Spell, Singer_Strokes, Singer_SpellNum, Singer_PenStyle";
                string sqlValuesStr = "@SingerId, @SingerName, @SingerType, @SingerSpell, @SingerStrokes, @SingerSpellNum, @SingerPenStyle";
                string SingerAddSqlStr = "insert into ktv_Singer ( " + sqlColumnStr + " ) values ( " + sqlValuesStr + " )";

                conn = CommonFunc.OleDbOpenConn(Global.CrazyktvDatabaseFile, "");
                cmd = new OleDbCommand(SingerAddSqlStr, conn);

                foreach (string AddStr in Addlist)
                {
                    list = new List<string>(AddStr.Split('*'));

                    switch (list[0])
                    {
                        case "ktv_Singer":
                            cmd.Parameters.AddWithValue("@SingerId", list[1]);
                            cmd.Parameters.AddWithValue("@SingerName", list[2]);
                            cmd.Parameters.AddWithValue("@SingerType", list[3]);
                            cmd.Parameters.AddWithValue("@SingerSpell", list[4]);
                            cmd.Parameters.AddWithValue("@SingerStrokes", list[5]);
                            cmd.Parameters.AddWithValue("@SingerSpellNum", list[6]);
                            cmd.Parameters.AddWithValue("@SingerPenStyle", list[7]);

                            cmd.ExecuteNonQuery();
                            cmd.Parameters.Clear();
                            lock (LockThis)
                            {
                                Global.TotalList[0]++;
                            }
                            break;
                    }
                    this.BeginInvoke((Action)delegate()
                    {
                        switch (TooltipName)
                        {
                            case "SongMaintenance":
                                SongMaintenance_Tooltip_Label.Text = "正在重建第 " + Global.TotalList[0] + " 位歌手資料,請稍待...";
                                break;
                            case "SingerMgr":
                                SingerMgr_Tooltip_Label.Text = "正在重建第 " + Global.TotalList[0] + " 位歌手資料,請稍待...";
                                break;
                        }
                    });
                }
                Addlist.Clear();
                Addlist = new List<string>();

                Global.SingerDT = new DataTable();
                string SongSingerQuerySqlStr = "select Singer_Name, Singer_Type from ktv_Singer";
                Global.SingerDT = CommonFunc.GetOleDbDataTable(Global.CrazyktvDatabaseFile, SongSingerQuerySqlStr, "");

                Global.AllSingerDT = new DataTable();
                string SongAllSingerQuerySqlStr = "select Singer_Name, Singer_Type from ktv_AllSinger";
                Global.AllSingerDT = CommonFunc.GetOleDbDataTable(Global.CrazyktvDatabaseFile, SongAllSingerQuerySqlStr, "");

                // 判斷是否要加入合唱歌手資料至歌庫歌手資料庫
                foreach (string ChorusSinger in ChorusSingerList)
                {
                    string ChorusSingerName = Regex.Replace(ChorusSinger, @"^\s*|\s*$", ""); //去除頭尾空白
                    // 查找資料庫歌庫歌手資料表
                    var querysinger = from row in Global.SingerDT.AsEnumerable()
                                      where row.Field<string>("Singer_Name").ToLower().Equals(ChorusSingerName.ToLower())
                                      select row;

                    if (querysinger.Count<DataRow>() == 0)
                    {
                        // 查找資料庫預設歌手資料表
                        var querysingerall = from row in Global.AllSingerDT.AsEnumerable()
                                             where row.Field<string>("Singer_Name").ToLower().Equals(ChorusSingerName.ToLower())
                                             select row;
                        if (querysingerall.Count<DataRow>() > 0)
                        {
                            foreach (DataRow row in querysingerall)
                            {
                                SingerName = row["Singer_Name"].ToString();
                                SingerType = row["Singer_Type"].ToString();

                                if (NotExistsSingerId.Count > 0)
                                {
                                    SingerId = NotExistsSingerId[0];
                                    NotExistsSingerId.RemoveAt(0);
                                }
                                else
                                {
                                    SingerId = MaxSingerId.ToString();
                                    MaxSingerId++;
                                }

                                spelllist = new List<string>();
                                spelllist = CommonFunc.GetSongNameSpell(SingerName);
                                Addlist.Add("ktv_Singer" + "*" + SingerId + "*" + SingerName + "*" + SingerType + "*" + spelllist[0] + "*" + spelllist[2] + "*" + spelllist[1] + "*" + spelllist[3]);
                                break;
                            }
                        }
                    }
                }

                foreach (string AddStr in Addlist)
                {
                    list = new List<string>(AddStr.Split('*'));

                    switch (list[0])
                    {
                        case "ktv_Singer":
                            cmd.Parameters.AddWithValue("@SingerId", list[1]);
                            cmd.Parameters.AddWithValue("@SingerName", list[2]);
                            cmd.Parameters.AddWithValue("@SingerType", list[3]);
                            cmd.Parameters.AddWithValue("@SingerSpell", list[4]);
                            cmd.Parameters.AddWithValue("@SingerStrokes", list[5]);
                            cmd.Parameters.AddWithValue("@SingerSpellNum", list[6]);
                            cmd.Parameters.AddWithValue("@SingerPenStyle", list[7]);

                            cmd.ExecuteNonQuery();
                            cmd.Parameters.Clear();
                            lock (LockThis)
                            {
                                Global.TotalList[0]++;
                            }
                            break;
                    }
                    this.BeginInvoke((Action)delegate()
                    {
                        switch (TooltipName)
                        {
                            case "SongMaintenance":
                                SongMaintenance_Tooltip_Label.Text = "正在重建第 " + Global.TotalList[0] + " 位歌手資料,請稍待...";
                                break;
                            case "SingerMgr":
                                SingerMgr_Tooltip_Label.Text = "正在重建第 " + Global.TotalList[0] + " 位歌手資料,請稍待...";
                                break;
                        }
                    });
                }
                Addlist.Clear();
                ChorusSingerList.Clear();
                conn.Close();
            }
        }

        private void Common_CheckBackupRemoveSongTask()
        {
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;
            if (Global.SongMgrBackupRemoveSong == "True")
            {
                if (Directory.Exists(Application.StartupPath + @"\SongMgr\RemoveSong"))
                {
                    List<string> RemoveFileList = new List<string>();
                    List<string> SupportFormat = new List<string>();
                    SupportFormat = new List<string>(Global.SongMgrSupportFormat.Split(';'));
                    

                    DirectoryInfo dir = new DirectoryInfo(Application.StartupPath + @"\SongMgr\RemoveSong");
                    FileInfo[] Files = dir.GetFiles("*", SearchOption.AllDirectories).Where(p => SupportFormat.Contains(p.Extension.ToLower())).ToArray();

                    foreach (FileInfo fi in Files)
                    {
                        if ((int)(DateTime.Now - fi.CreationTime).TotalDays > Convert.ToInt32(Global.MainCfgBackupRemoveSongDays))
                        {
                            if ((fi.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                            {
                                fi.Attributes = fi.Attributes & ~FileAttributes.ReadOnly;
                            }
                            RemoveFileList.Add(fi.FullName);
                        }
                    }

                    foreach (string FilePath in RemoveFileList)
                    {
                        try
                        {
                            File.Delete(FilePath);
                        }
                        catch
                        {
                            Global.SongLogDT.Rows.Add(Global.SongLogDT.NewRow());
                            Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][0] = "【刪除備份移除歌曲】無法完成刪除,因檔案已被占用。";
                            Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][1] = Global.SongLogDT.Rows.Count;
                        }
                    }
                }
            }
        }



    }


    class CommonFunc
    {
        public static void CreateConfigXmlFile(string ConfigFile)
        {

            XDocument xmldoc = new XDocument
                (
                    new XDeclaration("1.0", "utf-16", null),
                    new XElement("Configeruation")
                );
            xmldoc.Save(ConfigFile);
        }

        public static string LoadConfigXmlFile(string ConfigFile, string ConfigName)
        {
            string Value = "";
            try
            {
                XElement rootElement = XElement.Load(ConfigFile);
                var Query = from childNode in rootElement.Elements("setting")
                            where (string)childNode.Attribute("Name") == ConfigName
                            select childNode;

                foreach (XElement childNode in Query)
                {
                    Value = childNode.Value;
                }
            }
            catch
            {
                Path.GetFileName(ConfigFile);
                MessageBox.Show("【" + Path.GetFileName(ConfigFile) + "】設定檔內容有錯誤,請刪除後再執行。");
            }
            return Value;
        }

        public static void SaveConfigXmlFile(string ConfigFile, string ConfigName, string ConfigValue)
        {
            XDocument xmldoc = XDocument.Load(ConfigFile);
            XElement rootElement = xmldoc.XPathSelectElement("Configeruation");
            
            var Query =from childNode in rootElement.Elements("setting")
                       where (string)childNode.Attribute("Name") == ConfigName
                       select childNode;

            if (Query.ToList().Count > 0)
            {
                foreach (XElement childNode in Query)
                {
                    childNode.Element("Value").Value = ConfigValue;
                }
            }
            else
            {
                XElement AddNode = new XElement("setting", new XAttribute("Name", ConfigName), new XElement("Value",  ConfigValue));
                rootElement.Add(AddNode);
            }
            xmldoc.Save(ConfigFile);
        }

        public static OleDbConnection OleDbOpenConn(string Database, string Password)
        {
            string cnstr = "";
            if (Password != "")
            {
                cnstr = string.Format("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + Database + ";Jet OLEDB:Database Password=" + Password + ";");
            }
            else
            {
                cnstr = string.Format("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + Database + ";");
            }
            OleDbConnection icn = new OleDbConnection();
            icn.ConnectionString = cnstr;
            if (icn.State == ConnectionState.Open) icn.Close();
            try
            {
                icn.Open();
            }
            catch
            {
                
            }
            return icn;
        }

        public static DataTable GetOleDbDataTable(string Database, string OleDbString, string Password)
        {
            DataTable myDataTable = new DataTable();
            OleDbConnection icn = OleDbOpenConn(Database, Password);
            OleDbDataAdapter da = new OleDbDataAdapter(OleDbString, icn);
            DataSet ds = new DataSet();
            ds.Clear();
            da.Fill(ds);
            myDataTable = ds.Tables[0];
            if (icn.State == ConnectionState.Open) icn.Close();
            return myDataTable;
        }

        public static string OleDbCheckDB(string Database, string OleDbString, string Password)
        {
            string str = "";
            OleDbConnection icn = OleDbOpenConn(Database, Password);
            if (icn.State == ConnectionState.Open)
            {
                OleDbDataAdapter da = new OleDbDataAdapter(OleDbString, icn);
                DataSet ds = new DataSet();
                ds.Clear();

                try
                {
                    da.Fill(ds);
                    str = "OK";
                }
                catch
                {
                    str = "Error";
                }
                if (icn.State == ConnectionState.Open) icn.Close();
            }
            else
            {
                str = "Error";
            }
            return str;
        }

        public static void CompactAccessDB(string connectionString, string mdwfilename)
        {
            object[] oParams;
            object objJRO = Activator.CreateInstance(Type.GetTypeFromProgID("JRO.JetEngine"));

            oParams = new object[]
            {
                connectionString,
                "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + Application.StartupPath + @"\tempdb.mdb;Jet OLEDB:Engine Type=5"
            };

            try
            {
                objJRO.GetType().InvokeMember("CompactDatabase", System.Reflection.BindingFlags.InvokeMethod, null, objJRO, oParams);

                File.Copy(Application.StartupPath + @"\tempdb.mdb", mdwfilename, true);
                File.Delete(Application.StartupPath + @"\tempdb.mdb");
            }
            catch
            {
                Global.SongLogDT.Rows.Add(Global.SongLogDT.NewRow());
                Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][0] = "【壓縮並修復資料庫】無法完成資料庫壓縮並修復,因資料庫已由其它使用者開啟。";
                Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][1] = Global.SongLogDT.Rows.Count;
            }

            System.Runtime.InteropServices.Marshal.ReleaseComObject(objJRO);
            objJRO = null;
        }

        public static bool IsSongId(String str)
        {
            Regex r = new Regex(@"^(?:\d{5})?$|^(?:\d{6})?$");
            return r.IsMatch(str);
        }

        public static string GetSongLangStr(int SongLang, int ListType, string IndexOfList)
        {
            string Str;
            List<string> list = new List<string>();
            if (SongLang < 0) SongLang = 0;

            foreach (string langstr in Global.CrazyktvSongLangList)
            {
                list.Add(langstr);
            }
            list.Add("未知");

            if (IndexOfList == "null")
            {
                Str = list[SongLang];
            }
            else
            {
                int Value = list.IndexOf(IndexOfList);
                Str = Value.ToString();
            }
            return Str;
        }

        public static string GetSingerTypeStr(int SingerType, int ListType, string IndexOfList)
        {
            string Str;
            List<string> list = new List<string>();
            if (SingerType < 0) SingerType = 0;

            switch (ListType)
            {
                case 1:
                    list = new List<string>() { "男歌星", "女歌星", "樂團", "合唱歌曲", "外國男", "外國女", "外國樂團", "其它", "歌星姓氏", "全部歌星", "新進歌星" };
                    break;
                case 2:
                    list = new List<string>() { "男", "女", "團", "合唱", "外男", "外女", "外團", "未知", "歌星姓氏", "全部歌星", "新進" };
                    break;
                default:
                    list = new List<string>() { "男歌星", "女歌星", "樂團", "外國男", "外國女", "外國樂團", "其它", "新進歌星" };
                    break;
            }
            
            if (IndexOfList == "null")
            {
                Str = list[SingerType];
            }
            else
            {
                int Value = list.IndexOf(IndexOfList);
                Str = Value.ToString();

            }
            return Str;
        }

        public static string GetSongTrackStr(int SongTrack, int ListType, string IndexOfList)
        {
            string Str;
            List<string> list = new List<string>();
            if (SongTrack < 0) SongTrack = 0;

            switch (ListType)
            {
                case 1:
                    if (Global.SongMgrSongTrackMode == "True")
                    {
                        list = new List<string>() { "VR", "VL", "V3", "V4", "V5" };
                    }
                    else
                    {
                        list = new List<string>() { "VL", "VR", "V3", "V4", "V5" };
                    }
                    break;
                default:
                    if (Global.SongMgrSongTrackMode == "True")
                    {
                        list = new List<string>() { "右聲道 / 音軌2", "左聲道 / 音軌1", "音軌3", "音軌4", "音軌5" };
                    }
                    else
                    {
                        list = new List<string>() { "左聲道 / 音軌1", "右聲道 / 音軌2", "音軌3", "音軌4", "音軌5" };
                    }
                    break;
            }

            if (IndexOfList == "null")
            {
                Str = list[SongTrack];
            }
            else
            {
                int Value = list.IndexOf(IndexOfList);
                Str = Value.ToString();
            }
            return Str;
        }

        public static void GetRemainingSongId(int DigitCode)
        {
            if (File.Exists(Global.CrazyktvDatabaseFile) & Global.CrazyktvDatabaseVer != "Error")
            {
                List<string> StartIdlist = new List<string>();
                StartIdlist = new List<string>(Regex.Split(Global.SongMgrLangCode, ",", RegexOptions.None));
                StartIdlist.Add((DigitCode == 5) ? "100000" : "1000000");
                int RemainingSongId;

                DataTable dt = new DataTable();
                string SongQuerySqlStr = "select Song_Id, Song_Lang from ktv_Song order by Song_Id";
                dt = CommonFunc.GetOleDbDataTable(Global.CrazyktvDatabaseFile, SongQuerySqlStr, "");

                if (dt.Rows.Count != 0)
                {
                    int i;
                    foreach (string str in Global.CrazyktvSongLangList)
                    {
                        var query = from row in dt.AsEnumerable()
                                    where row.Field<string>("Song_Lang").Equals(str) &&
                                          row.Field<string>("Song_Id").Length == DigitCode
                                    orderby row.Field<string>("Song_Id") descending
                                    select row;

                        foreach (DataRow row in query)
                        {
                            i = Global.CrazyktvSongLangList.IndexOf(str);
                            RemainingSongId = Convert.ToInt32(StartIdlist[i + 1]) - Convert.ToInt32(row["Song_Id"]) - 1;
                            if (RemainingSongId < Global.RemainingSongID) Global.RemainingSongID = RemainingSongId;
                            break;
                        }
                    }
                }
                dt.Dispose();
            }
        }

        public static void GetMaxSongId(int DigitCode)
        {
            Global.MaxIDList = new List<int>();
            List<string> StartIdlist = new List<string>();
            StartIdlist = new List<string> (Regex.Split(Global.SongMgrLangCode, ",", RegexOptions.None));

            DataTable dt = new DataTable();
            string SongQuerySqlStr = "select Song_Id, Song_Lang from ktv_Song order by Song_Id";
            dt = CommonFunc.GetOleDbDataTable(Global.CrazyktvDatabaseFile, SongQuerySqlStr, "");

            Global.MaxIDList = new List<int>() { Convert.ToInt32(StartIdlist[0]) - 1, Convert.ToInt32(StartIdlist[1]) - 1,
                Convert.ToInt32(StartIdlist[2]) - 1, Convert.ToInt32(StartIdlist[3]) - 1, Convert.ToInt32(StartIdlist[4]) - 1,
                Convert.ToInt32(StartIdlist[5]) - 1, Convert.ToInt32(StartIdlist[6]) - 1, Convert.ToInt32(StartIdlist[7]) - 1,
                Convert.ToInt32(StartIdlist[8]) - 1, Convert.ToInt32(StartIdlist[9]) - 1 };

            if(dt.Rows.Count != 0)
            {
                int i;
                foreach (string str in Global.CrazyktvSongLangList)
                {
                    var query = from row in dt.AsEnumerable()
                                where row.Field<string>("Song_Lang").Equals(str) &&
                                      row.Field<string>("Song_Id").Length == DigitCode
                                orderby row.Field<string>("Song_Id") descending
                                select row;
                    
                    foreach (DataRow row in query)
                    {
                        i = Global.CrazyktvSongLangList.IndexOf(str);
                        Global.MaxIDList[i] = Convert.ToInt32(row["Song_Id"]);
                        break;
                    }
                }
            }
            dt.Dispose();
        }

        public static int GetMaxSingerId(string TableName, string DatabaseFile)
        {
            int i = 0;
            DataTable dt = new DataTable();
            string SongQuerySqlStr = "select Singer_Id from " + TableName + " order by Singer_Id";
            dt = CommonFunc.GetOleDbDataTable(DatabaseFile, SongQuerySqlStr, "");

            if (dt.Rows.Count != 0)
            {
                var query = from row in dt.AsEnumerable()
                            where row.Field<Int32>("Singer_Id") > 0
                            orderby row.Field<Int32>("Singer_Id") descending
                            select row;

                foreach (DataRow row in query)
                {
                    i = Convert.ToInt32(row["Singer_Id"]);
                    break;
                }
            }

            dt.Dispose();
            return i;
        }

        public static void GetNotExistsSongId(int DigitCode)
        {
            string MaxDigitCode = "";
            if (Global.SongMgrMaxDigitCode == "1") { MaxDigitCode = "D5"; } else { MaxDigitCode = "D6"; }

            Global.NotExistsSongIdDT = new DataTable();
            Global.NotExistsSongIdDT.Columns.Add("Song_Id", typeof(string));
            Global.NotExistsSongIdDT.Columns.Add("Song_Lang", typeof(string));
            
            List<string> StartIdlist = new List<string>();
            StartIdlist = new List<string> (Regex.Split(Global.SongMgrLangCode, ",", RegexOptions.None));

            DataTable dt = new DataTable();
            string SongQuerySqlStr = "select Song_Id, Song_Lang from ktv_Song order by Song_Id";
            dt = CommonFunc.GetOleDbDataTable(Global.CrazyktvDatabaseFile, SongQuerySqlStr, "");

            if (dt.Rows.Count != 0)
            {
                Parallel.ForEach(Global.CrazyktvSongLangList, (str, loopState) =>
                {
                    int iMin = Convert.ToInt32(StartIdlist[Global.CrazyktvSongLangList.IndexOf(str)]);
                    List<string> ExistsIdlist = new List<string>();

                    var query = from row in dt.AsEnumerable()
                                where row.Field<string>("Song_Lang").Equals(str) &&
                                      row.Field<string>("Song_Id").Length == DigitCode
                                orderby row.Field<string>("Song_Id")
                                select row;

                    if (query.Count<DataRow>() > 0)
                    {
                        foreach (DataRow row in query)
                        {
                            ExistsIdlist.Add(row["Song_Id"].ToString());
                        }

                        if (ExistsIdlist.Count > 0)
                        {
                            int iMax = Convert.ToInt32(ExistsIdlist[ExistsIdlist.Count - 1]);
                            Parallel.For(iMin, iMax, (i, ForloopState) =>
                            {
                                if (ExistsIdlist.IndexOf(i.ToString(MaxDigitCode)) < 0)
                                {
                                    DataRow idrow = Global.NotExistsSongIdDT.NewRow();
                                    idrow["Song_Id"] = i.ToString(MaxDigitCode);
                                    idrow["Song_Lang"] = str;
                                    Global.NotExistsSongIdDT.Rows.Add(idrow);
                                }
                            });
                        }
                    }
                    ExistsIdlist.Clear();
                });
            }
            dt.Dispose();
        }

        public static List<string> GetNotExistsSingerId(string TableName, string DatabaseFile)
        {
            List<string> list = new List<string>();
            List<int> ExistsIdlist = new List<int>();
            DataTable dt = new DataTable();
            string SongQuerySqlStr = "select Singer_Id from " + TableName + " order by Singer_Id";
            dt = CommonFunc.GetOleDbDataTable(DatabaseFile, SongQuerySqlStr, "");
            
            if (dt.Rows.Count != 0)
            {
                var query = from row in dt.AsEnumerable()
                            where row.Field<Int32>("Singer_Id") > 0
                            orderby row.Field<Int32>("Singer_Id")
                            select row;

                if (query.Count<DataRow>() > 0)
                {
                    foreach (DataRow row in query)
                    {
                        ExistsIdlist.Add(Convert.ToInt32(row["Singer_Id"]));
                    }

                    if (ExistsIdlist.Count > 0)
                    {
                        int iMin = 1;
                        int iMax = Convert.ToInt32(ExistsIdlist[ExistsIdlist.Count - 1]);
                        Parallel.For(iMin, iMax, (i, ForloopState) =>
                        {
                            if (ExistsIdlist.IndexOf(i) < 0)
                            {
                                list.Add(i.ToString());
                            }
                        });
                    }
                }
            }
            dt.Dispose();
            return list;
        }

        public static List<string> GetSongWordCount(string SongStr)
        {
            List<string> WordCountList = new List<string>() { "0", "False" };
            if (string.IsNullOrEmpty(SongStr)) return WordCountList;

            SongStr = Regex.Replace(SongStr, @"[\{\(\[｛（［【].+?[】］）｝\]\)\}]", ""); // 排除計算括號字數

            MatchCollection CJKCharMatches = Regex.Matches(SongStr, @"([\u2E80-\u33FF]|[\u4E00-\u9FCC\u3400-\u4DB5\uFA0E\uFA0F\uFA11\uFA13\uFA14\uFA1F\uFA21\uFA23\uFA24\uFA27-\uFA29]|[\ud840-\ud868][\udc00-\udfff]|\ud869[\udc00-\uded6\udf00-\udfff]|[\ud86a-\ud86c][\udc00-\udfff]|\ud86d[\udc00-\udf34\udf40-\udfff]|\ud86e[\udc00-\udc1d]|[\uac00-\ud7ff])");
            int CharCount = Regex.Matches(SongStr, @"[0-9][0-9'\-.]*").Count + Regex.Matches(SongStr, @"[A-Za-z][A-Za-z'\-.]*").Count + CJKCharMatches.Count;

            WordCountList[0] = CharCount.ToString();
            if (CJKCharMatches.Count == 0) { WordCountList[1] = "True"; }
            return WordCountList;
        }

        
        public static List<string> GetSongNameSpell(string SongStr)
        {
            List<string> SpellList = new List<string>() { "", "", "0", "" };
            if (string.IsNullOrEmpty(SongStr)) return SpellList;

            List<string> list = new List<string>();

            SongStr = Regex.Replace(SongStr, @"[\{\(\[｛（［【].+?[】］）｝\]\)\}]", ""); // 排除解析括號字串
            SongStr = Regex.Replace(SongStr, @"([\u2E80-\u33FF]|[\u4E00-\u9FCC\u3400-\u4DB5\uFA0E\uFA0F\uFA11\uFA13\uFA14\uFA1F\uFA21\uFA23\uFA24\uFA27-\uFA29]|[\ud840-\ud868][\udc00-\udfff]|\ud869[\udc00-\uded6\udf00-\udfff]|[\ud86a-\ud86c][\udc00-\udfff]|\ud86d[\udc00-\udf34\udf40-\udfff]|\ud86e[\udc00-\udc1d])", @" $1 "); // 以空格隔開中英字串
            SongStr = Regex.Replace(SongStr, @"^\s*|\s*$", ""); //去除頭尾空白

            if (SongStr != "")
            {
                list = new List<string>(Regex.Split(SongStr, @"[\s]+", RegexOptions.None));

                foreach (string str in list)
                {
                    Regex r = new Regex("^[A-Za-z0-9]");

                    if (r.IsMatch(str))
                    {
                        r = new Regex("^[A-Za-z]");
                        if (r.IsMatch(str))
                        {
                            SpellList[0] = SpellList[0] + str.Substring(0, 1).ToUpper(); // 拼音
                            switch (str.Substring(0, 1).ToUpper())
                            {
                                case "A":
                                case "B":
                                case "C":
                                    SpellList[1] = SpellList[1] + "2"; // 手機輸入
                                    break;
                                case "D":
                                case "E":
                                case "F":
                                    SpellList[1] = SpellList[1] + "3";
                                    break;
                                case "G":
                                case "H":
                                case "I":
                                    SpellList[1] = SpellList[1] + "4";
                                    break;
                                case "J":
                                case "K":
                                case "L":
                                    SpellList[1] = SpellList[1] + "5";
                                    break;
                                case "M":
                                case "N":
                                case "O":
                                    SpellList[1] = SpellList[1] + "6";
                                    break;
                                case "P":
                                case "Q":
                                case "R":
                                case "S":
                                    SpellList[1] = SpellList[1] + "7";
                                    break;
                                case "T":
                                case "U":
                                case "V":
                                    SpellList[1] = SpellList[1] + "8";
                                    break;
                                case "W":
                                case "X":
                                case "Y":
                                case "Z":
                                    SpellList[1] = SpellList[1] + "9";
                                    break;
                            }
                        }
                        else
                        {
                            r = new Regex(@"([\u2E80-\u33FF]|[\u4E00-\u9FCC\u3400-\u4DB5\uFA0E\uFA0F\uFA11\uFA13\uFA14\uFA1F\uFA21\uFA23\uFA24\uFA27-\uFA29]|[\ud840-\ud868][\udc00-\udfff]|\ud869[\udc00-\uded6\udf00-\udfff]|[\ud86a-\ud86c][\udc00-\udfff]|\ud86d[\udc00-\udf34\udf40-\udfff]|\ud86e[\udc00-\udc1d])");
                            if (r.IsMatch(SongStr))
                            {
                                for (int i = 0; i < str.Length; i++)
                                {
                                    switch (str.Substring(i, 1))
                                    {
                                        case "0":
                                        case "6":
                                            SpellList[0] = SpellList[0] + "ㄌ"; // 拼音
                                            break;
                                        case "1":
                                            SpellList[0] = SpellList[0] + "ㄧ";
                                            break;
                                        case "2":
                                            SpellList[0] = SpellList[0] + "ㄦ";
                                            break;
                                        case "3":
                                        case "4":
                                            SpellList[0] = SpellList[0] + "ㄙ";
                                            break;
                                        case "5":
                                            SpellList[0] = SpellList[0] + "ㄨ";
                                            break;
                                        case "7":
                                            SpellList[0] = SpellList[0] + "ㄑ";
                                            break;
                                        case "8":
                                            SpellList[0] = SpellList[0] + "ㄅ";
                                            break;
                                        case "9":
                                            SpellList[0] = SpellList[0] + "ㄐ";
                                            break;
                                    }
                                    SpellList[1] = SpellList[1] + str.Substring(i, 1); // 手機輸入
                                }
                            }
                            else
                            {
                                for (int i = 0; i < str.Length; i++)
                                {
                                    SpellList[0] = SpellList[0] + str.Substring(i, 1); // 拼音
                                    SpellList[1] = SpellList[1] + str.Substring(i, 1); // 手機輸入
                                }
                            }
                        }

                        if (SpellList[2] == "") SpellList[2] = "1"; // 筆劃
                        SpellList[3] = SpellList[3] + ""; // 筆形順序
                    }
                    else
                    {
                        // 查找資料庫拼音資料
                        var query = from row in Global.PhoneticsDT.AsEnumerable()
                                    where row.Field<string>("Word").Equals(str) & row.Field<Int16>("SortIdx") < 2
                                    select row;

                        foreach (DataRow row in query)
                        {
                            SpellList[0] = SpellList[0] + (row["Spell"].ToString()).Substring(0, 1); // 拼音

                            switch ((row["Spell"].ToString()).Substring(0, 1))
                            {
                                case "ㄅ":
                                case "ㄆ":
                                case "ㄇ":
                                case "ㄈ":
                                    SpellList[1] = SpellList[1] + "1"; // 手機輸入
                                    break;
                                case "ㄉ":
                                case "ㄊ":
                                case "ㄋ":
                                case "ㄌ":
                                    SpellList[1] = SpellList[1] + "2";
                                    break;
                                case "ㄍ":
                                case "ㄎ":
                                case "ㄏ":
                                    SpellList[1] = SpellList[1] + "3";
                                    break;
                                case "ㄐ":
                                case "ㄑ":
                                case "ㄒ":
                                    SpellList[1] = SpellList[1] + "4";
                                    break;
                                case "ㄓ":
                                case "ㄔ":
                                case "ㄕ":
                                case "ㄖ":
                                    SpellList[1] = SpellList[1] + "5";
                                    break;
                                case "ㄗ":
                                case "ㄘ":
                                case "ㄙ":
                                    SpellList[1] = SpellList[1] + "6";
                                    break;
                                case "ㄚ":
                                case "ㄛ":
                                case "ㄜ":
                                case "ㄝ":
                                    SpellList[1] = SpellList[1] + "7";
                                    break;
                                case "ㄞ":
                                case "ㄟ":
                                case "ㄠ":
                                case "ㄡ":
                                    SpellList[1] = SpellList[1] + "8";
                                    break;
                                case "ㄢ":
                                case "ㄣ":
                                case "ㄤ":
                                case "ㄥ":
                                case "ㄦ":
                                    SpellList[1] = SpellList[1] + "9";
                                    break;
                                case "ㄧ":
                                case "ㄨ":
                                case "ㄩ":
                                    SpellList[1] = SpellList[1] + "0";
                                    break;
                            }

                            if (SpellList[2] == "") SpellList[2] = row["Strokes"].ToString(); // 筆劃
                            SpellList[3] = SpellList[3] + (row["PenStyle"].ToString()).Substring(0, 1); // 筆形順序
                            break;
                        }
                    }
                }
            }
            else
            {
                SpellList[2] = "0";
            }
            return SpellList;
        }

        public static DataTable GetFavoriteUserList(int ListTpye)
        {
            Global.FavoriteUserDT = new DataTable();
            DataTable list = new DataTable();
            list.Columns.Add(new DataColumn("Display", typeof(string)));
            list.Columns.Add(new DataColumn("Value", typeof(int)));

            if (File.Exists(Global.CrazyktvDatabaseFile) & Global.CrazyktvDatabaseVer != "Error")
            {
                DataTable dt = new DataTable();
                string SongQuerySqlStr = "select User_Id, User_Name from ktv_User";
                dt = CommonFunc.GetOleDbDataTable(Global.CrazyktvDatabaseFile, SongQuerySqlStr, "");

                List<string> removelist = new List<string>() { "####", "****", "9999", "^NT" };
                List<int> RemoveRowsIdxlist = new List<int>();

                var query = from row in dt.AsEnumerable()
                            where removelist.Contains(row.Field<string>("User_Id")) ||
                                  removelist.Contains(row.Field<string>("User_Id").Substring(0, row.Field<string>("User_Id").Length - 1)) ||
                                  row.Field<string>("User_Name").Contains("錢櫃新歌")
                            select row;

                if (query.Count<DataRow>() > 0)
                {
                    foreach (DataRow row in query)
                    {
                        RemoveRowsIdxlist.Add(dt.Rows.IndexOf(row));
                    }

                    if (RemoveRowsIdxlist.Count > 0)
                    {
                        for (int i = RemoveRowsIdxlist.Count - 1; i >= 0; i--)
                        {
                            dt.Rows.RemoveAt(RemoveRowsIdxlist[i]);
                        }
                    }
                }


                if (dt.Rows.Count > 0)
                {
                    Global.FavoriteUserDT = dt;
                    foreach (DataRow row in dt.AsEnumerable())
                    {
                        list.Rows.Add(list.NewRow());
                        list.Rows[list.Rows.Count - 1][0] = row["User_Name"].ToString();
                        list.Rows[list.Rows.Count - 1][1] = list.Rows.Count;
                    }
                }
                else
                {
                    if (ListTpye == 0)
                    {
                        list.Rows.Add(list.NewRow());
                        list.Rows[list.Rows.Count - 1][0] = "無最愛用戶";
                        list.Rows[list.Rows.Count - 1][1] = list.Rows.Count;
                    }
                }
                dt.Dispose();
            }
            else
            {
                if (ListTpye == 0)
                {
                    list.Rows.Add(list.NewRow());
                    list.Rows[list.Rows.Count - 1][0] = "無最愛用戶";
                    list.Rows[list.Rows.Count - 1][1] = list.Rows.Count;
                }
            }
            return list;
        }

        public static FileAttributes RemoveAttribute(FileAttributes attributes, FileAttributes attributesToRemove)
        {
            return attributes & ~attributesToRemove;
        }

        public static List<int> GetSongLangCount()
        {
            List<int> SongLangCount = new List<int>() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            foreach (string langstr in Global.CrazyktvSongLangList)
            {
                var query = from row in Global.SongStatisticsDT.AsEnumerable()
                            where row.Field<string>("Song_Lang").Equals(langstr)
                            select row;
                if (query.Count<DataRow>() > 0)
                {
                    SongLangCount[Global.CrazyktvSongLangList.IndexOf(langstr)] = query.Count<DataRow>();
                    SongLangCount[10] += query.Count<DataRow>();
                }
                else
                {
                    SongLangCount[Global.CrazyktvSongLangList.IndexOf(langstr)] = 0;
                }
            }
            if (Directory.Exists(Global.SongMgrDestFolder))
            {
                List<string> SupportFormat = new List<string>();
                SupportFormat = new List<string>(Global.SongMgrSupportFormat.Split(';'));

                DirectoryInfo dir = new DirectoryInfo(Global.SongMgrDestFolder);
                FileInfo[] Files = dir.GetFiles("*", SearchOption.AllDirectories).Where(p => SupportFormat.Contains(p.Extension.ToLower())).ToArray();
                SongLangCount[11] = Files.Count();
            }
            return SongLangCount;
        }

        public static List<int> GetSingerTypeCount()
        {
            List<int> SingerTypeCount = new List<int>() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            List<int> SingerTypeList = new List<int>();

            foreach (string str in Global.CrazyktvSingerTypeList)
            {
                if (str != "未使用")
                {
                    SingerTypeList.Add(Global.CrazyktvSingerTypeList.IndexOf(str));
                }
            }

            foreach (int SingerTypeValue in SingerTypeList)
            {
                var query = from row in (Global.SingerMgrDefaultSingerDataTable == "ktv_Singer") ? Global.SingerDT.AsEnumerable() : Global.AllSingerDT.AsEnumerable()
                            where row.Field<string>("Singer_Type").Equals(SingerTypeValue.ToString())
                            select row;
                if (query.Count<DataRow>() > 0)
                {
                    SingerTypeCount[SingerTypeList.IndexOf(SingerTypeValue)] = query.Count<DataRow>();
                    SingerTypeCount[9] += query.Count<DataRow>();
                }
                else
                {
                    SingerTypeCount[SingerTypeList.IndexOf(SingerTypeValue)] = 0;
                }
            }
            return SingerTypeCount;
        }

        public static string GetWordUnicode(string word)
        {
            string Unicode = "";
            byte[] UnicodeByte = Encoding.UTF32.GetBytes(word);
            if (UnicodeByte[2] != 00)
            {
                Unicode = String.Format("{0:X}", UnicodeByte[2]) + String.Format("{0:X2}", UnicodeByte[1]) + String.Format("{0:X2}", UnicodeByte[0]);
            }
            else
            {
                Unicode = String.Format("{0:X2}", UnicodeByte[1]) + String.Format("{0:X2}", UnicodeByte[0]);
            }
            return Unicode;
        }

        public static string ConvToWide(string input)
        {
            char[] c = input.ToCharArray();
            for (int i = 0; i < c.Length; i++)
            {
                if (c[i] < 127) { c[i] = (char)(c[i] + 65248); }
            }
            return new string(c);
        }

        public static string ConvToNarrow(string input)
        {
            char[] c = input.ToCharArray();
            for (int i = 0; i < c.Length; i++)
            {
                if (c[i] > 65280 && c[i] < 65375) { c[i] = (char)(c[i] - 65248); }
            }
            return new string(c);
        }
    }
}
