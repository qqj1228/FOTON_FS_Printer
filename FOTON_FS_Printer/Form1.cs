using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using static FOTON_FS_Printer.Config;

namespace FOTON_FS_Printer {
    public partial class Form1 : Form {
        Logger log;
        Config cfg;
        Model db;
        SerialPortClass sp;
        ReportClass report;
        Timer timer;

        public Form1() {
            InitializeComponent();
            log = new Logger("./log", EnumLogLevel.LogLevelAll, true, 100);
            cfg = new Config(log);
            db = new Model(cfg, log);
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
                        }
                    }
                }
            }
        }

        void SerialDataReceived(object sender, SerialDataReceivedEventArgs e, byte[] bits) {
            Control con = this.ActiveControl;
            if (con is TextBox txt) {
                txt.Text = Encoding.Default.GetString(bits);
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

            //string str = report.testDegree(this.textBoxVehicleCode.Text);
            //this.textBoxVehicleType.Text = str;
        }

        private void buttonInput_Click(object sender, EventArgs e) {
            if (this.listBox1.SelectedItems.Count > 0) {
                string VIN = this.listBox1.SelectedItem.ToString();
                for (int i = 0; i < cfg.ExDBList.Count; i++) {
                    int len = cfg.ExDBList[i].TableList.Count;
                    for (int j = 0; j < len; j++) {
                        if (cfg.ExDBList[i].TableList[j] == cfg.DB.VehicleInfo) {
                            Dictionary<string, string> dicInfo = new Dictionary<string, string> {
                                { cfg.ColumnDic["VIN"], VIN },
                                { cfg.ColumnDic["ProductCode"], textBoxVehicleCode.Text },
                                { cfg.ColumnDic["VehicleType"], textBoxVehicleType.Text },
                                { cfg.ColumnDic["EngineCode"], textBoxEngineCode.Text }
                            };

                            string[,] rs = db.GetRecords(cfg.ExDBList[i].TableList[j], cfg.ColumnDic["VIN"], VIN, i);
                            if (rs != null) {
                                if (rs.GetLength(0) > 0) {
                                    KeyValuePair<string, string> pair = new KeyValuePair<string, string>(cfg.ColumnDic["VIN"], VIN);
                                    db.UpdateRecord(cfg.ExDBList[i].TableList[j], pair, dicInfo, i);
                                    MessageBox.Show("成功更新数据", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                } else {
                                    db.InsertRecord(cfg.ExDBList[i].TableList[j], dicInfo, i);
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
    }
}
