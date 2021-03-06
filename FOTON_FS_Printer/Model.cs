﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;

namespace FOTON_FS_Printer {
    public class Model {
        public string[] m_strConn;
        public string StrConfigFile { get; set; }
        readonly Logger log;
        readonly Config cfg;

        public Model(Config cfg, Logger log) {
            this.cfg = cfg;
            this.log = log;
            ReadConfig();
        }

        void ReadConfig() {
            m_strConn = new string[cfg.ExDBList.Count];
            for (int i = 0; i < m_strConn.Length; i++) {
                m_strConn[i] = "user id=" + cfg.DB.UserID + ";";
                m_strConn[i] += "password=" + cfg.DB.Pwd + ";";
                m_strConn[i] += "database=" + cfg.ExDBList[i].Name + ";";
                m_strConn[i] += "data source=" + cfg.DB.IP + "," + cfg.DB.Port;
            }
        }

        public void ShowDB(string StrTable, int DBIndex) {
            string StrSQL = "select * from " + StrTable;

            using (SqlConnection sqlConn = new SqlConnection(m_strConn[DBIndex])) {
                sqlConn.Open();
                SqlCommand sqlCmd = new SqlCommand(StrSQL, sqlConn);
                SqlDataReader sqlData = sqlCmd.ExecuteReader();
                string str = "";
                int c = sqlData.FieldCount;
                while (sqlData.Read()) {
                    for (int i = 0; i < c; i++) {
                        object obj = sqlData.GetValue(i);
                        if (obj.GetType() == typeof(DateTime)) {
                            str += ((DateTime)obj).ToString("yyyy-MM-dd") + "\t";
                        } else {
                            str += obj.ToString() + "\t";
                        }
                    }
                    str += "\n";
                }
                Console.WriteLine(str);
                sqlConn.Close();
            }
        }

        public string[] GetTableName(int DBIndex) {
            try {
                using (SqlConnection sqlConn = new SqlConnection(m_strConn[DBIndex])) {
                    sqlConn.Open();
                    DataTable schema = sqlConn.GetSchema("Tables");
                    int count = schema.Rows.Count;
                    string[] tableName = new string[count];
                    for (int i = 0; i < count; i++) {
                        DataRow row = schema.Rows[i];
                        foreach (DataColumn col in schema.Columns) {
                            if (col.Caption == "TABLE_NAME") {
                                if (col.DataType.Equals(typeof(DateTime))) {
                                    tableName[i] = string.Format("{0:d}", row[col]);
                                } else if (col.DataType.Equals(typeof(Decimal))) {
                                    tableName[i] = string.Format("{0:C}", row[col]);
                                } else {
                                    tableName[i] = string.Format("{0}", row[col]);
                                }
                            }
                        }
                    }
                    sqlConn.Close();
                    return tableName;
                }
            } catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: " + ex.Message);
                Console.ResetColor();
                log.TraceError(ex.Message);
            }
            return new string[] { "" };
        }

