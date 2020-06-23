using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace FOTON_FS_Printer {
    public class Config {
        public struct MainConfig {
            public int Interval { get; set; } // 单位秒
            public int NewTestLine { get; set; } // 是否用于新线
        }

        public struct SerialPortConfig {
            public string PortName { get; set; }
            public int BaudRate { get; set; }
            public int Parity { get; set; }
            public int DataBits { get; set; }
            public int StopBits { get; set; }
        }

        public struct DBConnConfig {
            public string IP { get; set; }
            public string Port { get; set; }
            public string UserID { get; set; }
            public string Pwd { get; set; }
            public List<string> VehicleInfoList;
            public string LastWorkStation { get; set; }
            public string ColumnConfig { get; set; }
            public List<string> RepeatColumn;
        }

        public struct ExportDBConfig {
            public string Name { get; set; } // 欲导出的数据库的名字
            public int LastID { get; set; } // 数据库最新记录的ID
            public List<string[]> TableList; // 欲导出的[表名, 排序字段]列表
        }

        public MainConfig Main;
        public SerialPortConfig Serial;
        public DBConnConfig DB;
        public List<ExportDBConfig> ExDBList;
        readonly Logger Log;
        string ConfigFile { get; set; }
        public Dictionary<string, string> ColumnDic;
        public List<string> ResultField; // 需要结果评判的字段pattern
        public List<string> DegreeFormat; // 需要输出角度单位的字段pattern

        public Config(Logger Log, string strConfigFile = "./config/config.xml") {
            this.Log = Log;
            this.ExDBList = new List<ExportDBConfig>();
            this.ColumnDic = new Dictionary<string, string>();
            this.ConfigFile = strConfigFile;
            LoadConfig();
            LoadColumnConfig();
        }

        ~Config() {
            SaveConfig();
        }

        void LoadConfig() {
            try {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(ConfigFile);
                XmlNode xnRoot = xmlDoc.SelectSingleNode("Config");
                XmlNodeList xnl = xnRoot.ChildNodes;

                foreach (XmlNode node in xnl) {
                    XmlNodeList xnlChildren = node.ChildNodes;
                    if (node.Name == "Main") {
                        foreach (XmlNode item in xnlChildren) {
                            if (item.Name == "Interval") {
                                int.TryParse(item.InnerText, out int result);
                                Main.Interval = result;
                            } else if (item.Name == "NewTestLine") {
                                int.TryParse(item.InnerText, out int result);
                                Main.NewTestLine = result;
                            }
                        }
                    } else if (node.Name == "SerialPort") {
                        foreach (XmlNode item in xnlChildren) {
                            if (item.Name == "PortName") {
                                Serial.PortName = item.InnerText;
                            } else if (item.Name == "BaudRate") {
                                int.TryParse(item.InnerText, out int result);
                                Serial.BaudRate = result;
                            } else if (item.Name == "Parity") {
                                int.TryParse(item.InnerText, out int result);
                                Serial.Parity = result;
                            } else if (item.Name == "DataBits") {
                                int.TryParse(item.InnerText, out int result);
                                Serial.DataBits = result;
                            } else if (item.Name == "StopBits") {
                                int.TryParse(item.InnerText, out int result);
                                Serial.StopBits = result;
                            }
                        }
                    } else if (node.Name == "DB") {
                        foreach (XmlNode item in xnlChildren) {
                            if (item.Name == "IP") {
                                DB.IP = item.InnerText;
                            } else if (item.Name == "Port") {
                                DB.Port = item.InnerText;
                            } else if (item.Name == "UserID") {
                                DB.UserID = item.InnerText;
                            } else if (item.Name == "Pwd") {
                                DB.Pwd = item.InnerText;
                            } else if (item.Name == "VehicleInfoList") {
                                DB.VehicleInfoList = new List<string>(item.InnerText.Split(','));
                            } else if (item.Name == "LastWorkStation") {
                                DB.LastWorkStation = item.InnerText;
                            } else if (item.Name == "ColumnConfig") {
                                DB.ColumnConfig = item.InnerText;
                            } else if (item.Name == "RepeatColumn") {
                                DB.RepeatColumn = new List<string>(item.InnerText.Split(','));
                            }
                        }
                    } else if (node.Name == "ExDB") {
                        ExportDBConfig TempExDB = new ExportDBConfig();

                        foreach (XmlNode item in xnlChildren) {
                            XmlNodeList xnlSubChildren = item.ChildNodes;
                            foreach (XmlNode subItem in xnlSubChildren) {
                                if (subItem.Name == "Name") {
                                    TempExDB.Name = subItem.InnerText;
                                } else if (subItem.Name == "LastID") {
                                    int.TryParse(subItem.InnerText, out int result);
                                    TempExDB.LastID = result;
                                } else if (subItem.Name == "TableList") {
                                    List<string> tables = new List<string>(subItem.InnerText.Split(','));
                                    List<string[]> sortItems = new List<string[]>();
                                    foreach (string table in tables) {
                                        sortItems.Add(table.Split('@'));
                                    }
                                    TempExDB.TableList = sortItems;
                                }
                            }
                            ExDBList.Add(TempExDB);
                        }
                    }
                }
            } catch (Exception e) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: " + e.Message);
                Console.ResetColor();
                Log.TraceError(e.Message);
            }
        }

        public void SaveConfig() {
            try {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(ConfigFile);
                XmlNode xnRoot = xmlDoc.SelectSingleNode("Config");
                XmlNodeList xnl = xnRoot.ChildNodes;

                foreach (XmlNode node in xnl) {
                    XmlNodeList xnlChildren = node.ChildNodes;
                    // 只操作了需要被修改的配置项
                    if (node.Name == "ExDB") {
                        for (int i = 0; i < ExDBList.Count; i++) {
                            XmlNodeList xnlSubChildren = xnlChildren[i].ChildNodes;
                            foreach (XmlNode item in xnlSubChildren) {
                                if (item.Name == "LastID") {
                                    item.InnerText = ExDBList[i].LastID.ToString();
                                }
                            }
                        }
                    }
                }

                xmlDoc.Save(ConfigFile);
            } catch (Exception e) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: " + e.Message);
                Console.ResetColor();
                Log.TraceError(e.Message);
            }
        }

        void LoadColumnConfig() {
            try {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load("./config/" + this.DB.ColumnConfig);
                XmlNode xnRoot = xmlDoc.SelectSingleNode("Column");
                XmlNodeList xnl = xnRoot.ChildNodes;

                foreach (XmlNode node in xnl) {
                    if (node.Name != "#comment") {
                        if (node.Name == "ResultField") {
                            ResultField = new List<string>(node.InnerText.Split(','));
                        } else if (node.Name == "DegreeFormat") {
                            DegreeFormat = new List<string>(node.InnerText.Split(','));
                        } else {
                            ColumnDic.Add(node.Name, node.InnerText);
                        }
                    }
                }
            } catch (Exception e) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: " + e.Message);
                Console.ResetColor();
                Log.TraceError(e.Message);
            }
        }
    }
}
