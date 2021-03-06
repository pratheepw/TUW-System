﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using myClass;
using System.Globalization;

namespace TUW_System
{
    public partial class frmS5_YarnCheckStock : DevExpress.XtraEditors.XtraForm
    {
        cDatabase db = new cDatabase(Module.TUW99);
        DataTable dtScan;
        CultureInfo clinfo_th = new CultureInfo("th-TH");
        DateTimeFormatInfo dtfinfo;
        string strSerial;//เก็บซีเรียลก่อนเซฟเพื่อใช้ในกรณีแสดง error

        public frmS5_YarnCheckStock()
        {
            InitializeComponent();
        }
        public void NewData()
        {
            //Left panel
            txtBarcode.Text = "";
            lblCarton.Text = "0";
            dtScan = new DataTable();
            DataColumn dc = new DataColumn();
            dc.ColumnName = "SERIAL";
            dc.DataType = typeof(string);
            dtScan.Columns.Add(dc);
            dc = new DataColumn();
            dc.ColumnName = "CARTON_NO";
            dc.DataType = typeof(string);
            dtScan.Columns.Add(dc);
            dc = new DataColumn();
            dc.ColumnName = "WEIGHT";
            dc.DataType = typeof(decimal);
            dtScan.Columns.Add(dc);
            gridControl1.DataSource = dtScan;
            gridView1.PopulateColumns();
            gridView1.OptionsView.EnableAppearanceEvenRow = true;
            gridView1.OptionsView.EnableAppearanceOddRow = true;
            gridView1.OptionsView.ColumnAutoWidth = false;
            gridView1.BestFitColumns();
            //Right panel
            cboMonth.SelectedIndex = -1;
            cboYear.SelectedIndex = -1;
        }
        public void SaveData()
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                db.ConnectionOpen();
                db.BeginTrans();
                foreach (DataRow dr in dtScan.Rows)
                {
                    strSerial = dr["SERIAL"].ToString();
                    string strSQL = "INSERT INTO YARNSTOCKBEGINDETAIL(MONTHYEAR,SERIAL)VALUES('"+cboYear.Text+cboMonth.Tag.ToString()+"','"+strSerial+"')";
                    db.Execute(strSQL);
                }
                db.CommitTrans();
                MessageBox.Show(dtScan.Rows.Count+"  carton(s).", "ํSave complete...", MessageBoxButtons.OK, MessageBoxIcon.Information);
                for (int i = gridView1.DataRowCount-1; i >=0; i--)
                {
                    gridView1.DeleteRow(i);
                }
                lblCarton.Text = gridView1.DataRowCount.ToString();
            }
            catch (Exception ex)
            {
                db.RollbackTrans();
                MessageBox.Show(ex.Message+" "+strSerial, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                db.ConnectionClose();
            }
            //สรุปข้อมูลใน gridView2 ใหม่อีกครั้ง
            GetCarton(cboMonth.Tag.ToString(), cboYear.Text);
            this.Cursor = Cursors.Default;
        }
        public void ExportExcel()
        {
            SaveFileDialog theOpenFile = new SaveFileDialog();
            string strTemp;
            theOpenFile.Filter = "Microsoft Excel Document|*.xls";
            if (theOpenFile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                strTemp = theOpenFile.FileName;
                gridView2.ExportToXls(strTemp);
            }
        }