        public string[] GetTableColumns(string strTableName, int DBIndex) {
            try {
                using (SqlConnection sqlConn = new SqlConnection(m_strConn[DBIndex])) {
                    sqlConn.Open();
                    DataTable schema = sqlConn.GetSchema("Columns", new string[] { null, null, strTableName });
                    schema.DefaultView.Sort = "ORDINAL_POSITION";
                    schema = schema.DefaultView.ToTable();
                    int count = schema.Rows.Count;
                    string[] ColumnName = new string[count];
                    for (int i = 0; i < count; i++) {
                        DataRow row = schema.Rows[i];
                        foreach (DataColumn col in schema.Columns) {
                            if (col.Caption == "COLUMN_NAME") {
                                if (col.DataType.Equals(typeof(DateTime))) {
                                    ColumnName[i] = string.Format("{0:d}", row[col]);
                                } else if (col.DataType.Equals(typeof(Decimal))) {
                                    ColumnName[i] = string.Format("{0:C}", row[col]);
                                } else {
                                    ColumnName[i] = string.Format("{0}", row[col]);
                                }
                            }
                        }
                    }
                    sqlConn.Close();
                    return ColumnName;
                }
            } catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: " + ex.Message);
                Console.ResetColor();
                log.TraceError(ex.Message);
            }
            return new string[] { "" };
        }

        public int GetRecordsCount(string strTableName, int DBIndex) {
            string strSQL = "select count(*) from " + strTableName;
            log.TraceInfo("SQL: " + strSQL);
            int count = 0;
            try {
                using (SqlConnection sqlConn = new SqlConnection(m_strConn[DBIndex])) {
                    SqlCommand sqlCmd = new SqlCommand(strSQL, sqlConn);
                    sqlConn.Open();
                    count = (int)sqlCmd.ExecuteScalar();
                    sqlConn.Close();
                }
            } catch (Exception ex) {
                Console.Error.WriteLine(ex.Message);
                log.TraceError(ex.Message);
            }
            return count;
        }

        public int ModifyDB(string strTableName, string[] strID, string[,] strValue, int DBIndex) {
            int iRet = 0;
            int iRowNum = strValue.GetLength(0);
            int iColNum = strValue.GetLength(1);
            int iIDNum = strID.Length;
            log.TraceInfo(string.Format("iRowNum:{0}, iIDNum:{1}", iRowNum, iIDNum));
            if (iRowNum == iIDNum) {
                iRet += UpdateDB(strTableName, strID, strValue, DBIndex);
            } else if (iRowNum > iIDNum) {
                string[,] strUpdate = new string[iIDNum, iColNum];
                Array.Copy(strValue, 0, strUpdate, 0, iIDNum * iColNum);
                iRet += UpdateDB(strTableName, strID, strUpdate, DBIndex);

                string[,] strInsert = new string[iRowNum - iIDNum, iColNum];
                Array.Copy(strValue, iIDNum * iColNum, strInsert, 0, (iRowNum - iIDNum) * iColNum);
                iRet += InsertDB(strTableName, strInsert, DBIndex);
            } else {
                iRet = -1;
            }
            return iRet;
        }

        int UpdateDB(string strTableName, string[] strID, string[,] strValue, int DBIndex) {
            int iRet = 0;
            int iRowNum = strValue.GetLength(0);
            if (iRowNum * strID.Length == 0) {
                return -1;
            }
            string[] strColumns = GetTableColumns(strTableName, DBIndex);
            try {
                using (SqlConnection sqlConn = new SqlConnection(m_strConn[DBIndex])) {
                    sqlConn.Open();
                    for (int i = 0; i < iRowNum; i++) {
                        string strSQL = "update ";
                        string strSet = "set ";
                        for (int j = 1; j < strColumns.Length; j++) {
                            strSet += strColumns[j] + " = '" + strValue[i, j - 1] + "', ";
                        }
                        strSet = strSet.Remove(strSet.Length - 2);
                        strSQL += string.Format("{0} {1} where ID = '{2}'", strTableName, strSet, strID[i]);
                        log.TraceInfo("SQL: " + strSQL);
                        SqlCommand sqlCmd = new SqlCommand(strSQL, sqlConn);
                        iRet += sqlCmd.ExecuteNonQuery();
                    }
                    sqlConn.Close();
                }
            } catch (Exception ex) {
                Console.Error.WriteLine(ex.Message);
                log.TraceError(ex.Message);
            }
            return iRet;
        }

        int InsertDB(string strTableName, string[,] strValue, int DBIndex) {
            int iRet = 0;
            int iRowNum = strValue.GetLength(0);
            int iColNum = strValue.GetLength(1);
            if (iRowNum * iColNum == 0) {
                return -1;
            }
            try {
                using (SqlConnection sqlConn = new SqlConnection(m_strConn[DBIndex])) {
                    sqlConn.Open();
                    for (int i = 0; i < iRowNum; i++) {
                        string strSQL = "insert " + strTableName + " values (";
                        for (int j = 0; j < iColNum; j++) {
                            strSQL += "'" + strValue[i, j] + "', ";
                        }
                        strSQL = strSQL.Remove(strSQL.Length - 2);
                        strSQL += ")";
                        log.TraceInfo("SQL: " + strSQL);
                        SqlCommand sqlCmd = new SqlCommand(strSQL, sqlConn);
                        iRet += sqlCmd.ExecuteNonQuery();
                    }
                    sqlConn.Close();
                }
            } catch (Exception ex) {
                Console.Error.WriteLine(ex.Message);
                log.TraceError(ex.Message);
            }
            return iRet;
        }

        public int InsertRecord(string strTableName, Dictionary<string, string> dicValue, int DBIndex) {
            int iRet = 0;
            int iColNum = dicValue.Count;
            if (iColNum <= 0) {
                return -1;
            }
            try {
                using (SqlConnection sqlConn = new SqlConnection(m_strConn[DBIndex])) {
                    sqlConn.Open();
                    string strSQL = "insert " + strTableName + " (";
                    foreach (string key in dicValue.Keys) {
                        strSQL += key + ", ";
                    }
                    strSQL = strSQL.Remove(strSQL.Length - 2);
                    strSQL += ")";
                    strSQL += " values (";
                    foreach (string value in dicValue.Values) {
                        strSQL += "'" + value + "', ";
                    }
                    strSQL = strSQL.Remove(strSQL.Length - 2);
                    strSQL += ")";
                    log.TraceInfo("SQL: " + strSQL);
                    SqlCommand sqlCmd = new SqlCommand(strSQL, sqlConn);
                    iRet = sqlCmd.ExecuteNonQuery();
                    sqlConn.Close();
                }
            } catch (Exception ex) {
                Console.Error.WriteLine(ex.Message);
                log.TraceError(ex.Message);
            }
            return iRet;
        }

        public int UpdateRecord(string strTableName, KeyValuePair<string, string> pair, Dictionary<string, string> dicValue, int DBIndex) {
            int iRet = 0;
            int iColNum = dicValue.Count;
            if (iColNum <= 0) {
                return -1;
            }
            try {
                using (SqlConnection sqlConn = new SqlConnection(m_strConn[DBIndex])) {
                    sqlConn.Open();
                    string strSQL = "update " + strTableName + " set ";
                    foreach (var item in dicValue) {
                        strSQL += item.Key + " = '" + item.Value + "', ";
                    }
                    strSQL = strSQL.Remove(strSQL.Length - 2);
                    strSQL += " where " + pair.Key + " = '" + pair.Value + "'";
                    log.TraceInfo("SQL: " + strSQL);
                    SqlCommand sqlCmd = new SqlCommand(strSQL, sqlConn);
                    iRet = sqlCmd.ExecuteNonQuery();
                    sqlConn.Close();
                }
            } catch (Exception ex) {
                Console.Error.WriteLine(ex.Message);
                log.TraceError(ex.Message);
            }
            return iRet;
        }

        public int DeleteDB(string strTableName, string[] strID, int DBIndex) {
            int iRet = 0;
            int length = strID.Length;
            try {
                using (SqlConnection sqlConn = new SqlConnection(m_strConn[DBIndex])) {
                    sqlConn.Open();
                    for (int i = 0; i < length; i++) {
                        string strSQL = "delete from " + strTableName + " where ID = '" + strID[i] + "'";
                        log.TraceInfo("SQL: " + strSQL);
                        SqlCommand sqlCmd = new SqlCommand(strSQL, sqlConn);
                        iRet += sqlCmd.ExecuteNonQuery();
                    }
                    sqlConn.Close();
                }
            } catch (Exception ex) {
                Console.Error.WriteLine(ex.Message);
                log.TraceError(ex.Message);
            }
            return iRet;
        }

        string[,] SelectDB(string strSQL, int DBIndex) {
            string[,] records = null;
            try {
                int count = 0;
                List<string[]> rowList;
                using (SqlConnection sqlConn = new SqlConnection(m_strConn[DBIndex])) {
                    SqlCommand sqlCmd = new SqlCommand(strSQL, sqlConn);
                    sqlConn.Open();
                    SqlDataReader sqlData = sqlCmd.ExecuteReader();
                    count = sqlData.FieldCount;
                    rowList = new List<string[]>();
                    while (sqlData.Read()) {
                        string[] items = new string[count];
                        for (int i = 0; i < count; i++) {
                            object obj = sqlData.GetValue(i);
                            if (obj.GetType() == typeof(DateTime)) {
                                items[i] = ((DateTime)obj).ToString("yyyy-MM-dd HH:mm:ss");
                            } else {
                                items[i] = obj.ToString();
                            }
                        }
                        rowList.Add(items);
                    }
                    sqlConn.Close();
                }
                records = new string[rowList.Count, count];
                for (int i = 0; i < rowList.Count; i++) {
                    for (int j = 0; j < count; j++) {
                        records[i, j] = rowList[i][j];
                    }
                }
                return records;
            } catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: " + ex.Message);
                Console.ResetColor();
                log.TraceError(ex.Message);
            }
            return records;
        }

        public string[,] GetLikeRecords(string strTableName, string strColumn, string strValue, int DBIndex, string orderby) {
            string strSQL = "select " + strColumn + " from " + strTableName + " where " + strColumn + " like '%" + strValue + "%'";
            if (orderby != null && orderby.Length > 0) {
                strSQL += " order by " + orderby;
            }
            log.TraceInfo("SQL: " + strSQL);
            string[,] strArr = SelectDB(strSQL, DBIndex);
            log.TraceInfo(GetDBResultLog(strTableName, false, DBIndex, strArr));
            return strArr;
        }

        public string[,] GetRecords(string strTableName, string strColumn, string strValue, int DBIndex, string orderby) {
            string strSQL = "select * from " + strTableName + " where " + strColumn + " = '" + strValue + "'";
            if (orderby != null && orderby.Length > 0) {
                strSQL += " order by " + orderby;
            }
            log.TraceInfo("SQL: " + strSQL);
            string[,] strArr = SelectDB(strSQL, DBIndex);
            log.TraceInfo(GetDBResultLog(strTableName, true, DBIndex, strArr));
            return strArr;
        }

        public string[,] GetRecordsOneCol(string strTableName, string strSelectCol, string strWhereCol, string strValue, int DBIndex, string orderby) {
            string strSQL = "select " + strSelectCol + " from " + strTableName + " where " + strWhereCol + " = '" + strValue + "'";
            if (orderby != null && orderby.Length > 0) {
                strSQL += " order by " + orderby;
            }
            log.TraceInfo("SQL: " + strSQL);
            string[,] strArr = SelectDB(strSQL, DBIndex);
            log.TraceInfo(GetDBResultLog(strTableName, false, DBIndex, strArr));
            return strArr;
        }

        /// <summary>
        /// 获取最新的表记录，返回[ID, VIN]数组
        /// </summary>
        /// <param name="strTableName"></param>
        /// <param name="index"></param>
        /// <param name="orderby"></param>
        /// <returns></returns>
        public string[,] GetNewVIN(string strTableName, int index, string orderby) {
            string strSQL;
            if (orderby != null && orderby.Length > 0) {
                strSQL = "select top (1) " + orderby + ", VIN from " + strTableName + " order by " + orderby + " desc";
            } else {
                strSQL = "select top (1) ID, VIN from " + strTableName + "  order by ID desc";
            }
            log.TraceInfo("SQL: " + strSQL);
            string[,] strArr = SelectDB(strSQL, index);
            log.TraceInfo(GetDBResultLog(strTableName, false, index, strArr));
            return strArr;
        }

        /// <summary>
        /// 获取车型信息记录，返回一个字符串数组内含[整车编号, 车型, 引擎号]
        /// </summary>
        /// <param name="VIN"></param>
        /// <returns></returns>
        public string[] GetVehicleInfo(string VIN) {
            string[] ret = new string[3] { "", "", "" }; // [整车编号, 车型, 引擎号]
            for (int i = 0; i < cfg.ExDBList.Count; i++) {
                int len = cfg.ExDBList[i].TableList.Count;
                for (int j = 0; j < len; j++) {
                    string TableName = cfg.ExDBList[i].TableList[j][0];
                    if (cfg.DB.VehicleInfoList.Contains(TableName)) {
                        string[,] rs = this.GetRecords(cfg.ExDBList[i].TableList[j][0], cfg.ColumnDic["VIN"], VIN, i, cfg.ExDBList[i].TableList[j][1]);
                        if (rs != null) {
                            if (rs.GetLength(0) > 0) {
                                string[] col = this.GetTableColumns(cfg.ExDBList[i].TableList[j][0], i);
                                for (int k = 0; k < col.Length; k++) {
                                    if (rs[0, k].Length > 0 || rs[0, k] != "-") {
                                        if (col[k] == cfg.ColumnDic["ProductCode"]) {
                                            ret[0] = rs[0, k];
                                        } else if (col[k] == cfg.ColumnDic["VehicleType"]) {
                                            ret[1] = rs[0, k];
                                        } else if (col[k] == cfg.ColumnDic["EngineCode"]) {
                                            ret[2] = rs[0, k];
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return ret;
        }

        public Dictionary<string, string> GetVehicleResult(string VIN) {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            for (int i = 0; i < cfg.ExDBList.Count; i++) {
                int len = cfg.ExDBList[i].TableList.Count;
                for (int j = 0; j < len; j++) {
                    string TableName = cfg.ExDBList[i].TableList[j][0];
                    string SortItem = cfg.ExDBList[i].TableList[j][1];
                    string[,] rs = this.GetRecords(TableName, cfg.ColumnDic["VIN"], VIN, i, SortItem);
                    string[] col = this.GetTableColumns(TableName, i);
                    if (rs != null) {
                        if (rs.GetLength(0) > 0) {
                            for (int n = 0; n < rs.GetLength(0); n++) {
                                for (int k = 0; k < col.Length; k++) {
                                    for (int m = 0; m < cfg.DB.RepeatColumn.Count; m++) {
                                        if (cfg.DB.RepeatColumn[m] == col[k]) {
                                            string colName = TableName + "." + col[k];
                                            if (dic.ContainsKey(colName)) {
                                                if (rs[n, k].Length > 0) {
                                                    dic[colName] = rs[n, k];
                                                }
                                            } else {
                                                dic.Add(colName, rs[n, k]);
                                            }
                                        } else {
                                            if (dic.ContainsKey(col[k])) {
                                                // 如果不是默认数据就使用该数据，是默认数据的话就用上一条记录的数据
                                                if (rs[n, k].Length > 0 && rs[n, k] != "-" && rs[n, k] != "x") {
                                                    dic[col[k]] = rs[n, k];
                                                }
                                            } else {
                                                dic.Add(col[k], rs[n, k]);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return dic;
        }

        /// <summary>
        /// 返回用于输出log记录的查询结果字符串
        /// </summary>
        /// <param name="strTableName">表名</param>
        /// <param name="bShowCol">是否输出字段名</param>
        /// <param name="index">数据库索引</param>
        /// <param name="results">查询结果</param>
        /// <param name="iMaxQTY">最大输出记录数量，若小于0则表示不限制输出数量</param>
        /// <returns></returns>
        private string GetDBResultLog(string strTableName, bool bShowCol, int index, string[,] results, int iMaxQTY = -1) {
            string strRet = "Connection string: " + m_strConn[index] + Environment.NewLine;
            if (bShowCol) {
                string[] cols = GetTableColumns(strTableName, index);
                foreach (string col in cols) {
                    strRet += col + ",";
                }
                strRet += Environment.NewLine;
            }
            int iStart = 0;
            if (iMaxQTY >= 0) {
                iStart = results.GetLength(0) - iMaxQTY;
                if (iStart < 0) {
                    iStart = 0;
                }
            }
            for (int rowNum = iStart; rowNum < results.GetLength(0); rowNum++) {
                for (int colNum = 0; colNum < results.GetLength(1); colNum++) {
                    strRet += results[rowNum, colNum] + ",";
                }
                strRet += Environment.NewLine;
            }
            return strRet;
        }
    }
}
