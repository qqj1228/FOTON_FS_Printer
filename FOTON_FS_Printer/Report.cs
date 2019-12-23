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
            if (strOri.Length > 0 && strOri != "x") {
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

        /// <summary>
        /// 特殊处理ABS结果项，含两处特殊处理
        /// 1、处理ABS阀结果项。如果用于老线并且ABS总结果为合格，则ABS阀的分项结果不管原来的值为多少都为合格。
        /// 2、处理ABSResult项。如果ABS_DTC_Des项为不合格的话ABSResult项也为不合格，其余情况不处理，例如ABS_DTC_Des == "---"，ABS未检测到DTC结果
        /// </summary>
        /// <param name="dicData"></param>
        void HandleABSResult(Dictionary<string, string> dicData) {
            if (Cfg.ColumnDic.ContainsKey("ABS_DTC_Des") && Cfg.ColumnDic.ContainsKey("ABSResult")) {
                bool ContainsABSKey = dicData.ContainsKey(Cfg.ColumnDic["ABS_ValveFL"])
                    && dicData.ContainsKey(Cfg.ColumnDic["ABS_ValveFR"])
                    && dicData.ContainsKey(Cfg.ColumnDic["ABS_ValveRL"])
                    && dicData.ContainsKey(Cfg.ColumnDic["ABS_ValveRR"])
                    && dicData.ContainsKey(Cfg.ColumnDic["ABSResult"]);
                if (ContainsABSKey && Cfg.Main.NewTestLine != 1 && NormalizeResult(dicData[Cfg.ColumnDic["ABSResult"]]) == "OK") {
                    dicData[Cfg.ColumnDic["ABS_ValveFL"]] = dicData[Cfg.ColumnDic["ABSResult"]];
                    dicData[Cfg.ColumnDic["ABS_ValveFR"]] = dicData[Cfg.ColumnDic["ABSResult"]];
                    dicData[Cfg.ColumnDic["ABS_ValveRL"]] = dicData[Cfg.ColumnDic["ABSResult"]];
                    dicData[Cfg.ColumnDic["ABS_ValveRR"]] = dicData[Cfg.ColumnDic["ABSResult"]];
                }
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

        /// <summary>
        /// 特殊处理报表中的两个时间项，均改为当前时间
        /// </summary>
        /// <param name="content">整个检测报表的内容</param>
        /// <returns>处理时间后的检测报表内容</returns>
        string HandleDateTime(string content) {
            string ret = content;
            string now = DateTime.Now.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            ret = ret.Replace("$LeaveFactoryTime$", now);
            ret = ret.Replace("$TestTime$", now);
            return ret;
        }

        string ReplaceData(string ori, Dictionary<string, string> dicData) {
            string ret = ori;
            bool totalResult = true;
            HandleABSResult(dicData);
            ret = HandleDateTime(ret);
            foreach (var item in Cfg.ColumnDic) {
                if (!dicData.ContainsKey(item.Value)) {
                    // 模板内的报表项不包含在结果数据dicData中
                    ret = ret.Replace("$" + item.Key + "$", "-");
                } else if (dicData[item.Value].Length == 0 || dicData[item.Value] == "-") {
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

        string NormalizeResult(string value) {
            string ret;
            if (int.TryParse(value, out int result)) {
                if (result > 0) {
                    ret = "OK";
                } else {
                    ret = "不合格";
                }
            } else {
                if (value.Contains('O') || value.Contains('o') || value.Contains('P') || value.Contains('p')) {
                    if (value.Contains('N') || value.Contains('n')) {
                        ret = "不合格";
                    } else {
                        ret = "OK";
                    }
                } else {
                    ret = "不合格";
                }
            }
            return ret;
        }
    }
}
