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
            string ret = "";
            string minute = "";
            string[] arr = strOri.Split('.');
            if (arr.Length > 1) {
                double.TryParse("." + arr[1], out double result);
                result *= 60;
                minute = result.ToString() + "'";
            }
            ret = arr[0] + "°" + minute;
            return ret;
        }

        string ReplaceData(string ori, Dictionary<string, string> dicData) {
            string ret = ori;
            foreach (var item in Cfg.ColumnDic) {
                if (item.Value == "" || !dicData.ContainsKey(item.Value)) {
                    ret = ret.Replace("$" + item.Key + "$", "-");
                } else {
                    foreach (string field in Cfg.ResultField) {
                        if (item.Key == field) {
                            if (int.TryParse(dicData[item.Value], out int result)) {
                                if (result > 0) {
                                    dicData[item.Value] = "OK";
                                } else {
                                    dicData[item.Value] = "不合格";
                                }
                            } else {
                                if (dicData[item.Value] == "O") {
                                    dicData[item.Value] = "OK";
                                } else {
                                    dicData[item.Value] = "不合格";
                                }
                            }
                        }
                    }
                    foreach (string degree in Cfg.DegreeFormat) {
                        if (item.Key == degree) {
                            dicData[item.Value] = Str2Degree(dicData[item.Value]);
                        }
                    }
                    ret = ret.Replace("$" + item.Key + "$", dicData[item.Value]);
                }
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
