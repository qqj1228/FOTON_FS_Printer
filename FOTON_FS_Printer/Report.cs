using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FOTON_FS_Printer {
    class ReportClass {
        readonly Logger Log;
        readonly Config Cfg;
        string StrTempPath { get; set; }
        Queue<string> FileQueue { get; set; }
        int MaxFileQty { get; set; }

        public ReportClass(Config cfg, Logger log, int maxFileQty) {
            this.Log = log;
            this.Cfg = cfg;
            this.StrTempPath = ".\\temp\\";
            this.MaxFileQty = maxFileQty;
            FileQueue = new Queue<string>();
            CreateLogPath();
        }

        void CreateLogPath() {
            if (!Directory.Exists(StrTempPath)) {
                Directory.CreateDirectory(StrTempPath);
            }
        }

        void GenFileQueue() {
            DirectoryInfo dirinfo = new DirectoryInfo(StrTempPath);
            FileInfo[] Files = dirinfo.GetFiles();
            // 递增排序
            Array.Sort<FileInfo>(Files, (FileInfo x, FileInfo y) => { return x.LastWriteTime.CompareTo(y.LastWriteTime); });
            // 递减排序
            //Array.Sort<FileInfo>(Files, (FileInfo x, FileInfo y) => { return y.LastWriteTime.CompareTo(x.LastWriteTime); });
            foreach (var item in Files) {
                FileQueue.Enqueue(StrTempPath + item.Name);
            }
        }

        void UpdateFileQueue() {
            if (MaxFileQty > 0) {
                int qty = FileQueue.Count - MaxFileQty + 1;
                if (qty > 0) {
                    for (int i = 0; i < qty; i++) {
                        File.Delete(FileQueue.Dequeue());
                    }
                }
            }
        }

        string Str2Degree(string strOri) {
            string ret = "-";
            string minute = "";
            if (strOri != "" && strOri != "x") {
                if (strOri.Contains("°")) {
                    ret = strOri.Replace(".", "'");
                } else {
                    string[] arr = strOri.Split('.');
                    if (arr.Length > 1) {
                        double.TryParse("." + arr[1], out double result);
                        result = result * 60 + 0.5;
                        int iRet = (int)result;
                        minute = iRet.ToString() + "'";
                    }
                    ret = arr[0] + "°" + minute;
                }
            }
            return ret;
        }

        public string testDegree(string str) {
            return Str2Degree(str);
        }

        /// <summary>
        /// 特殊处理ABSResult项，如果ABS_DTC_Des项为不合格的话ABSResult项也为不合格，
        /// 其余情况不处理，例如ABS_DTC_Des == "---"，ABS未检测到DTC结果
        /// </summary>
        /// <param name="dicData"></param>
        void HandleABSResult(Dictionary<string, string> dicData) {
            if (Cfg.ColumnDic.ContainsKey("ABS_DTC_Des") && Cfg.ColumnDic.ContainsKey("ABSResult")) {
                if (dicData.ContainsKey(Cfg.ColumnDic["ABS_DTC_Des"]) && dicData.ContainsKey(Cfg.ColumnDic["ABSResult"])) {
                    if (int.TryParse(dicData[Cfg.ColumnDic["ABS_DTC_Des"]], out int result)) {
                        if (result <= 0) {
                            dicData[Cfg.ColumnDic["ABSResult"]] = "X";
                        }
                    } else if (dicData[Cfg.ColumnDic["ABS_DTC_Des"]].Contains('N') || dicData[Cfg.ColumnDic["ABS_DTC_Des"]].Contains('n') || dicData[Cfg.ColumnDic["ABS_DTC_Des"]].Contains('X') || dicData[Cfg.ColumnDic["ABS_DTC_Des"]].Contains('x')) {
                        dicData[Cfg.ColumnDic["ABSResult"]] = "X";
                    }
                }
            }
        }

        string ReplaceData(string ori, Dictionary<string, string> dicData) {
            string ret = ori;
            bool totalResult = true;
            HandleABSResult(dicData);
            foreach (var item in Cfg.ColumnDic) {
                if (!dicData.ContainsKey(item.Value)) {
                    // 模板内的报表项不包含在结果数据dicData中
                    ret = ret.Replace("$" + item.Key + "$", "-");
                } else if (dicData[item.Value] == "" || dicData[item.Value] == "-") {
                    // 结果数据dicData中为默认值的报表项
                    ret = ret.Replace("$" + item.Key + "$", "-");
                } else if (dicData[item.Value] == "---" || dicData[item.Value] == "--") {
                    // 结果数据dicData中为"---"表示ABS未检测到DTC描述，不做结果显示转换，不判定为不合格
                    ret = ret.Replace("$" + item.Key + "$", "---");
                } else {
                    if (Cfg.ResultField.Contains(item.Key)) {
                        if (int.TryParse(dicData[item.Value], out int result)) {
                            if (result > 0) {
                                dicData[item.Value] = "OK";
                            } else {
                                dicData[item.Value] = "不合格";
                                totalResult = false;
                            }
                        } else {
                            if (dicData[item.Value].Contains('O') || dicData[item.Value].Contains('o') || dicData[item.Value].Contains('P') || dicData[item.Value].Contains('p')) {
                                if (dicData[item.Value].Contains('N') || dicData[item.Value].Contains('n')) {
                                    dicData[item.Value] = "不合格";
                                    totalResult = false;
                                } else {
                                    dicData[item.Value] = "OK";
                                }
                            } else {
                                dicData[item.Value] = "不合格";
                                totalResult = false;
                            }
                        }
                    }
                    if (Cfg.DegreeFormat.Contains(item.Key)) {
                        dicData[item.Value] = Str2Degree(dicData[item.Value]);
                    }
                    ret = ret.Replace("$" + item.Key + "$", dicData[item.Value]);
                }
            }
            if (totalResult) {
                ret = ret.Replace("$TestResult$", "合格");
            } else {
                ret = ret.Replace("$TestResult$", "不合格");
            }
            return ret;
        }

        public void WriteReport(string FileName, Dictionary<string, string> dicData) {
            string content = File.ReadAllText(".\\config\\Page.htm", Encoding.GetEncoding(936));
            File.WriteAllText(this.StrTempPath + FileName + ".htm", ReplaceData(content, dicData), Encoding.GetEncoding(936));
            UpdateFileQueue();
            FileQueue.Enqueue(StrTempPath + FileName);
        }

        public string GetReportFile(string fileName) {
            return this.StrTempPath + fileName + ".htm";
        }
    }
}
