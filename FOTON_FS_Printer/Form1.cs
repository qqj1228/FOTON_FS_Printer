using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using static FOTON_FS_Printer.Config;

namespace FOTON_FS_Printer {
    public partial class Form1 : Form {
        Logger log;
        Config cfg;
        Model db;
        MainFileVersion fileVer;
        SerialPortClass sp;
        ReportClass report;
        Timer timer;

        public Form1() {
            InitializeComponent();
            log = new Logger("./log", EnumLogLevel.LogLevelAll, true, 100);
            cfg = new Config(log);
            db = new Model(cfg, log);
            fileVer = new MainFileVersion();
            this.label_Version.Text = "Ver: " + fileVer.AssemblyVersion.ToString() + ", 用于" + (cfg.Main.NewTestLine > 0 ? "新检测线" : "老检测线");
            report = new ReportClass(cfg, log, 100);
            timer = new Timer();
            timer.Tick += new EventHandler(TimerEventProcessor);
            timer.Interval = cfg.Main.Interval * 1000;
            timer.Start();
            if (cfg.Serial.PortName != "") {
                sp = new SerialPortClass(
                    cfg.Serial.PortName,
                    cfg.Serial.BaudRate,
                    (Parity)cfg.Serial.Parity,
                    cfg.Serial.DataBits,
                    (StopBits)cfg.Serial.StopBits
                );
                sp.DataReceived += new SerialPortClass.SerialPortDataReceiveEventArgs(SerialDataReceived);
                sp.OpenPort();
            }
        }