        private void GetCarton(string strMonth,string strYear)
        {
            string strSQL = "SELECT * FROM YARNSTOCKBEGINDETAIL WHERE MONTHYEAR='" + strYear + strMonth + "'";
            DataTable dt = db.GetDataTable(strSQL);
            lblCartonAll.Text = dt.Rows.Count.ToString();
            gridControl2.DataSource = dt;
            gridView2.Columns["Register"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            gridView2.Columns["Register"].DisplayFormat.FormatString = "g";
            gridView2.OptionsView.EnableAppearanceEvenRow = true;
            gridView2.OptionsView.EnableAppearanceOddRow = true;
            gridView2.OptionsView.ColumnAutoWidth = false;
            gridView2.BestFitColumns();
        }

        private void frmTS5_YarnCheckStock_Load(object sender, EventArgs e)
        {
            txtBarcode.Properties.MaxLength = 8;
            cboMonth.Properties.TextEditStyle=DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            cboYear.Properties.TextEditStyle=DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            dtfinfo = clinfo_th.DateTimeFormat;
            List<string> lstMonth = new List<string>(dtfinfo.MonthNames);
            foreach (string strName in lstMonth)
            {
                cboMonth.Properties.Items.Add(strName);
            }
            cboYear.Properties.Items.Add(DateTime.Today.AddYears(-1).Year);
            cboYear.Properties.Items.Add(DateTime.Today.AddYears(0).Year);
            cboYear.Properties.Items.Add(DateTime.Today.AddYears(1).Year);
            NewData();

        }
        private void gridView1_CustomDrawRowIndicator(object sender, DevExpress.XtraGrid.Views.Grid.RowIndicatorCustomDrawEventArgs e)
        {
            if (e.Info.IsRowIndicator)
            {
                e.Info.DisplayText = (e.RowHandle + 1).ToString();
                e.Info.ImageIndex = -1;
            }
            gridView1.IndicatorWidth = 30;
        }
        private void txtBarcode_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != (char)13) { return; }
            txtBarcode.Text = txtBarcode.Text.ToUpper();
            try
            {
                //ป้องกันการยิงซ้ำ   
                foreach (DataRow dr in dtScan.Rows)
                {
                    if (Equals(txtBarcode.Text, dr["SERIAL"].ToString())) { throw new ApplicationException("Duplicate"); }
                }
                //เช็คว่ากล่องนี้ยังมีอยู่ในสต็อกหรือไม่
                db.ConnectionOpen();
                DataTable dt = db.GetDataTable("SELECT CTNNO,NETWEIGHT,SYSDELETE FROM YARNGENBARCODE WHERE SERIAL='" + txtBarcode.Text + "'");
                if(dt==null||dt.Rows.Count==0)
                    throw new ApplicationException("NoBarcode");
                else
                {
                    if(dt.Rows[0]["SYSDELETE"].ToString() =="1")
                    throw new ApplicationException("CartonOut");
                    else
                    {
                        DataRow drNew = dtScan.NewRow();
                        drNew["SERIAL"] = txtBarcode.Text;
                        drNew["CARTON_NO"] = dt.Rows[0]["CTNNO"];
                        drNew["WEIGHT"] = dt.Rows[0]["NETWEIGHT"];
                        dtScan.Rows.Add(drNew);
                        gridControl1.DataSource = dtScan;
                        lblCarton.Text = gridView1.DataRowCount.ToString();
                    }
                }
            }
            catch (SystemException ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (ApplicationException ex)
            {
                switch (ex.Message)
                { 
                    case "Duplicate":
                        break;
                    case "CartonOut":
                        MessageBox.Show("เส้นด้ายกล่องนี้ได้ทำการถูกยิงออกจากสต็อกแล้ว","Warning",MessageBoxButtons.OK,MessageBoxIcon.Warning);
                        break;
                    case "NoBarcode":
                        MessageBox.Show("ไม่พบซีเรียลนี้ในประวัติการทำบาร์โค๊ด","Error",MessageBoxButtons.OK,MessageBoxIcon.Error);
                        break;
                }
            }
            db.ConnectionClose();
            txtBarcode.Text = "";
        }
        private void gridControl1_ProcessGridKey(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && !gridView1.IsEditing)
            {
                if (MessageBox.Show("คุณต้องการลบ serial: " + gridView1.GetFocusedRowCellDisplayText("SERIAL") + " หรือไม่", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    gridView1.DeleteSelectedRows();
                    lblCarton.Text = gridView1.DataRowCount.ToString();
                }
            }        
        }
        private void cboMonth_SelectedIndexChanged(object sender, EventArgs e)
        {
            try 
	        {	        
		        cboMonth.Tag=(cboMonth.SelectedIndex+1).ToString().PadLeft(2,'0');
                GetCarton(cboMonth.Tag.ToString(),cboYear.Text);
	        }
	        catch{}
        }
        private void cboYear_SelectedIndexChanged(object sender, EventArgs e)
        {
            try 
	        {	        
		        GetCarton(cboMonth.Tag.ToString(),cboYear.Text);
	        }
	        catch{}
        }
        private void gridView2_CustomDrawRowIndicator(object sender, DevExpress.XtraGrid.Views.Grid.RowIndicatorCustomDrawEventArgs e)
        {
            if (e.Info.IsRowIndicator)
            {
                e.Info.DisplayText = (e.RowHandle + 1).ToString();
                e.Info.ImageIndex = -1;

            }
            gridView2.IndicatorWidth = 40;
        }
        private void btnTransfer_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            db.ConnectionOpen();
            try
            {
                db.BeginTrans();
                string monthYear=cboYear.Text+(cboMonth.SelectedIndex+1).ToString().PadLeft(2,'0');
                //******************update yarnid,weight table yarnstockbegindetail***********************************
                db.Execute("update a set a.yarnid=b.yarnid,a.weight=b.netweight from yarnstockbegindetail a inner join yarngenbarcode b on a.serial=b.serial where a.monthyear='"+monthYear+"'");
                //******************update table yarnstockbegin
                db.Execute("update yarnstockbegin set weight=0 where monthyear='"+monthYear+"'");
                db.Execute("update yarnstockbegin  set weight=(	select isnull(sum(weight),0)from yarnstockbegindetail  where monthyear='"+monthYear+"' and yarnid=yarnstockbegin.yarnid) where monthyear='"+monthYear+"'");
                db.Execute("update yarnstockbegin set carton=0 where monthyear='"+monthYear+"'");
                db.Execute("update a set a.carton=b.carton from yarnstockbegin a inner join (select yarnid,count(serial)as carton from yarnstockbegindetail where monthyear='"+monthYear+"' group by yarnid)b on a.yarnid=b.yarnid where a.monthyear='"+monthYear+"'");
                //*******************update table yarncode************************************************************
                db.Execute("update a set a.wgt_begin=b.weight,a.wgt=b.weight from yarncode a inner join yarnstockbegin b on  a.id=b.yarnid and b.monthyear='" + monthYear + "'");
                db.CommitTrans();
                MessageBox.Show("ยกยอดเสร็จสมบูรณ์", "ยกยอด...", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                db.RollbackTrans();
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            db.ConnectionClose();
            this.Cursor = Cursors.Default;
        }















    }
}