        void TimerEventProcessor(Object myObject, EventArgs myEventArgs) {
            for (int i = 0; i < cfg.ExDBList.Count; i++) {
                int len = cfg.ExDBList[i].TableList.Count;
                for (int j = 0; j < len; j++) {
                    if (cfg.ExDBList[i].TableList[j] == cfg.DB.LastWorkStation) {
                        string[,] rs = db.GetNewRecords(cfg.ExDBList[i].TableList[j], i);
                        if (rs != null) {
                            this.label_DB_Status.Text = "【与数据库连接正常】";
                            this.label_DB_Status.ForeColor = Color.Black;

                            int rowNum = rs.GetLength(0);
                            if (rowNum > 0) {
                                string[] col = db.GetTableColumns(cfg.ExDBList[i].TableList[j], i);
                                // 计算ID, VIN字段索引值
                                int IDIndex = 0;
                                int VINIndex = 1;
                                int iCount = 0;
                                for (int k = 0; k < col.Length && iCount <= 2; k++) {
                                    if (col[k] == cfg.ColumnDic["ID"]) {
                                        IDIndex = k;
                                        ++iCount;
                                    } else if (col[k] == cfg.ColumnDic["VIN"]) {
                                        VINIndex = k;
                                        ++iCount;
                                    }
                                }
                                // 将最新记录的VIN号填入UI中
                                this.textBoxVIN.Text = rs[rowNum - 1, VINIndex];
                                string[] vi = db.GetVehicleInfo(rs[rowNum - 1, VINIndex]);
                                this.textBoxVehicleCode.Text = vi[0];
                                this.textBoxVehicleType.Text = vi[1];
                                this.textBoxEngineCode.Text = vi[2];

                                // 若vi项均不为空的话就自动显示检测结果报表
                                if (vi[0] != "" && vi[1] != "" && vi[2] != "") {
                                    string VIN = rs[rowNum - 1, VINIndex];
                                    Dictionary<string, string> dic = db.GetVehicleResult(VIN);
                                    if (dic.Count > 0) {
                                        report.WriteReport(VIN, dic);
                                        Process.Start(report.GetReportFile(VIN));
                                    } else {
                                        MessageBox.Show("该VIN号车辆无检测结果数据！", "出错", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    }
                                }
                                // 修改LastID值
                                ExportDBConfig TempExDB = cfg.ExDBList[i];
                                int.TryParse(rs[rowNum - 1, IDIndex], out int result);
                                TempExDB.LastID = result;
                                cfg.ExDBList[i] = TempExDB;
                            }
                        } else {
                            this.label_DB_Status.Text = "【与数据库连接出错】";
                            this.label_DB_Status.ForeColor = Color.Red;
                        }
                    }
                }
            }
        }

        void SerialDataReceived(object sender, SerialDataReceivedEventArgs e, byte[] bits) {
            Control con = this.ActiveControl;
            if (con is TextBox txt) {
                // 跨UI线程调用UI控件要使用Invoke
                this.Invoke((EventHandler)delegate {
                    txt.Text = Encoding.Default.GetString(bits);
                    if (bits.Contains<byte>(0x0d) || bits.Contains<byte>(0x0a)) {
                        if (con.Name == "textBoxVIN") {
                            this.textBoxVehicleCode.Focus();
                        } else if (con.Name == "textBoxVehicleCode") {
                            this.textBoxVehicleType.Focus();
                        } else if (con.Name == "textBoxVehicleType") {
                            this.textBoxEngineCode.Focus();
                        } else if (con.Name == "textBoxEngineCode") {
                            this.textBoxVIN.Focus();
                        }
                    }
                });
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e) {
            if (sp != null) {
                sp.ClosePort();
            }
        }

        private void buttonSearch_Click(object sender, EventArgs e) {
            string VIN = textBoxVIN.Text;
            this.listBox1.Items.Clear();
            this.listBox1.Items.Add("正在查询。。。");
            if (VIN.Length > 17) {
                MessageBox.Show("VIN号长度大于17，将会截取前17位", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                VIN = VIN.Substring(0, 17);
                textBoxVIN.Text = VIN;
            }
            HashSet<string> VINSet = new HashSet<string>();
            string[,] rs;
            for (int i = 0; i < cfg.ExDBList.Count; i++) {
                int len = cfg.ExDBList[i].TableList.Count;
                for (int j = 0; j < len; j++) {
                    rs = db.GetLikeRecords(cfg.ExDBList[i].TableList[j], cfg.ColumnDic["VIN"], VIN, i);
                    if (rs != null) {
                        int length = rs.GetLength(0);
                        for (int k = 0; k < length; k++) {
                            VINSet.Add(rs[k, 0]);
                        }
                    }
                }
            }

            this.listBox1.Items.Clear();
            foreach (string item in VINSet) {
                listBox1.Items.Add(item);
            }

            // 测试角度转换功能
            //string str = report.testDegree(this.textBoxVehicleCode.Text);
            //this.textBoxVehicleType.Text = str;
        }

        private void buttonInput_Click(object sender, EventArgs e) {
            if (this.listBox1.SelectedItems.Count > 0) {
                string VIN = this.listBox1.SelectedItem.ToString();
                for (int i = 0; i < cfg.ExDBList.Count; i++) {
                    int len = cfg.ExDBList[i].TableList.Count;
                    for (int j = 0; j < len; j++) {
                        string TableName = cfg.ExDBList[i].TableList[j];
                        if (cfg.DB.VehicleInfoList.Contains(TableName)) {
                            string[] col = db.GetTableColumns(TableName, i);
                            Dictionary<string, string> dicInfo = new Dictionary<string, string> {
                                { cfg.ColumnDic["VIN"], VIN },
                            };
                            if (col.Contains<string>(cfg.ColumnDic["ProductCode"])) {
                                dicInfo.Add(cfg.ColumnDic["ProductCode"], textBoxVehicleCode.Text);
                            }
                            if (col.Contains<string>(cfg.ColumnDic["VehicleType"])) {
                                dicInfo.Add(cfg.ColumnDic["VehicleType"], textBoxVehicleType.Text);
                            }
                            if (col.Contains<string>(cfg.ColumnDic["EngineCode"])) {
                                dicInfo.Add(cfg.ColumnDic["EngineCode"], textBoxEngineCode.Text);
                            }
                            if (col.Contains<string>(cfg.ColumnDic["ABSResult"]) && cfg.Main.NewTestLine > 0) {
                                dicInfo.Add(cfg.ColumnDic["ABSResult"], GetABSResult(VIN) ? "O" : "X");
                            }
                            string[,] rs = db.GetRecords(TableName, cfg.ColumnDic["VIN"], VIN, i);
                            if (rs != null) {
                                if (rs.GetLength(0) > 0) {
                                    KeyValuePair<string, string> pair = new KeyValuePair<string, string>(cfg.ColumnDic["VIN"], VIN);
                                    db.UpdateRecord(TableName, pair, dicInfo, i);
                                    MessageBox.Show("成功更新数据", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                } else {
                                    db.InsertRecord(TableName, dicInfo, i);
                                    MessageBox.Show("成功插入数据", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e) {
            if (listBox1.SelectedItem != null) {
                string VIN = listBox1.SelectedItem.ToString();
                string[] vi = db.GetVehicleInfo(VIN);
                this.textBoxVehicleCode.Text = vi[0];
                this.textBoxVehicleType.Text = vi[1];
                this.textBoxEngineCode.Text = vi[2];
            }
        }

        private void listBox1_DoubleClick(object sender, EventArgs e) {
            if (listBox1.SelectedItem != null) {
                string VIN = listBox1.SelectedItem.ToString();
                Dictionary<string, string> dic = db.GetVehicleResult(VIN);
                if (dic.Count > 0) {
                    report.WriteReport(VIN, dic);
                    Process.Start(report.GetReportFile(VIN));
                } else {
                    MessageBox.Show("该VIN号车辆无检测结果数据！", "出错", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void textBox_KeyPress(object sender, KeyPressEventArgs e) {
            if (e.KeyChar == (char)Keys.Enter) {
                TextBox tb = sender as TextBox;
                if (tb.Name == "textBoxEngineCode") {
                    this.textBoxVIN.Focus();
                } else {
                    SendKeys.Send("{tab}");
                }
            }
        }

        bool GetABSResult(string VIN) {
            string[,] rs;
            int valvePassed = -1;
            int sensorPassed = -1;
            for (int i = 0; i < cfg.ExDBList.Count; i++) {
                int len = cfg.ExDBList[i].TableList.Count;
                for (int j = 0; j < len; j++) {
                    if (cfg.ExDBList[i].TableList[j] == "ABS_Valve") {
                        rs = db.GetRecordsOneCol(cfg.ExDBList[i].TableList[j], "Passed", "VIN", VIN, i);
                        if (rs != null) {
                            int rowNum = rs.GetLength(0);
                            if (rowNum > 0) {
                                int.TryParse(rs[rowNum - 1, 0], out int result);
                                valvePassed = result;
                            }
                        }
                    } else if (cfg.ExDBList[i].TableList[j] == "Static_ABS") {
                        rs = db.GetRecordsOneCol(cfg.ExDBList[i].TableList[j], "Passed", "VIN", VIN, i);
                        if (rs != null) {
                            int rowNum = rs.GetLength(0);
                            if (rowNum > 0) {
                                int.TryParse(rs[rowNum - 1, 0], out int result);
                                sensorPassed = result;
                            }
                        }
                    }
                }
            }
            return valvePassed + sensorPassed >= 2;
        }

    }

    // 获取文件版本类
    public class MainFileVersion {
        public Version AssemblyVersion {
            get { return ((Assembly.GetEntryAssembly()).GetName()).Version; }
        }

        public Version AssemblyFileVersion {
            get { return new Version(FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location).FileVersion); }
        }

        public string AssemblyInformationalVersion {
            get { return FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location).ProductVersion; }
        }
    }

}
